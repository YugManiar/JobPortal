using System.ComponentModel.DataAnnotations;
using JobPortal.Application.Models;
using JobPortal.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JobPortal.Web.Models.ViewModels
{
    // Powers the job board (Index) — search filter + result list.
    // Kept separate from JobPosting entity so Views never bind directly
    // to EF entities (avoids over-posting and keeps Web decoupled from Domain).
    public class JobListViewModel
    {
        public JobSearchFilter Filter { get; set; } = new();
        public List<JobPostingSummaryViewModel> Jobs { get; set; } = new();
    }

    public class JobPostingSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public JobType JobType { get; set; }
        public decimal? SalaryMin { get; set; }
        public decimal? SalaryMax { get; set; }
        public DateTime PostedDateUtc { get; set; }
    }

    // Powers the "Post a Job" form.
    public class JobCreateViewModel
    {
        [Required(ErrorMessage = "Job title is required.")]
        [StringLength(150)]
        [Display(Name = "Job Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please describe the role.")]
        [Display(Name = "Job Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Job Type")]
        public JobType JobType { get; set; }

        [Display(Name = "Minimum Salary")]
        [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number.")]
        public decimal? SalaryMin { get; set; }

        [Display(Name = "Maximum Salary")]
        [Range(0, double.MaxValue, ErrorMessage = "Salary must be a positive number.")]
        public decimal? SalaryMax { get; set; }

        [Display(Name = "Application Deadline")]
        [DataType(DataType.Date)]
        public DateTime? ApplicationDeadlineUtc { get; set; }

        // Populated by the controller; not bound from the form itself.
        public SelectList? JobTypeOptions { get; set; }
    }
}