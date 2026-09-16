using JobPortal.Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JobPortal.Domain.Entities
{
    public class JobPosting
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public JobType JobType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalaryMin { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? SalaryMax { get; set; }

        public DateTime PostedDateUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ApplicationDeadlineUtc { get; set; }

        public bool IsActive { get; set; } = true;

        // FK — the Employer who owns this posting
        [Required]
        public string EmployerId { get; set; } = string.Empty;

        [ForeignKey(nameof(EmployerId))]
        public virtual ApplicationUser Employer { get; set; } = null!;

        public virtual ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
    }
}