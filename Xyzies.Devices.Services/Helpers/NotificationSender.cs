using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

using StackExchange.Redis;

using Xyzies.Devices.Services.Common.Cache;
using Xyzies.Devices.Services.Common.Enums;
using Xyzies.Devices.Services.Helpers.Interfaces;
using Xyzies.Devices.Services.Helpers.Options;
using Xyzies.Devices.Services.Models;

namespace Xyzies.Devices.Services.Helpers
{
    public class NotificationSender : INotificationSender
    {
        private const string KeyPrefixShadow = "shadowkey";

        private readonly IDatabase _rediscache = null;
        private readonly double _expireTimeBeforeSendAlertSeconds;
        private readonly ILogger<NotificationSender> _logger = null;

        public NotificationSender(IOptionsMonitor<NotificationSenderExtentionOptions> optionsMonitor, RedisStore rediscache, ILogger<NotificationSender> logger)
        {
            _rediscache = rediscache.RedisCache ??
                throw new ArgumentNullException(nameof(rediscache));

            _expireTimeBeforeSendAlertSeconds = optionsMonitor?.CurrentValue?.ExpireTimeBeforeSendAlertSeconds ??
                throw new ArgumentNullException(nameof(NotificationSender));

            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendAlertOnOffLinePrepareByExpirationTime(SelectFunc funcType, string udid)
        {
            string objkey = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = funcType,
                    Udid = udid,
            });

            if (funcType == SelectFunc.Offline)
            {
                string objkeyold = JsonConvert.SerializeObject(new DeviceNotificationKey()
                {
                    FuncType = SelectFunc.Online,
                        Udid = udid,
                });

                await CommonConditionForRedisMethod(funcType, udid, objkey, objkeyold);
            }
            else
            {
                string objkeyold = JsonConvert.SerializeObject(new DeviceNotificationKey()
                {
                    FuncType = SelectFunc.Offline,
                        Udid = udid,
                });

                await CommonConditionForRedisMethod(funcType, udid, objkey, objkeyold);
            }
        }

        public async Task SendAlertInOutlocationPrepareByExpirationTime(SelectFunc funcType, string udid)
        {
            string objkey = JsonConvert.SerializeObject(new DeviceNotificationKey()
            {
                FuncType = funcType,
                    Udid = udid,
            });

            if (funcType == SelectFunc.InLocation)
            {
                string objkeyold = JsonConvert.SerializeObject(new DeviceNotificationKey()
                {
                    FuncType = SelectFunc.OutOfLocation,
                        Udid = udid,
                });

                await CommonConditionForRedisMethod(funcType, udid, objkey, objkeyold);
            }
            else
            {
                string objkeyoldnext = JsonConvert.SerializeObject(new DeviceNotificationKey()
                {
                    FuncType = SelectFunc.OutOfLocation,
                        Udid = udid,
                });

                try
                {
                    if (await _rediscache.KeyExistsAsync(KeyPrefixShadow + objkeyoldnext) ||
                        await _rediscache.KeyExistsAsync(objkeyoldnext))
                    {
                        return;
                    }
                }
                catch (RedisConnectionException)
                {
                    RedisConnection.ForceReconnect();
                    return;
                }
                catch (ObjectDisposedException ex)
                {
                    RedisConnection.ForceReconnect();
                    _logger.LogCritical("Cannot force reconnect to redis cache!!", ex);
                    return;
                }

                string objkeyold = JsonConvert.SerializeObject(new DeviceNotificationKey()
                {
                    FuncType = SelectFunc.InLocation,
                        Udid = udid,
                });

                await CommonConditionForRedisMethod(funcType, udid, objkey, objkeyold);
            }
        }

        public async Task NotificationForChangeLocation(string udid, bool calcIsLocation)
        {
            if (!calcIsLocation)
            {
                await SendAlertInOutlocationPrepareByExpirationTime(SelectFunc.OutOfLocation, udid);
            }
            else
            {
                await SendAlertInOutlocationPrepareByExpirationTime(SelectFunc.InLocation, udid);
            }
        }
        private async Task CommonConditionForRedisMethod(SelectFunc funcType, string udid, string objkey, string objkeyold)
        {
            try
            {
                if (await _rediscache.KeyExistsAsync(KeyPrefixShadow + objkeyold))
                {
                    await _rediscache.KeyDeleteAsync(KeyPrefixShadow + objkeyold);
                    await _rediscache.KeyDeleteAsync(objkeyold);
                }
                else
                {
                    await _rediscache.KeyDeleteAsync(objkeyold);
                    await PrepareNotificationObject(objkey, funcType, udid);
                }
            }
            catch (RedisConnectionException exm)
            {
                RedisConnection.ForceReconnect();
                _logger.LogError($"Error RedisConnectionException  EXC: {exm.Message} {exm.StackTrace} {exm.Source}");
            }
            catch (ObjectDisposedException ex)
            {
                RedisConnection.ForceReconnect();
                _logger.LogCritical("Cannot force reconnect to redis cache!!", ex);
            }
        }

        private async Task PrepareNotificationObject(string objkey, SelectFunc funcType, string udid)
        {
            try
            {
                string obj = JsonConvert.SerializeObject(new DeviceNotificationModel()
                {
                    FuncType = funcType,
                        Udid = udid,
                        LastHeartBeat = DateTime.UtcNow
                });

                _logger.LogInformation($"PrepareNotificationObject udid: {udid}, Type: {funcType.ToString()}");

                await _rediscache.StringSetAsync(objkey, obj);
                await _rediscache.StringSetAsync(KeyPrefixShadow + objkey, objkey, TimeSpan.FromSeconds(_expireTimeBeforeSendAlertSeconds));
            }
            catch (RedisConnectionException exm)
            {
                RedisConnection.ForceReconnect();
                _logger.LogError($"Error RedisConnectionException  EXC: {exm.Message} {exm.StackTrace} {exm.Source}");
            }
            catch (ObjectDisposedException ex)
            {
                RedisConnection.ForceReconnect();
                _logger.LogCritical("Cannot force reconnect to redis cache!!", ex);
            }
        }

    }
}
