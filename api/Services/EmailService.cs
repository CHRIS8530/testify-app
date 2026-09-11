namespace Testify.Api.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string username, string resetToken);
    Task SendInvitationEmailAsync(string email, string projectName, string role, string inviteToken);
}

public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(string email, string username, string resetToken)
    {
        var resetUrl = $"http://localhost:5173/reset-password?token={resetToken}";
        
        _logger.LogInformation(
            "MOCK EMAIL - Password Reset\n" +
            "To: {Email}\n" +
            "Subject: Reset Your Testify Password\n\n" +
            "Hi {Username},\n\n" +
            "Click here to reset your password:\n" +
            "{ResetUrl}\n\n" +
            "This link expires in 1 hour.",
            email, username, resetUrl
        );

        await Task.CompletedTask;
    }

        public async Task SendInvitationEmailAsync(string email, string projectName, string role, string inviteToken)
    {
        var acceptUrl = $"http://localhost:5173/accept-invite?token={inviteToken}";
        
        _logger.LogInformation(
            "MOCK EMAIL - Project Invitation\n" +
            "To: {Email}\n" +
            "Subject: You've been invited to {ProjectName}\n\n" +
            "Hi {Email},\n\n" +
            "You've been invited to collaborate on '{ProjectName}' as a {Role}.\n\n" +
            "Accept and join:\n" +
            "{AcceptUrl}\n\n" +
            "This link expires in 7 days.",
            email, projectName, email, projectName, role, acceptUrl
        );

        await Task.CompletedTask;
    }
}