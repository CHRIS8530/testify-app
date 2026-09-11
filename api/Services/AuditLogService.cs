using System.Text.Json;
using Testify.Api.Data;
using Testify.Api.Models;

namespace Testify.Api.Services;

public interface IAuditLogService
{
    Task LogActionAsync(Guid? userId, string action, string? resourceType, Guid? resourceId, Guid? projectId, Dictionary<string, object>? details, string? ipAddress);
}

public class AuditLogService : IAuditLogService
{
    private readonly TestifyDbContext _db;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(TestifyDbContext db, ILogger<AuditLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogActionAsync(Guid? userId, string action, string? resourceType, Guid? resourceId, Guid? projectId, Dictionary<string, object>? details, string? ipAddress)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                ProjectId = projectId,
                Details = details != null ? JsonSerializer.Serialize(details) : null,
                IpAddress = ipAddress,
                Timestamp = DateTime.UtcNow
            };

            _db.AuditLogs.Add(auditLog);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit action: {Action}", action);
        }
    }
}