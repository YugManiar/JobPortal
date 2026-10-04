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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<JobController> _logger;

        public JobController(
            IJobRepository jobRepository,
            UserManager<ApplicationUser> userManager,
            ILogger<JobController> logger)
        {
            _jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: /Job — the public job board. Candidates browse/search here; no login required.
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

        // GET: /Job/Create — Employer-only posting form.
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

        // POST: /Job/Create
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