using System;

namespace Testify.Api.Models
{
    public class Defect
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = "Medium";
        public string Status { get; set; } = "Open";
        public Guid TestCaseId { get; set; }
        public Guid TestRunResultId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public TestCase TestCase { get; set; } = null!;
        public TestRunResult TestRunResult { get; set; } = null!;
    }
}