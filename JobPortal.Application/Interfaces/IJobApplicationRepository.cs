using JobPortal.Domain.Entities;

namespace JobPortal.Application.Interfaces
{
    public interface IJobApplicationRepository
    {
        Task<JobApplication> CreateAsync(JobApplication application, CancellationToken cancellationToken = default);

        Task<bool> HasAppliedAsync(int jobPostingId, string candidateId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<JobApplication>> GetByCandidateIdAsync(
            string candidateId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<JobApplication>> GetByJobPostingIdAsync(
            int jobPostingId, CancellationToken cancellationToken = default);
    }
}