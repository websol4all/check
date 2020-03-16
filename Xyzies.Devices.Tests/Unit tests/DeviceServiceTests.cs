using AutoFixture;
using FluentAssertions;
using Mapster;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Data.Repository;
using Xyzies.Devices.Services.Exceptions;
using Xyzies.Devices.Services.Helpers.Interfaces;
using Xyzies.Devices.Services.Models;
using Xyzies.Devices.Services.Models.Branch;
using Xyzies.Devices.Services.Models.Company;
using Xyzies.Devices.Services.Models.DeviceModels;
using Xyzies.Devices.Services.Models.Tenant;
using Xyzies.Devices.Services.Models.User;
using Xyzies.Devices.Services.Requests.Device;
using Xyzies.Devices.Services.Service;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Tests.Unit_tests
{
    public class DeviceServiceTests : IClassFixture<BaseTest>
    {
        private readonly BaseTest _baseTest = null;
        private ILogger<DeviceService> _loggerMock;
        private Mock<IHttpService> _httpServiceMock;
        private Mock<IValidationHelper> _validationHelperMock;
        private Mock<IHubContext<WebHubService>> webHubContext;
        private Mock<INotificationSender> _notificationSenderExtentionMock;

        private readonly IDeviceService _deviceService = null;

        public DeviceServiceTests(BaseTest baseTest)
        {
            _baseTest = baseTest ?? throw new ArgumentNullException(nameof(baseTest));
            _baseTest.DbContext.ClearContext();

            _baseTest.Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => _baseTest.Fixture.Behaviors.Remove(b));
            _baseTest.Fixture.Behaviors.Add(new OmitOnRecursionBehavior(1));

            _loggerMock = Mock.Of<ILogger<DeviceService>>();
            _httpServiceMock = new Mock<IHttpService>();
            _validationHelperMock = new Mock<IValidationHelper>();

            webHubContext = new Mock<IHubContext<WebHubService>>();
            var hubClients = new Mock<IHubClients>();
            webHubContext.Setup(h => h.Clients)
                      .Returns(hubClients.Object);
            _notificationSenderExtentionMock = new Mock<INotificationSender>();

            _deviceService = new DeviceService(
                _loggerMock,
                _httpServiceMock.Object,
                new DeviceRepository(_baseTest.DbContext),
                _validationHelperMock.Object,
                webHubContext.Object,
                new DeviceHistoryRepository(_baseTest.DbContext),
                _notificationSenderExtentionMock.Object);
        }

        [Fact]
        public async Task ShouldReturnFailIfRequestNullWhenCreateDevice()
        {
            // Arrange
            string token = _baseTest.Fixture.Create<string>();

            // Act
            Func<Task> result = async () => await _deviceService.Create(null, token);

            //Assert
            await result.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ShouldReturnFailIfTokenNullWhenCreateDevice()
        {
            // Arrange
            var request = _baseTest.Fixture.Create<CreateDeviceRequest>();

            // Act
            Func<Task> result = async () => await _deviceService.Create(request, null);

            //Assert
            await result.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ShouldReturnFailIfUditAlreadyExistWhenCreateDevice()
        {
            // Arrange
            var request = _baseTest.Fixture.Create<CreateDeviceRequest>();
            _baseTest.DbContext.Devices.Add(request.Adapt<Device>());
            _baseTest.DbContext.SaveChanges();

            // Act
            Func<Task> result = async () => await _deviceService.Create(request, _baseTest.Fixture.Create<string>());

            //Assert
            await result.Should().ThrowAsync<ApplicationException>();
        }

        [Fact]
        public async Task ShouldReturnSuccessAndCreatedDeviceAsAdmin()
        {
            // Arrange
            int companyId = 5;
            string token = _baseTest.Fixture.Create<string>();
            var request = _baseTest.Fixture.Build<CreateDeviceRequest>().With(x => x.CompanyId, companyId).Create();

            _validationHelperMock.Setup(x => x.GetCompanyIdByPermission(token, It.IsAny<string[]>(), companyId)).ReturnsAsync(companyId);

            // Act
            var result = await _deviceService.Create(request, token);

            //Assert
            _baseTest.DbContext.Devices.Count().Should().Be(1);
            var device = _baseTest.DbContext.Devices.First();
            device.Id.Should().Be(result);
            device.Udid.Should().Be(request.Udid);
            device.Latitude.Should().Be(request.Latitude);
            device.Longitude.Should().Be(request.Longitude);
            device.Radius.Should().Be(request.Radius);
            device.CompanyId.Should().Be(request.CompanyId);
            device.BranchId.Should().Be(request.BranchId);
            device.IsDeleted.Should().BeFalse();
            device.Phone.Should().Be(request.Phone);
            device.DeviceName.Should().Be(request.DeviceName);
        }

        [Fact]
        public async Task ShouldReturnSuccessAndCreatedDeviceAsSuperviser()
        {
            // Arrange
            int companyId = 5;
            int userCompanyId = 10;
            string token = _baseTest.Fixture.Create<string>();
            var request = _baseTest.Fixture.Build<CreateDeviceRequest>().With(x => x.CompanyId, companyId).Create();

            _validationHelperMock.Setup(x => x.GetCompanyIdByPermission(token, It.IsAny<string[]>(), companyId)).ReturnsAsync(userCompanyId);

            // Act
            var result = await _deviceService.Create(request, token);

            //Assert
            _baseTest.DbContext.Devices.Count().Should().Be(1);
            var device = _baseTest.DbContext.Devices.First();
            device.Id.Should().Be(result);
            device.Udid.Should().Be(request.Udid);
            device.Latitude.Should().Be(request.Latitude);
            device.Longitude.Should().Be(request.Longitude);
            device.Radius.Should().Be(request.Radius);
            device.CompanyId.Should().Be(userCompanyId);
            device.BranchId.Should().Be(request.BranchId);
            device.IsDeleted.Should().BeFalse();
            device.Phone.Should().Be(request.Phone);
            device.DeviceName.Should().Be(request.DeviceName);
        }

        [Fact]
        public async Task ShouldReturnFailIfRequestNullWhenUpdateDevice()
        {
            // Arrange
            string token = _baseTest.Fixture.Create<string>();
            var deviceId = _baseTest.Fixture.Create<Guid>();

            // Act
            Func<Task> result = async () => await _deviceService.Update(null, deviceId, token);

            //Assert
            await result.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ShouldReturnFailIfTokenNullWhenUpdateDevice()
        {
            // Arrange
            var request = _baseTest.Fixture.Create<BaseDeviceRequest>();
            var deviceId = _baseTest.Fixture.Create<Guid>();

            // Act
            Func<Task> result = async () => await _deviceService.Update(request, deviceId, null);

            //Assert
            await result.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ShouldReturnFailIfDeviceNotExistWhenUpdateDevice()
        {
            // Arrange
            string token = _baseTest.Fixture.Create<string>();
            var request = _baseTest.Fixture.Create<BaseDeviceRequest>();
            var deviceId = _baseTest.Fixture.Create<Guid>();

            // Act
            Func<Task> result = async () => await _deviceService.Update(request, deviceId, token);

            //Assert
            await result.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task ShouldReturnSuccessAndUpdatedDeviceAsSuperviser()
        {
            // Arrange
            int companyId = 10;
            int userCompanyId = 20;

            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var device = _baseTest.Fixture.Build<Device>()
                                  .With(x => x.CompanyId, userCompanyId)
                                  .Without(x => x.DeviceHistory).Create();
            device = (await _baseTest.DbContext.Devices.AddAsync(device)).Entity;
            _baseTest.DbContext.SaveChanges();

            string token = _baseTest.Fixture.Create<string>();
            var request = _baseTest.Fixture.Build<BaseDeviceRequest>().With(x => x.CompanyId, companyId).Create();

            _validationHelperMock.Setup(x => x.GetCompanyIdByPermission(token, It.IsAny<string[]>(), companyId)).ReturnsAsync(userCompanyId);

            // Act
            await _deviceService.Update(request, device.Id, token);
            var updatedDevice = _baseTest.DbContext.Devices.First();

            //Assert
            updatedDevice.Id.Should().Be(device.Id);
            updatedDevice.Udid.Should().Be(device.Udid);
            updatedDevice.CompanyId.Should().Be(userCompanyId);
            updatedDevice.Latitude.Should().Be(request.Latitude);
            updatedDevice.Longitude.Should().Be(request.Longitude);
            updatedDevice.Radius.Should().Be(request.Radius);
            updatedDevice.BranchId.Should().Be(request.BranchId);
            updatedDevice.DeviceName.Should().Be(request.DeviceName);
            updatedDevice.Phone.Should().Be(request.Phone);
        }

        [Fact]
        public async Task ShouldReturnSuccessAndUpdatedDeviceAsAdmin()
        {
            // Arrange
            int companyId = 10;
            int userCompanyId = 20;

            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var device = _baseTest.Fixture.Build<Device>()
                                          .With(x => x.CompanyId, userCompanyId)
                                          .Without(x => x.DeviceHistory)
                                          .Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            string token = _baseTest.Fixture.Create<string>();
            var request = _baseTest.Fixture.Build<BaseDeviceRequest>().With(x => x.CompanyId, companyId).Create();

            _validationHelperMock.Setup(x => x.GetCompanyIdByPermission(token, It.IsAny<string[]>(), companyId)).ReturnsAsync(companyId);

            // Act
            await _deviceService.Update(request, device.Id, token);
            var updatedDevice = _baseTest.DbContext.Devices.First();

            //Assert
            updatedDevice.Id.Should().Be(device.Id);
            updatedDevice.Udid.Should().Be(device.Udid);
            updatedDevice.CompanyId.Should().Be(companyId);
            updatedDevice.Latitude.Should().Be(request.Latitude);
            updatedDevice.Longitude.Should().Be(request.Longitude);
            updatedDevice.Radius.Should().Be(request.Radius);
            updatedDevice.BranchId.Should().Be(request.BranchId);
            updatedDevice.DeviceName.Should().Be(request.DeviceName);
            updatedDevice.Phone.Should().Be(request.Phone);
        }

        [Fact]
        public async Task ShouldReturnFailIfTokenNullWhenDeleteDevice()
        {
            // Arrange
            var request = _baseTest.Fixture.Create<BaseDeviceRequest>();
            var deviceId = _baseTest.Fixture.Create<Guid>();

            // Act
            Func<Task> result = async () => await _deviceService.Delete(deviceId, null);

            //Assert
            await result.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ShouldReturnFailIfDeviceNotExistWhenDeleteDevice()
        {
            // Arrange
            string token = _baseTest.Fixture.Create<string>();
            var request = _baseTest.Fixture.Create<BaseDeviceRequest>();
            var deviceId = _baseTest.Fixture.Create<Guid>();

            // Act
            Func<Task> result = async () => await _deviceService.Delete(deviceId, token);

            //Assert
            await result.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task ShouldReturnFailIfUserNotHasAccessWhenDeleteDevice()
        {
            // Arrange
            int companyId = 5;
            int userCompanyId = 6;
            var device = _baseTest.Fixture.Build<Device>().With(x => x.CompanyId, companyId).Without(x => x.DeviceHistory).Create();
            device = _baseTest.DbContext.Devices.Add(device).Entity;
            _baseTest.DbContext.SaveChanges();

            string token = _baseTest.Fixture.Create<string>();
            var request = _baseTest.Fixture.Create<BaseDeviceRequest>();

            _validationHelperMock.Setup(x => x.GetCompanyIdByPermission(token, It.IsAny<string[]>(), companyId)).ReturnsAsync(userCompanyId);

            // Act
            Func<Task> result = async () => await _deviceService.Delete(device.Id, token);

            //Assert
            await result.Should().ThrowAsync<AccessException>();
        }

        [Fact]
        public async Task ShouldReturnSuccessAndDeleteDevice()
        {
            // Arrange
            int companyId = 10;

            var device = _baseTest.Fixture.Build<Device>().With(x => x.CompanyId, companyId).Without(x => x.DeviceHistory).Create();
            device = _baseTest.DbContext.Devices.Add(device).Entity;
            _baseTest.DbContext.SaveChanges();

            string token = _baseTest.Fixture.Create<string>();

            _validationHelperMock.Setup(x => x.GetCompanyIdByPermission(token, It.IsAny<string[]>(), companyId)).ReturnsAsync(companyId);

            // Act
            await _deviceService.Delete(device.Id, token);
            var deletedDevice = _baseTest.DbContext.Devices.First();

            //Assert
            deletedDevice.Id.Should().Be(device.Id);
            deletedDevice.IsDeleted.Should().BeTrue();
        }

        [Fact]
        public async Task ShouldReturnAllDevicesForAdmin()
        {
            // Arrange
            CreateDevicesAndSetupMockWithParams();
            var devices = _baseTest.DbContext.Devices.ToList();
            // Act
            var result = await _deviceService.GetAll(new FilteringModel(), new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.Equal(result.Total, devices.Count());
        }

        [Fact]
        public async Task ShouldReturnNotAllDevicesForSuperviser()
        {
            // Arrange
            var companyId = 100500;
            CreateDevicesAndSetupMockWithParams(companyId);
            var devices = _baseTest.DbContext.Devices.ToList();
            // Act
            var result = await _deviceService.GetAll(new FilteringModel(), new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.NotEqual(result.Total, devices.Count());
            Assert.True(result.Result.All(x => x.Company.Id == companyId));
        }

        [Fact]
        public async Task ShouldReturnDeviceForAdmin()
        {
            // Arrange
            CreateDevicesAndSetupMockWithParams();
            var device = _baseTest.DbContext.Devices.FirstOrDefault();
            // Act
            var result = await _deviceService.GetById(It.IsAny<string>(), device.Id);
            //Assert
            Assert.Equal(device.Id, result.Id);
        }

        [Fact]
        public async Task ShouldReturnDeviceForSuperviser()
        {
            // Arrange
            var companyId = 100500;
            CreateDevicesAndSetupMockWithParams(companyId);
            var device = _baseTest.DbContext.Devices.FirstOrDefault();
            // Act
            var result = await _deviceService.GetById(It.IsAny<string>(), device.Id);
            //Assert
            Assert.Equal(device.Id, result.Id);
            Assert.Equal(device.CompanyId, result.Company.Id);
        }

        [Fact]
        public async Task ShouldReturnFilteredByCompanyIdsDevicesForAdmin()
        {
            // Arrange
            CreateDevicesAndSetupMockWithParams();
            var companyIds = new List<int> { _baseTest.DbContext.Devices.First().CompanyId, _baseTest.DbContext.Devices.Last().CompanyId };
            var filter = new FilteringModel { CompanyIds = companyIds };
            // Act
            var result = await _deviceService.GetAll(filter, new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.True(result.Result.All(x => filter.CompanyIds.Contains(x.Company.Id)));
            Assert.False(result.Result.Any(x => !filter.CompanyIds.Contains(x.Company.Id)));
        }

        [Fact]
        public async Task ShouldReturnFilteredByBranchIdsDevicesForAdmin()
        {
            // Arrange
            CreateDevicesAndSetupMockWithParams();
            var branchIds = new List<Guid> { _baseTest.DbContext.Devices.First().BranchId, _baseTest.DbContext.Devices.Last().BranchId };
            var filter = new FilteringModel { BranchIds = branchIds };
            // Act
            var result = await _deviceService.GetAll(filter, new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.True(result.Result.All(x => filter.BranchIds.Contains(x.Branch.Id)));
            Assert.False(result.Result.Any(x => !filter.BranchIds.Contains(x.Branch.Id)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldReturnFilteredIsOnlineDevicesForAdmin(bool isOnline)
        {
            // Arrange
            CreateDevicesAndSetupMockWithParams();
            var filter = new FilteringModel { IsOnline = isOnline };
            // Act
            var result = await _deviceService.GetAll(filter, new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.True(result.Result.All(x => x.IsOnline == isOnline));
            Assert.False(result.Result.Any(x => x.IsOnline != isOnline));
        }

        [Fact]
        public async Task ShouldReturnQuickSearchedByIdDevicesForAdmin()
        {
            // Arrange
            CreateDevicesAndSetupMockWithParams();
            var searchPhrase = _baseTest.DbContext.Devices.First().Id.ToString().Substring(0, 3);
            var filter = new FilteringModel { SearchPhrase = searchPhrase };
            // Act
            var result = await _deviceService.GetAll(filter, new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.True(result.Result.All(x => x.Id.ToString().ToLower().Contains(searchPhrase.ToLower()) || x.Udid.ToLower().Contains(searchPhrase.ToLower())));
        }

        [Fact]
        public async Task ShouldReturnQuickSearchedByUdidDevicesForAdmin()
        {
            // Arrange
            CreateDevicesAndSetupMockWithParams();
            var searchPhrase = _baseTest.DbContext.Devices.First().Udid.Substring(0, 3);
            var filter = new FilteringModel { SearchPhrase = searchPhrase };
            // Act
            var result = await _deviceService.GetAll(filter, new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.True(result.Result.All(x => x.Id.ToString().ToLower().Contains(searchPhrase.ToLower()) || x.Udid.ToLower().Contains(searchPhrase.ToLower())));
        }

        [Fact]
        public async Task ShouldReturnQuickSearchedEmptyForAdmin()
        {
            // Arrange
            CreateDevicesAndSetupMockWithParams();
            var searchPhrase = "";
            var filter = new FilteringModel { SearchPhrase = searchPhrase };
            // Act
            var result = await _deviceService.GetAll(filter, new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.Equal(_baseTest.DbContext.Devices.Count(), result.Result.Count());
        }

        [Fact]
        public async Task ShouldReturnFilteredByCompanyIdsDevicesForSuperviser()
        {
            // Arrange
            var comapnyId = 100500;
            CreateDevicesAndSetupMockWithParams(comapnyId);
            var companyIds = new List<int> { _baseTest.DbContext.Devices.First().CompanyId, _baseTest.DbContext.Devices.Last().CompanyId };
            var filter = new FilteringModel { CompanyIds = companyIds };
            // Act
            var result = await _deviceService.GetAll(filter, new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.True(result.Result.All(x => x.Company.Id == comapnyId));
        }

        [Fact]
        public async Task ShouldReturnFilteredByBranchIdsDevicesForSuperviser()
        {
            // Arrange
            var companyId = 100500;
            CreateDevicesAndSetupMockWithParams(companyId);
            var branchIds = new List<Guid> { _baseTest.DbContext.Devices.First().BranchId, _baseTest.DbContext.Devices.Last().BranchId };
            var filter = new FilteringModel { BranchIds = branchIds };
            // Act
            var result = await _deviceService.GetAll(filter, new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.True(result.Result.All(x => x.Company.Id == companyId));
            Assert.True(result.Result.All(x => filter.BranchIds.Contains(x.Branch.Id)));
            Assert.False(result.Result.Any(x => !filter.BranchIds.Contains(x.Branch.Id)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldReturnFilteredIsOnlineDevicesForSuperviser(bool isOnline)
        {
            // Arrange
            var comapnyId = 100500;
            CreateDevicesAndSetupMockWithParams(comapnyId);
            var devices = _baseTest.DbContext.Devices.ToList();
            var history = devices.First(x => x.CompanyId == comapnyId).DeviceHistory.OrderByDescending(x => x.CreatedOn).First();
            history.IsOnline = isOnline;
            _baseTest.DbContext.DeviceHistory.Update(history);
            _baseTest.DbContext.SaveChanges();
            var filter = new FilteringModel { IsOnline = isOnline };
            // Act
            var result = await _deviceService.GetAll(filter, new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.True(result.Result.All(x => x.Company.Id == comapnyId));
            Assert.True(result.Result.All(x => x.IsOnline == isOnline));
            Assert.False(result.Result.Any(x => x.IsOnline != isOnline));
        }

        [Fact]
        public async Task ShouldReturnQuickSearchedByUdidDevicesForSuperviser()
        {
            // Arrange
            var comapnyId = 100500;
            CreateDevicesAndSetupMockWithParams(comapnyId);
            var searchPhrase = _baseTest.DbContext.Devices.First(x => x.CompanyId == comapnyId).Udid.Substring(0, 3);
            var filter = new FilteringModel { SearchPhrase = searchPhrase };
            // Act
            var result = await _deviceService.GetAll(filter, new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.True(result.Result.All(x => x.Id.ToString().ToLower().Contains(searchPhrase.ToLower()) || x.Udid.ToLower().Contains(searchPhrase.ToLower())));
            Assert.True(result.Result.All(x => x.Company.Id == comapnyId));
        }

        [Fact]
        public async Task ShouldReturnQuickSearchedByIdDevicesForSuperviser()
        {
            // Arrange
            var comapnyId = 100500;
            CreateDevicesAndSetupMockWithParams(comapnyId);
            var searchPhrase = _baseTest.DbContext.Devices.First(x => x.CompanyId == comapnyId).Id.ToString().Substring(0, 3);
            var filter = new FilteringModel { SearchPhrase = searchPhrase };
            // Act
            var result = await _deviceService.GetAll(filter, new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.True(result.Result.All(x => x.Id.ToString().ToLower().Contains(searchPhrase.ToLower()) || x.Udid.ToLower().Contains(searchPhrase.ToLower())));
            Assert.True(result.Result.All(x => x.Company.Id == comapnyId));
        }

        [Fact]
        public async Task ShouldReturnQuickSearchedEmptyForSuperviser()
        {
            // Arrange
            var comapnyId = 100500;
            CreateDevicesAndSetupMockWithParams(comapnyId);
            var searchPhrase = "";
            var filter = new FilteringModel { SearchPhrase = searchPhrase };
            // Act
            var result = await _deviceService.GetAll(filter, new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.True(result.Result.All(x => x.Company.Id == comapnyId));
            Assert.Equal(_baseTest.DbContext.Devices.Count(x => x.CompanyId == comapnyId), result.Result.Count());
        }

        [Theory]
        [InlineData(nameof(DeviceModel.Id), "", "desc")]
        [InlineData(nameof(DeviceModel.Id), "", "asc")]
        [InlineData(nameof(DeviceModel.Id), "", "")]

        [InlineData(nameof(DeviceModel.Udid), "", "desc")]
        [InlineData(nameof(DeviceModel.Udid), "", "asc")]
        [InlineData(nameof(DeviceModel.Udid), "", "")]

        [InlineData(nameof(DeviceModel.StatusSince), "", "desc")]
        [InlineData(nameof(DeviceModel.StatusSince), "", "asc")]
        [InlineData(nameof(DeviceModel.StatusSince), "", "")]

        [InlineData(nameof(DeviceModel.IsInLocation), "", "desc")]
        [InlineData(nameof(DeviceModel.IsInLocation), "", "asc")]
        [InlineData(nameof(DeviceModel.IsInLocation), "", "")]

        [InlineData(nameof(DeviceModel.Branch), nameof(BranchModel.BranchName), "desc")]
        [InlineData(nameof(DeviceModel.Branch), nameof(BranchModel.BranchName), "asc")]
        [InlineData(nameof(DeviceModel.Branch), nameof(BranchModel.BranchName), "")]

        [InlineData(nameof(DeviceModel.Company), nameof(CompanyModel.CompanyName), "desc")]
        [InlineData(nameof(DeviceModel.Company), nameof(CompanyModel.CompanyName), "asc")]
        [InlineData(nameof(DeviceModel.Company), nameof(CompanyModel.CompanyName), "")]

        [InlineData(nameof(DeviceModel.LoggedInAs), nameof(UserModel.DisplayName), "desc")]
        [InlineData(nameof(DeviceModel.LoggedInAs), nameof(UserModel.DisplayName), "asc")]
        [InlineData(nameof(DeviceModel.LoggedInAs), nameof(UserModel.DisplayName), "")]

        [InlineData(nameof(DeviceModel.Tenant), nameof(TenantModel.Name), "desc")]
        [InlineData(nameof(DeviceModel.Tenant), nameof(TenantModel.Name), "asc")]
        [InlineData(nameof(DeviceModel.Tenant), nameof(TenantModel.Name), "")]
        public async Task ShouldReturnSortedDevices(string property, string nestedProperty, string order)
        {
            // Arrange
            CreateDevicesAndSetupMockWithParams();
            var notSortedDeviceModels = await _deviceService.GetAll(new FilteringModel(), new LazyLoadParameters(), new Sorting(), It.IsAny<string>());
            var values = GetDeviceModelVauels(property, nestedProperty, notSortedDeviceModels.Result.ToList());
            values = order == "asc" ? values.OrderBy(x => x).ToList() : values.OrderByDescending(x => x).ToList();
            // Act
            var result = await _deviceService.GetAll(new FilteringModel(), new LazyLoadParameters(),
                new Sorting()
                {
                    Order = order,
                    SortBy = string.IsNullOrWhiteSpace(nestedProperty) ? property : nestedProperty
                }, It.IsAny<string>());
            var resultValues = GetDeviceModelVauels(property, nestedProperty, result.Result.ToList());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.Equal(values, resultValues);
        }

        [Theory]
        [InlineData(nameof(DeviceModel.IsOnline), "desc")]
        [InlineData(nameof(DeviceModel.IsOnline), "asc")]
        [InlineData(nameof(DeviceModel.IsOnline), "")]
        public async Task ShouldReturnSortedDevicesByStatus(string property, string order)
        {
            // Arrange
            CreateDevicesAndSetupMockWithParams();
            var sorting = new Sorting { SortBy = property, Order = order };
            // Act
            var result = await _deviceService.GetAll(new FilteringModel(), new LazyLoadParameters(), sorting, It.IsAny<string>());
            //Assert
            Assert.NotEmpty(result.Result);
            Assert.Equal(_baseTest.DbContext.Devices.Count(), result.Result.Count());
            if (order == "asc")
            {
                result.Result.Should().BeInAscendingOrder(x => x.Type);
            }
            else
            {
                result.Result.Should().BeInDescendingOrder(x => x.Type);
            }
        }

        private List<object> GetDeviceModelVauels(string property, string nestedProperty, List<DeviceModel> models)
        {
            var values = new List<object>();
            foreach (var model in models)
            {
                var prop = model.GetType().GetProperty(property);
                object propValue = prop.GetValue(model);
                if (propValue != null && !string.IsNullOrWhiteSpace(nestedProperty))
                {
                    prop = propValue.GetType().GetProperty(nestedProperty);
                    propValue = prop.GetValue(propValue);
                }
                values.Add(propValue);
            }
            return values;
        }

        private void CreateDevicesAndSetupMockWithParams(int? companyId = null, bool isDeleted = false)
        {
            var company = _baseTest.Fixture.Build<CompanyModel>().With(x => x.Id, companyId ?? _baseTest.Fixture.Create<int>()).Create();
            var branch = _baseTest.Fixture.Create<BranchModel>();
            var user = _baseTest.Fixture.Create<UserModel>();
            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var devices = _baseTest.Fixture.Build<Device>()
                                                .With(x => x.BranchId, branch.Id)
                                                .With(x => x.CompanyId, companyId ?? company.Id)
                                                .With(x => x.IsDeleted, isDeleted).CreateMany().ToList();
            devices.ForEach(x =>
            {
                x.DeviceHistory = _baseTest.Fixture.Build<DeviceHistory>()
                                                        .With(h => h.LoggedInUserId, user.Id)
                                                        .With(h => h.DeviceId, x.Id)
                                                        .With(h => h.CompanyId, companyId ?? company.Id)
                                                        .CreateMany().ToList();
            });

            var anotherBranch = _baseTest.Fixture.Create<BranchModel>();
            var anotherUser = _baseTest.Fixture.Create<UserModel>();
            var anotherCompany = _baseTest.Fixture.Create<CompanyModel>();
            var anotherCompanyDevices = _baseTest.Fixture.Build<Device>()
                                                .With(x => x.BranchId, anotherBranch.Id)
                                                .With(x => x.CompanyId, anotherCompany.Id)
                                                .CreateMany().ToList();
            anotherCompanyDevices.ForEach(x =>
            {
                x.DeviceHistory = _baseTest.Fixture.Build<DeviceHistory>()
                                                               .With(h => h.LoggedInUserId, anotherUser.Id)
                                                               .With(h => h.DeviceId, x.Id)
                                                               .CreateMany().ToList();
            });

            _baseTest.DbContext.Devices.AddRange(devices);
            _baseTest.DbContext.Devices.AddRange(anotherCompanyDevices);
            _baseTest.DbContext.SaveChanges();
            var tenant = _baseTest.Fixture.Build<TenantFullModel>().With(x => x.Companies, new List<CompanyModel> { company, anotherCompany }).Create();
            _validationHelperMock.Setup(x => x.GetCompanyIdByPermission(It.IsAny<string>(), It.IsAny<string[]>(), null)).ReturnsAsync(companyId);
            _httpServiceMock.Setup(x => x.GetCompaniesByIds(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<CompanyModel> { company, anotherCompany });
            _httpServiceMock.Setup(x => x.GetBranchesByIds(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<BranchModel> { branch, anotherBranch });
            _httpServiceMock.Setup(x => x.GetUsersByIdsAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<UserModel> { user, anotherUser });
            _httpServiceMock.Setup(x => x.GetCompanyById(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(company);
            _httpServiceMock.Setup(x => x.GetTenantsByIds(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<TenantFullModel>() { tenant });
            _httpServiceMock.Setup(x => x.GetTenantSingleByCompanyId(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(tenant);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(3, 3)]
        public async Task ShouldReturnSuccsessWhithLazyLoadingWhenGetAllDevice(int skip, int take)
        {
            // Arrange
            var request = _baseTest.Fixture.Create<BaseDeviceRequest>();
            int firstCompanyId = 10;
            var token = _baseTest.Fixture.Create<string>();

            CreateDevicesAndSetupMockWithParams();

            var device = _baseTest.Fixture.Build<Device>().With(x => x.CompanyId, firstCompanyId)
                                                .CreateMany(10).ToList();
            device.ForEach(x =>
            {
                x.DeviceHistory = _baseTest.Fixture.Build<DeviceHistory>()
                                                   .With(h => h.DeviceId, x.Id)
                                                   .CreateMany()
                                                   .ToList();
            });

            _baseTest.DbContext.Devices.AddRange(device);

            _baseTest.DbContext.SaveChanges();

            var filters = new LazyLoadParameters { Offset = skip, Limit = take };
            // Act
            var result = await _deviceService.GetAll(new FilteringModel(), filters, new Sorting(), token);

            //Assert
            result.Total.Should().Be(_baseTest.DbContext.Devices.Count());
            _baseTest.DbContext.Devices.Skip(skip).Take(take).Count().Should().Be(result.Result.Count());
            result.Offset.Should().Be(skip);
            result.Limit.Should().Be(take);
        }

        // [Fact]
        // public async Task ShouldReturnDevicePhones()
        // {
        //     // Arrange
        //     CreateDevicesAndSetupMockWithParams();
        //     var device = _baseTest.DbContext.Devices.FirstOrDefault();
        //     // Act
        //     var result = await _deviceService.GetDevicePhonesByUdidAsync(device.Udid);

        //     //Assert
        //     result.Should().NotBeNull();
        //     result.Should().BeOfType(typeof(DevicePhonesModel));
        // }

        [Fact]
        public async Task ShouldReturnExceptionOnGetDevicePhones()
        {
            // Arrange

            // Act
            Func<Task> result = async () => await _deviceService.GetDevicePhonesByUdidAsync(It.IsAny<string>(), It.IsAny<string>());

            //Assert
            await result.Should().ThrowAsync<ArgumentNullException>();

        }


    }
}
