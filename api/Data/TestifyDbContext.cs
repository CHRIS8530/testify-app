using Microsoft.EntityFrameworkCore;
using Testify.Api.Models;

namespace Testify.Api.Data
{
    public class TestifyDbContext : DbContext
    {
        public TestifyDbContext(DbContextOptions<TestifyDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<TestCase> TestCases { get; set; }
        public DbSet<TestRun> TestRuns { get; set; }
        public DbSet<TestRunResult> TestRunResults { get; set; }
        public DbSet<Defect> Defects { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Users table constraints
            modelBuilder.Entity<User>()
                .HasKey(u => u.Id);
            
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Projects -> Users (owner)
            modelBuilder.Entity<Project>()
                .HasOne(p => p.Owner)
                .WithMany(u => u.OwnedProjects)
                .HasForeignKey(p => p.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // ProjectMembers -> Projects + Users
            modelBuilder.Entity<ProjectMember>()
                .HasOne(pm => pm.Project)
                .WithMany(p => p.Members)
                .HasForeignKey(pm => pm.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectMember>()
                .HasOne(pm => pm.User)
                .WithMany(u => u.ProjectMemberships)
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // TestCases -> Projects
            modelBuilder.Entity<TestCase>()
                .HasOne(tc => tc.Project)
                .WithMany(p => p.TestCases)
                .HasForeignKey(tc => tc.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // TestRuns -> Projects
            modelBuilder.Entity<TestRun>()
                .HasOne(tr => tr.Project)
                .WithMany(p => p.TestRuns)
                .HasForeignKey(tr => tr.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // TestRunResults -> TestRun + TestCase
            modelBuilder.Entity<TestRunResult>()
                .HasOne(trr => trr.TestRun)
                .WithMany(tr => tr.Results)
                .HasForeignKey(trr => trr.TestRunId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TestRunResult>()
                .HasOne(trr => trr.TestCase)
                .WithMany()
                .HasForeignKey(trr => trr.TestCaseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TestRunResult>()
                .HasIndex(trr => new { trr.TestRunId, trr.TestCaseId })
                .IsUnique();

            // Defects -> TestRunResult + TestCase
            modelBuilder.Entity<Defect>()
                .HasOne(d => d.TestCase)
                .WithMany()
                .HasForeignKey(d => d.TestCaseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Defect>()
                .HasOne(d => d.TestRunResult)
                .WithMany()
                .HasForeignKey(d => d.TestRunResultId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}