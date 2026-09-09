using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Models;

namespace Testify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/projects/{projectId}/runs")]
    public class TestRunsController : ControllerBase
    {
        private readonly TestifyDbContext _db;

        public TestRunsController(TestifyDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetTestRuns(Guid projectId, [FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            if (page < 1 || limit < 1 || limit > 100)
                return BadRequest(new { error = new { message = "Invalid page or limit", code = "INVALID_PARAMS" }, status = 400 });

            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
                return NotFound(new { error = new { message = "Project not found", code = "NOT_FOUND" }, status = 404 });

            var skip = (page - 1) * limit;
            var runs = await _db.TestRuns
                .Where(tr => tr.ProjectId == projectId)
                .Include(tr => tr.Results)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            var total = await _db.TestRuns.CountAsync(tr => tr.ProjectId == projectId);

            var runsWithPassRate = runs.Select(r => new
            {
                id = r.Id,
                status = r.Status,
                total_cases = r.Results.Count,
                passed = r.Results.Count(res => res.Result == "Pass"),
                pass_rate = r.Results.Count > 0 ? (int)Math.Round(100.0 * r.Results.Count(res => res.Result == "Pass") / r.Results.Count) : 0,
                created_at = r.CreatedAt,
                completed_at = r.CompletedAt
            });

            return Ok(new
            {
                data = runsWithPassRate,
                pagination = new { page, limit, total, pages = (int)Math.Ceiling((double)total / limit) },
                status = 200
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateTestRun(Guid projectId, [FromBody] CreateTestRunRequest req)
        {
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
                return NotFound(new { error = new { message = "Project not found", code = "NOT_FOUND" }, status = 404 });

            if (req.TestCaseIds == null || req.TestCaseIds.Length == 0)
                return BadRequest(new { error = new { message = "At least one test case must be selected", code = "NO_CASES" }, status = 400 });

            var testRun = new TestRun
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Status = "In Progress",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.TestRuns.Add(testRun);
            await _db.SaveChangesAsync();

            foreach (var caseId in req.TestCaseIds)
            {
                var testCase = await _db.TestCases.FirstOrDefaultAsync(tc => tc.Id == caseId && tc.ProjectId == projectId);
                if (testCase != null)
                {
                    var result = new TestRunResult
                    {
                        Id = Guid.NewGuid(),
                        TestRunId = testRun.Id,
                        TestCaseId = caseId,
                        Result = "Not Run",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _db.TestRunResults.Add(result);
                }
            }

            await _db.SaveChangesAsync();

            return StatusCode(201, new { data = testRun, status = 201 });
        }

        [HttpGet("{runId}")]
        public async Task<IActionResult> GetTestRun(Guid projectId, Guid runId)
        {
            var testRun = await _db.TestRuns
                .Include(tr => tr.Results)
                .FirstOrDefaultAsync(tr => tr.Id == runId && tr.ProjectId == projectId);

            if (testRun == null)
                return NotFound(new { error = new { message = "Test run not found", code = "NOT_FOUND" }, status = 404 });

            var results = await _db.TestRunResults
                .Where(r => r.TestRunId == runId)
                .Include(r => r.TestCase)
                .ToListAsync();

            return Ok(new
            {
                data = new
                {
                    id = testRun.Id,
                    status = testRun.Status,
                    results = results.Select(r => new
                    {
                        id = r.Id,
                        test_case_id = r.TestCaseId,
                        test_case_title = r.TestCase.Title,
                        result = r.Result,
                        notes = r.Notes,
                        created_at = r.CreatedAt
                    }),
                    created_at = testRun.CreatedAt,
                    completed_at = testRun.CompletedAt
                },
                status = 200
            });
        }

        [HttpPatch("{runId}")]
        public async Task<IActionResult> UpdateTestRun(Guid projectId, Guid runId, [FromBody] UpdateTestRunRequest req)
        {
            var testRun = await _db.TestRuns.FirstOrDefaultAsync(tr => tr.Id == runId && tr.ProjectId == projectId);
            if (testRun == null)
                return NotFound(new { error = new { message = "Test run not found", code = "NOT_FOUND" }, status = 404 });

            if (req.Results != null && req.Results.Length > 0)
            {
                foreach (var result in req.Results)
                {
                    var runResult = await _db.TestRunResults.FirstOrDefaultAsync(r => r.Id == result.ResultId);
                    if (runResult != null)
                    {
                        runResult.Result = result.Result;
                        runResult.Notes = result.Notes;
                        runResult.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(req.Status))
            {
                testRun.Status = req.Status;
                if (req.Status == "Complete")
                    testRun.CompletedAt = DateTime.UtcNow;
            }

            testRun.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { data = testRun, status = 200 });
        }

        [HttpDelete("{runId}")]
        public async Task<IActionResult> DeleteTestRun(Guid projectId, Guid runId)
        {
            var testRun = await _db.TestRuns.FirstOrDefaultAsync(tr => tr.Id == runId && tr.ProjectId == projectId);
            if (testRun == null)
                return NotFound(new { error = new { message = "Test run not found", code = "NOT_FOUND" }, status = 404 });

            _db.TestRuns.Remove(testRun);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CreateTestRunRequest
    {
        public Guid[] TestCaseIds { get; set; } = Array.Empty<Guid>();
    }

    public class UpdateTestRunRequest
    {
        public string? Status { get; set; }
        public UpdateTestRunResultItem[]? Results { get; set; }
    }

    public class UpdateTestRunResultItem
    {
        public Guid ResultId { get; set; }
        public string Result { get; set; } = "";
        public string? Notes { get; set; }
    }
}