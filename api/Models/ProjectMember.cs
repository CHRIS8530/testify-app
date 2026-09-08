using System;

namespace Testify.Api.Models
{
    public class ProjectMember
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Guid UserId { get; set; }
        public string? Role { get; set; }
        public DateTime CreatedAt { get; set; }

        public Project Project { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}