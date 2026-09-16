using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace JobPortal.Domain.Entities
{
    // Extends IdentityUser to leverage ASP.NET Core Identity for auth.
    // Role distinction (Employer/Candidate) is handled via Identity Roles, seeded in Week 2.
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? CompanyName { get; set; } // populated only when role = Employer

        [MaxLength(300)]
        public string? ProfileImageUrl { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}