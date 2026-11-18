namespace Testify.Api.Services;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string username, string resetToken);
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
}