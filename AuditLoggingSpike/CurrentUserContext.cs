namespace AuditLoggingSpike;

public class CurrentUserContext
{
    /CorrelationId links multiple audit entries from the same operation (e.g. one API call updating 3 entities).
    public string UserId { get; set; } = "test-user";
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
}