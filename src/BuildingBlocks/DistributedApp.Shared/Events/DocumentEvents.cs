using System.Text.Json.Serialization;

namespace DistributedApp.Shared.Events;

public abstract record BaseEvent
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; init; } = Guid.NewGuid();

    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("eventType")]
    public string EventType { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record DocumentSubmittedEvent : BaseEvent
{
    public DocumentSubmittedEvent()
    {
        EventType = nameof(DocumentSubmittedEvent);
    }

    [JsonPropertyName("documentId")]
    public Guid DocumentId { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; init; } = string.Empty;

    [JsonPropertyName("assignedApprover")]
    public string AssignedApprover { get; init; } = string.Empty;

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }
}

public record DocumentApprovedEvent : BaseEvent
{
    public DocumentApprovedEvent()
    {
        EventType = nameof(DocumentApprovedEvent);
    }

    [JsonPropertyName("documentId")]
    public Guid DocumentId { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; init; } = string.Empty;

    [JsonPropertyName("approver")]
    public string Approver { get; init; } = string.Empty;

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }
}

public record DocumentRejectedEvent : BaseEvent
{
    public DocumentRejectedEvent()
    {
        EventType = nameof(DocumentRejectedEvent);
    }

    [JsonPropertyName("documentId")]
    public Guid DocumentId { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; init; } = string.Empty;

    [JsonPropertyName("approver")]
    public string Approver { get; init; } = string.Empty;

    [JsonPropertyName("comment")]
    public string? Comment { get; init; }
}
