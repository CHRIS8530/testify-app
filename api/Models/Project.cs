using System;
using System.Collections.Generic;

namespace Testify.Api.Models
{
    public class Project
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User Owner { get; set; } = null!;
        public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
        public ICollection<TestCase> TestCases { get; set; } = new List<TestCase>();
        public ICollection<TestRun> TestRuns { get; set; } = new List<TestRun>();
    }
}