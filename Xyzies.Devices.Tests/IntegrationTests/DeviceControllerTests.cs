using FluentAssertions;
using AutoFixture;
using IdentityServiceClient;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xyzies.Devices.API.Models;
using Xyzies.Devices.Tests.Models.User;
using Xyzies.Devices.Services.Requests.Device;
using System.Net.Http;
using Newtonsoft.Json;
using Xyzies.Devices.Data.Entity;
using System.Linq;
using Xyzies.Devices.Services.Models.DeviceModels;
using Xyzies.Devices.Services.Models;
using Xyzies.Devices.Data.Common;
using Xyzies.TWC.DisputeService.Tests.Extensions;
using Xyzies.Devices.Services.Models.Tenant;

namespace Xyzies.Devices.Tests.IntegrationTests
{
    public class DeviceControllerTests : IClassFixture<BaseIntegrationTest>
    {
        private readonly BaseIntegrationTest _baseTest = null;
        private readonly string _baseDeviceUrl = "devices";

        public DeviceControllerTests(BaseIntegrationTest baseTest)
        {
            _baseTest = baseTest ?? throw new ArgumentNullException(nameof(baseTest));
            _baseTest.DbContext.ClearContext();
        }

        #region all api in this controller

        [Theory]
        [InlineData("Post", "")]
        [InlineData("Put", "/209c2193-f52b-4fb9-833c-fd1c5051b6c5")]
        [InlineData("Get", "/209c2193-f52b-4fb9-833c-fd1c5051b6c5")]
        [InlineData("Delete", "/209c2193-f52b-4fb9-833c-fd1c5051b6c5")]
        [InlineData("Post", "/setup")]
        [InlineData("Get", "")]
        public async Task ShouldReturUnauthorizedIFUserHasNotToken(string httpMethodName, string url)
        {
            // Arrange
            HttpMethod httpMethod = new HttpMethod(httpMethodName);
            HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, $"{_baseTest.HttpClient.BaseAddress}{_baseDeviceUrl}{url}");

            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = null;
            var response = await _baseTest.HttpClient.SendAsync(requestMessage);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        }

        [Theory]
        [InlineData("Get", "/209c2193-f52b-4fb9-833c-fd1c5051b6c5")]
        [InlineData("Delete", "/209c2193-f52b-4fb9-833c-fd1c5051b6c5")]
        [InlineData("Get", "")]
        public async Task ShouldReturForbidResultIFUserHasNotAccess(string httpMethodName, string url, string mediaType = null)
        {
            // Arrange
            HttpMethod httpMethod = new HttpMethod(httpMethodName);
            HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, $"{_baseTest.HttpClient.BaseAddress}{_baseDeviceUrl}{url}");

            if (!string.IsNullOrWhiteSpace(mediaType))
            {
                requestMessage.Content = new StringContent(string.Empty, Encoding.UTF8, mediaType);
            }

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.Manager.Email, Password = ConstTest.Password, Scope = ConstTest.DefaultScopeHasNotAccess };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.SendAsync(requestMessage);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task ShouldReturForbidResultIFUserHasNotAccessWhenPostDevice()
        {
            // Arrange
            var request = _baseTest.Fixture.Build<CreateDeviceRequest>().With(x => x.Phone, "38099-777-77-77").Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.Manager.Email, Password = ConstTest.Password, Scope = ConstTest.DefaultScopeHasNotAccess };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PostAsync(_baseDeviceUrl, content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task ShouldReturForbidResultIFUserHasNotAccessWhenPutDevice()
        {
            // Arrange
            var request = _baseTest.Fixture.Build<BaseDeviceRequest>().With(x => x.Phone, "38099-777-77-77").Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.Manager.Email, Password = ConstTest.Password, Scope = ConstTest.DefaultScopeHasNotAccess };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var deviceId = Guid.NewGuid();
            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PutAsync($"{_baseDeviceUrl}/{deviceId}", content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        #endregion

        #region Post new device

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public async Task ShouldReturBadRequestResultIFDeviceUdidNotSendWhenPostDevice(string udid)
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);
            var request = _baseTest.Fixture.Build<CreateDeviceRequest>()
                                           .With(x => x.Udid, udid)
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PostAsync(_baseDeviceUrl, content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(-1, 0)]
        [InlineData(-1, -1)]
        public async Task ShouldReturBadRequestResultIFRadiusOrCompanyIdHasNegativeValueSendWhenPostDevice(int companyId, double radius)
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);
            var request = _baseTest.Fixture.Build<CreateDeviceRequest>()
                                           .With(x => x.CompanyId, companyId)
                                           .With(x => x.Radius, radius)
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PostAsync(_baseDeviceUrl, content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ShouldReturBadRequestResultIFDeviceAlreadyExistSendWhenPostDevice()
        {
            // Arrange
            var device = _baseTest.Fixture.Build<Device>()
                                          .Without(x => x.DeviceHistory)
                                          .Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);
            var request = _baseTest.Fixture.Build<CreateDeviceRequest>()
                                           .With(x => x.Udid, device.Udid)
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PostAsync(_baseDeviceUrl, content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ShouldReturBadRequestResultIFCompanyNotExistSendWhenPostDevice()
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);
            var request = _baseTest.Fixture.Build<CreateDeviceRequest>()
                                           .With(x => x.CompanyId, 0)
                                           .With(x => x.BranchId, _baseTest.Branch.Id)
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PostAsync(_baseDeviceUrl, content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ShouldReturBadRequestResultIFBranchNotExistSendWhenPostDevice()
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);
            var request = _baseTest.Fixture.Build<CreateDeviceRequest>()
                                           .With(x => x.CompanyId, _baseTest.Company.Id)
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PostAsync(_baseDeviceUrl, content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ShouldReturBadRequestResultIFBranchHasNotCompanySendWhenPostDevice()
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var request = _baseTest.Fixture.Build<CreateDeviceRequest>()
                                           .With(x => x.CompanyId, _baseTest.CompanyWithoutAnyBranch.Id)
                                           .With(x => x.BranchId, _baseTest.Branch.Id)
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PostAsync(_baseDeviceUrl, content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ShouldReturBadRequestResultForSupervisorHasAnotherCompanyWithoutBrachWhenPostDevice()
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.SupervisorWithCompanyWithoutBranch.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var request = _baseTest.Fixture.Build<CreateDeviceRequest>()
                                           .With(x => x.CompanyId, _baseTest.Company.Id)
                                           .With(x => x.BranchId, _baseTest.Branch.Id)
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PostAsync(_baseDeviceUrl, content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }


        [Theory]
        [InlineData(ConstTest.Role.AccountAdmin)]
        [InlineData(ConstTest.Role.OperationAdmin)]
        [InlineData(ConstTest.Role.SystemAdmin)]
        public async Task ShouldReturSuccessResultForAdminWhenPostDevice(string roleName)
        {
            // Arrange
            var user = _baseTest.Users[roleName];
            var userLoginOptiomTest = new UserLoginOption { UserName = user.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var request = _baseTest.Fixture.Build<CreateDeviceRequest>()
                                           .With(x => x.CompanyId, _baseTest.Company.Id)
                                           .With(x => x.BranchId, _baseTest.Branch.Id)
                                           .With(x => x.Phone, "38099-777-77-77")
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PostAsync(_baseDeviceUrl, content);
            response.EnsureSuccessStatusCode();

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status201Created);

            var device = _baseTest.DbContext.Devices.First();
            device.Udid.Should().Be(request.Udid);
            device.HexnodeUdid.Should().Be(request.HexnodeUdid);
            device.IsDeleted.Should().BeFalse();
            device.IsPending.Should().BeFalse();
            device.Longitude.Should().Be(request.Longitude);
            device.Latitude.Should().Be(request.Latitude);
            device.Radius.Should().Be(request.Radius);
            device.BranchId.Should().Be(request.BranchId);
            device.CompanyId.Should().Be(request.CompanyId);
        }

        [Fact]
        public async Task ShouldReturSuccessResultForSupervisorWhenPostDevice()
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.Supervisor.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var request = _baseTest.Fixture.Build<CreateDeviceRequest>()
                                           .With(x => x.CompanyId, _baseTest.SupervisorWithCompanyWithoutBranch.CompanyId)
                                           .With(x => x.BranchId, _baseTest.Branch.Id)
                                           .With(x => x.Phone, "38099-777-77-77")
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PostAsync(_baseDeviceUrl, content);
            response.EnsureSuccessStatusCode();

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status201Created);

            var device = _baseTest.DbContext.Devices.First();
            device.Udid.Should().Be(request.Udid);
            device.HexnodeUdid.Should().Be(request.HexnodeUdid);
            device.IsDeleted.Should().BeFalse();
            device.IsPending.Should().BeFalse();
            device.Longitude.Should().Be(request.Longitude);
            device.Latitude.Should().Be(request.Latitude);
            device.Radius.Should().Be(request.Radius);
            device.BranchId.Should().Be(request.BranchId);
            device.CompanyId.Should().Be(_baseTest.Supervisor.CompanyId);
        }

        #endregion

        #region Put device

        [Theory]
        [InlineData(0, -1)]
        [InlineData(-1, 0)]
        [InlineData(-1, -1)]
        public async Task ShouldReturBadRequestResultIFRadiusOrCompanyIdHasNegativeValueSendWhenPutDevice(int companyId, double radius)
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);
            var request = _baseTest.Fixture.Build<BaseDeviceRequest>()
                                           .With(x => x.CompanyId, companyId)
                                           .With(x => x.Radius, radius)
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var deviceId = Guid.NewGuid();

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PutAsync($"{_baseDeviceUrl}/{deviceId}", content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ShouldReturBadRequestResultIFCompanyNotExistSendWhenPutDevice()
        {
            // Arrange
            var device = _baseTest.Fixture.Build<Device>().Without(x => x.DeviceHistory).Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);
            var request = _baseTest.Fixture.Build<BaseDeviceRequest>()
                                           .With(x => x.CompanyId, 0)
                                           .With(x => x.BranchId, _baseTest.Branch.Id)
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PutAsync($"{_baseDeviceUrl}/{device.Id}", content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ShouldReturBadRequestResultIFBranchNotExistSendWhenPutDevice()
        {
            var device = _baseTest.Fixture.Build<Device>().Without(x => x.DeviceHistory).Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);
            var request = _baseTest.Fixture.Build<CreateDeviceRequest>()
                                           .With(x => x.CompanyId, _baseTest.Company.Id)
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PutAsync($"{_baseDeviceUrl}/{device.Id}", content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ShouldReturBadRequestResultIFBranchHasNotCompanySendWhenPutDevice()
        {
            // Arrange
            var device = _baseTest.Fixture.Build<Device>().Without(x => x.DeviceHistory).Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var request = _baseTest.Fixture.Build<BaseDeviceRequest>()
                                           .With(x => x.CompanyId, _baseTest.CompanyWithoutAnyBranch.Id)
                                           .With(x => x.BranchId, _baseTest.Branch.Id)
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PutAsync($"{_baseDeviceUrl}/{device.Id}", content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ShouldReturNotFoundResultIFDeviceNotExistSendWhenPutDevice()
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var deviceId = Guid.NewGuid();

            var request = _baseTest.Fixture.Build<BaseDeviceRequest>()
                                           .With(x => x.CompanyId, _baseTest.Company.Id)
                                           .With(x => x.BranchId, _baseTest.Branch.Id)
                                           .With(x => x.Phone, "38099-777-77-77")
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PutAsync($"{_baseDeviceUrl}/{deviceId}", content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task ShouldReturBadRequestResultForSupervisorHasAnotherCompanyWithoutBrachWhenPutDevice()
        {
            // Arrange
            var device = _baseTest.Fixture.Build<Device>().Without(x => x.DeviceHistory).Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.SupervisorWithCompanyWithoutBranch.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var request = _baseTest.Fixture.Build<BaseDeviceRequest>()
                                           .With(x => x.CompanyId, _baseTest.Company.Id)
                                           .With(x => x.BranchId, _baseTest.Branch.Id)
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PutAsync($"{_baseDeviceUrl}/{device.Id}", content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Theory]
        [InlineData(ConstTest.Role.AccountAdmin)]
        [InlineData(ConstTest.Role.OperationAdmin)]
        [InlineData(ConstTest.Role.SystemAdmin)]
        public async Task ShouldReturSuccessResultForAdminWhenPutDevice(string roleName)
        {
            // Arrange
            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var device = _baseTest.Fixture.Build<Device>()
                                          .Without(x => x.DeviceHistory)
                                          .Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            var user = _baseTest.Users[roleName];
            var userLoginOptiomTest = new UserLoginOption { UserName = user.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Create };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var request = _baseTest.Fixture.Build<BaseDeviceRequest>()
                                           .With(x => x.CompanyId, _baseTest.Company.Id)
                                           .With(x => x.BranchId, _baseTest.Branch.Id)
                                           .With(x => x.Phone, "38099-777-77-77")
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PutAsync($"{_baseDeviceUrl}/{device.Id}", content);
            response.EnsureSuccessStatusCode();

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status204NoContent);

            await _baseTest.DbContext.Entry(device).ReloadAsync();
            var deviceFromDb = _baseTest.DbContext.Devices.First(x => x.Id == device.Id);

            deviceFromDb.HexnodeUdid.Should().Be(request.HexnodeUdid);
            deviceFromDb.IsDeleted.Should().BeFalse();
            deviceFromDb.IsPending.Should().BeFalse();
            deviceFromDb.Longitude.Should().Be(request.Longitude);
            deviceFromDb.Latitude.Should().Be(request.Latitude);
            deviceFromDb.Radius.Should().Be(request.Radius);
            deviceFromDb.BranchId.Should().Be(request.BranchId);
            deviceFromDb.CompanyId.Should().Be(request.CompanyId);
        }

        [Fact]
        public async Task ShouldReturSuccessResultForSupervisorWhenPutDevice()
        {
            // Arrange
            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var device = _baseTest.Fixture.Build<Device>()
                                          .With(x => x.Id, Guid.NewGuid())
                                          .Without(x => x.DeviceHistory)
                                          .Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.Supervisor.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Update };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var request = _baseTest.Fixture.Build<BaseDeviceRequest>()
                                           .With(x => x.CompanyId, _baseTest.SupervisorWithCompanyWithoutBranch.CompanyId)
                                           .With(x => x.BranchId, _baseTest.Branch.Id)
                                           .With(x => x.Phone, "38099-777-77-77")
                                           .Create();
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.PutAsync($"{_baseDeviceUrl}/{device.Id}", content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
            response.EnsureSuccessStatusCode();

            await _baseTest.DbContext.Entry(device).ReloadAsync();
            var deviceFromDb = _baseTest.DbContext.Devices.First();

            deviceFromDb.HexnodeUdid.Should().Be(request.HexnodeUdid);
            deviceFromDb.IsDeleted.Should().BeFalse();
            deviceFromDb.IsPending.Should().BeFalse();
            deviceFromDb.Longitude.Should().Be(request.Longitude);
            deviceFromDb.Latitude.Should().Be(request.Latitude);
            deviceFromDb.Radius.Should().Be(request.Radius);
            deviceFromDb.BranchId.Should().Be(request.BranchId);
            deviceFromDb.CompanyId.Should().Be(_baseTest.Supervisor.CompanyId);
        }

        #endregion

        #region Delete device

        [Fact]
        public async Task ShouldReturNotFoundResultIFDeviceNotExistSendWhenDeleteDevice()
        {
            // Arrange
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            var deviceId = Guid.NewGuid();

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.DeleteAsync($"{_baseDeviceUrl}/{deviceId}");

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task ShouldRetuForbidResultIFSupervisorHasAnotherCompanyWhenDeleteDevice()
        {
            // Arrange
            var device = _baseTest.Fixture.Build<Device>()
                                          .With(x => x.Id, Guid.NewGuid())
                                          .With(x => x.CompanyId, _baseTest.Supervisor.CompanyId)
                                          .With(x => x.BranchId, _baseTest.Branch.Id)
                                          .Without(x => x.DeviceHistory)
                                          .Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.SupervisorWithCompanyWithoutBranch.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.DeleteAsync($"{_baseDeviceUrl}/{device.Id}");

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Theory]
        [InlineData(ConstTest.Role.AccountAdmin)]
        [InlineData(ConstTest.Role.OperationAdmin)]
        [InlineData(ConstTest.Role.SystemAdmin)]
        [InlineData(ConstTest.Role.Supervisor)]
        public async Task ShouldRetuSuccessResultWhenDeleteDevice(string roleName)
        {
            // Arrange
            var user = _baseTest.Users[roleName];
            var device = _baseTest.Fixture.Build<Device>()
                                          .With(x => x.Id, Guid.NewGuid())
                                          .With(x => x.CompanyId, _baseTest.Company.Id)
                                          .With(x => x.BranchId, _baseTest.Branch.Id)
                                          .Without(x => x.DeviceHistory)
                                          .Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = user.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.DeleteAsync($"{_baseDeviceUrl}/{device.Id}");
            response.EnsureSuccessStatusCode();

            //Assert
            await _baseTest.DbContext.Entry(device).ReloadAsync();
            response.StatusCode.Should().Be(StatusCodes.Status200OK);
            _baseTest.DbContext.Devices.Count().Should().Be(1);
            _baseTest.DbContext.Devices.First().IsDeleted.Should().BeTrue();
        }

        #endregion

        #region Get device by id

        [Fact]
        public async Task ShouldReturnForbidResultIFSupervisorHasAnotherCompanyWhenGetDeviceById()
        {
            // Arrange
            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var device = _baseTest.Fixture.Build<Device>()
                                          .With(x => x.Id, Guid.NewGuid())
                                          .With(x => x.CompanyId, _baseTest.Supervisor.CompanyId)
                                          .With(x => x.BranchId, _baseTest.Branch.Id)
                                          .Without(x => x.DeviceHistory)
                                          .Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.SupervisorWithCompanyWithoutBranch.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync($"{_baseDeviceUrl}/{device.Id}");

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Theory]
        [InlineData(ConstTest.Role.AccountAdmin)]
        [InlineData(ConstTest.Role.OperationAdmin)]
        [InlineData(ConstTest.Role.SystemAdmin)]
        [InlineData(ConstTest.Role.Supervisor)]
        public async Task ShouldReturnSuccessResultWhenGetDeviceById(string roleName)
        {
            // Arrange
            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var device = _baseTest.Fixture.Build<Device>()
                                          .With(x => x.CompanyId, _baseTest.Supervisor.CompanyId)
                                          .With(x => x.BranchId, _baseTest.Branch.Id)
                                          .Without(x => x.DeviceHistory)
                                          .Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            var user = _baseTest.Users[roleName];
            var userLoginOptiomTest = new UserLoginOption { UserName = user.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync($"{_baseDeviceUrl}/{device.Id}");

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var deviceModel = JsonConvert.DeserializeObject<DeviceModel>(responseString);

            //Assert
            deviceModel.Id.Should().Be(device.Id);
            deviceModel.Udid.Should().Be(device.Udid);
            deviceModel.HexnodeUdid.Should().Be(device.HexnodeUdid);
            deviceModel.Radius.Should().Be(device.Radius);
            deviceModel.Latitude.Should().Be(device.Latitude);
            deviceModel.Longitude.Should().Be(device.Longitude);
            deviceModel.Company.Id.Should().Be(device.CompanyId);
            deviceModel.Company.CompanyName.Should().Be(_baseTest.Company.CompanyName);
            deviceModel.Branch.Id.Should().Be(device.BranchId);
            deviceModel.Branch.BranchName.Should().Be(_baseTest.Branch.BranchName);
        }

        #endregion

        #region Get device list
        [Fact]
        public async Task ShouldReturnSuccessResultWithFilterByCompanyIdWhenGetDeviceList()
        {
            // Arrange
            int countDevicesWithTestCompany = 5;
            int expectedCount = 10;
            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var deviceList = _baseTest.Fixture.Build<Device>()
                                          .CreateMany(10).ToList();
            deviceList.AddRange(_baseTest.Fixture.Build<Device>()
                                          .With(x => x.CompanyId, _baseTest.CompanyWithoutAnyBranch.Id)
                                          .CreateMany(countDevicesWithTestCompany));
            deviceList.AddRange(_baseTest.Fixture.Build<Device>()
                                          .With(x => x.CompanyId, _baseTest.Company.Id)
                                          .CreateMany(countDevicesWithTestCompany));
            deviceList.ForEach(x =>
            {
                x.DeviceHistory = _baseTest.Fixture.CreateMany<DeviceHistory>().ToList();
            });
            _baseTest.DbContext.Devices.AddRange(deviceList);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            string uri = $"{_baseDeviceUrl}?{nameof(FilteringModel.CompanyIds)}[0]={_baseTest.CompanyWithoutAnyBranch.Id}&{nameof(FilteringModel.CompanyIds)}[1]={_baseTest.Company.Id}";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceModel>>(responseString);

            //Assert
            result.Total.Should().Be(expectedCount);
            result.Result.Count().Should().Be(expectedCount);
            var countDeviceWithFirstCompany = result.Result.Where(x => x.Company.Id == _baseTest.Company.Id).Count();
            var countDeviceWithSecondCompany = result.Result.Where(x => x.Company.Id == _baseTest.CompanyWithoutAnyBranch.Id).Count();

            countDeviceWithFirstCompany.Should().Be(countDevicesWithTestCompany);
            countDeviceWithSecondCompany.Should().Be(countDevicesWithTestCompany);
            (countDeviceWithFirstCompany + countDeviceWithSecondCompany).Should().Be(expectedCount);
        }

        [Fact]
        public async Task ShouldReturnSuccessResultWithFilterByCompanyIdForSupervisorWhenGetDeviceList()
        {
            // Arrange
            int countDevicesWithTestCompany = 5;
            int expectedCount = 5;
            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var deviceList = _baseTest.Fixture.Build<Device>()
                                          .CreateMany(10).ToList();
            deviceList.AddRange(_baseTest.Fixture.Build<Device>()
                                          .With(x => x.CompanyId, _baseTest.CompanyWithoutAnyBranch.Id)
                                          .CreateMany(countDevicesWithTestCompany));
            deviceList.AddRange(_baseTest.Fixture.Build<Device>()
                                          .With(x => x.CompanyId, _baseTest.Supervisor.CompanyId)
                                          .CreateMany(countDevicesWithTestCompany));
            deviceList.ForEach(x =>
            {
                x.DeviceHistory = _baseTest.Fixture.CreateMany<DeviceHistory>().ToList();
            });
            _baseTest.DbContext.Devices.AddRange(deviceList);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.Supervisor.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            string uri = $"{_baseDeviceUrl}?{nameof(FilteringModel.CompanyIds)}[0]={_baseTest.CompanyWithoutAnyBranch.Id}&{nameof(FilteringModel.CompanyIds)}[1]={_baseTest.Company.Id}";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceModel>>(responseString);

            //Assert
            result.Total.Should().Be(expectedCount);
            result.Result.Count().Should().Be(expectedCount);
            var countDeviceWithFirstCompany = result.Result.Where(x => x.Company.Id == _baseTest.Company.Id).Count();

            result.Result.Count().Should().Be(expectedCount);
            result.Result.All(x => x.Company.Id == _baseTest.Supervisor.CompanyId).Should().BeTrue();
        }

        [Fact]
        public async Task ShouldReturnSuccessResultWithFilterByBranchIdWhenGetDeviceList()
        {
            // Arrange
            int countDevicesWithTestBranch = 5;
            int expectedCount = 10;

            var testBranchId = Guid.NewGuid();
            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var deviceList = _baseTest.Fixture.Build<Device>()
                                          .CreateMany(10).ToList();
            deviceList.AddRange(_baseTest.Fixture.Build<Device>()
                                          .With(x => x.BranchId, _baseTest.Branch.Id)
                                          .CreateMany(countDevicesWithTestBranch));
            deviceList.AddRange(_baseTest.Fixture.Build<Device>()
                                          .With(x => x.BranchId, testBranchId)
                                          .CreateMany(countDevicesWithTestBranch));
            deviceList.ForEach(x =>
            {
                x.DeviceHistory = _baseTest.Fixture.CreateMany<DeviceHistory>().ToList();
            });
            _baseTest.DbContext.Devices.AddRange(deviceList);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            string uri = $"{_baseDeviceUrl}?{nameof(FilteringModel.BranchIds)}[0]={_baseTest.Branch.Id}&{nameof(FilteringModel.BranchIds)}[1]={testBranchId}";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceModel>>(responseString);

            //Assert
            result.Total.Should().Be(expectedCount);
            result.Result.Count().Should().Be(expectedCount);

            var countDeviceWithFirstBranch = result.Result.Where(x => x.Branch != null && x.Branch.Id == _baseTest.Branch.Id).Count();
            var countDeviceWithSecondBranch = result.Result.Where(x => x.Branch == null).Count();

            countDeviceWithFirstBranch.Should().Be(countDevicesWithTestBranch);
            countDeviceWithSecondBranch.Should().Be(countDevicesWithTestBranch);
            (countDeviceWithFirstBranch + countDeviceWithSecondBranch).Should().Be(expectedCount);
        }

        [Fact]
        public async Task ShouldReturnSuccessResultWithFilterByOnlineWhenGetDeviceList()
        {
            // Arrange
            int countDevicesOnline = 5;

            var deviceList = _baseTest.Fixture.Build<Device>()
                                          .Without(x => x.DeviceHistory)
                                          .CreateMany(10).ToList();
            deviceList.AddRange(_baseTest.Fixture.Build<Device>()
                                          .With(x => x.BranchId, _baseTest.Branch.Id)
                                          .Without(x => x.DeviceHistory)
                                          .CreateMany(countDevicesOnline));
            _baseTest.DbContext.Devices.AddRange(deviceList);
            _baseTest.DbContext.SaveChanges();

            var deviceHistoryList = deviceList.Take(countDevicesOnline).Select(x => _baseTest.Fixture.Build<DeviceHistory>()
                                                                                                    .With(h => h.DeviceId, x.Id)
                                                                                                    .With(h => h.IsOnline, true)
                                                                                                    .Create()).ToList();
            deviceHistoryList.AddRange(deviceList.Skip(countDevicesOnline).Select(x => _baseTest.Fixture.Build<DeviceHistory>()
                                                                                                    .With(h => h.DeviceId, x.Id)
                                                                                                    .With(h => h.IsOnline, false)
                                                                                                    .Create()));
            _baseTest.DbContext.DeviceHistory.AddRange(deviceHistoryList);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            string uri = $"{_baseDeviceUrl}?{nameof(FilteringModel.IsOnline)}={true}";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceModel>>(responseString);

            //Assert
            result.Total.Should().Be(countDevicesOnline);
            result.Result.Count().Should().Be(countDevicesOnline);
            result.Result.All(x => x.IsOnline == true).Should().BeTrue();
        }

        [Fact]
        public async Task ShouldReturnSuccessResultWithFilterByOfflineWhenGetDeviceList()
        {
            // Arrange
            int countDevicesOnline = 5;

            var deviceList = _baseTest.Fixture.Build<Device>()
                                          .Without(x => x.DeviceHistory)
                                          .CreateMany(10).ToList();
            deviceList.AddRange(_baseTest.Fixture.Build<Device>()
                                          .With(x => x.BranchId, _baseTest.Branch.Id)
                                          .Without(x => x.DeviceHistory)
                                          .CreateMany(countDevicesOnline));

            _baseTest.DbContext.Devices.AddRange(deviceList);
            _baseTest.DbContext.SaveChanges();
            var deviceHistoryList = deviceList.Take(countDevicesOnline).Select(x => _baseTest.Fixture.Build<DeviceHistory>()
                                                                                                    .With(h => h.DeviceId, x.Id)
                                                                                                    .With(h => h.IsOnline, false)
                                                                                                    .Create()).ToList();
            deviceHistoryList.AddRange(deviceList.Skip(countDevicesOnline).Select(x => _baseTest.Fixture.Build<DeviceHistory>()
                                                                                                    .With(h => h.DeviceId, x.Id)
                                                                                                    .With(h => h.IsOnline, true)
                                                                                                    .Create()));
            _baseTest.DbContext.DeviceHistory.AddRange(deviceHistoryList);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            string uri = $"{_baseDeviceUrl}?{nameof(FilteringModel.IsOnline)}={false}";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceModel>>(responseString);

            //Assert
            result.Total.Should().Be(countDevicesOnline);
            result.Result.Count().Should().Be(countDevicesOnline);
            result.Result.All(x => x.IsOnline == false).Should().BeTrue();
        }

        [Fact]
        public async Task ShouldReturnSuccessResultWithFilterSearchByIdWhenGetDeviceList()
        {
            // Arrange
            var deviceId = Guid.Parse("24e39d1c-8306-485b-ad35-cbf9c5fe8247");

            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var deviceList = _baseTest.Fixture.Build<Device>()
                                          .CreateMany(10).ToList();
            deviceList.Add(_baseTest.Fixture.Build<Device>()
                                          .With(x => x.Id, deviceId)
                                          .Create());
            deviceList.ForEach(x =>
            {
                x.DeviceHistory = _baseTest.Fixture.CreateMany<DeviceHistory>().ToList();
            });

            _baseTest.DbContext.Devices.AddRange(deviceList);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            string uri = $"{_baseDeviceUrl}?{nameof(FilteringModel.SearchPhrase)}=8306-485b-ad35-cbf9c";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceModel>>(responseString);

            //Assert
            result.Total.Should().Be(1);
            result.Result.Count().Should().Be(1);
            result.Result.First().Id.Should().Be(deviceId);
        }

        [Fact]
        public async Task ShouldReturnSuccessResultWithFilterSearchByUdidWhenGetDeviceList()
        {
            // Arrange
            string udidTest = "test-udid-cbf9c5fe8247";

            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var deviceList = _baseTest.Fixture.Build<Device>()
                                          .CreateMany(10).ToList();
            deviceList.Add(_baseTest.Fixture.Build<Device>()
                                          .With(x => x.Udid, udidTest)
                                          .Create());
            deviceList.ForEach(x =>
            {
                x.DeviceHistory = _baseTest.Fixture.CreateMany<DeviceHistory>().ToList();
            });

            _baseTest.DbContext.Devices.AddRange(deviceList);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            string uri = $"{_baseDeviceUrl}?{nameof(FilteringModel.SearchPhrase)}=udid-cb";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceModel>>(responseString);

            //Assert
            result.Total.Should().Be(1);
            result.Result.Count().Should().Be(1);
            result.Result.First().Udid.Should().Be(udidTest);
        }

        [Theory]
        [InlineData(0, 5)]
        [InlineData(3, 3)]
        public async Task ShouldReturnSuccessResultWithLazyLoadParametersWhenGetDeviceList(int skip, int take)
        {
            // Arrange
            int deviceCount = 10;
            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var deviceList = _baseTest.Fixture.Build<Device>()
                                          .Without(x => x.DeviceHistory)
                                          .CreateMany(deviceCount).ToList();
            deviceList.ForEach(x =>
            {
                x.DeviceHistory = _baseTest.Fixture.CreateMany<DeviceHistory>().ToList();
            });
            _baseTest.DbContext.Devices.AddRange(deviceList);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            string uri = $"{_baseDeviceUrl}?{nameof(LazyLoadParameters.Offset)}={skip}&{nameof(LazyLoadParameters.Limit)}={take}";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceModel>>(responseString);

            //Assert
            result.Offset.Should().Be(skip);
            result.Limit.Should().Be(take);
            result.Total.Should().Be(deviceCount);
            result.Result.Count().Should().Be(_baseTest.DbContext.Devices.Skip(skip).Take(take).Count());
        }

        [Theory]
        [InlineData(nameof(DeviceModel.Id), "desc")]
        [InlineData(nameof(DeviceModel.Id), "asc")]
        [InlineData(nameof(DeviceModel.Id), "")]

        [InlineData(nameof(DeviceModel.Udid), "desc")]
        [InlineData(nameof(DeviceModel.Udid), "asc")]
        [InlineData(nameof(DeviceModel.Udid), "")]

        [InlineData(nameof(DeviceModel.StatusSince), "desc")]
        [InlineData(nameof(DeviceModel.StatusSince), "asc")]
        [InlineData(nameof(DeviceModel.StatusSince), "")]

        [InlineData(nameof(DeviceModel.IsOnline), "desc")]
        [InlineData(nameof(DeviceModel.IsOnline), "asc")]
        [InlineData(nameof(DeviceModel.IsOnline), "")]

        [InlineData(nameof(DeviceModel.IsInLocation), "desc")]
        [InlineData(nameof(DeviceModel.IsInLocation), "asc")]
        [InlineData(nameof(DeviceModel.IsInLocation), "")]

        [InlineData(nameof(DeviceModel.Branch.BranchName), "desc")]
        [InlineData(nameof(DeviceModel.Branch.BranchName), "asc")]
        [InlineData(nameof(DeviceModel.Branch.BranchName), "")]

        [InlineData(nameof(DeviceModel.Company.CompanyName), "desc")]
        [InlineData(nameof(DeviceModel.Company.CompanyName), "asc")]
        [InlineData(nameof(DeviceModel.Company.CompanyName), "")]

        [InlineData(nameof(DeviceModel.LoggedInAs.DisplayName), "desc")]
        [InlineData(nameof(DeviceModel.LoggedInAs.DisplayName), "asc")]
        [InlineData(nameof(DeviceModel.LoggedInAs.DisplayName), "")]
        public async Task ShouldReturnSuccessResultWithSortingWhenGetDeviceList(string sortBy, string order)
        {
            // Arrange
            int deviceCount = 10;
            var deviceList = _baseTest.Fixture.Build<Device>()
                                          .Without(x => x.DeviceHistory)
                                          .CreateMany(deviceCount).ToList();

            _baseTest.DbContext.Devices.AddRange(deviceList);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            string uri = $"{_baseDeviceUrl}?{nameof(Sorting.SortBy)}={sortBy}&{nameof(Sorting.Order)}={order}";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceModel>>(responseString);

            //Assert
            var expression = typeof(DeviceModel).GetExpression<DeviceModel>(sortBy);

            if (order == "asc")
            {
                bool resultSequence = result.Result.SequenceEqual(result.Result.OrderBy(expression));
                resultSequence.Should().BeTrue();
            }
            else
            {
                bool resultSequence = result.Result.SequenceEqual(result.Result.OrderByDescending(expression));
                resultSequence.Should().BeTrue();
            }
        }

        [Theory]
        [InlineData(ConstTest.Role.AccountAdmin)]
        [InlineData(ConstTest.Role.OperationAdmin)]
        [InlineData(ConstTest.Role.SystemAdmin)]
        public async Task ShouldReturnSuccessResultForAdminsWhenGetDeviceList(string roleName)
        {
            // Arrange
            int expectedCount = 10;
            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var deviceList = _baseTest.Fixture.Build<Device>()
                                          .CreateMany(expectedCount).ToList();
            deviceList.ForEach(x =>
            {
                x.DeviceHistory = _baseTest.Fixture.CreateMany<DeviceHistory>().ToList();
            });
            _baseTest.DbContext.Devices.AddRange(deviceList);
            _baseTest.DbContext.SaveChanges();
            var user = _baseTest.Users[roleName];
            var userLoginOptiomTest = new UserLoginOption { UserName = user.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            string uri = $"{_baseDeviceUrl}";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceModel>>(responseString);

            //Assert
            result.Total.Should().Be(expectedCount);
            result.Result.Count().Should().Be(expectedCount);
        }


        [Theory]
        [InlineData(ConstTest.Role.AccountAdmin)]
        [InlineData(ConstTest.Role.OperationAdmin)]
        [InlineData(ConstTest.Role.SystemAdmin)]
        [InlineData(ConstTest.Role.Supervisor)]
        public async Task ShouldReturnSuccessResultOneDeviceWithCorrectFieldsWhenGetDeviceList(string roleName)
        {
            // Arrange
            var user = _baseTest.Users[roleName];

            var device = _baseTest.Fixture.Build<Device>()
                                          .With(x => x.CompanyId, _baseTest.Company.Id)
                                          .With(x => x.BranchId, _baseTest.Branch.Id)
                                          .Without(x => x.DeviceHistory).Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            var historyMockList = _baseTest.Fixture.Build<DeviceHistory>()
                                          .With(x => x.DeviceId, device.Id)
                                          .With(x => x.LoggedInUserId, user.Id)
                                          .With(x => x.CreatedOn, DateTime.Now.AddMonths(-1))
                                          .Without(x => x.Device)
                                          .CreateMany(2);
            var expextedHistory = historyMockList.First();
            var expectedCreatedOn = DateTime.Now;
            expextedHistory.CreatedOn = expectedCreatedOn;

            _baseTest.DbContext.DeviceHistory.AddRange(historyMockList);
            _baseTest.DbContext.SaveChanges();

            var userLoginOptiomTest = new UserLoginOption { UserName = user.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            string uri = $"{_baseDeviceUrl}";

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var response = await _baseTest.HttpClient.GetAsync(uri);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<LazyLoadedResult<DeviceModel>>(responseString);

            //Assert
            result.Total.Should().Be(1);
            result.Result.Count().Should().Be(1);
            result.Result.First().Id.Should().Be(device.Id);
            result.Result.First().Udid.Should().Be(device.Udid);
            result.Result.First().HexnodeUdid.Should().Be(device.HexnodeUdid);
            result.Result.First().IsOnline.Should().Be(expextedHistory.IsOnline);
            result.Result.First().IsInLocation.Should().Be(expextedHistory.IsInLocation);
            result.Result.First().StatusSince.Should().Be(expectedCreatedOn);
            result.Result.First().IsPending.Should().Be(device.IsPending);
            result.Result.First().IsDeleted.Should().Be(device.IsDeleted);
            result.Result.First().Latitude.Should().Be(device.Latitude);
            result.Result.First().Longitude.Should().Be(device.Longitude);
            result.Result.First().Radius.Should().Be(device.Radius);
            result.Result.First().CreatedOn.Should().Be(device.CreatedOn);
            result.Result.First().CurrentDeviceLocationLatitude.Should().Be(expextedHistory.CurrentDeviceLocationLatitude);
            result.Result.First().CurrentDeviceLocationLongitude.Should().Be(expextedHistory.CurrentDeviceLocationLongitude);
            result.Result.First().LoggedInAs.Id.Should().Be(user.Id);
            result.Result.First().LoggedInAs.DisplayName.Should().Be(user.DisplayName);
            result.Result.First().Company.Id.Should().Be(_baseTest.Company.Id);
            result.Result.First().Company.CompanyName.Should().Be(_baseTest.Company.CompanyName);
            result.Result.First().Branch.Id.Should().Be(_baseTest.Branch.Id);
            result.Result.First().Branch.BranchName.Should().Be(_baseTest.Branch.BranchName);
        }

        #endregion

        #region PendingPost

        [Fact]
        public async Task ShouldReturnBadRequestWhenPendingPost()
        {
            // Arrange
            string uri = $"{_baseDeviceUrl}/setup";
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var content = new StringContent(JsonConvert.SerializeObject(new SetupDeviceRequest()), Encoding.UTF8, "application/json");
            var response = await _baseTest.HttpClient.PostAsync(uri, content);

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task ShouldReturnSuccessResultWhenPendingPost()
        {
            // Arrange
            var request = _baseTest.Fixture.Create<SetupDeviceRequest>();

            string uri = $"{_baseDeviceUrl}/setup";
            var userLoginOptiomTest = new UserLoginOption { UserName = _baseTest.OperationAdmin.Email, Password = ConstTest.Password, Scope = Const.Permissions.Device.Delete };
            var userToken = await _baseTest.HttpServiceTest.GetAuthorizationToken(userLoginOptiomTest);

            // Act
            _baseTest.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(userToken.TokenType, userToken.AccessToken);
            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
            var response = await _baseTest.HttpClient.PostAsync(uri, content);

            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();

            //Assert
            response.StatusCode.Should().Be(StatusCodes.Status201Created);
            var deviceId = JsonConvert.DeserializeObject<Guid>(responseString);
            deviceId.Should().NotBeEmpty();
        }

        #endregion
    }
}
