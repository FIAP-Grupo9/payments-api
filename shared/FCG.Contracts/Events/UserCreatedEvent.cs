namespace FCG.Contracts.Events;

public record UserCreatedEvent(
    Guid MessageId,
    DateTime OccurredAt,
    Guid UserId,
    string Name,
    string Email,
    string Role);
