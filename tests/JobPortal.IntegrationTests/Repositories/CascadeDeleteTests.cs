using FluentAssertions;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JobPortal.IntegrationTests.Repositories
{
    // Protects the Restrict/Cascade split we hand-configured in Week 1 to
    // dodge SQL Server's "multiple cascade paths" error. If someone later
    // simplifies that configuration without understanding why it's split
    // this way, this test fails immediately instead of corrupting data
    // silently in production.
    [Collection("Database collection")]
    public class CascadeDeleteTests : IAsyncLifetime
    {
        private readonly TestDatabaseFixture _fixture;

        public CascadeDeleteTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
        }

        // Runs automatically before EVERY test method in this class.
        // Deletes in FK-safe order (children before parents) so each test
        // starts from a guaranteed-empty state, regardless of what ran
        // before it — in this class, the other test class, or a previous
        // crashed run.

        public async Task InitializeAsync()
        {
            _fixture.Context.ChangeTracker.Clear();

            await _fixture.Context.Database.ExecuteSqlRawAsync(
                "DELETE FROM JobApplications");

            await _fixture.Context.Database.ExecuteSqlRawAsync(
                "DELETE FROM JobPostings");

            await _fixture.Context.Database.ExecuteSqlRawAsync(
                "DELETE FROM AspNetUsers");

            _fixture.Context.ChangeTracker.Clear();
        }


        public Task DisposeAsync() => Task.CompletedTask;

        private static ApplicationUser CreateTestUser(string email, string fullName) => new()
        {
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            FullName = fullName
        };

        [Fact]
        public async Task DeletingJobPosting_CascadesAndDeletesItsApplications()
        {
            // Arrange
            var employer = CreateTestUser("cascade-employer@test.com", "Cascade Employer");
            var candidate = CreateTestUser("cascade-candidate@test.com", "Cascade Candidate");
            _fixture.Context.Users.AddRange(employer, candidate);

            var job = new JobPosting
            {
                Title = "Job To Delete",
                Description = "Desc",
                Location = "Remote",
                JobType = JobType.PartTime,
                Employer = employer
            };
            _fixture.Context.JobPostings.Add(job);
            await _fixture.Context.SaveChangesAsync();

            _fixture.Context.JobApplications.Add(new JobApplication
            {
                JobPostingId = job.Id,
                CandidateId = candidate.Id,
                ResumeUrl = "/uploads/resumes/cascade.pdf"
            });
            await _fixture.Context.SaveChangesAsync();

            // Act — delete the job itself.
            _fixture.Context.JobPostings.Remove(job);
            await _fixture.Context.SaveChangesAsync();

            // Assert — its application should be gone too (Cascade).
            var remainingApplications = await _fixture.Context.JobApplications
                .Where(a => a.JobPostingId == job.Id)
                .ToListAsync();

            remainingApplications.Should().BeEmpty();
        }

        [Fact]
        public async Task DeletingCandidate_DoesNotCascadeDeleteUnrelatedJobPostings()
        {
            // Arrange
            var employer = CreateTestUser("safe-employer@test.com", "Safe Employer");
            _fixture.Context.Users.Add(employer);

            var job = new JobPosting
            {
                Title = "Unrelated Job",
                Description = "Desc",
                Location = "Remote",
                JobType = JobType.Internship,
                Employer = employer
            };
            _fixture.Context.JobPostings.Add(job);
            await _fixture.Context.SaveChangesAsync();

            var candidate = CreateTestUser("deletable-candidate@test.com", "Deletable Candidate");
            _fixture.Context.Users.Add(candidate);
            await _fixture.Context.SaveChangesAsync();

            // Act — delete the candidate (who never applied to anything here).
            _fixture.Context.Users.Remove(candidate);
            await _fixture.Context.SaveChangesAsync();

            // Assert — the employer's unrelated job posting must still exist.
            var jobStillExists = await _fixture.Context.JobPostings.AnyAsync(j => j.Id == job.Id);
            jobStillExists.Should().BeTrue();
        }
    }
}