
using FluentAssertions;
using JobPortal.Domain.Entities;
using JobPortal.Application.Interfaces;
using JobPortal.Web.Controllers;
using JobPortal.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace JobPortal.UnitTests.Controllers
{
    public class AccountControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<SignInManager<ApplicationUser>> _mockSignInManager;
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            // Mock UserManager
            var userStore = new Mock<IUserStore<ApplicationUser>>();

            _mockUserManager = new Mock<UserManager<ApplicationUser>>(
                userStore.Object,
                null!, null!, null!, null!,
                null!, null!, null!, null!);

            // Mock SignInManager
            var contextAccessor = new Mock<IHttpContextAccessor>();
            contextAccessor.Setup(x => x.HttpContext)
                .Returns(new DefaultHttpContext());

            var claimsFactory =
                new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

            var schemeProvider =
                new Mock<IAuthenticationSchemeProvider>();

            var userConfirmation =
                new Mock<IUserConfirmation<ApplicationUser>>();

            _mockSignInManager = new Mock<SignInManager<ApplicationUser>>(
                _mockUserManager.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                Options.Create(new IdentityOptions()),
                NullLogger<SignInManager<ApplicationUser>>.Instance,
                schemeProvider.Object,
                userConfirmation.Object);

            // Create controller with mocked dependencies
            var mockJobRepository = new Mock<IJobRepository>();
            var mockJobApplicationRepository = new Mock<IJobApplicationRepository>();

            _controller = new AccountController(
                _mockUserManager.Object,
                _mockSignInManager.Object,
                mockJobRepository.Object,
                mockJobApplicationRepository.Object,
                NullLogger<AccountController>.Instance);

            // Mock URL helper for local redirects
            var urlHelper = new Mock<IUrlHelper>();

            urlHelper.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
                .Returns((string url) =>
                    url.StartsWith("/") &&
                    !url.StartsWith("//") &&
                    !url.StartsWith("/\\"));

            _controller.Url = urlHelper.Object;
        }

        // TEST 1: Register GET
        [Fact]
        public void Register_Get_ReturnsView()
        {
            var result = _controller.Register();

            var view = result.Should()
                .BeOfType<ViewResult>().Subject;

            view.Model.Should()
                .BeOfType<RegisterViewModel>();
        }

        // TEST 2: Invalid registration
        [Fact]
        public async Task Register_InvalidModel_ReturnsView()
        {
            _controller.ModelState.AddModelError(
                "Email", "Email is required");

            var model = new RegisterViewModel();

            var result = await _controller.Register(model);

            result.Should().BeOfType<ViewResult>();

            _mockUserManager.Verify(x =>
                x.CreateAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        // TEST 3: Successful candidate registration
        [Fact]
        public async Task Register_ValidCandidate_RedirectsToJobIndex()
        {
            var model = new RegisterViewModel
            {
                FullName = "Test Candidate",
                Email = "candidate@test.com",
                Password = "Test@12345",
                ConfirmPassword = "Test@12345",
                Role = "Candidate"
            };

            _mockUserManager
                .Setup(x => x.CreateAsync(
                    It.IsAny<ApplicationUser>(),
                    model.Password))
                .ReturnsAsync(IdentityResult.Success);

            _mockUserManager
                .Setup(x => x.AddToRoleAsync(
                    It.IsAny<ApplicationUser>(),
                    "Candidate"))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _controller.Register(model);

            var redirect = result.Should()
                .BeOfType<RedirectToActionResult>().Subject;

            redirect.ActionName.Should().Be("Index");
            redirect.ControllerName.Should().Be("Job");

            _mockUserManager.Verify(x =>
                x.CreateAsync(
                    It.Is<ApplicationUser>(u =>
                        u.Email == model.Email &&
                        u.FullName == model.FullName &&
                        u.CompanyName == null),
                    model.Password),
                Times.Once);

            _mockSignInManager.Verify(x =>
                x.SignInAsync(
                    It.IsAny<ApplicationUser>(),
                    false,
                    null),
                Times.Once);
        }

        // TEST 4: Employer without company name
        [Fact]
        public async Task Register_EmployerWithoutCompany_ReturnsView()
        {
            var model = new RegisterViewModel
            {
                FullName = "Test Employer",
                Email = "employer@test.com",
                Password = "Test@12345",
                ConfirmPassword = "Test@12345",
                Role = "Employer",
                CompanyName = ""
            };

            var result = await _controller.Register(model);

            result.Should().BeOfType<ViewResult>();

            _controller.ModelState.IsValid.Should().BeFalse();

            _mockUserManager.Verify(x =>
                x.CreateAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>()),
                Times.Never);
        }

        // TEST 5: Duplicate registration / Identity failure
        [Fact]
        public async Task Register_CreateFails_ReturnsViewWithError()
        {
            var model = new RegisterViewModel
            {
                FullName = "Test User",
                Email = "existing@test.com",
                Password = "Test@12345",
                Role = "Candidate"
            };

            var error = new IdentityError
            {
                Code = "DuplicateEmail",
                Description = "Email already exists"
            };

            _mockUserManager
                .Setup(x => x.CreateAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(error));

            var result = await _controller.Register(model);

            result.Should().BeOfType<ViewResult>();
            _controller.ModelState.IsValid.Should().BeFalse();

            _mockSignInManager.Verify(x =>
                x.SignInAsync(
                    It.IsAny<ApplicationUser>(),
                    It.IsAny<bool>(),
                    It.IsAny<string?>()),
                Times.Never);
        }

        // TEST 6: Login GET
        [Fact]
        public void Login_Get_ReturnsView()
        {
            var result = _controller.Login("/Job/Index");

            var view = result.Should()
                .BeOfType<ViewResult>().Subject;

            var model = view.Model.Should()
                .BeOfType<LoginViewModel>().Subject;

            model.ReturnUrl.Should().Be("/Job/Index");
        }

        // TEST 7: Invalid login model
        [Fact]
        public async Task Login_InvalidModel_ReturnsView()
        {
            _controller.ModelState.AddModelError(
                "Email", "Email required");

            var model = new LoginViewModel();

            var result = await _controller.Login(model);

            result.Should().BeOfType<ViewResult>();

            _mockSignInManager.Verify(x =>
                x.PasswordSignInAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        // TEST 8: Successful login
        [Fact]
        public async Task Login_ValidCredentials_Redirects()
        {
            var model = new LoginViewModel
            {
                Email = "candidate@test.com",
                Password = "Test@12345",
                RememberMe = false
            };

            _mockSignInManager
                .Setup(x => x.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    false,
                    false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            var result = await _controller.Login(model);

            var redirect = result.Should()
                .BeOfType<RedirectToActionResult>().Subject;

            redirect.ActionName.Should().Be("Index");
            redirect.ControllerName.Should().Be("Job");
        }

        // TEST 9: Incorrect password
        [Fact]
        public async Task Login_InvalidCredentials_ReturnsError()
        {
            var model = new LoginViewModel
            {
                Email = "candidate@test.com",
                Password = "WrongPassword"
            };

            _mockSignInManager
                .Setup(x => x.PasswordSignInAsync(
                    model.Email,
                    model.Password,
                    false,
                    false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            var result = await _controller.Login(model);

            result.Should().BeOfType<ViewResult>();

            _controller.ModelState.IsValid.Should().BeFalse();
        }

        // TEST 10: Successful logout
        [Fact]
        public async Task Logout_SignsOutAndRedirects()
        {
            _mockSignInManager
                .Setup(x => x.SignOutAsync())
                .Returns(Task.CompletedTask);

            var result = await _controller.Logout();

            var redirect = result.Should()
                .BeOfType<RedirectToActionResult>().Subject;

            redirect.ActionName.Should().Be("Index");
            redirect.ControllerName.Should().Be("Job");

            _mockSignInManager.Verify(x =>
                x.SignOutAsync(), Times.Once);
        }

        // TEST 11: Access denied page
        [Fact]
        public void AccessDenied_ReturnsView()
        {
            var result = _controller.AccessDenied();

            result.Should().BeOfType<ViewResult>();
        }
    }
}
