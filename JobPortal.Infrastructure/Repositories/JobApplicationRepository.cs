using JobPortal.Application.Exceptions;
using JobPortal.Application.Interfaces;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Infrastructure.Repositories
{
    public class JobApplicationRepository : IJobApplicationRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobApplicationRepository> _logger;

        // SQL Server unique-constraint violation error numbers.
        private const int UniqueConstraintViolation = 2627;
        private const int UniqueIndexViolation = 2601;

        public JobApplicationRepository(ApplicationDbContext context, ILogger<JobApplicationRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<JobApplication> CreateAsync(JobApplication application, CancellationToken cancellationToken = default)
        {
            if (application is null)
                throw new ArgumentNullException(nameof(application));

            try
            {
                await _context.JobApplications.AddAsync(application, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return application;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx &&
                                                (sqlEx.Number == UniqueConstraintViolation || sqlEx.Number == UniqueIndexViolation))
            {
                _logger.LogWarning(ex, "Duplicate application attempt: Job {JobId}, Candidate {CandidateId}",
                    application.JobPostingId, application.CandidateId);
                throw new DuplicateApplicationException("You have already applied to this job.");
            }
            catch (SqlException ex) when (IsTimeout(ex))
            {
                _logger.LogError(ex, "Database timeout creating JobApplication");
                throw new RepositoryException("Timed out submitting your application. Please try again.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating JobApplication");
                throw new RepositoryException("A database error occurred while submitting your application.", ex);
            }
        }

        public async Task<bool> HasAppliedAsync(int jobPostingId, string candidateId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(candidateId))
                throw new ArgumentException("Candidate id must be provided.", nameof(candidateId));

            try
            {
                return await _context.JobApplications
                    .AsNoTracking()
                    .AnyAsync(a => a.JobPostingId == jobPostingId && a.CandidateId == candidateId, cancellationToken);
            }
            catch (SqlException ex) when (IsTimeout(ex))
            {
                _logger.LogError(ex, "Database timeout checking application status for Job {JobId}", jobPostingId);
                throw new RepositoryException("Timed out checking your application status.", ex);
            }
        }

        public async Task<IReadOnlyList<JobApplication>> GetByCandidateIdAsync(
            string candidateId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(candidateId))
                throw new ArgumentException("Candidate id must be provided.", nameof(candidateId));

            try
            {
                return await _context.JobApplications
                    .Include(a => a.JobPosting)
                    .AsNoTracking()
                    .Where(a => a.CandidateId == candidateId)
                    .OrderByDescending(a => a.AppliedDateUtc)
                    .ToListAsync(cancellationToken);
            }
            catch (SqlException ex) when (IsTimeout(ex))
            {
                _logger.LogError(ex, "Database timeout retrieving applications for candidate {CandidateId}", candidateId);
                throw new RepositoryException("Timed out retrieving your applications.", ex);
            }
        }

        public async Task<IReadOnlyList<JobApplication>> GetByJobPostingIdAsync(
            int jobPostingId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.JobApplications
                    .Include(a => a.Candidate)
                    .AsNoTracking()
                    .Where(a => a.JobPostingId == jobPostingId)
                    .OrderByDescending(a => a.AppliedDateUtc)
                    .ToListAsync(cancellationToken);
            }
            catch (SqlException ex) when (IsTimeout(ex))
            {
                _logger.LogError(ex, "Database timeout retrieving applicants for Job {JobId}", jobPostingId);
                throw new RepositoryException("Timed out retrieving applicants.", ex);
            }
        }

        private static bool IsTimeout(SqlException ex) => ex.Number is -2 or -1 or 1205;
    }
}