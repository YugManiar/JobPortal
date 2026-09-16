using JobPortal.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Domain.Entities
{
    public class JobApplication
    {
        public int Id { get; set; }

        [Required]
        public int JobPostingId { get; set; }

        [ForeignKey(nameof(JobPostingId))]
        public virtual JobPosting JobPosting { get; set; } = null!;

        [Required]
        public string CandidateId { get; set; } = string.Empty;

        [ForeignKey(nameof(CandidateId))]
        public virtual ApplicationUser Candidate { get; set; } = null!;

        [Required, MaxLength(300)]
        public string ResumeUrl { get; set; } = string.Empty; // path/blob URL to uploaded resume

        [MaxLength(2000)]
        public string? CoverLetter { get; set; }

        public DateTime AppliedDateUtc { get; set; } = DateTime.UtcNow;

        [Required]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Submitted;
    }
}