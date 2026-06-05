namespace FCG.Contracts.Events;

public record OrderPlacedEvent(
    Guid MessageId,
    DateTime OccurredAt,
    Guid OrderId,
    Guid UserId,
    string UserEmail,
    string UserName,
    Guid GameId,
    string GameName,
    decimal OriginalPrice,
    decimal FinalPrice,
    Guid? PromocaoId);
