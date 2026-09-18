using JobPortal.Domain.Enums;

namespace JobPortal.Application.Models
{
    // Encapsulates candidate-facing search/filter criteria for the job board.
    // Kept as a plain query object so the repository signature stays stable
    // as filter criteria grow (Open/Closed Principle).
    public class JobSearchFilter
    {
        public string? Keyword { get; set; }
        public string? Location { get; set; }
        public JobType? JobType { get; set; }
    }
}