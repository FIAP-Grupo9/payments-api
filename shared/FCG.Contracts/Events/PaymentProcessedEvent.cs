namespace FCG.Contracts.Events;

public record PaymentProcessedEvent(
    Guid MessageId,
    DateTime OccurredAt,
    Guid OrderId,
    Guid UserId,
    string UserEmail,
    Guid GameId,
    string GameName,
    decimal Amount,
    PaymentStatus Status,
    DateTime ProcessedAt,
    string? FailureReason,
    Guid? PromocaoId);
