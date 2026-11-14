namespace DistributedApp.Shared.Messaging;

public static class EventBusConstants
{
    public const string ExchangeName = "document.events.exchange";

    // Routing keys
    public const string RoutingKeyDocumentSubmitted = "document.submitted";
    public const string RoutingKeyDocumentApproved = "document.approved";
    public const string RoutingKeyDocumentRejected = "document.rejected";
    public const string RoutingKeyAllDocumentEvents = "document.*";

    // Queues
    public const string NotificationQueue = "document.notifications.queue";
    public const string AuditQueue = "document.audit.queue";
    public const string DeadLetterQueue = "document.dead-letter.queue";
    public const string DeadLetterExchange = "document.dlx.exchange";
}
