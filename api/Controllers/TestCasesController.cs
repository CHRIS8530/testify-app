using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Models;

namespace Testify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/projects/{projectId}/cases")]
    public class TestCasesController : ControllerBase
    {
        private readonly TestifyDbContext _db;

        public TestCasesController(TestifyDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetTestCases(Guid projectId, [FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            if (page < 1 || limit < 1 || limit > 100)
                return BadRequest(new { error = new { message = "Invalid page or limit", code = "INVALID_PARAMS" }, status = 400 });

            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
                return NotFound(new { error = new { message = "Project not found", code = "NOT_FOUND" }, status = 404 });

            var skip = (page - 1) * limit;
            var cases = await _db.TestCases
                .Where(tc => tc.ProjectId == projectId)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            var total = await _db.TestCases.CountAsync(tc => tc.ProjectId == projectId);

            return Ok(new
            {
                data = cases.Select(c => new { id = c.Id, title = c.Title, priority = c.Priority, created_at = c.CreatedAt }),
                pagination = new { page, limit, total, pages = (int)Math.Ceiling((double)total / limit) },
                status = 200
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateTestCase(Guid projectId, [FromBody] CreateTestCaseRequest req)
        {
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
                return NotFound(new { error = new { message = "Project not found", code = "NOT_FOUND" }, status = 404 });

            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest(new { error = new { message = "Title is required", code = "INVALID_TITLE" }, status = 400 });

            var testCase = new TestCase
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = req.Title,
                Preconditions = req.Preconditions ?? "",
                Steps = req.Steps ?? "",
                ExpectedResult = req.ExpectedResult ?? "",
                Priority = req.Priority ?? "Medium",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.TestCases.Add(testCase);
            await _db.SaveChangesAsync();

            return StatusCode(201, new { data = testCase, status = 201 });
        }

        [HttpGet("{caseId}")]
        public async Task<IActionResult> GetTestCase(Guid projectId, Guid caseId)
        {
            var testCase = await _db.TestCases.FirstOrDefaultAsync(tc => tc.Id == caseId && tc.ProjectId == projectId);
            if (testCase == null)
                return NotFound(new { error = new { message = "Test case not found", code = "NOT_FOUND" }, status = 404 });

            return Ok(new { data = testCase, status = 200 });
        }

        [HttpPatch("{caseId}")]
        public async Task<IActionResult> UpdateTestCase(Guid projectId, Guid caseId, [FromBody] UpdateTestCaseRequest req)
        {
            var testCase = await _db.TestCases.FirstOrDefaultAsync(tc => tc.Id == caseId && tc.ProjectId == projectId);
            if (testCase == null)
                return NotFound(new { error = new { message = "Test case not found", code = "NOT_FOUND" }, status = 404 });

            if (!string.IsNullOrWhiteSpace(req.Title))
                testCase.Title = req.Title;
            if (req.Preconditions != null)
                testCase.Preconditions = req.Preconditions;
            if (req.Steps != null)
                testCase.Steps = req.Steps;
            if (req.ExpectedResult != null)
                testCase.ExpectedResult = req.ExpectedResult;
            if (req.Priority != null)
                testCase.Priority = req.Priority;

            testCase.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { data = testCase, status = 200 });
        }

        [HttpDelete("{caseId}")]
        public async Task<IActionResult> DeleteTestCase(Guid projectId, Guid caseId)
        {
            var testCase = await _db.TestCases.FirstOrDefaultAsync(tc => tc.Id == caseId && tc.ProjectId == projectId);
            if (testCase == null)
                return NotFound(new { error = new { message = "Test case not found", code = "NOT_FOUND" }, status = 404 });

            _db.TestCases.Remove(testCase);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CreateTestCaseRequest
    {
        public string Title { get; set; } = "";
        public string? Preconditions { get; set; }
        public string? Steps { get; set; }
        public string? ExpectedResult { get; set; }
        public string? Priority { get; set; }
    }

    public class UpdateTestCaseRequest
    {
        public string? Title { get; set; }
        public string? Preconditions { get; set; }
        public string? Steps { get; set; }
        public string? ExpectedResult { get; set; }
        public string? Priority { get; set; }
    }
}