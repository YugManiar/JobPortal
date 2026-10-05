using JobPortal.Application.Exceptions;
using JobPortal.Application.Interfaces;
using JobPortal.Application.Models;
using JobPortal.Domain.Entities;
using JobPortal.Domain.Enums;
using JobPortal.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace JobPortal.Web.Controllers
{
    public class JobController : Controller
    {
        private readonly IJobRepository _jobRepository;
        private readonly IJobApplicationRepository _jobApplicationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<JobController> _logger;

        public JobController(
            IJobRepository jobRepository,
            IJobApplicationRepository jobApplicationRepository,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment,
            ILogger<JobController> logger)
        {
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _jobApplicationRepository = jobApplicationRepository ?? throw new ArgumentNullException(nameof(jobApplicationRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ============================================================
        // GET: /Job — public job board. Candidates browse/search here.
        // ============================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Index(
            string? keyword,
            string? location,
            JobType? jobType,
            CancellationToken cancellationToken)
        {
            var filter = new JobSearchFilter
            {
                Keyword = keyword,
                Location = location,
                JobType = jobType
            };

            try
            {
                var jobs = await _jobRepository.GetAllActiveAsync(filter, cancellationToken);

                var viewModel = new JobListViewModel
                {
                    Filter = filter,
                    Jobs = jobs.Select(MapToSummary).ToList()
                };

                return View(viewModel);
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Failed to load job board");
                TempData["ErrorMessage"] = "We couldn't load job listings right now. Please try again shortly.";
                return View(new JobListViewModel { Filter = filter });
            }
        }

        // ============================================================
        // GET: /Job/Details/5 — public job detail page.
        // ============================================================
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
        {
            try
            {
                var job = await _jobRepository.GetByIdAsync(id, cancellationToken);
                if (job is null || !job.IsActive)
                    return NotFound();

                var viewModel = new JobDetailsViewModel
                {
                    Id = job.Id,
                    Title = job.Title,
                    Description = job.Description,
                    Location = job.Location,
                    JobType = job.JobType,
                    SalaryMin = job.SalaryMin,
                    SalaryMax = job.SalaryMax,
                    PostedDateUtc = job.PostedDateUtc,
                    ApplicationDeadlineUtc = job.ApplicationDeadlineUtc,
                    CompanyName = job.Employer?.CompanyName ?? job.Employer?.FullName ?? "Confidential"
                };

                if (User.Identity?.IsAuthenticated == true && User.IsInRole("Candidate"))
                {
                    var candidateId = _userManager.GetUserId(User)!;
                    viewModel.CandidateHasApplied = await _jobApplicationRepository.HasAppliedAsync(
                        id, candidateId, cancellationToken);
                }

                return View(viewModel);
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Failed to load job details for Job {JobId}", id);
                TempData["ErrorMessage"] = "We couldn't load this job posting right now.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================================
        // GET: /Job/Create — Employer-only posting form.
        // ============================================================
        [HttpGet]
        [Authorize(Roles = "Employer")]
        public IActionResult Create()
        {
            var viewModel = new JobCreateViewModel
            {
                JobTypeOptions = GetJobTypeSelectList()
            };
            return View(viewModel);
        }

        // ============================================================
        // POST: /Job/Create
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employer")]
        public async Task<IActionResult> Create(JobCreateViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                model.JobTypeOptions = GetJobTypeSelectList();
                return View(model);
            }

            var employerId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(employerId))
            {
                return Challenge(); // safety net — shouldn't trigger given [Authorize]
            }

            var jobPosting = new JobPosting
            {
                Title = model.Title,
                Description = model.Description,
                Location = model.Location,
                JobType = model.JobType,
                SalaryMin = model.SalaryMin,
                SalaryMax = model.SalaryMax,
                ApplicationDeadlineUtc = model.ApplicationDeadlineUtc,
                EmployerId = employerId,
                IsActive = true
            };

            try
            {
                await _jobRepository.CreateAsync(jobPosting, cancellationToken);
                TempData["SuccessMessage"] = "Your job posting is now live.";
                return RedirectToAction(nameof(Index));
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Failed to create job posting for employer {EmployerId}", employerId);
                ModelState.AddModelError(string.Empty, "We couldn't save your job posting. Please try again.");
                model.JobTypeOptions = GetJobTypeSelectList();
                return View(model);
            }
        }

        // ============================================================
        // GET: /Job/Apply/5 — Candidate-only application form.
        // ============================================================
        [HttpGet]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> Apply(int id, CancellationToken cancellationToken)
        {
            var job = await _jobRepository.GetByIdAsync(id, cancellationToken);
            if (job is null || !job.IsActive)
                return NotFound();

            var candidateId = _userManager.GetUserId(User)!;
            if (await _jobApplicationRepository.HasAppliedAsync(id, candidateId, cancellationToken))
            {
                TempData["InfoMessage"] = "You've already applied to this job.";
                return RedirectToAction(nameof(Details), new { id });
            }

            return View(new JobApplyViewModel { JobPostingId = id, JobTitle = job.Title });
        }

        // ============================================================
        // POST: /Job/Apply/5
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Candidate")]
        public async Task<IActionResult> Apply(JobApplyViewModel model, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Manual file validation — DataAnnotations can't validate IFormFile content directly.
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            const long maxFileSizeBytes = 5 * 1024 * 1024; // 5MB

            var extension = Path.GetExtension(model.ResumeFile!.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(model.ResumeFile), "Only PDF or Word documents are accepted.");
                return View(model);
            }
            if (model.ResumeFile.Length > maxFileSizeBytes)
            {
                ModelState.AddModelError(nameof(model.ResumeFile), "Resume file must be smaller than 5MB.");
                return View(model);
            }

            var candidateId = _userManager.GetUserId(User)!;

            try
            {
                var resumeUrl = await SaveResumeFileAsync(model.ResumeFile, candidateId, cancellationToken);

                var application = new JobApplication
                {
                    JobPostingId = model.JobPostingId,
                    CandidateId = candidateId,
                    ResumeUrl = resumeUrl,
                    CoverLetter = model.CoverLetter,
                    Status = ApplicationStatus.Submitted
                };

                await _jobApplicationRepository.CreateAsync(application, cancellationToken);

                TempData["SuccessMessage"] = "Your application was submitted successfully!";
                return RedirectToAction(nameof(Details), new { id = model.JobPostingId });
            }
            catch (DuplicateApplicationException)
            {
                TempData["InfoMessage"] = "You've already applied to this job.";
                return RedirectToAction(nameof(Details), new { id = model.JobPostingId });
            }
            catch (RepositoryException ex)
            {
                _logger.LogError(ex, "Failed to submit application for Job {JobId}", model.JobPostingId);
                ModelState.AddModelError(string.Empty, "We couldn't submit your application. Please try again.");
                return View(model);
            }
        }

        // ============================================================
        // Private helpers
        // ============================================================

        private async Task<string> SaveResumeFileAsync(IFormFile resumeFile, string candidateId, CancellationToken cancellationToken)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "resumes");
            Directory.CreateDirectory(uploadsFolder); // no-op if it already exists

            var uniqueFileName = $"{candidateId}_{Guid.NewGuid()}{Path.GetExtension(resumeFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await resumeFile.CopyToAsync(stream, cancellationToken);
            }

            return $"/uploads/resumes/{uniqueFileName}";
        }

        private static SelectList GetJobTypeSelectList() =>
            new(Enum.GetValues(typeof(JobType)));

        private static JobPostingSummaryViewModel MapToSummary(JobPosting job) => new()
        {
            Id = job.Id,
            Title = job.Title,
            CompanyName = job.Employer?.CompanyName ?? job.Employer?.FullName ?? "Confidential",
            Location = job.Location,
            JobType = job.JobType,
            SalaryMin = job.SalaryMin,
            SalaryMax = job.SalaryMax,
            PostedDateUtc = job.PostedDateUtc
        };
    }
}