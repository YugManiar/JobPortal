using System.ComponentModel.DataAnnotations;
using JobPortal.Domain.Enums;

namespace JobPortal.Web.Models.ViewModels
{
    public class JobDetailsViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public JobType JobType { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public DateTime PostedDateUtc { get; set; }
        public DateTime? ApplicationDeadlineUtc { get; set; }
        public string CompanyName { get; set; } = string.Empty;

        public bool CandidateHasApplied { get; set; }
    }

    public class JobApplyViewModel
    {
        public int JobPostingId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please attach your resume.")]
        [Display(Name = "Resume (PDF or Word, max 5MB)")]
        public IFormFile? ResumeFile { get; set; }

        [StringLength(2000)]
        [Display(Name = "Cover Letter (optional)")]
        [DataType(DataType.MultilineText)]
        public string? CoverLetter { get; set; }
    }
}