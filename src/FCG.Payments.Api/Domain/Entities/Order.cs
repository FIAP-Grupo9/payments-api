namespace FCG.Payments.Api.Domain.Entities;

public enum OrderStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class Order
{
    public Guid Id { get; set; }            // = OrderId vindo do OrderPlacedEvent
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public Guid GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public Guid? PromocaoId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}
