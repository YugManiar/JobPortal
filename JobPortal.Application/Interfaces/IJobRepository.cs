using JobPortal.Application.Models;
using JobPortal.Domain.Entities;

namespace JobPortal.Application.Interfaces
{
    public interface IJobRepository
    {
        Task<JobPosting?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<JobPosting>> GetAllActiveAsync(
            JobSearchFilter? filter = null,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<JobPosting>> GetByEmployerIdAsync(
            string employerId,
            CancellationToken cancellationToken = default);

        Task<JobPosting> CreateAsync(JobPosting jobPosting, CancellationToken cancellationToken = default);

        Task UpdateAsync(JobPosting jobPosting, CancellationToken cancellationToken = default);

        Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}