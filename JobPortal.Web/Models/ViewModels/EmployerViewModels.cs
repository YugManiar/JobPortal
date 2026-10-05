using System.ComponentModel.DataAnnotations;
using JobPortal.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JobPortal.Web.Models.ViewModels
{
    public class MyJobsViewModel
    {
        public List<JobPostingSummaryViewModel> Jobs { get; set; } = new();
    }

    public class JobEditViewModel
    {
        public int Id { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Job Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Job Description")]
        public string Description { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Job Type")]
        public JobType JobType { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Minimum Salary")]
        public decimal? SalaryMin { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Maximum Salary")]
        public decimal? SalaryMax { get; set; }

        [Display(Name = "Application Deadline")]
        [DataType(DataType.Date)]
        public DateTime? ApplicationDeadlineUtc { get; set; }

        [Display(Name = "Listing is active")]
        public bool IsActive { get; set; }

        public SelectList? JobTypeOptions { get; set; }
    }

    public class ApplicantsViewModel
    {
        public int JobPostingId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public List<ApplicantViewModel> Applicants { get; set; } = new();
    }

    public class ApplicantViewModel
    {
        public int ApplicationId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string CandidateEmail { get; set; } = string.Empty;
        public string ResumeUrl { get; set; } = string.Empty;
        public string? CoverLetter { get; set; }
        public DateTime AppliedDateUtc { get; set; }
        public ApplicationStatus Status { get; set; }
    }

    public class UpdateApplicationStatusViewModel
    {
        [Required]
        public int ApplicationId { get; set; }

        [Required]
        public ApplicationStatus NewStatus { get; set; }

        [Required]
        public int JobPostingId { get; set; } // for redirect back to Applicants view
    }
}