using System;
using System.Threading.Tasks;
using Xunit;
using Xyzies.Devices.Services.Common.Cache;
using Xyzies.Devices.Services.Helpers;
using Xyzies.Devices.Services.Helpers.Interfaces;
using AutoFixture;
using Xyzies.Devices.Services.Common.Enums;
using Newtonsoft.Json;
using Xyzies.Devices.Services.Models;
using Moq;
using Microsoft.Extensions.Logging;

namespace Xyzies.Devices.Tests.Unit_tests
{
    public class NotificationSenderTests : IClassFixture<BaseTest>
    {
        private const string KeyPrefixShadow = "shadowkey";

        private readonly BaseTest _baseTest = null;
        private readonly INotificationSender _notificationSender = null;
        private RedisStore cache;

        public NotificationSenderTests(BaseTest baseTest)
        {
            _baseTest = baseTest ?? throw new ArgumentNullException(nameof(baseTest));
            var loggerMock = new Mock<ILogger<NotificationSender>>();
            cache = _baseTest.redisStore;
            _notificationSender = new NotificationSender(_baseTest._notificationSenderExtentionOptions, cache, loggerMock.Object);
        }

        [Fact]
        public async Task ShouldAddEventToRedisCacheOnLine()
        {
            // Arrange
            string UDID = _baseTest.Fixture.Create<string>();
            SelectFunc funcType = SelectFunc.Online;
            string objkeyold = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.Offline,
                Udid = UDID,
            });
            await cache.RedisCache.StringSetAsync(objkeyold, UDID);

            // Act
            await _notificationSender.SendAlertOnOffLinePrepareByExpirationTime(funcType, UDID);

            string objkey = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.Online,
                Udid = UDID,
            });

            //Assert
            string result = await cache.RedisCache.StringGetAsync(objkey);
            string resultOld = await cache.RedisCache.StringGetAsync(objkeyold);

            Assert.Null(resultOld);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldNotAddEventToRedisCacheOnLine()
        {
            // Arrange
            string UDID = _baseTest.Fixture.Create<string>();
            SelectFunc funcType = SelectFunc.Online;
            string objkeyold = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.Offline,
                Udid = UDID,
            });
            await cache.RedisCache.StringSetAsync(KeyPrefixShadow + objkeyold, UDID);

            // Act
            await _notificationSender.SendAlertOnOffLinePrepareByExpirationTime(funcType, UDID);

            string objkey = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.Online,
                Udid = UDID,
            });

            //Assert
            string result = await cache.RedisCache.StringGetAsync(objkey);
            string resultOld = await cache.RedisCache.StringGetAsync(objkeyold);

            Assert.Null(resultOld);
            Assert.Null(result);
        }

        [Fact]
        public async Task ShouldAddEventToRedisCacheOffLine()
        {
            // Arrange
            string UDID = _baseTest.Fixture.Create<string>();
            SelectFunc funcType = SelectFunc.Offline;
            string objkeyold = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.Online,
                Udid = UDID,
            });
            await cache.RedisCache.StringSetAsync(objkeyold, UDID);

            // Act
            await _notificationSender.SendAlertOnOffLinePrepareByExpirationTime(funcType, UDID);

            string objkey = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.Offline,
                Udid = UDID,
            });

            //Assert
            string result = await cache.RedisCache.StringGetAsync(objkey);
            string resultOld = await cache.RedisCache.StringGetAsync(objkeyold);

            Assert.Null(resultOld);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldNotAddEventToRedisCacheOffLine()
        {
            // Arrange
            string UDID = _baseTest.Fixture.Create<string>();
            SelectFunc funcType = SelectFunc.Offline;
            string objkeyold = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.Online,
                Udid = UDID,
            });
            await cache.RedisCache.StringSetAsync(KeyPrefixShadow + objkeyold, UDID);

            // Act
            await _notificationSender.SendAlertOnOffLinePrepareByExpirationTime(funcType, UDID);

            string objkey = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.Offline,
                Udid = UDID,
            });

            //Assert
            string result = await cache.RedisCache.StringGetAsync(objkey);
            string resultOld = await cache.RedisCache.StringGetAsync(objkeyold);

            Assert.Null(resultOld);
            Assert.Null(result);
        }

        [Fact]
        public async Task ShouldAddEventToRedisCacheInLocation()
        {
            // Arrange
            string UDID = _baseTest.Fixture.Create<string>();
            SelectFunc funcType = SelectFunc.InLocation;
            string objkeyold = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.OutOfLocation,
                Udid = UDID,
            });
            await cache.RedisCache.StringSetAsync(objkeyold, UDID);

            // Act
            await _notificationSender.SendAlertInOutlocationPrepareByExpirationTime(funcType, UDID);

            string objkey = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.InLocation,
                Udid = UDID,
            });

            //Assert
            string result = await cache.RedisCache.StringGetAsync(objkey);
            string resultOld = await cache.RedisCache.StringGetAsync(objkeyold);

            Assert.Null(resultOld);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldNotAddEventToRedisCacheInLocation()
        {
            // Arrange
            string UDID = _baseTest.Fixture.Create<string>();
            SelectFunc funcType = SelectFunc.InLocation;
            string objkeyold = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.OutOfLocation,
                Udid = UDID,
            });
            await cache.RedisCache.StringSetAsync(objkeyold, UDID);

            // Act
            await _notificationSender.SendAlertInOutlocationPrepareByExpirationTime(funcType, UDID);

            string objkey = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.InLocation,
                Udid = UDID,
            });

            //Assert
            string result = await cache.RedisCache.StringGetAsync(objkey);
            string resultOld = await cache.RedisCache.StringGetAsync(objkeyold);

            Assert.Null(resultOld);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldAddEventToRedisCacheOutLocation()
        {
            // Arrange
            string UDID = _baseTest.Fixture.Create<string>();
            SelectFunc funcType = SelectFunc.OutOfLocation;
            string objkeyold = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.InLocation,
                Udid = UDID,
            });
            await cache.RedisCache.StringSetAsync(objkeyold, UDID);

            // Act
            await _notificationSender.SendAlertInOutlocationPrepareByExpirationTime(funcType, UDID);

            string objkey = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.OutOfLocation,
                Udid = UDID,
            });

            //Assert
            string result = await cache.RedisCache.StringGetAsync(objkey);
            string resultOld = await cache.RedisCache.StringGetAsync(objkeyold);

            Assert.Null(resultOld);
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldNotAddEventToRedisCacheOutLocation()
        {
            // Arrange
            string UDID = _baseTest.Fixture.Create<string>();
            SelectFunc funcType = SelectFunc.OutOfLocation;
            string objkeyold = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.InLocation,
                Udid = UDID,
            });
            await cache.RedisCache.StringSetAsync(objkeyold, UDID);

            // Act
            await _notificationSender.SendAlertInOutlocationPrepareByExpirationTime(funcType, UDID);

            string objkey = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = SelectFunc.OutOfLocation,
                Udid = UDID,
            });

            //Assert
            string result = await cache.RedisCache.StringGetAsync(objkey);
            string resultOld = await cache.RedisCache.StringGetAsync(objkeyold);

            Assert.Null(resultOld);
            Assert.NotNull(result);
        }
    }
}
