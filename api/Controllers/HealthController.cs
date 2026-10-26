using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;

namespace Testify.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    private readonly TestifyDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(TestifyDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Test database connection
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");

            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                database = "connected"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new
            {
                status = "unhealthy",
                timestamp = DateTime.UtcNow,
                database = "disconnected"
            });
        }
    }
}