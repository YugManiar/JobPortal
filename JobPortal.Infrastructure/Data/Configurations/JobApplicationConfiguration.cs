using JobPortal.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobPortal.Infrastructure.Data.Configurations
{
    public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
    {
        public void Configure(EntityTypeBuilder<JobApplication> builder)
        {
            builder.ToTable("JobApplications");
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Status).HasConversion<string>();

            // Business rule: one application per candidate per job
            builder.HasIndex(a => new { a.JobPostingId, a.CandidateId }).IsUnique();

            builder.HasOne(a => a.JobPosting)
                   .WithMany(j => j.Applications)
                   .HasForeignKey(a => a.JobPostingId)
                   .OnDelete(DeleteBehavior.Cascade); // delete job → delete its applications

            builder.HasOne(a => a.Candidate)
                   .WithMany(u => u.JobApplications)
                   .HasForeignKey(a => a.CandidateId)
                   .OnDelete(DeleteBehavior.Restrict); // avoids multi-cascade-path conflict
        }
    }
}