using JobPortal.Application.Exceptions;
using JobPortal.Application.Interfaces;
using JobPortal.Application.Models;
using JobPortal.Domain.Entities;
using JobPortal.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobPortal.Infrastructure.Repositories
{
    public class JobRepository : IJobRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobRepository> _logger;

        public JobRepository(ApplicationDbContext context, ILogger<JobRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<JobPosting?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id), "Job posting id must be positive.");

            try
            {
                return await _context.JobPostings
                    .Include(j => j.Employer)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
            }
            catch (SqlException ex) when (IsTimeout(ex))
            {
                _logger.LogError(ex, "Database timeout retrieving JobPosting {JobId}", id);
                throw new RepositoryException($"Timed out retrieving job posting {id}.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving JobPosting {JobId}", id);
                throw new RepositoryException($"A database error occurred while retrieving job posting {id}.", ex);
            }
        }

        public async Task<IReadOnlyList<JobPosting>> GetAllActiveAsync(
            JobSearchFilter? filter = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                IQueryable<JobPosting> query = _context.JobPostings
                    .Include(j => j.Employer)
                    .AsNoTracking()
                    .Where(j => j.IsActive);

                if (filter is not null)
                {
                    if (!string.IsNullOrWhiteSpace(filter.Keyword))
                    {
                        var keyword = filter.Keyword.Trim();
                        query = query.Where(j => j.Title.Contains(keyword) || j.Description.Contains(keyword));
                    }

                    if (!string.IsNullOrWhiteSpace(filter.Location))
                    {
                        var location = filter.Location.Trim();
                        query = query.Where(j => j.Location.Contains(location));
                    }

                    if (filter.JobType.HasValue)
                        query = query.Where(j => j.JobType == filter.JobType.Value);
                }

                return await query
                    .OrderByDescending(j => j.PostedDateUtc)
                    .ToListAsync(cancellationToken);
            }
            catch (SqlException ex) when (IsTimeout(ex))
            {
                _logger.LogError(ex, "Database timeout listing active job postings");
                throw new RepositoryException("Timed out retrieving job postings. Please try again.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error listing active job postings");
                throw new RepositoryException("A database error occurred while retrieving job postings.", ex);
            }
        }

        public async Task<IReadOnlyList<JobPosting>> GetByEmployerIdAsync(
            string employerId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(employerId))
                throw new ArgumentException("Employer id must be provided.", nameof(employerId));

            try
            {
                return await _context.JobPostings
                    .AsNoTracking()
                    .Where(j => j.EmployerId == employerId)
                    .OrderByDescending(j => j.PostedDateUtc)
                    .ToListAsync(cancellationToken);
            }
            catch (SqlException ex) when (IsTimeout(ex))
            {
                _logger.LogError(ex, "Database timeout retrieving jobs for employer {EmployerId}", employerId);
                throw new RepositoryException("Timed out retrieving your job postings.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error retrieving jobs for employer {EmployerId}", employerId);
                throw new RepositoryException("A database error occurred while retrieving your job postings.", ex);
            }
        }

        public async Task<JobPosting> CreateAsync(JobPosting jobPosting, CancellationToken cancellationToken = default)
        {
            if (jobPosting is null)
                throw new ArgumentNullException(nameof(jobPosting));

            try
            {
                await _context.JobPostings.AddAsync(jobPosting, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                return jobPosting;
            }
            catch (SqlException ex) when (IsTimeout(ex))
            {
                _logger.LogError(ex, "Database timeout creating JobPosting");
                throw new RepositoryException("Timed out saving the job posting. Please try again.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating JobPosting");
                throw new RepositoryException("A database error occurred while saving the job posting.", ex);
            }
        }

        public async Task UpdateAsync(JobPosting jobPosting, CancellationToken cancellationToken = default)
        {
            if (jobPosting is null)
                throw new ArgumentNullException(nameof(jobPosting));

            try
            {
                _context.JobPostings.Update(jobPosting);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency conflict updating JobPosting {JobId}", jobPosting.Id);
                throw new RepositoryException($"Job posting {jobPosting.Id} was modified by another process.", ex);
            }
            catch (SqlException ex) when (IsTimeout(ex))
            {
                _logger.LogError(ex, "Database timeout updating JobPosting {JobId}", jobPosting.Id);
                throw new RepositoryException("Timed out updating the job posting.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating JobPosting {JobId}", jobPosting.Id);
                throw new RepositoryException("A database error occurred while updating the job posting.", ex);
            }
        }

        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            if (id <= 0)
                throw new ArgumentOutOfRangeException(nameof(id));

            try
            {
                var job = await _context.JobPostings.FindAsync(new object[] { id }, cancellationToken);
                if (job is null)
                    return false;

                _context.JobPostings.Remove(job);
                await _context.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (SqlException ex) when (IsTimeout(ex))
            {
                _logger.LogError(ex, "Database timeout deleting JobPosting {JobId}", id);
                throw new RepositoryException("Timed out deleting the job posting.", ex);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting JobPosting {JobId}", id);
                throw new RepositoryException("A database error occurred while deleting the job posting.", ex);
            }
        }

        // SQL error numbers: -2 = client-side command timeout, -1 = connection-level timeout,
        // 1205 = deadlock victim (treated as transient/retryable in the same bucket).
        private static bool IsTimeout(SqlException ex) =>
            ex.Number is -2 or -1 or 1205;
    }
}