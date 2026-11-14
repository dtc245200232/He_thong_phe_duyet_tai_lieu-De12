namespace DistributedApp.Shared.Models;

public record NotificationDto(
    Guid Id,
    string Recipient,
    Guid DocumentId,
    string Title,
    string Message,
    bool IsRead,
    DateTime CreatedAt,
    string? CorrelationId = null
);
