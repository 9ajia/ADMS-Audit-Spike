using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AuditLoggingSpike;

public class AuditLoggingInterceptor : SaveChangesInterceptor
{
    private readonly CurrentUserContext _userContext;

    public AuditLoggingInterceptor(CurrentUserContext userContext)
    {
        _userContext = userContext;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var auditEntries = new List<AuditEntry>();

        // Loop through tracked entities
        foreach (var entry in context.ChangeTracker.Entries())
        {
            // Only audit entities marked IAuditable
            if (entry.Entity is not IAuditable)
                continue;

            if (entry.State is EntityState.Added
                or EntityState.Modified
                or EntityState.Deleted)
            {
                var audit = CreateAuditEntry(entry);
                auditEntries.Add(audit);
            }
        }

        if (auditEntries.Count > 0)
        {
            context.Set<AuditEntry>().AddRange(auditEntries);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private AuditEntry CreateAuditEntry(EntityEntry entry)
    {
        var entityName = entry.Entity.GetType().Name;

        var keyProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        var keyValue = keyProperty?.CurrentValue?.ToString() ?? "<no-key>";

        var eventType = entry.State switch
        {
            EntityState.Added => AuditEventType.Created,
            EntityState.Modified => AuditEventType.Updated,
            EntityState.Deleted => AuditEventType.Deleted,
            _ => throw new NotSupportedException($"State {entry.State} is not supported.")
        };

        var changedFields = new Dictionary<string, object?>();

        if (entry.State == EntityState.Added)
        {
            foreach (var prop in entry.Properties)
            {
                changedFields[prop.Metadata.Name] = new
                {
                    Old = (object?)null,
                    New = prop.CurrentValue
                };
            }
        }
        else if (entry.State == EntityState.Modified)
        {
            foreach (var prop in entry.Properties.Where(p => p.IsModified))
            {
                changedFields[prop.Metadata.Name] = new
                {
                    Old = prop.OriginalValue,
                    New = prop.CurrentValue
                };
            }
        }
        else if (entry.State == EntityState.Deleted)
        {
            foreach (var prop in entry.Properties)
            {
                changedFields[prop.Metadata.Name] = new
                {
                    Old = prop.OriginalValue,
                    New = (object?)null
                };
            }
        }

        var json = JsonSerializer.Serialize(new
        {
            Entity = entityName,
            EntityId = keyValue,
            EventType = eventType.ToString(),
            ChangedFields = changedFields
        }, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return new AuditEntry
        {
            EntityName = entityName,
            EntityId = keyValue,
            EventType = eventType,
            ChangesJson = json,
            UserId = _userContext.UserId,
            CorrelationId = _userContext.CorrelationId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
