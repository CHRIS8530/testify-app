using System;
using System.Collections.Generic;

namespace Testify.Api.Models
{
    public class TestRun
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Status { get; set; } = "In Progress";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }

        public Project Project { get; set; } = null!;
        public ICollection<TestRunResult> Results { get; set; } = new List<TestRunResult>();
    }
}