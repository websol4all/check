using AutoFixture;
using FluentAssertions;
using Mapster;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Data.Repository;
using Xyzies.Devices.Services.Exceptions;
using Xyzies.Devices.Services.Helpers.Interfaces;
using Xyzies.Devices.Services.Models.DeviceHistory;
using Xyzies.Devices.Services.Requests.Device;
using Xyzies.Devices.Services.Service;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Tests.Unit_tests
{
    public class DeviceHistoryServiceTests : IClassFixture<BaseTest>
    {
        private readonly BaseTest _baseTest = null;

        private ILogger<DeviceHistoryService> _loggerMock;
        private Mock<IValidationHelper> _validationHelperMock;

        private readonly IDeviceHistoryService _deviceHistoryService = null;

        public DeviceHistoryServiceTests(BaseTest baseTest)
        {
            _baseTest = baseTest ?? throw new ArgumentNullException(nameof(baseTest));
            _baseTest.DbContext.ClearContext();

            _loggerMock = Mock.Of<ILogger<DeviceHistoryService>>();
            _validationHelperMock = new Mock<IValidationHelper>();

            _deviceHistoryService = new DeviceHistoryService(_loggerMock, new DeviceRepository(_baseTest.DbContext), _validationHelperMock.Object, new DeviceHistoryRepository(_baseTest.DbContext));
        }

        [Fact]
        public async Task ShouldReturnFailIfTokenNullWhenGetHistoryByDeviceId()
        {
            // Arrange
            var request = _baseTest.Fixture.Create<BaseDeviceRequest>();
            var deviceId = _baseTest.Fixture.Create<Guid>();

            // Act
            Func<Task> result = async () => await _deviceHistoryService.GetHistoryByDeviceId(null, deviceId);

            //Assert
            await result.Should().ThrowAsync<ArgumentNullException>();
        }

        [Fact]
        public async Task ShouldReturnFailIfDeviceNotFoundWhenGetHistoryByDeviceId()
        {
            // Arrange
            var request = _baseTest.Fixture.Create<BaseDeviceRequest>();
            var deviceId = _baseTest.Fixture.Create<Guid>();
            var token = _baseTest.Fixture.Create<string>();

            // Act
            Func<Task> result = async () => await _deviceHistoryService.GetHistoryByDeviceId(token, deviceId);

            //Assert
            await result.Should().ThrowAsync<KeyNotFoundException>();
        }

        [Fact]
        public async Task ShouldReturnFailIfSuperviserHasAnotherCompanyWhenGetHistoryByDeviceId()
        {
            // Arrange
            var request = _baseTest.Fixture.Create<BaseDeviceRequest>();
            int deviceCompanyId = 10;
            int userCompanyId = 20;
            var token = _baseTest.Fixture.Create<string>();
            var device = _baseTest.Fixture.Build<Device>().With(x => x.CompanyId, deviceCompanyId)
                                                .Without(x => x.DeviceHistory).Create();
            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.SaveChanges();

            _validationHelperMock.Setup(x => x.GetCompanyIdByPermission(token, It.IsAny<string[]>(), null)).ReturnsAsync(userCompanyId);
            // Act
            Func<Task> result = async () => await _deviceHistoryService.GetHistoryByDeviceId(token, device.Id);

            //Assert
            await result.Should().ThrowAsync<AccessException>();
        }

        [Fact]
        public async Task ShouldReturnSuccsessIfSuperviserHasDeviceCompanyWhenGetHistoryByDeviceId()
        {
            // Arrange
            var request = _baseTest.Fixture.Create<BaseDeviceRequest>();
            int userCompanyId = 10;
            int companyId = 20;
            int deviceHistoryWhithUserCompanyCount = 3;
            int deviceHistoryWithAnotherCompanyCount = 7;
            var token = _baseTest.Fixture.Create<string>();
            var device = _baseTest.Fixture.Build<Device>()
                                                .With(x => x.Id, Guid.NewGuid())
                                                .With(x => x.CompanyId, userCompanyId)
                                                .Without(x => x.DeviceHistory)
                                                .Create();
            var deviceHistoryList = _baseTest.Fixture.Build<DeviceHistory>().With(x => x.DeviceId, device.Id)
                                                                  .With(x => x.CompanyId, userCompanyId)
                                                                  .Without(x => x.Device)
                                                                  .CreateMany(deviceHistoryWhithUserCompanyCount).ToList();
            deviceHistoryList.AddRange(_baseTest.Fixture.Build<DeviceHistory>().With(x => x.Device, device)
                                                                     .With(x => x.CompanyId, companyId)
                                                                     .Without(x => x.Device)
                                                                     .CreateMany(deviceHistoryWithAnotherCompanyCount));

            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.DeviceHistory.AddRange(deviceHistoryList);
            _baseTest.DbContext.SaveChanges();

            _validationHelperMock.Setup(x => x.GetCompanyIdByPermission(token, It.IsAny<string[]>(), null)).ReturnsAsync(userCompanyId);
            // Act
            var result = await _deviceHistoryService.GetHistoryByDeviceId(token, device.Id);

            //Assert
            result.Result.Count().Should().Be(deviceHistoryWhithUserCompanyCount);
            result.Total.Should().Be(deviceHistoryWhithUserCompanyCount);
            result.Result.All(x => x.CompanyId == userCompanyId).Should().BeTrue();
            result.Result.Should().BeInDescendingOrder(x => x.CreatedOn);
        }

        [Fact]
        public async Task ShouldReturnSuccsessForAdminWhenGetHistoryByDeviceId()
        {
            // Arrange
            var request = _baseTest.Fixture.Create<BaseDeviceRequest>();
            int firstCompanyId = 10;
            int secondCompanyId = 20;
            int deviceHistoryWhithUserCompanyCount = 3;
            int deviceHistoryWithAnotherCompanyCount = 7;
            var token = _baseTest.Fixture.Create<string>();
            var device = _baseTest.Fixture.Build<Device>()
                                                .With(x => x.CompanyId, firstCompanyId)
                                                .With(x => x.Id, Guid.NewGuid())
                                                .Without(x => x.DeviceHistory)
                                                .Create();
            var deviceHistoryList = _baseTest.Fixture.Build<DeviceHistory>().With(x => x.DeviceId, device.Id)
                                                                  .With(x => x.CompanyId, firstCompanyId)
                                                                  .Without(x => x.Device)
                                                                  .CreateMany(deviceHistoryWhithUserCompanyCount)
                                                                  .ToList();
            deviceHistoryList.AddRange(_baseTest.Fixture.Build<DeviceHistory>().With(x => x.DeviceId, device.Id)
                                                                     .With(x => x.CompanyId, secondCompanyId)
                                                                     .Without(x => x.Device)
                                                                     .CreateMany(deviceHistoryWithAnotherCompanyCount));

            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.DeviceHistory.AddRange(deviceHistoryList);
            _baseTest.DbContext.SaveChanges();


            _validationHelperMock.Setup(x => x.GetCompanyIdByPermission(token, It.IsAny<string[]>(), null)).ReturnsAsync((int?)null);
            // Act
            var result = await _deviceHistoryService.GetHistoryByDeviceId(token, device.Id);

            //Assert
            result.Result.Count().Should().Be(_baseTest.DbContext.DeviceHistory.Count());
            result.Total.Should().Be(_baseTest.DbContext.DeviceHistory.Count());
            result.Result.Should().BeInDescendingOrder(x => x.CreatedOn);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(3, 3)]
        public async Task ShouldReturnSuccsessWhithLazyLoadingWhenGetHistoryByDeviceId(int skip, int take)
        {
            // Arrange
            var request = _baseTest.Fixture.Create<BaseDeviceRequest>();
            int firstCompanyId = 10;
            int secondCompanyId = 20;
            int deviceHistoryWhithUserCompanyCount = 3;
            int deviceHistoryWithAnotherCompanyCount = 7;
            var token = _baseTest.Fixture.Create<string>();
            var device = _baseTest.Fixture.Build<Device>()
                                                .With(x => x.Id, Guid.NewGuid())
                                                .With(x => x.CompanyId, firstCompanyId)
                                                .Without(x => x.DeviceHistory)
                                                .Create();
            var deviceHistoryList = _baseTest.Fixture.Build<DeviceHistory>().With(x => x.DeviceId, device.Id)
                                                                  .With(x => x.CompanyId, firstCompanyId)
                                                                  .Without(x => x.Device)
                                                                  .CreateMany(deviceHistoryWhithUserCompanyCount).ToList();
            deviceHistoryList.AddRange(_baseTest.Fixture.Build<DeviceHistory>().With(x => x.DeviceId, device.Id)
                                                                     .With(x => x.CompanyId, secondCompanyId)
                                                                     .Without(x => x.Device)
                                                                     .CreateMany(deviceHistoryWithAnotherCompanyCount));

            _baseTest.DbContext.Devices.Add(device);
            _baseTest.DbContext.DeviceHistory.AddRange(deviceHistoryList);
            _baseTest.DbContext.SaveChanges();

            var filters = new LazyLoadParameters { Offset = skip, Limit = take };

            _validationHelperMock.Setup(x => x.GetCompanyIdByPermission(token, It.IsAny<string[]>(), null)).ReturnsAsync((int?)null);
            // Act
            var result = await _deviceHistoryService.GetHistoryByDeviceId(token, device.Id, filters);

            //Assert
            result.Total.Should().Be(_baseTest.DbContext.DeviceHistory.Count());
            _baseTest.DbContext.DeviceHistory.Skip(skip).Take(take).Count().Should().Be(result.Result.Count());
            result.Offset.Should().Be(skip);
            result.Limit.Should().Be(take);
        }
    }
}
