using System.Collections.Concurrent;
using Testify.Api.Services;

namespace Testify.Api.Tests;

// Replaces the real email service in tests so a test can read the password reset token the user would have received.
public class CapturingEmailService : IEmailService
{
    public ConcurrentDictionary<string, string> ResetTokens { get; } = new();

    public Task SendPasswordResetEmailAsync(string email, string username, string resetToken)
    {
        ResetTokens[email] = resetToken;
        return Task.CompletedTask;
    }

    public Task SendInvitationEmailAsync(string email, string projectName, string role, string inviteToken)
    {
        return Task.CompletedTask;
    }
}
