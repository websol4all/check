using System;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using StackExchange.Redis;

using Xyzies.Devices.Services.Helpers;
using Xyzies.Devices.Services.Models;
using Xyzies.Devices.Services.Models.BackGroundTaskModel;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Services.Common.Cache
{
    public class RedisStore
    {
        private const string EXPIRED_KEYS_CHANNEL = "__keyevent@0__:expired";
        private const string KeyPrefixShadow = "shadowkey";

        public IBackgroundTaskQueue Queue { get; }

        public RedisStore(
            IBackgroundTaskQueue queue,
            IServiceScopeFactory serviceScopeFactory)
        {
            Queue = queue ??
                throw new ArgumentNullException(nameof(queue));

            RedisConnection.Connection.GetSubscriber()
                .Subscribe(EXPIRED_KEYS_CHANNEL, (channel, value) =>
                {
                    Guid id = Guid.NewGuid();

                    string key = value.ToString().Replace(KeyPrefixShadow, "");

                    var funcKey = RedisCache.StringGet(key);

                    var serializedObj = JsonConvert.DeserializeObject<DeviceNotificationModel>(funcKey);

                    Queue.QueueBackgroundWorkItem(new BackgroundTask()
                    {
                        Id = id,
                            WorkMethod = NotificationExtentions.GetNotificationMethod(serializedObj, serviceScopeFactory)
                    });
                });
        }

        public IDatabase RedisCache => RedisConnection.Connection.GetDatabase();
    }
}
