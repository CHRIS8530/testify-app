using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;

namespace Testify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/projects/{projectId}/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly TestifyDbContext _db;

        public DashboardController(TestifyDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard(Guid projectId)
        {
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
                return NotFound(new { error = new { message = "Project not found", code = "NOT_FOUND" }, status = 404 });

            var totalCases = await _db.TestCases.CountAsync(tc => tc.ProjectId == projectId);
            var totalRuns = await _db.TestRuns.CountAsync(tr => tr.ProjectId == projectId);
            
            var runs = await _db.TestRuns
                .Where(tr => tr.ProjectId == projectId)
                .Include(tr => tr.Results)
                .ToListAsync();

            var totalResults = runs.SelectMany(r => r.Results).ToList();
            var passedResults = totalResults.Count(r => r.Result == "Pass");
            var passRate = totalResults.Count > 0 ? (int)Math.Round(100.0 * passedResults / totalResults.Count) : 0;

            var openDefects = await _db.Defects
                .Where(d => _db.TestCases.Any(tc => tc.Id == d.TestCaseId && tc.ProjectId == projectId) && d.Status == "Open")
                .ToListAsync();

            var defectsBySeverity = openDefects
                .GroupBy(d => d.Severity)
                .Select(g => new { severity = g.Key, count = g.Count() })
                .ToList();

            var defectsByStatus = await _db.Defects
                .Where(d => _db.TestCases.Any(tc => tc.Id == d.TestCaseId && tc.ProjectId == projectId))
                .GroupBy(d => d.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToListAsync();

            var recentDefects = await _db.Defects
                .Where(d => _db.TestCases.Any(tc => tc.Id == d.TestCaseId && tc.ProjectId == projectId))
                .OrderByDescending(d => d.CreatedAt)
                .Take(5)
                .Select(d => new { id = d.Id, title = d.Title, severity = d.Severity, status = d.Status, created_at = d.CreatedAt })
                .ToListAsync();

            return Ok(new
            {
                data = new
                {
                    total_cases = totalCases,
                    total_runs = totalRuns,
                    pass_rate = passRate,
                    total_results = totalResults.Count,
                    passed = passedResults,
                    failed = totalResults.Count(r => r.Result == "Fail"),
                    blocked = totalResults.Count(r => r.Result == "Blocked"),
                    not_run = totalResults.Count(r => r.Result == "Not Run"),
                    open_defects = openDefects.Count,
                    defects_by_severity = defectsBySeverity,
                    defects_by_status = defectsByStatus,
                    recent_defects = recentDefects
                },
                status = 200
            });
        }
    }
}