using JobPortal.Domain.Enums;

namespace JobPortal.Web.Models.ViewModels
{
    public class MyApplicationsViewModel
    {
        public List<MyApplicationItemViewModel> Applications { get; set; } = new();
    }

    public class MyApplicationItemViewModel
    {
        public int ApplicationId { get; set; }
        public int JobPostingId { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime AppliedDateUtc { get; set; }
        public ApplicationStatus Status { get; set; }
        public bool JobIsStillActive { get; set; }
    }
}