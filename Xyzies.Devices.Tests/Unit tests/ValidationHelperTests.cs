using AutoFixture;
using FluentAssertions;
using IdentityServiceClient.Service;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using Xyzies.Devices.Services.Helpers;
using Xyzies.Devices.Services.Helpers.Interfaces;
using Xyzies.Devices.Services.Models.Branch;
using Xyzies.Devices.Services.Models.Company;
using Xyzies.Devices.Services.Models.User;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Tests.Unit_tests
{
    public class ValidationHelperTests : IClassFixture<BaseTest>
    {
        private readonly BaseTest _baseTest = null;

        private ILogger<ValidationHelper> _loggerMock;
        private Mock<IHttpService> _httpServiceMock;
        private Mock<IIdentityManager> _identityManagerMock;

        private readonly IValidationHelper _validationHelper = null;

        public ValidationHelperTests(BaseTest baseTest)
        {
            _baseTest = baseTest ?? throw new ArgumentNullException(nameof(baseTest));
            _baseTest.DbContext.ClearContext();

            _loggerMock = Mock.Of<ILogger<ValidationHelper>>();
            _httpServiceMock = new Mock<IHttpService>();
            _identityManagerMock = new Mock<IIdentityManager>();
            _validationHelper = new ValidationHelper(_loggerMock, _httpServiceMock.Object, _identityManagerMock.Object);
        }

        [Fact]
        public async Task ShouldReturnFailIfCompanyNotExistWhenValidateCompanyAndBranch()
        {
            // Arrange
            int companyId = 5;
            string token = _baseTest.Fixture.Create<string>();
            Guid branchId = Guid.Parse("596e029a-7eb7-44b5-a724-5099ead0f70a");

            _httpServiceMock.Setup(x => x.GetCompanyById(companyId, token)).ReturnsAsync((CompanyModel)null);
            // Act
            Func<Task> result = async () => await _validationHelper.ValidateCompanyAndBranch(companyId, branchId, token);

            //Assert
            await result.Should().ThrowAsync<ApplicationException>();
        }

        [Fact]
        public async Task ShouldReturnFailIfBranchNotExistWhenValidateCompanyAndBranch()
        {
            // Arrange
            int companyId = 5;
            string token = _baseTest.Fixture.Create<string>();
            Guid branchId = Guid.Parse("596e029a-7eb7-44b5-a724-5099ead0f70a");
            var companyModel = _baseTest.Fixture.Create<CompanyModel>();

            _httpServiceMock.Setup(x => x.GetCompanyById(companyId, token)).ReturnsAsync(companyModel);
            _httpServiceMock.Setup(x => x.GetBranchById(branchId, token)).ReturnsAsync((BranchModel)null);

            // Act
            Func<Task> result = async () => await _validationHelper.ValidateCompanyAndBranch(companyId, branchId, token);

            //Assert
            await result.Should().ThrowAsync<ApplicationException>();
        }

        [Fact]
        public async Task ShouldReturnFailIfBranchHasNotCurrentCompanyWhenValidateCompanyAndBranch()
        {
            // Arrange
            int companyId = 5;
            string token = _baseTest.Fixture.Create<string>();
            Guid branchId = Guid.Parse("596e029a-7eb7-44b5-a724-5099ead0f70a");
            var companyModel = _baseTest.Fixture.Build<CompanyModel>().With(x => x.Id, companyId).Create();
            var branchModel = _baseTest.Fixture.Build<BranchModel>().With(x => x.CompanyId, 6).Create();

            _httpServiceMock.Setup(x => x.GetCompanyById(companyId, token)).ReturnsAsync(companyModel);
            _httpServiceMock.Setup(x => x.GetBranchById(branchId, token)).ReturnsAsync(branchModel);

            // Act
            Func<Task> result = async () => await _validationHelper.ValidateCompanyAndBranch(companyId, branchId, token);

            //Assert
            await result.Should().ThrowAsync<ApplicationException>();
        }

        [Fact]
        public async Task ShouldReturnSuccessWhenGetCompanyIdByPermissionAsSuperviser()
        {
            // Arrange
            int companyId = 5;
            int userCompanyId = 6;
            string token = _baseTest.Fixture.Create<string>();
            string[] superviserScopes = _baseTest.Fixture.Create<string[]>();
            var userModel = _baseTest.Fixture.Build<UserModel>().With(x => x.CompanyId, userCompanyId).Create();

            _httpServiceMock.Setup(x => x.GetCurrentUser(token)).ReturnsAsync(userModel);
            _identityManagerMock.Setup(x => x.HasAccess(token, superviserScopes)).ReturnsAsync(false);

            // Act
            var correctCompanyId = await _validationHelper.GetCompanyIdByPermission(token, superviserScopes, companyId);

            //Assert
            correctCompanyId.Should().Be(userCompanyId);
        }

        [Fact]
        public async Task ShouldReturnSuccessWhenGetCompanyIdByPermissionAsAdmin()
        {
            //// Arrange
            int companyId = 5;
            string token = _baseTest.Fixture.Create<string>();
            string[] adminScopes = _baseTest.Fixture.Create<string[]>();
            var userModel = _baseTest.Fixture.Build<UserModel>().With(x => x.CompanyId, 0).With(x => x.Scopes, adminScopes).Create();

            _httpServiceMock.Setup(x => x.GetCurrentUser(token)).ReturnsAsync(userModel);

            // Act
            var correctCompanyId = await _validationHelper.GetCompanyIdByPermission(token, adminScopes, companyId);

            //Assert
            correctCompanyId.Should().Be(companyId);
        }

    }
}
