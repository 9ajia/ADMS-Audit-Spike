using Microsoft.EntityFrameworkCore;

namespace AuditLoggingSpike;

public class Book : IAuditable
{
    public int Id { get; set; }          
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
}
public class AuditEntry
{
    public int Id { get; set; }

    public string EntityName { get; set; } = "";
    public string EntityId { get; set; } = "";

    public AuditEventType EventType { get; set; }

    public string ChangesJson { get; set; } = "";

    public string UserId { get; set; } = "";

    public string CorrelationId { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}
public class AuditLoggingSpikeDbContext : DbContext
{
    public AuditLoggingSpikeDbContext(DbContextOptions<AuditLoggingSpikeDbContext> options)
        : base(options)
    {
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // For the spike, we keep config minimal.
    }
}