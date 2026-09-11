using Microsoft.AspNetCore.Mvc;
using Testify.Api.Data;
using Testify.Api.Models;
using Testify.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Testify.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly TestifyDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        TestifyDbContext db,
        IJwtService jwtService,
        IPasswordService passwordService,
        IEmailService emailService,
        IAuditLogService auditLogService,
        ILogger<AuthController> logger)
    {
        _db = db;
        _jwtService = jwtService;
        _passwordService = passwordService;
        _emailService = emailService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        // Validate email
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains("@"))
            return BadRequest(new { error = new { message = "Email is required and must be valid", code = "INVALID_EMAIL" }, status = 400 });

        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            return BadRequest(new { error = new { message = "Email already registered", code = "EMAIL_EXISTS" }, status = 400 });

        // Validate username
        if (string.IsNullOrWhiteSpace(req.Username) || req.Username.Length < 3 || req.Username.Length > 50)
            return BadRequest(new { error = new { message = "Username must be 3-50 characters", code = "INVALID_USERNAME" }, status = 400 });

        if (await _db.Users.AnyAsync(u => u.Username == req.Username))
            return BadRequest(new { error = new { message = "Username already taken", code = "USERNAME_EXISTS" }, status = 400 });

        // Validate password
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 8)
            return BadRequest(new { error = new { message = "Password must be at least 8 characters", code = "INVALID_PASSWORD" }, status = 400 });

        if (!req.Password.Any(char.IsUpper) || !req.Password.Any(char.IsDigit) || !req.Password.Any(c => !char.IsLetterOrDigit(c)))
            return BadRequest(new { error = new { message = "Password must contain uppercase, number, and special character", code = "WEAK_PASSWORD" }, status = 400 });

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = req.Email,
            Username = req.Username,
            PasswordHash = _passwordService.HashPassword(req.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            user.Id,
            "register",
            "user",
            user.Id,
            null,
            new Dictionary<string, object> { { "email", user.Email } },
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return CreatedAtAction(nameof(Register), new { id = user.Id }, new
        {
            id = user.Id,
            email = user.Email,
            username = user.Username,
            createdAt = user.CreatedAt
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Unauthorized(new { error = new { message = "Email and password required", code = "INVALID_CREDENTIALS" }, status = 401 });

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null || !_passwordService.VerifyPassword(req.Password, user.PasswordHash))
            return Unauthorized(new { error = new { message = "Invalid email or password", code = "INVALID_CREDENTIALS" }, status = 401 });

        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Username);
        var refreshToken = Guid.NewGuid().ToString();
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(7);

        var dbRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = refreshTokenExpiry,
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(dbRefreshToken);
        await _db.SaveChangesAsync();

        // Set httpOnly cookie
        Response.Cookies.Append(
            "refreshToken",
            refreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = refreshTokenExpiry
            }
        );

        await _auditLogService.LogActionAsync(
            user.Id,
            "login",
            "user",
            user.Id,
            null,
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Ok(new
        {
            accessToken,
            expiresIn = 900,
            user = new { id = user.Id, email = user.Email, username = user.Username }
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            return Unauthorized(new { error = new { message = "Refresh token required", code = "INVALID_TOKEN" }, status = 401 });

        var dbToken = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == req.RefreshToken);

        if (dbToken == null || !dbToken.IsValid)
            return Unauthorized(new { error = new { message = "Invalid or expired refresh token", code = "INVALID_TOKEN" }, status = 401 });

        var user = dbToken.User;
        var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Username);

        return Ok(new
        {
            accessToken = newAccessToken,
            expiresIn = 900
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            return Unauthorized(new { error = new { message = "Refresh token required", code = "INVALID_TOKEN" }, status = 401 });

        var dbToken = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == req.RefreshToken);
        if (dbToken == null)
            return Unauthorized(new { error = new { message = "Token not found", code = "INVALID_TOKEN" }, status = 401 });

        dbToken.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            dbToken.UserId,
            "logout",
            "user",
            dbToken.UserId,
            null,
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email))
            return Ok(new { message = "Password reset email sent" }); // Always return success for security

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user == null)
            return Ok(new { message = "Password reset email sent" }); // Security: don't leak whether email exists

        var resetToken = Guid.NewGuid().ToString();
        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = resetToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };

        _db.PasswordResets.Add(passwordReset);
        await _db.SaveChangesAsync();

        await _emailService.SendPasswordResetEmailAsync(user.Email, user.Username, resetToken);

        await _auditLogService.LogActionAsync(
            user.Id,
            "forgot_password",
            "user",
            user.Id,
            null,
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Ok(new { message = "Password reset email sent" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest(new { error = new { message = "Token and password required", code = "INVALID_INPUT" }, status = 400 });

        var passwordReset = await _db.PasswordResets
            .Include(pr => pr.User)
            .FirstOrDefaultAsync(pr => pr.Token == req.Token);

        if (passwordReset == null || !passwordReset.IsValid)
            return BadRequest(new { error = new { message = "Invalid or expired reset token", code = "INVALID_TOKEN" }, status = 400 });

        if (req.NewPassword.Length < 8)
            return BadRequest(new { error = new { message = "Password must be at least 8 characters", code = "WEAK_PASSWORD" }, status = 400 });

        if (!req.NewPassword.Any(char.IsUpper) || !req.NewPassword.Any(char.IsDigit) || !req.NewPassword.Any(c => !char.IsLetterOrDigit(c)))
            return BadRequest(new { error = new { message = "Password must contain uppercase, number, and special character", code = "WEAK_PASSWORD" }, status = 400 });

        var user = passwordReset.User;
        user.PasswordHash = _passwordService.HashPassword(req.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        passwordReset.UsedAt = DateTime.UtcNow;

        // Revoke all active refresh tokens
        var activeTokens = await _db.RefreshTokens
            .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
            .ToListAsync();

        foreach (var token in activeTokens)
            token.RevokedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _auditLogService.LogActionAsync(
            user.Id,
            "reset_password",
            "user",
            user.Id,
            null,
            null,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Ok(new { message = "Password reset successful" });
    }
}

// Request/Response DTOs
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}