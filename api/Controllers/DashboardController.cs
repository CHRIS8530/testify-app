using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Services;

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
            var (denied, _) = await this.RequireProjectAccessAsync(_db, projectId, ProjectRole.Viewer);
            if (denied is not null) return denied;

            var totalCases = await _db.TestCases.CountAsync(tc => tc.ProjectId == projectId);
            var totalRuns = await _db.TestRuns.CountAsync(tr => tr.ProjectId == projectId);
            var runsCompleted = await _db.TestRuns.CountAsync(tr => tr.ProjectId == projectId && tr.Status == "Complete");

            // Every result of every run in this project. The pass rate comes from RunStats, so every screen shows the same rule.
            var results = _db.TestRunResults.Where(r => r.TestRun.ProjectId == projectId);
            var totalResults = await results.CountAsync();
            var passed = await results.CountAsync(r => r.Result == "Pass");
            var failed = await results.CountAsync(r => r.Result == "Fail");
            var blocked = await results.CountAsync(r => r.Result == "Blocked");
            var notRun = await results.CountAsync(r => r.Result == "Not Run");

            // "Current" pass rate is the pass rate of the latest run.
            var latest = await _db.TestRuns
                .Where(tr => tr.ProjectId == projectId)
                .OrderByDescending(tr => tr.CreatedAt)
                .Select(tr => new
                {
                    tr.Id,
                    tr.Status,
                    tr.CreatedAt,
                    Total = tr.Results.Count,
                    Passed = tr.Results.Count(r => r.Result == "Pass"),
                    Failed = tr.Results.Count(r => r.Result == "Fail"),
                    Blocked = tr.Results.Count(r => r.Result == "Blocked"),
                    NotRun = tr.Results.Count(r => r.Result == "Not Run")
                })
                .FirstOrDefaultAsync();

            object? latestRun = latest is null
                ? null
                : new
                {
                    id = latest.Id,
                    status = latest.Status,
                    created_at = latest.CreatedAt,
                    total = latest.Total,
                    passed = latest.Passed,
                    failed = latest.Failed,
                    blocked = latest.Blocked,
                    not_run = latest.NotRun,
                    pass_rate = RunStats.PassRate(latest.Passed, latest.Total)
                };

            var projectDefects = _db.Defects.Where(d => d.TestCase.ProjectId == projectId);

            var openBySeverity = await projectDefects
                .Where(d => d.Status == "Open")
                .GroupBy(d => d.Severity)
                .Select(g => new { severity = g.Key, count = g.Count() })
                .ToListAsync();

            var defectsByStatus = await projectDefects
                .GroupBy(d => d.Status)
                .Select(g => new { status = g.Key, count = g.Count() })
                .ToListAsync();

            var recentDefects = await projectDefects
                .OrderByDescending(d => d.CreatedAt)
                .Take(5)
                .Select(d => new { id = d.Id, title = d.Title, severity = d.Severity, status = d.Status, created_at = d.CreatedAt })
                .ToListAsync();

            return Ok(new
            {
                total_cases = totalCases,
                total_runs = totalRuns,
                runs_completed = runsCompleted,
                pass_rate = RunStats.PassRate(passed, totalResults),
                total_results = totalResults,
                passed,
                failed,
                blocked,
                not_run = notRun,
                latest_run = latestRun,
                open_defects = openBySeverity.Sum(x => x.count),
                defects_by_severity = openBySeverity,
                defects_by_status = defectsByStatus,
                recent_defects = recentDefects
            });
        }
    }
}