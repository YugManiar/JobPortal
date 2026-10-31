using FluentAssertions;
using JobPortal.Application.Exceptions;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Infrastructure.Repositories;
using JobPortal.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JobPortal.IntegrationTests.Repositories
{
    [Collection("Database collection")]
    public class JobApplicationRepositoryTests : IAsyncLifetime
    {
        private readonly TestDatabaseFixture _fixture;
        private readonly JobApplicationRepository _repository;

        public JobApplicationRepositoryTests(TestDatabaseFixture fixture)
        {
            _fixture = fixture;
            _repository = new JobApplicationRepository(
                _fixture.Context,
                NullLogger<JobApplicationRepository>.Instance);
        }

        // Runs automatically before EVERY test method in this class.

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
        public async Task CreateAsync_SameCandidateAppliesTwice_ThrowsDuplicateApplicationException()
        {
            // Arrange — seed one employer, one job, one candidate.
            var employer = CreateTestUser("employer1@test.com", "Test Employer");
            var candidate = CreateTestUser("candidate1@test.com", "Test Candidate");
            _fixture.Context.Users.AddRange(employer, candidate);

            var job = new JobPosting
            {
                Title = "Test Job",
                Description = "Test Description",
                Location = "Remote",
                JobType = JobType.FullTime,
                Employer = employer
            };
            _fixture.Context.JobPostings.Add(job);
            await _fixture.Context.SaveChangesAsync();

            var firstApplication = new JobApplication
            {
                JobPostingId = job.Id,
                CandidateId = candidate.Id,
                ResumeUrl = "/uploads/resumes/first.pdf"
            };
            await _repository.CreateAsync(firstApplication);

            // Act — the SAME candidate tries to apply to the SAME job again.
            var secondApplication = new JobApplication
            {
                JobPostingId = job.Id,
                CandidateId = candidate.Id,
                ResumeUrl = "/uploads/resumes/second.pdf"
            };
            var act = async () => await _repository.CreateAsync(secondApplication);

            // Assert — this proves the unique index we configured in
            // JobApplicationConfiguration.cs actually blocks the duplicate
            // at the database level, not just in application code.
            await act.Should().ThrowAsync<DuplicateApplicationException>();
        }

        [Fact]
        public async Task HasAppliedAsync_AfterApplying_ReturnsTrue()
        {
            var employer = CreateTestUser("employer2@test.com", "Employer Two");
            var candidate = CreateTestUser("candidate2@test.com", "Candidate Two");
            _fixture.Context.Users.AddRange(employer, candidate);

            var job = new JobPosting
            {
                Title = "Another Job",
                Description = "Desc",
                Location = "NYC",
                JobType = JobType.Contract,
                Employer = employer
            };
            _fixture.Context.JobPostings.Add(job);
            await _fixture.Context.SaveChangesAsync();

            await _repository.CreateAsync(new JobApplication
            {
                JobPostingId = job.Id,
                CandidateId = candidate.Id,
                ResumeUrl = "/uploads/resumes/test.pdf"
            });

            var result = await _repository.HasAppliedAsync(job.Id, candidate.Id);

            result.Should().BeTrue();
        }
    }
}