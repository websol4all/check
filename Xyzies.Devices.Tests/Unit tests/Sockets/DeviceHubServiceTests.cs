using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AutoFixture;

using FluentAssertions;

using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Moq;

using StackExchange.Redis;

using Xunit;

using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Data.Repository;
using Xyzies.Devices.Services.Common.Cache;
using Xyzies.Devices.Services.Helpers.Interfaces;
using Xyzies.Devices.Services.Models;
using Xyzies.Devices.Services.Models.DeviceSocket;
using Xyzies.Devices.Services.Models.Tenant;
using Xyzies.Devices.Services.Service;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Tests.Unit_tests.Sockets
{
    public class DeviceHubServiceTests : IClassFixture<BaseTest>
    {
        private Mock<IBackgroundTaskQueue> _backGroundTaskMock;
        private Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private Mock<IConnectionMultiplexer> mockMultiplexer;
        private RedisStore redisCache;
        private Mock<IHubContext<WebHubService>> webHubContext;
        private Mock<IDatabase> redisDataBaseMock;
        private Mock<BadDisconnectSocketService> _badDisconnectSocketServiceMock;
        private Mock<INotificationSender> _notificationSenderExtentionMock;

        private IMemoryCache _memoryCache = null;
        private readonly BaseTest _baseTest = null;

        public DeviceHubServiceTests(BaseTest baseTest)
        {
            _baseTest = baseTest ??
                throw new ArgumentNullException(nameof(baseTest));

            _memoryCache = baseTest.memoryCache ??
                throw new ArgumentNullException(nameof(baseTest));
            _baseTest.DbContext.ClearContext();

            _backGroundTaskMock = new Mock<IBackgroundTaskQueue>();
            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

            webHubContext = new Mock<IHubContext<WebHubService>>();

            var hubClients = new Mock<IHubClients>();
            var clientCallbacks = new Mock<IClientProxy>();

            webHubContext.Setup(h => h.Clients)
                .Returns(hubClients.Object);

            hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(clientCallbacks.Object);
            hubClients.Setup(c => c.All).Returns(clientCallbacks.Object);
            mockMultiplexer = new Mock<IConnectionMultiplexer>();

            mockMultiplexer.Setup(_ => _.IsConnected).Returns(false);

            redisDataBaseMock = new Mock<IDatabase>();

            mockMultiplexer
                .Setup(_ => _.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(redisDataBaseMock.Object);

            var mockSubscr = new Mock<ISubscriber>();

            Action<RedisChannel, RedisValue> handler = (asd, sda) => { };

            mockSubscr.Setup(x => x.Subscribe(It.IsAny<RedisChannel>(), handler, CommandFlags.None));

            mockMultiplexer.Setup(x => x.GetSubscriber(It.IsAny<object>())).Returns(mockSubscr.Object);

            _notificationSenderExtentionMock = new Mock<INotificationSender>();

            RedisConnection.SetConnectionMultiplexer(mockMultiplexer.Object);
            redisCache = new RedisStore(_backGroundTaskMock.Object, _serviceScopeFactoryMock.Object);

            var loggerMock = Mock.Of<ILogger<BadDisconnectSocketService>>();

            _badDisconnectSocketServiceMock = new Mock<BadDisconnectSocketService>(loggerMock);
        }

        [Fact]
        public async Task ShouldCreatedDeviceHistoryBySendUpdateLocationAsyncFromDevice()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();

            var userId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var deviceIdIndb = Guid.NewGuid();

            var deviceUpdate = _baseTest.Fixture.Build<DeviceUpdateLocationModel>()
                .With(x => x.Lat, 48.419431)
                .With(x => x.Long, 35.128651)
                .Create();

            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceIdIndb,
                LoggedInUserId = userId
            };

            var deviceFromDb = new Device()
            {
                Latitude = 48.423659,
                Longitude = 35.121916,
                BranchId = branchId,
                CompanyId = 123123,
                Radius = 1000,
                Udid = UDID,
                Id = deviceIdIndb,
                DeviceHistory = new [] { deviceHistory }
            };

            var loggerMock = Mock.Of<ILogger<DeviceHubService>>();

            var deviceService = new Mock<IDeviceService>();
            var webHubService = new Mock<IHubContext<WebHubService>>();
            deviceService.Setup(x => x.GetDeviceByUdidAsync(UDID)).ReturnsAsync(deviceFromDb);

            redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true);
            redisDataBaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync((RedisValue)UDID);

            var context = new Mock<HubCallerContext>();

            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            deviceHistoryRepository.Add(deviceHistory);

            context.Setup(x => x.ConnectionId).Returns(contextId);

            var webHubContext = new Mock<IHubContext<WebHubService>>();

            var hubClients = new Mock<IHubClients>();
            var clientCallbacks = new Mock<IClientProxy>();

            webHubContext.Setup(h => h.Clients)
                .Returns(hubClients.Object);
            hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(clientCallbacks.Object);

            var socketHubService = new DeviceHubService(loggerMock,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            socketHubService.Context = context.Object;

            // Act

            await socketHubService.UpdateLocation(deviceUpdate);

            //Assert

            _baseTest.DbContext.DeviceHistory.Count().Should().Be(2);
            var deviceHistoryExpend = _baseTest.DbContext.DeviceHistory.OrderByDescending(x => x.CreatedOn).First();
            deviceHistoryExpend.IsInLocation.Should().Be(true);
            deviceHistoryExpend.IsOnline.Should().Be(true);
            deviceHistoryExpend.LoggedInUserId.Should().Be(userId);
            deviceHistoryExpend.CompanyId.Should().Be(deviceFromDb.CompanyId);
            deviceHistoryExpend.DeviceRadius.Should().Be(deviceFromDb.Radius);
        }

        [Fact]
        public async Task ShouldNotCreatedDeviceHistoryBySendUpdateLocationAsyncFromDeviceIfDifferenceSmallerWhen10Meters()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();

            var userId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var deviceIdIndb = Guid.NewGuid();

            var deviceUpdate = _baseTest.Fixture.Build<DeviceUpdateLocationModel>()
                .With(x => x.Lat, 48.419431)
                .With(x => x.Long, 35.128651)
                .Create();

            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceIdIndb,
                LoggedInUserId = userId,
                CurrentDeviceLocationLatitude = 48.419432,
                CurrentDeviceLocationLongitude = 35.128652,
                IsInLocation = true
            };

            var deviceFromDb = new Device()
            {
                Latitude = 48.423659,
                Longitude = 35.121916,
                BranchId = branchId,
                CompanyId = 123123,
                Radius = 1000,
                Udid = UDID,
                Id = deviceIdIndb,
                DeviceHistory = new [] { deviceHistory }
            };

            var loggerMock = Mock.Of<ILogger<DeviceHubService>>();

            var deviceService = new Mock<IDeviceService>();
            var webHubService = new Mock<IHubContext<WebHubService>>();
            deviceService.Setup(x => x.GetDeviceByUdidAsync(UDID)).ReturnsAsync(deviceFromDb);

            var context = new Mock<HubCallerContext>();

            redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true);
            redisDataBaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync((RedisValue)UDID);

            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            deviceHistoryRepository.Add(deviceHistory);

            context.Setup(x => x.ConnectionId).Returns(contextId);

            var webHubContext = new Mock<IHubContext<WebHubService>>();

            var hubClients = new Mock<IHubClients>();
            var clientCallbacks = new Mock<IClientProxy>();

            webHubContext.Setup(h => h.Clients)
                .Returns(hubClients.Object);
            hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(clientCallbacks.Object);

            var socketHubService = new DeviceHubService(loggerMock,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);
            socketHubService.Context = context.Object;

            // Act
            await socketHubService.UpdateLocation(deviceUpdate);

            //Assert

            _baseTest.DbContext.DeviceHistory.Count().Should().Be(1);
        }

        [Fact]
        public async Task ShouldNotFoundHistoryBySendUpdateLocationAsyncFromDevice()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();
            string UDIDnew = _baseTest.Fixture.Create<string>();

            var userId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var deviceIdIndb = Guid.NewGuid();

            var deviceUpdate = _baseTest.Fixture.Build<DeviceUpdateLocationModel>()
                .With(x => x.Lat, 48.419431)
                .With(x => x.Long, 35.128651)
                .Create();

            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceIdIndb,
                LoggedInUserId = userId
            };

            var loggerMock = Mock.Of<ILogger<DeviceHubService>>();

            var deviceService = new Mock<IDeviceService>();
            var webHubService = new Mock<IHubContext<WebHubService>>();
            deviceService.Setup(x => x.GetDeviceByUdidAsync(UDIDnew)).Returns(Task.FromResult<Device>(null));

            var context = new Mock<HubCallerContext>();
            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            deviceHistoryRepository.Add(deviceHistory);

            redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(false);
            redisDataBaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync((RedisValue)UDID);

            context.Setup(x => x.ConnectionId).Returns(contextId);
            webHubService.Setup(x => x.Clients.Group(It.IsAny<string>())).Returns(Mock.Of<IClientProxy>());

            var socketHubService = new DeviceHubService(loggerMock,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            socketHubService.Context = context.Object;

            // Act
            await socketHubService.UpdateLocation(deviceUpdate);

            //Assert

            _baseTest.DbContext.DeviceHistory.Count().Should().Be(1);
        }

        [Fact]
        public async Task ShouldNotAddHistoryBySendUpdateLocationAsyncFromDevice()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();
            string UDIDnew = _baseTest.Fixture.Create<string>();

            var userId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var deviceIdIndb = Guid.NewGuid();

            var deviceUpdate = _baseTest.Fixture.Build<DeviceUpdateLocationModel>()
                .With(x => x.Lat, 48.419431)
                .With(x => x.Long, 35.128651)
                .Create();

            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceIdIndb,
                LoggedInUserId = userId,
                IsInLocation = true,
                CreatedOn = DateTime.Now
            };

            var deviceFromDb = new Device()
            {
                Latitude = 48.419431,
                Longitude = 35.128651,
                BranchId = branchId,
                CompanyId = 123123,
                Radius = 1000,
                Udid = UDID,
                Id = deviceIdIndb,
                DeviceHistory = new [] { deviceHistory }
            };

            var loggerMock = Mock.Of<ILogger<DeviceHubService>>();

            var deviceService = new Mock<IDeviceService>();
            var webHubService = new Mock<IHubContext<WebHubService>>();
            deviceService.Setup(x => x.GetDeviceByUdidAsync(UDID)).ReturnsAsync(deviceFromDb);

            var context = new Mock<HubCallerContext>();
            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            deviceHistoryRepository.Add(deviceHistory);

            redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true);
            redisDataBaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync((RedisValue)UDID);

            context.Setup(x => x.ConnectionId).Returns(contextId);
            webHubService.Setup(x => x.Clients.Group(It.IsAny<string>())).Returns(Mock.Of<IClientProxy>());

            var socketHubService = new DeviceHubService(loggerMock,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            socketHubService.Context = context.Object;

            // Act
            await socketHubService.UpdateLocation(deviceUpdate);

            //Assert

            _baseTest.DbContext.DeviceHistory.Count().Should().Be(1);
        }

        [Fact]
        public async Task ShouldLoggingMethodCallIfNotFoundConnectionOnUpdateLocation()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();

            var deviceUpdate = _baseTest.Fixture.Build<DeviceUpdateLocationModel>()
                .With(x => x.Lat, 48.419431)
                .With(x => x.Long, 35.128651)
                .Create();

            redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(false);

            var loggerMock = new Mock<ILogger<DeviceHubService>>();
            var deviceService = new Mock<IDeviceService>();

            var context = new Mock<HubCallerContext>();
            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            var webHubService = new Mock<IHubContext<WebHubService>>();
            context.Setup(x => x.ConnectionId).Returns(contextId);

            var socketHubService = new DeviceHubService(loggerMock.Object,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            socketHubService.Context = context.Object;

            // Act
            await socketHubService.UpdateLocation(deviceUpdate);

            //Assert
            loggerMock.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task ShouldCreatedDeviceHistoryBySendUpdateSalesRepAsyncFromDevice()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();

            var userId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var deviceIdIndb = Guid.NewGuid();
            var newSalesRep = Guid.NewGuid();

            var deviceUpdate = _baseTest.Fixture.Build<DeviceUpdateSalesRepModel>()
                .With(x => x.SalesRepId, newSalesRep)
                .Create();

            var deviceFromDb = new Device()
            {
                Latitude = 48.423659,
                Longitude = 35.121916,
                BranchId = branchId,
                CompanyId = 123123,
                Radius = 500,
                Udid = UDID,
                Id = deviceIdIndb
            };

            redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true);

            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceIdIndb,
                LoggedInUserId = userId
            };

            var loggerMock = Mock.Of<ILogger<DeviceHubService>>();

            var deviceService = new Mock<IDeviceService>();
            deviceService.Setup(x => x.GetDeviceByUdidAsync(UDID)).ReturnsAsync(deviceFromDb);

            var context = new Mock<HubCallerContext>();
            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            deviceHistoryRepository.Add(deviceHistory);

            redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true);
            redisDataBaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync((RedisValue)UDID);

            context.Setup(x => x.ConnectionId).Returns(contextId);

            var webHubContext = new Mock<IHubContext<WebHubService>>();

            var hubClients = new Mock<IHubClients>();
            var clientCallbacks = new Mock<IClientProxy>();

            webHubContext.Setup(h => h.Clients)
                .Returns(hubClients.Object);

            hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(clientCallbacks.Object);

            var socketHubService = new DeviceHubService(loggerMock,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            socketHubService.Context = context.Object;

            // Act
            await socketHubService.UpdateSalesRep(deviceUpdate);

            //Assert

            _baseTest.DbContext.DeviceHistory.Count().Should().Be(2);
            var deviceHistoryExpend = _baseTest.DbContext.DeviceHistory.Last();

            deviceHistoryExpend.LoggedInUserId.Should().Be(newSalesRep);
        }

        [Fact]
        public async Task ShouldNotFoundHistoryBySendUpdateSalesRepAsyncFromDevice()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();

            var userId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var deviceIdIndb = Guid.NewGuid();
            var newSalesRep = Guid.NewGuid();

            var deviceUpdate = _baseTest.Fixture.Build<DeviceUpdateSalesRepModel>()
                .With(x => x.SalesRepId, newSalesRep)
                .Create();

            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceIdIndb,
                LoggedInUserId = userId
            };

            var loggerMock = Mock.Of<ILogger<DeviceHubService>>();
            var deviceService = new Mock<IDeviceService>();
            deviceService.Setup(x => x.GetDeviceByUdidAsync(UDID)).Returns(Task.FromResult<Device>(null));

            var context = new Mock<HubCallerContext>();
            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            var webHubService = new Mock<IHubContext<WebHubService>>();
            deviceHistoryRepository.Add(deviceHistory);

            context.Setup(x => x.ConnectionId).Returns(contextId);

            var socketHubService = new DeviceHubService(loggerMock,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            socketHubService.Context = context.Object;

            // Act
            await socketHubService.UpdateSalesRep(deviceUpdate);

            //Assert

            _baseTest.DbContext.DeviceHistory.Count().Should().Be(1);
        }

        [Fact]
        public async Task ShouldNotUpdateDeviceHistoryBySendUpdateSalesRepAsyncFromDeviceIfSalesRepSame()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();

            var userId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var deviceIdIndb = Guid.NewGuid();

            var deviceUpdate = _baseTest.Fixture.Build<DeviceUpdateSalesRepModel>()
                .With(x => x.SalesRepId, userId)
                .Create();

            redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true);

            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceIdIndb,
                LoggedInUserId = userId
            };
            var deviceFromDb = new Device()
            {
                Latitude = 48.423659,
                Longitude = 35.121916,
                BranchId = branchId,
                CompanyId = 123123,
                Radius = 500,
                Udid = UDID,
                Id = deviceIdIndb,
                DeviceHistory = new [] { deviceHistory }
            };

            var loggerMock = Mock.Of<ILogger<DeviceHubService>>();

            var deviceService = new Mock<IDeviceService>();
            deviceService.Setup(x => x.GetDeviceByUdidAsync(UDID)).ReturnsAsync(deviceFromDb);

            var context = new Mock<HubCallerContext>();
            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            deviceHistoryRepository.Add(deviceHistory);

            redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true);
            redisDataBaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync((RedisValue)UDID);

            context.Setup(x => x.ConnectionId).Returns(contextId);

            var webHubContext = new Mock<IHubContext<WebHubService>>();

            var hubClients = new Mock<IHubClients>();
            var clientCallbacks = new Mock<IClientProxy>();

            webHubContext.Setup(h => h.Clients)
                .Returns(hubClients.Object);

            hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(clientCallbacks.Object);

            var socketHubService = new DeviceHubService(loggerMock,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            socketHubService.Context = context.Object;

            // Act

            await socketHubService.UpdateSalesRep(deviceUpdate);

            //Assert

            _baseTest.DbContext.DeviceHistory.Count().Should().Be(1);
            var deviceHistoryExpend = _baseTest.DbContext.DeviceHistory.Last();

            deviceHistoryExpend.LoggedInUserId.Should().Be(userId);
        }

        [Fact]
        public async Task ShouldConnectedToSocketAndAddDeviceHistory()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();

            var userId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var deviceIdIndb = Guid.NewGuid();
            var newSalesRep = Guid.NewGuid();

            var deviceFromDb = new Device()
            {
                Latitude = 48.423659,
                Longitude = 35.121916,
                BranchId = branchId,
                CompanyId = 123123,
                Radius = 500,
                Udid = UDID,
                Id = deviceIdIndb
            };

            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceIdIndb,
                LoggedInUserId = userId,
                IsOnline = true,
                IsInLocation = true
            };

            var loggerMock = Mock.Of<ILogger<DeviceHubService>>();
            var deviceService = new Mock<IDeviceService>();
            deviceService.Setup(x => x.GetDeviceByUdidAsync(UDID)).ReturnsAsync(deviceFromDb);

            var context = new Mock<HubCallerContext>();
            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            var webHubService = new Mock<IHubContext<WebHubService>>();
            deviceHistoryRepository.Add(deviceHistory);

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(x => x.Request.Query["udid"]).Returns(UDID);

            var heartbeatFeatureMock = new Mock<IConnectionHeartbeatFeature>();

            context.Setup(x => x.ConnectionId).Returns(contextId);
            context.Setup(x => x.Features.Get<IHttpContextFeature>().HttpContext).Returns(httpContextMock.Object);
            context.Setup(x => x.Features.Get<IConnectionHeartbeatFeature>()).Returns(heartbeatFeatureMock.Object);

            var socketHubService = new DeviceHubService(loggerMock,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            socketHubService.Context = context.Object;

            // Act
            await socketHubService.OnConnectedAsync();

            //Assert
            _baseTest.DbContext.DeviceHistory.Count().Should().Be(2);
            _baseTest.DbContext.DeviceHistory.LastOrDefault().DeviceId.Should().Be(deviceIdIndb);
            _baseTest.DbContext.DeviceHistory.LastOrDefault().IsOnline.Should().Be(true);
            _baseTest.DbContext.DeviceHistory.LastOrDefault().IsInLocation.Should().Be(true);
        }

        //[Fact]
        //public async Task ShouldConnectedToSocketAndAddDeviceHistoryHaveSemiConnections()
        //{
        //    // Arrange
        //    string contextId = _baseTest.Fixture.Create<string>();
        //    string UDID = _baseTest.Fixture.Create<string>();

        //    var userId = Guid.NewGuid();
        //    var branchId = Guid.NewGuid();
        //    var deviceIdIndb = Guid.NewGuid();
        //    var newSalesRep = Guid.NewGuid();

        //    var deviceFromDb = new Device()
        //    {
        //        Latitude = 48.423659,
        //        Longitude = 35.121916,
        //        BranchId = branchId,
        //        CompanyId = 123123,
        //        Radius = 500,
        //        Udid = UDID,
        //        Id = deviceIdIndb
        //    };

        //    var deviceHistory = new DeviceHistory()
        //    {
        //        DeviceId = deviceIdIndb,
        //        LoggedInUserId = userId,
        //        IsOnline = true,
        //        IsInLocation = true
        //    };

        //    var loggerMock = Mock.Of<ILogger<DeviceHubService>>();
        //    var deviceService = new Mock<IDeviceService>();
        //    deviceService.Setup(x => x.GetDeviceByUdidAsync(UDID)).ReturnsAsync(deviceFromDb);

        //    var context = new Mock<HubCallerContext>();
        //    var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
        //    var webHubService = new Mock<IHubContext<WebHubService>>();
        //    deviceHistoryRepository.Add(deviceHistory);

        //    var httpContextMock = new Mock<HttpContext>();
        //    httpContextMock.Setup(x => x.Request.Query["udid"]).Returns(UDID);

        //    var heartbeatFeatureMock = new Mock<IConnectionHeartbeatFeature>();

        //    context.Setup(x => x.ConnectionId).Returns(contextId);
        //    context.Setup(x => x.Features.Get<IHttpContextFeature>().HttpContext).Returns(httpContextMock.Object);
        //    context.Setup(x => x.Features.Get<IConnectionHeartbeatFeature>()).Returns(heartbeatFeatureMock.Object);

        //    var socketHubService = new DeviceHubService(loggerMock,
        //        deviceHistoryRepository,
        //        deviceService.Object,
        //        webHubContext.Object,
        //        redisCache,
        //        _backGroundTaskMock.Object,
        //        _badDisconnectSocketServiceMock.Object,
        //       _memoryCache,
        //        _notificationSenderExtentionMock.Object);

        //    socketHubService.Context = context.Object;
        //    // Act
        //    await socketHubService.OnConnectedAsync();

        //    //Assert
        //    _baseTest.DbContext.DeviceHistory.Count().Should().Be(2);
        //     redisCache.RedisCache.KeyExists(UDID).Should().Be(true);
        //    _baseTest.DbContext.DeviceHistory.LastOrDefault().DeviceId.Should().Be(deviceIdIndb);
        //    _baseTest.DbContext.DeviceHistory.LastOrDefault().IsOnline.Should().Be(true);
        //    _baseTest.DbContext.DeviceHistory.LastOrDefault().IsInLocation.Should().Be(true);

        //    redisCache.RedisCache.KeyDelete(UDID);
        //}

        [Fact]
        public async Task ShouldDisconnectSocketAndAddDeviceHistory()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = contextId;

            var userId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var deviceIdIndb = Guid.NewGuid();
            var newSalesRep = Guid.NewGuid();

            var serviceProvider = _baseTest.Fixture.Create<TenantModel>();
            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceIdIndb,
                LoggedInUserId = userId,
                IsOnline = false,
                IsInLocation = false
            };

            var deviceFromDb = new Device()
            {
                Latitude = 48.423659,
                Longitude = 35.121916,
                BranchId = branchId,
                CompanyId = 123123,
                Radius = 500,
                Udid = UDID,
                Id = deviceIdIndb,
                DeviceHistory = new List<DeviceHistory>() { deviceHistory }
            };

            redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true);
            redisDataBaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(UDID);

            var loggerMock = Mock.Of<ILogger<DeviceHubService>>();
            var deviceService = new Mock<IDeviceService>();
            deviceService.Setup(x => x.GetDeviceByUdidAsync(UDID)).ReturnsAsync(deviceFromDb);

            var context = new Mock<HubCallerContext>();
            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            var webHubService = new Mock<IHubContext<WebHubService>>();
            deviceHistoryRepository.Add(deviceHistory);

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(x => x.Request.Query["udid"]).Returns(UDID);

            var heartbeatFeatureMock = new Mock<IConnectionHeartbeatFeature>();

            context.Setup(x => x.ConnectionId).Returns(contextId);
            context.Setup(x => x.Features.Get<IHttpContextFeature>().HttpContext).Returns(httpContextMock.Object);
            context.Setup(x => x.Features.Get<IConnectionHeartbeatFeature>()).Returns(heartbeatFeatureMock.Object);

            var socketHubService = new DeviceHubService(loggerMock,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            socketHubService.Context = context.Object;

            // Act
            await socketHubService.OnDisconnectedAsync(new Exception());

            //Assert
            _baseTest.DbContext.DeviceHistory.Count().Should().Be(2);
            _baseTest.DbContext.DeviceHistory.LastOrDefault().DeviceId.Should().Be(deviceIdIndb);
            _baseTest.DbContext.DeviceHistory.LastOrDefault().IsOnline.Should().Be(false);
            _baseTest.DbContext.DeviceHistory.LastOrDefault().IsInLocation.Should().Be(false);
        }

        //[Fact]
        //public async Task ShouldDisconnectSocketAndNotAddDeviceHistoryIfHaveMoreOneConnection()
        //{
        //    // Arrange
        //    string contextId = _baseTest.Fixture.Create<string>();
        //    string UDID = _baseTest.Fixture.Create<string>();

        //    var userId = Guid.NewGuid();
        //    var branchId = Guid.NewGuid();
        //    var deviceIdIndb = Guid.NewGuid();
        //    var newSalesRep = Guid.NewGuid();

        //    var deviceHistory = new DeviceHistory()
        //    {
        //        DeviceId = deviceIdIndb,
        //        LoggedInUserId = userId,
        //        IsOnline = false,
        //        IsInLocation = false
        //    };

        //    var deviceFromDb = new Device()
        //    {
        //        Latitude = 48.423659,
        //        Longitude = 35.121916,
        //        BranchId = branchId,
        //        CompanyId = 123123,
        //        Radius = 500,
        //        Udid = UDID,
        //        Id = deviceIdIndb,
        //        DeviceHistory = new List<DeviceHistory>() { deviceHistory }
        //    };

        //    redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true);
        //    redisDataBaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(UDID);

        //    var loggerMock = Mock.Of<ILogger<DeviceHubService>>();
        //    var deviceService = new Mock<IDeviceService>();
        //    deviceService.Setup(x => x.GetDeviceByUdidAsync(UDID)).ReturnsAsync(deviceFromDb);

        //    var context = new Mock<HubCallerContext>();
        //    var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
        //    var webHubService = new Mock<IHubContext<WebHubService>>();
        //    deviceHistoryRepository.Add(deviceHistory);

        //    var httpContextMock = new Mock<HttpContext>();
        //    httpContextMock.Setup(x => x.Request.Query["udid"]).Returns(UDID);

        //    var heartbeatFeatureMock = new Mock<IConnectionHeartbeatFeature>();

        //    context.Setup(x => x.ConnectionId).Returns(contextId);
        //    context.Setup(x => x.Features.Get<IHttpContextFeature>().HttpContext).Returns(httpContextMock.Object);
        //    context.Setup(x => x.Features.Get<IConnectionHeartbeatFeature>()).Returns(heartbeatFeatureMock.Object);

        //    var socketHubService = new DeviceHubService(loggerMock,
        //        deviceHistoryRepository,
        //        deviceService.Object,
        //        webHubContext.Object,
        //        redisCache,
        //        _backGroundTaskMock.Object,
        //        _badDisconnectSocketServiceMock.Object,
        //        _memoryCache,
        //        _notificationSenderExtentionMock.Object);

        //    socketHubService.Context = context.Object;

        //    // Act
        //    await socketHubService.OnDisconnectedAsync(new Exception());

        //    //Assert
        //    _baseTest.DbContext.DeviceHistory.Count().Should().Be(1);

        //}

        [Fact]
        public async Task ShouldConnectDeviceFirstTime()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();

            var userId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var deviceIdIndb = Guid.NewGuid();
            var newSalesRep = Guid.NewGuid();

            var deviceFromDb = new Device()
            {
                Latitude = 48.423659,
                Longitude = 35.121916,
                BranchId = branchId,
                CompanyId = 123123,
                Radius = 1000,
                Udid = UDID,
                Id = deviceIdIndb
            };

            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceIdIndb,
                LoggedInUserId = userId,
                IsOnline = true,
                IsInLocation = true
            };

            var loggerMock = Mock.Of<ILogger<DeviceHubService>>();
            var deviceService = new Mock<IDeviceService>();
            deviceService.SetupSequence(x => x.GetDeviceByUdidAsync(UDID))
                .ReturnsAsync(null)
                .ReturnsAsync(deviceFromDb);

            var context = new Mock<HubCallerContext>();
            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(x => x.Request.Query["udid"]).Returns(UDID);

            var heartbeatFeatureMock = new Mock<IConnectionHeartbeatFeature>();

            context.Setup(x => x.ConnectionId).Returns(contextId);
            context.Setup(x => x.Features.Get<IHttpContextFeature>().HttpContext).Returns(httpContextMock.Object);
            context.Setup(x => x.Features.Get<IConnectionHeartbeatFeature>()).Returns(heartbeatFeatureMock.Object);

            var socketHubService = new DeviceHubService(loggerMock,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            socketHubService.Context = context.Object;

            // Act
            await socketHubService.OnConnectedAsync();

            //Assert
            _baseTest.DbContext.DeviceHistory.Count().Should().Be(1);
            _baseTest.DbContext.DeviceHistory.LastOrDefault().DeviceId.Should().Be(deviceIdIndb);
            _baseTest.DbContext.DeviceHistory.LastOrDefault().IsOnline.Should().Be(true);
            _baseTest.DbContext.DeviceHistory.LastOrDefault().IsInLocation.Should().Be(true);
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenKeyIsNotExistAsyncFromDevice()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();

            var userId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var deviceIdIndb = Guid.NewGuid();

            var deviceUpdate = _baseTest.Fixture.Build<DeviceUpdateLocationModel>()
                .With(x => x.Lat, 48.419431)
                .With(x => x.Long, 35.128651)
                .Create();

            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceIdIndb,
                LoggedInUserId = userId
            };

            var deviceFromDb = new Device()
            {
                Latitude = 48.423659,
                Longitude = 35.121916,
                BranchId = branchId,
                CompanyId = 123123,
                Radius = 1000,
                Udid = UDID,
                Id = deviceIdIndb,
                DeviceHistory = new [] { deviceHistory }
            };

            var loggerMock = Mock.Of<ILogger<DeviceHubService>>();

            var deviceService = new Mock<IDeviceService>();
            var webHubService = new Mock<IHubContext<WebHubService>>();
            deviceService.Setup(x => x.GetDeviceByUdidAsync(UDID)).ReturnsAsync(deviceFromDb);

            redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).Throws(new Exception());

            var context = new Mock<HubCallerContext>();

            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            deviceHistoryRepository.Add(deviceHistory);

            context.Setup(x => x.ConnectionId).Returns(contextId);

            var webHubContext = new Mock<IHubContext<WebHubService>>();

            var hubClients = new Mock<IHubClients>();
            var clientCallbacks = new Mock<IClientProxy>();

            webHubContext.Setup(h => h.Clients)
                .Returns(hubClients.Object);
            hubClients.Setup(c => c.Group(It.IsAny<string>())).Returns(clientCallbacks.Object);

            var socketHubService = new DeviceHubService(loggerMock,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            socketHubService.Context = context.Object;

            // Act

            //Assert

            await Assert.ThrowsAsync<Exception>(async() => await socketHubService.UpdateLocation(deviceUpdate));
        }

        [Fact]
        public async Task ShouldThrowExceptionDisconnectSocket()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = contextId;

            var userId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var deviceIdIndb = Guid.NewGuid();
            var newSalesRep = Guid.NewGuid();

            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceIdIndb,
                LoggedInUserId = userId,
                IsOnline = false,
                IsInLocation = false
            };

            var deviceFromDb = new Device()
            {
                Latitude = 48.423659,
                Longitude = 35.121916,
                BranchId = branchId,
                CompanyId = 123123,
                Radius = 500,
                Udid = UDID,
                Id = deviceIdIndb,
                DeviceHistory = new List<DeviceHistory>() { deviceHistory }
            };

            redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(true);
            redisDataBaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(UDID);

            var loggerMock = new Mock<ILogger<DeviceHubService>>();
            var deviceService = new Mock<IDeviceService>();
            deviceService.SetupSequence(x => x.GetDeviceByUdidAsync(UDID)).ReturnsAsync(null);

            var context = new Mock<HubCallerContext>();
            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            var webHubService = new Mock<IHubContext<WebHubService>>();
            deviceHistoryRepository.Add(deviceHistory);

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(x => x.Request.Query["udid"]).Returns(UDID);

            var heartbeatFeatureMock = new Mock<IConnectionHeartbeatFeature>();

            context.Setup(x => x.ConnectionId).Returns(contextId);
            context.Setup(x => x.Features.Get<IHttpContextFeature>().HttpContext).Returns(httpContextMock.Object);
            context.Setup(x => x.Features.Get<IConnectionHeartbeatFeature>()).Returns(heartbeatFeatureMock.Object);

            var socketHubService = new DeviceHubService(loggerMock.Object,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            socketHubService.Context = context.Object;

            // Act
            await socketHubService.OnDisconnectedAsync(new Exception());

            //Assert
            loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task ShouldKeyIsNotExistAsyncFromDeviceDisconnectSocket()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();

            var userId = Guid.NewGuid();
            var branchId = Guid.NewGuid();
            var deviceIdIndb = Guid.NewGuid();
            var newSalesRep = Guid.NewGuid();

            var deviceHistory = new DeviceHistory()
            {
                DeviceId = deviceIdIndb,
                LoggedInUserId = userId,
                IsOnline = false,
                IsInLocation = false
            };

            var deviceFromDb = new Device()
            {
                Latitude = 48.423659,
                Longitude = 35.121916,
                BranchId = branchId,
                CompanyId = 123123,
                Radius = 500,
                Udid = UDID,
                Id = deviceIdIndb,
                DeviceHistory = new List<DeviceHistory>() { deviceHistory }
            };

            redisDataBaseMock.Setup(x => x.KeyExistsAsync(It.IsAny<RedisKey>(), CommandFlags.None)).ReturnsAsync(false);

            var loggerMock = new Mock<ILogger<DeviceHubService>>();
            var deviceService = new Mock<IDeviceService>();
            deviceService.SetupSequence(x => x.GetDeviceByUdidAsync(UDID)).ReturnsAsync(null);

            var context = new Mock<HubCallerContext>();
            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            var webHubService = new Mock<IHubContext<WebHubService>>();
            deviceHistoryRepository.Add(deviceHistory);

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(x => x.Request.Query["udid"]).Returns(UDID);

            var heartbeatFeatureMock = new Mock<IConnectionHeartbeatFeature>();

            context.Setup(x => x.ConnectionId).Returns(contextId);
            context.Setup(x => x.Features.Get<IHttpContextFeature>().HttpContext).Returns(httpContextMock.Object);
            context.Setup(x => x.Features.Get<IConnectionHeartbeatFeature>()).Returns(heartbeatFeatureMock.Object);

            var socketHubService = new DeviceHubService(loggerMock.Object,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            socketHubService.Context = context.Object;

            // Act
            await socketHubService.OnDisconnectedAsync(new Exception());

            //Assert
            loggerMock.Verify(x => x.Log(LogLevel.Debug, It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task ShouldPingFromClientSideBySocketSetToMemory()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();
            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            var loggerMock = new Mock<ILogger<DeviceHubService>>();
            var context = new Mock<HubCallerContext>();
            var deviceService = new Mock<IDeviceService>();
            var httpContextMock = new Mock<HttpContext>();

            httpContextMock.Setup(x => x.Request.Query["udid"]).Returns(UDID);

            var heartbeatFeatureMock = new Mock<IConnectionHeartbeatFeature>();

            context.Setup(x => x.ConnectionId).Returns(contextId);

            var socketHubService = new DeviceHubService(loggerMock.Object,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            _memoryCache.Set(contextId, "1");
            socketHubService.Context = context.Object;

            // Act
            socketHubService.PingFromClientSide(new object());
            var result = _memoryCache.Get(contextId);

            //Assert
            Assert.Same(string.Empty, result);
        }

        [Fact]
        public async Task ShouldPingFromClientSideBySocketSetToMemoryNew()
        {
            // Arrange
            string contextId = _baseTest.Fixture.Create<string>();
            string fakeContextId = _baseTest.Fixture.Create<string>();
            string UDID = _baseTest.Fixture.Create<string>();
            var deviceHistoryRepository = new DeviceHistoryRepository(_baseTest.DbContext);
            var loggerMock = new Mock<ILogger<DeviceHubService>>();
            var context = new Mock<HubCallerContext>();
            var deviceService = new Mock<IDeviceService>();
            var httpContextMock = new Mock<HttpContext>();

            httpContextMock.Setup(x => x.Request.Query["udid"]).Returns(UDID);

            var heartbeatFeatureMock = new Mock<IConnectionHeartbeatFeature>();

            context.Setup(x => x.ConnectionId).Returns(contextId);

            var socketHubService = new DeviceHubService(loggerMock.Object,
                deviceHistoryRepository,
                deviceService.Object,
                webHubContext.Object,
                redisCache,
                _backGroundTaskMock.Object,
                _badDisconnectSocketServiceMock.Object,
                _memoryCache,
                _notificationSenderExtentionMock.Object);

            _memoryCache.Set(fakeContextId, "1");
            socketHubService.Context = context.Object;

            // Act
            socketHubService.PingFromClientSide(new object());
            var result = _memoryCache.Get(contextId);

            //Assert
            Assert.Same(string.Empty, result);
        }
    }
}
