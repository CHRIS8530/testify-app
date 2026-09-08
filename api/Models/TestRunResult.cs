using System;

namespace Testify.Api.Models
{
    public class TestRunResult
    {
        public Guid Id { get; set; }
        public Guid TestRunId { get; set; }
        public Guid TestCaseId { get; set; }
        public string Result { get; set; } = "Not Run";
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public TestRun TestRun { get; set; } = null!;
        public TestCase TestCase { get; set; } = null!;
    }
}