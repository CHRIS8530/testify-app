using System;
using System.Collections.Generic;

namespace Testify.Api.Models;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? ResourceType { get; set; }
    public Guid? ResourceId { get; set; }
    public Guid? ProjectId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation
    public User? User { get; set; }
    public Project? Project { get; set; }
}