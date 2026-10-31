using FluentAssertions;
using JobPortal.Application.Interfaces;
using JobPortal.Domain.Entities;
using JobPortal.Web.Controllers;
using JobPortal.Web.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace JobPortal.UnitTests.Controllers
{
    public class JobControllerTests
    {
        private readonly Mock<IJobRepository> _mockJobRepository = new();
        private readonly Mock<IJobApplicationRepository> _mockApplicationRepository = new();
        private readonly Mock<IWebHostEnvironment> _mockEnvironment = new();
        private readonly UserManager<ApplicationUser> _mockUserManager;
        private readonly JobController _controller;

        public JobControllerTests()
        {
            // UserManager has a complex constructor — mocking it needs a
            // fake IUserStore underneath. This is boilerplate specific to
            // testing anything that depends on ASP.NET Core Identity.
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new UserManager<ApplicationUser>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            _controller = new JobController(
                _mockJobRepository.Object,
                _mockApplicationRepository.Object,
                _mockUserManager,
                _mockEnvironment.Object,
                NullLogger<JobController>.Instance);
        }

        [Fact]
        public async Task Create_WithInvalidModel_ReturnsViewWithoutCallingRepository()
        {
            // Arrange — simulate a validation failure (e.g. missing Title).
            _controller.ModelState.AddModelError("Title", "Title is required");
            var model = new JobCreateViewModel();

            // Act
            var result = await _controller.Create(model, CancellationToken.None);

            // Assert — the controller must bail out BEFORE touching the
            // repository at all when the model is invalid.
            result.Should().BeOfType<ViewResult>();
            _mockJobRepository.Verify(
                r => r.CreateAsync(It.IsAny<JobPosting>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Details_JobDoesNotExist_ReturnsNotFound()
        {
            _mockJobRepository
                .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((JobPosting?)null);

            var result = await _controller.Details(999, CancellationToken.None);

            result.Should().BeOfType<NotFoundResult>();
        }


        [Fact]
        public async Task Edit_JobBelongsToAnotherEmployer_ReturnsForbidden()
        {
            // Arrange
            var claims = new[]
            {
        new Claim(ClaimTypes.NameIdentifier, "employer-1")
    };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(claims, "TestAuthentication"))
                }
            };

            var job = new JobPosting
            {
                Id = 10,
                Title = "Software Developer",
                EmployerId = "employer-2",
                IsActive = true
            };

            _mockJobRepository
                .Setup(r => r.GetByIdAsync(
                    10,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(job);

            // Act
            var result = await _controller.Edit(
                10,
                CancellationToken.None);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }



        [Fact]
        public async Task Edit_JobDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            _mockJobRepository
                .Setup(r => r.GetByIdAsync(
                    999,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((JobPosting?)null);

            // Act
            var result = await _controller.Edit(
                999,
                CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }


        [Fact]
        public async Task Details_JobIsInactive_ReturnsNotFound()
        {
            // A job that exists but has been deactivated (IsActive = false)
            // should behave identically to a missing job from the
            // candidate's perspective — it must not be viewable.
            _mockJobRepository
                .Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JobPosting { Id = 5, IsActive = false, Title = "Closed Job" });

            var result = await _controller.Details(5, CancellationToken.None);

            result.Should().BeOfType<NotFoundResult>();
        }
    }
}