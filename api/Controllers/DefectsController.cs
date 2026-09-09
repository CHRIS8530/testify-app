using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Models;

namespace Testify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/projects/{projectId}/defects")]
    public class DefectsController : ControllerBase
    {
        private readonly TestifyDbContext _db;

        public DefectsController(TestifyDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetDefects(Guid projectId, [FromQuery] string? status = null, [FromQuery] string? severity = null, [FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            if (page < 1 || limit < 1 || limit > 100)
                return BadRequest(new { error = new { message = "Invalid page or limit", code = "INVALID_PARAMS" }, status = 400 });

            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
                return NotFound(new { error = new { message = "Project not found", code = "NOT_FOUND" }, status = 404 });

            var query = _db.Defects.Where(d => _db.TestCases.Any(tc => tc.Id == d.TestCaseId && tc.ProjectId == projectId));

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(d => d.Status == status);
            if (!string.IsNullOrWhiteSpace(severity))
                query = query.Where(d => d.Severity == severity);

            var skip = (page - 1) * limit;
            var defects = await query.Skip(skip).Take(limit).ToListAsync();
            var total = await query.CountAsync();

            return Ok(new
            {
                data = defects.Select(d => new { id = d.Id, title = d.Title, severity = d.Severity, status = d.Status, created_at = d.CreatedAt }),
                pagination = new { page, limit, total, pages = (int)Math.Ceiling((double)total / limit) },
                status = 200
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateDefect(Guid projectId, [FromBody] CreateDefectRequest req)
        {
            var testCase = await _db.TestCases.FirstOrDefaultAsync(tc => tc.Id == req.TestCaseId && tc.ProjectId == projectId);
            if (testCase == null)
                return NotFound(new { error = new { message = "Test case not found", code = "NOT_FOUND" }, status = 404 });

            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest(new { error = new { message = "Title is required", code = "INVALID_TITLE" }, status = 400 });

            var defect = new Defect
            {
                Id = Guid.NewGuid(),
                Title = req.Title,
                Description = req.Description ?? "",
                Severity = req.Severity ?? "Medium",
                Status = "Open",
                TestCaseId = req.TestCaseId,
                TestRunResultId = req.TestRunResultId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Defects.Add(defect);
            await _db.SaveChangesAsync();

            return StatusCode(201, new { data = defect, status = 201 });
        }

        [HttpGet("{defectId}")]
        public async Task<IActionResult> GetDefect(Guid projectId, Guid defectId)
        {
            var defect = await _db.Defects.FirstOrDefaultAsync(d => d.Id == defectId && _db.TestCases.Any(tc => tc.Id == d.TestCaseId && tc.ProjectId == projectId));
            if (defect == null)
                return NotFound(new { error = new { message = "Defect not found", code = "NOT_FOUND" }, status = 404 });

            return Ok(new { data = defect, status = 200 });
        }

        [HttpPatch("{defectId}")]
        public async Task<IActionResult> UpdateDefect(Guid projectId, Guid defectId, [FromBody] UpdateDefectRequest req)
        {
            var defect = await _db.Defects.FirstOrDefaultAsync(d => d.Id == defectId && _db.TestCases.Any(tc => tc.Id == d.TestCaseId && tc.ProjectId == projectId));
            if (defect == null)
                return NotFound(new { error = new { message = "Defect not found", code = "NOT_FOUND" }, status = 404 });

            if (!string.IsNullOrWhiteSpace(req.Status))
            {
                defect.Status = req.Status;
                if (req.Status == "Resolved")
                    defect.ResolvedAt = DateTime.UtcNow;
            }

            if (!string.IsNullOrWhiteSpace(req.Severity))
                defect.Severity = req.Severity;

            defect.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { data = defect, status = 200 });
        }

        [HttpDelete("{defectId}")]
        public async Task<IActionResult> DeleteDefect(Guid projectId, Guid defectId)
        {
            var defect = await _db.Defects.FirstOrDefaultAsync(d => d.Id == defectId && _db.TestCases.Any(tc => tc.Id == d.TestCaseId && tc.ProjectId == projectId));
            if (defect == null)
                return NotFound(new { error = new { message = "Defect not found", code = "NOT_FOUND" }, status = 404 });

            _db.Defects.Remove(defect);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CreateDefectRequest
    {
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? Severity { get; set; }
        public Guid TestCaseId { get; set; }
        public Guid TestRunResultId { get; set; }
    }

    public class UpdateDefectRequest
    {
        public string? Status { get; set; }
        public string? Severity { get; set; }
    }
}