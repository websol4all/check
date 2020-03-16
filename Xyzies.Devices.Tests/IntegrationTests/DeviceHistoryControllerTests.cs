using AutoFixture;
using FluentAssertions;
using IdentityServiceClient;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Services.Models.DeviceHistory;
using Xyzies.Devices.Tests.IntegrationTests.Services;
using Xyzies.Devices.Tests.Models.User;

namespace Xyzies.Devices.Tests.IntegrationTests
{
    public class DeviceHistoryControllerTests : IClassFixture<BaseIntegrationTest>
    {
        private readonly BaseIntegrationTest _baseTest = null;

        public DeviceHistoryControllerTests(BaseIntegrationTest baseTest)
        {
            _baseTest = baseTest ?? throw new ArgumentNullException(nameof(baseTest));
            _baseTest.DbContext.ClearContext();
        }

        #region Get history by device id

        [Fact]
        public async Task ShouldReturUnauthorizedIFUserHasNotTokenWhenGetHistory()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            string uriGetComments = $"devices/{deviceId}/history";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = null;
            var response = await _baseTest.HttpClient.GetAsync(uriGetComments);

            response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Fact]
        public async Task ShouldReturForbidResultIFUserHasNotAccessWhenGetHistory()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            string uriGetComments = $"devices/{deviceId}/history";

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.Manager.Email, Password = ConstTest.Password, Scope = ConstTest.DefaultScopeHasNotAccess };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uriGetComments);

            response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task ShouldReturForbidResultIFDeviceNotExistWhenGetHistory()
        {
            // Arrange
            var deviceId = Guid.NewGuid();
            string uriGetComments = $"devices/{deviceId}/history";

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.History.Read };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uriGetComments);

            response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(3, 3)]
        public async Task ShouldReturnDeviceHistoryWithLazyLoadingWhenGetHistory(int skip, int take)
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Comment.Read };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            int historyCount = 10;
            var device = _baseTest.Fixture.Build<Device>().With(x=>x.Id, Guid.NewGuid()).Without(x => x.DeviceHistory).Create();
            var historyMockList = _baseTest.Fixture.Build<DeviceHistory>()
                                          .With(x => x.DeviceId, device.Id)
                                          .With(x => x.LoggedInUserId, _baseTest.Supervisor.Id)
                                          .Without(x => x.Device)
                                          .CreateMany(historyCount);

            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.DeviceHistory.AddRange(historyMockList);
            _baseTest.DbContext.SaveChanges();

            string uriGetComments = $"devices/{device.Id}/history?{nameof(LazyLoadParameters.Offset)}={skip}&{nameof(LazyLoadParameters.Limit)}={take}";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uriGetComments);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var comments = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceHistoryModel>>(responseString);

            //Assert
            comments.Offset.Should().Be(skip);
            comments.Limit.Should().Be(take);
            comments.Total.Should().Be(historyCount);
            comments.Result.Count().Should().Be(_baseTest.DbContext.DeviceHistory.Skip(skip).Take(take).Count());
        }


        [Theory]
        [InlineData(ConstTest.Role.AccountAdmin)]
        [InlineData(ConstTest.Role.OperationAdmin)]
        [InlineData(ConstTest.Role.SystemAdmin)]
        public async Task ShouldReturAllHistoryWhenGetHistory(string roleName)
        {
            // Arrange
            var user = _baseTest.Users[roleName];
            var userLoginOptiomTest = new UserLoginOption { UserName = user.Email, Password = ConstTest.Password, Scope = Const.Permissions.Comment.Read };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            int historyCount = 10;
            var device = _baseTest.Fixture.Build<Device>().With(x => x.Id, Guid.NewGuid()).Without(x => x.DeviceHistory).Create();
            var historyMockList = _baseTest.Fixture.Build<DeviceHistory>()
                                         .With(x => x.DeviceId, device.Id)
                                         .With(x => x.LoggedInUserId, _baseTest.Supervisor.Id)
                                         .Without(x => x.Device)
                                         .CreateMany(historyCount);

            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.DeviceHistory.AddRange(historyMockList);
            _baseTest.DbContext.SaveChanges();

            string uriGetComments = $"devices/{device.Id}/history";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uriGetComments);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var comments = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceHistoryModel>>(responseString);

            //Assert
            comments.Total.Should().Be(historyCount);
            comments.Result.Count().Should().Be(_baseTest.DbContext.DeviceHistory.Count());
            comments.Result.Should().BeInDescendingOrder(x => x.CreatedOn);
        }

        [Fact]
        public async Task ShouldReturHistoryForSupervisorWhenGetHistory()
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.Supervisor.Email, Password = ConstTest.Password, Scope = Const.Permissions.Comment.Read };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            int historyCountWithUserCompanyId = 8;
            int historyCountWithOtherCompanyId = 5;

            var device = _baseTest.Fixture.Build<Device>()
                                           .With(x => x.Id, Guid.NewGuid())
                                           .With(x=>x.CompanyId, _baseTest.Supervisor.CompanyId)
                                           .Without(x => x.DeviceHistory).Create();
            var historyMockList = _baseTest.Fixture.Build<DeviceHistory>()
                                         .With(x => x.DeviceId, device.Id)
                                         .With(x => x.LoggedInUserId, _baseTest.OperationAdmin.Id)
                                         .With(x => x.CompanyId, _baseTest.Supervisor.CompanyId)
                                         .Without(x => x.Device)
                                         .CreateMany(historyCountWithUserCompanyId).ToList();
            historyMockList.AddRange(_baseTest.Fixture.Build<DeviceHistory>()
                                         .With(x => x.DeviceId, device.Id)
                                         .With(x => x.LoggedInUserId, _baseTest.OperationAdmin.Id)
                                         .With(x => x.CompanyId, _baseTest.Supervisor.CompanyId + 1)
                                         .Without(x => x.Device)
                                         .CreateMany(historyCountWithOtherCompanyId));

            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.DeviceHistory.AddRange(historyMockList);
            _baseTest.DbContext.SaveChanges();

            string uriGetComments = $"devices/{device.Id}/history";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uriGetComments);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var comments = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceHistoryModel>>(responseString);

            //Assert
            comments.Total.Should().Be(historyCountWithUserCompanyId);
            comments.Result.Count().Should().Be(historyCountWithUserCompanyId);
            comments.Result.Should().BeInDescendingOrder(x => x.CreatedOn);
        }

        [Fact]
        public async Task ShouldReturForbidIfSupervisorHasAnotherCompanyWhenGetHistory()
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.Supervisor.Email, Password = ConstTest.Password, Scope = Const.Permissions.Comment.Read };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            int historyCount = 5;
            int anotherCompanyId = _baseTest.Supervisor.CompanyId.Value + 1;

            var device = _baseTest.Fixture.Build<Device>()
                                           .With(x => x.CompanyId, anotherCompanyId)
                                           .Without(x => x.DeviceHistory).Create();
           
            var historyMockList = _baseTest.Fixture.Build<DeviceHistory>()
                                         .With(x => x.DeviceId, device.Id)
                                         .With(x => x.LoggedInUserId, _baseTest.OperationAdmin.Id)
                                         .With(x => x.CompanyId, _baseTest.Supervisor.CompanyId)
                                         .Without(x => x.Device)
                                         .CreateMany(historyCount);

            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.DeviceHistory.AddRange(historyMockList);
            _baseTest.DbContext.SaveChanges();

            string uriGetComments = $"devices/{device.Id}/history";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uriGetComments);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        #endregion
    }
}
