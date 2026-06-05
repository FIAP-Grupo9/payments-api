using FCG.Contracts.Events;
using FCG.Payments.Api.Application;
using FCG.Payments.Api.Domain.Entities;
using FCG.Payments.Api.Infrastructure.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace FCG.Payments.Api.Infrastructure.Consumers;

public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly PaymentsDbContext _db;
    private readonly IPaymentSimulator _simulator;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(
        PaymentsDbContext db,
        IPaymentSimulator simulator,
        IPublishEndpoint publisher,
        ILogger<OrderPlacedConsumer> logger)
    {
        _db = db;
        _simulator = simulator;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var e = context.Message;
        var ct = context.CancellationToken;

        // Camada 1 de idempotência: dedup por MessageId
        if (await _db.ProcessedMessages.AnyAsync(p => p.MessageId == e.MessageId, ct))
        {
            _logger.LogInformation("OrderPlacedEvent já processado, ignorando. MessageId={MessageId}", e.MessageId);
            return;
        }

        // A entidade Order usa OrderId do evento como PK — duplicidade detectada se existir.
        var existing = await _db.Orders.FirstOrDefaultAsync(o => o.Id == e.OrderId, ct);
        Order order;
        if (existing is not null)
        {
            _logger.LogWarning("Order já existe (OrderId={OrderId}). Reaproveitando estado.", e.OrderId);
            order = existing;
            if (order.Status != OrderStatus.Pending)
            {
                // Já processado antes mas dedup falhou (raríssimo) — apenas registra e sai.
                _db.ProcessedMessages.Add(new ProcessedMessage
                {
                    MessageId = e.MessageId,
                    MessageType = nameof(OrderPlacedEvent),
                    ProcessedAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(ct);
                return;
            }
        }
        else
        {
            order = new Order
            {
                Id = e.OrderId,
                UserId = e.UserId,
                UserEmail = e.UserEmail,
                GameId = e.GameId,
                GameName = e.GameName,
                Amount = e.FinalPrice,
                PromocaoId = e.PromocaoId,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _db.Orders.Add(order);
        }

        var sim = _simulator.Simulate(e.FinalPrice);
        order.Status = sim.Approved ? OrderStatus.Approved : OrderStatus.Rejected;
        order.ProcessedAt = DateTime.UtcNow;
        order.FailureReason = sim.FailureReason;

        _db.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = e.MessageId,
            MessageType = nameof(OrderPlacedEvent),
            ProcessedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        var resultEvent = new PaymentProcessedEvent(
            MessageId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            OrderId: order.Id,
            UserId: order.UserId,
            UserEmail: order.UserEmail,
            GameId: order.GameId,
            GameName: order.GameName,
            Amount: order.Amount,
            Status: sim.Approved ? PaymentStatus.Approved : PaymentStatus.Rejected,
            ProcessedAt: order.ProcessedAt!.Value,
            FailureReason: order.FailureReason,
            PromocaoId: order.PromocaoId);

        await _publisher.Publish(resultEvent, ct);

        _logger.LogInformation(
            "Pagamento processado. OrderId={OrderId}, Status={Status}, Amount={Amount}",
            order.Id, order.Status, order.Amount);
    }
}
