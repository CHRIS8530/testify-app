using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Testify.Api.Data;
using Testify.Api.Models;

namespace Testify.Api.Controllers
{
    [ApiController]
    [Route("api/v1/projects")]
    public class ProjectsController : ControllerBase
    {
        private readonly TestifyDbContext _db;

        public ProjectsController(TestifyDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects([FromQuery] int page = 1, [FromQuery] int limit = 10)
        {
            if (page < 1 || limit < 1 || limit > 100)
                return BadRequest(new { error = new { message = "Invalid page or limit", code = "INVALID_PARAMS" }, status = 400 });

            var skip = (page - 1) * limit;
            var projects = await _db.Projects
                .Include(p => p.Members)
                .Skip(skip)
                .Take(limit)
                .ToListAsync();

            var total = await _db.Projects.CountAsync();

            return Ok(new
            {
                data = projects.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    description = p.Description,
                    owner_id = p.OwnerId,
                    members_count = p.Members.Count,
                    created_at = p.CreatedAt,
                    updated_at = p.UpdatedAt
                }),
                pagination = new { page, limit, total, pages = (int)Math.Ceiling((double)total / limit) },
                status = 200
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name) || req.Name.Length > 255)
                return BadRequest(new { error = new { message = "Name is required and must be <= 255 characters", code = "INVALID_NAME" }, status = 400 });

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = req.Name,
                Description = req.Description ?? "",
                OwnerId = req.OwnerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Projects.Add(project);
            await _db.SaveChangesAsync();

            return StatusCode(201, new { data = project, status = 201 });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject(Guid id)
        {
            var project = await _db.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound(new { error = new { message = "Project not found", code = "NOT_FOUND" }, status = 404 });

            return Ok(new
            {
                data = new
                {
                    id = project.Id,
                    name = project.Name,
                    description = project.Description,
                    owner_id = project.OwnerId,
                    members = project.Members.Select(m => new { id = m.Id, user_id = m.UserId, role = m.Role }),
                    created_at = project.CreatedAt,
                    updated_at = project.UpdatedAt
                },
                status = 200
            });
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequest req)
        {
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound(new { error = new { message = "Project not found", code = "NOT_FOUND" }, status = 404 });

            if (!string.IsNullOrWhiteSpace(req.Name))
                project.Name = req.Name;
            if (req.Description != null)
                project.Description = req.Description;

            project.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { data = project, status = 200 });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound(new { error = new { message = "Project not found", code = "NOT_FOUND" }, status = 404 });

            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CreateProjectRequest
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
    }

    public class UpdateProjectRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}