using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;

namespace JobPortal.Application.Interfaces
{
    public interface IJobApplicationRepository
    {

        Task UpdateStatusAsync(int applicationId, ApplicationStatus newStatus, string employerId, CancellationToken cancellationToken = default);

        Task<JobApplication> CreateAsync(JobApplication application, CancellationToken cancellationToken = default);

        Task<bool> HasAppliedAsync(int jobPostingId, string candidateId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<JobApplication>> GetByCandidateIdAsync(
            string candidateId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<JobApplication>> GetByJobPostingIdAsync(
            int jobPostingId, CancellationToken cancellationToken = default);
    }
}