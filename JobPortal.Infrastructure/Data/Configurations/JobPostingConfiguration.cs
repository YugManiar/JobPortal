using JobPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobPortal.Infrastructure.Data.Configurations
{
    public class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
    {
        public void Configure(EntityTypeBuilder<JobPosting> builder)
        {
            builder.ToTable("JobPostings");
            builder.HasKey(j => j.Id);

            builder.Property(j => j.JobType).HasConversion<string>();

            builder.HasIndex(j => j.IsActive);
            builder.HasIndex(j => new { j.Title, j.Location });

            builder.HasOne(j => j.Employer)
                   .WithMany(u => u.JobPostings)
                   .HasForeignKey(j => j.EmployerId)
                   .OnDelete(DeleteBehavior.Restrict); // avoids multi-cascade-path conflict
        }
    }
}