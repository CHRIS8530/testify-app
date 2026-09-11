using System;

namespace Testify.Api.Models;

public class PasswordReset
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;

    public bool IsValid => UsedAt == null && DateTime.UtcNow < ExpiresAt;
}