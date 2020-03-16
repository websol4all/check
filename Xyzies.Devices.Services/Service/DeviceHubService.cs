using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using StackExchange.Redis;

using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Data.Repository.Behaviour;
using Xyzies.Devices.Services.Common.Cache;
using Xyzies.Devices.Services.Common.Enums;
using Xyzies.Devices.Services.Helpers;
using Xyzies.Devices.Services.Helpers.Interfaces;
using Xyzies.Devices.Services.Models;
using Xyzies.Devices.Services.Models.DeviceSocket;
using Xyzies.Devices.Services.Requests.Device;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Services.Service
{
    public class DeviceHubService : Hub
    {
        private const string KeyPrefix = "DeviceSocket-";

        private IMemoryCache _memoryCache = null;

        private readonly ILogger<DeviceHubService> _logger = null;
        private readonly IDeviceHistoryRepository _deviceHistoryRepository = null;
        private readonly IDeviceService _deviceService = null;
        private readonly IHubContext<WebHubService> _webHubContext;
        private readonly IDatabase _rediscache = null;
        private readonly BadDisconnectSocketService _badDisconnectSocketService = null;
        private readonly INotificationSender _notificationSenderExtention = null;

        public IBackgroundTaskQueue Queue { get; }

        public DeviceHubService(ILogger<DeviceHubService> logger,
            IDeviceHistoryRepository deviceHistoryRepository,
            IDeviceService deviceService,
            IHubContext<WebHubService> webHubContext,
            RedisStore rediscache,
            IBackgroundTaskQueue queue,
            BadDisconnectSocketService badDisconnectSocketService,
            IMemoryCache memoryCache,
            INotificationSender notificationSenderExtention)
        {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _deviceHistoryRepository = deviceHistoryRepository ??
                throw new ArgumentNullException(nameof(deviceHistoryRepository));
            _deviceService = deviceService ??
                throw new ArgumentNullException(nameof(deviceService));
            _webHubContext = webHubContext ??
                throw new ArgumentNullException(nameof(webHubContext));
            _rediscache = rediscache.RedisCache ??
                throw new ArgumentNullException(nameof(rediscache));
            Queue = queue ??
                throw new ArgumentNullException(nameof(queue));
            _notificationSenderExtention = notificationSenderExtention ??
                throw new ArgumentNullException(nameof(notificationSenderExtention));

            _badDisconnectSocketService = badDisconnectSocketService ??
                throw new ArgumentNullException(nameof(badDisconnectSocketService));
            _memoryCache = memoryCache ??
                throw new ArgumentNullException(nameof(memoryCache));
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var httpContext = Context.Features.Get<IHttpContextFeature>().HttpContext;
                string udid = httpContext.Request.Query["udid"];
                string udidPrefix = KeyPrefix + udid;
                var key = KeyPrefix + Context.ConnectionId;

                _logger.LogInformation($"Start OnConnectedAsync {Context.ConnectionId} udid: {udid}", Context.ConnectionId, udid);
                _badDisconnectSocketService.InitConnectionMonitoring(Context);

                await _rediscache.StringSetAsync(key, udid);

                if (await _rediscache.KeyExistsAsync(udidPrefix))
                {
                    string oldConnectionId = await _rediscache.StringGetSetAsync(udidPrefix, Context.ConnectionId);
                    _badDisconnectSocketService.DisconnectClient(oldConnectionId);
                }
                else
                    await _rediscache.StringSetAsync(udidPrefix, Context.ConnectionId);

                var deviceFromDb = await _deviceService.GetDeviceByUdidAsync(udid);

                if (deviceFromDb == null)
                {
                    await _deviceService.Setup(new SetupDeviceRequest { Udid = udid });
                    deviceFromDb = await _deviceService.GetDeviceByUdidAsync(udid);

                    var lastHistoryByDevice = (await _deviceHistoryRepository.GetAsync(x => x.DeviceId == deviceFromDb.Id))
                        .OrderByDescending(y => y.CreatedOn)
                        .FirstOrDefault();

                    await AddAndSendDeviceHistoryToAll(CreateDeviceHistoryOnConnect(deviceFromDb, lastHistoryByDevice));
                }
                else
                {
                    var lastHistoryByDevice = (await _deviceHistoryRepository.GetAsync(x => x.DeviceId == deviceFromDb.Id))
                        .OrderByDescending(y => y.CreatedOn)
                        .FirstOrDefault();

                    await AddAndSendDeviceHistory(udid, CreateDeviceHistoryOnConnect(deviceFromDb, lastHistoryByDevice));

                    await _notificationSenderExtention.SendAlertOnOffLinePrepareByExpirationTime(SelectFunc.Online, udid);
                }

                await base.OnConnectedAsync();
                _memoryCache.Set(Context.ConnectionId, string.Empty, GetCacheEntryOptions());
                _logger.LogInformation($"Finish OnConnectedAsync: {Context.ConnectionId}, udid: {udid}", Context.ConnectionId, udid);
            }
            catch (RedisConnectionException exm)
            {
                RedisConnection.ForceReconnect();
                _logger.LogError($"Error RedisConnectionException ContextID: {Context.ConnectionId} EXC: {exm.Message} {exm.StackTrace} {exm.Source}");
            }
            catch (ObjectDisposedException ex)
            {
                RedisConnection.ForceReconnect();
                _logger.LogCritical("Cannot force reconnect to redis cache!!", ex);

            }
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            try
            {
                var key = KeyPrefix + Context.ConnectionId;

                if (await _rediscache.KeyExistsAsync(key))
                {
                    string udid = await _rediscache.StringGetAsync(key);

                    _logger.LogInformation($"Start OnDisconnectedAsync {Context.ConnectionId}, udid: {udid}", Context.ConnectionId, udid);

                    string udidPrefix = KeyPrefix + udid;

                    string currentKeyInRedis = await _rediscache.StringGetAsync(udidPrefix);

                    if (!string.IsNullOrEmpty(currentKeyInRedis) && currentKeyInRedis.Equals(Context.ConnectionId))
                    {
                        var deviceFromDb = await _deviceService.GetDeviceByUdidAsync(udid);

                        var lastHistoryByDevice = (await _deviceHistoryRepository.GetAsync(x => x.DeviceId == deviceFromDb.Id))
                            .OrderByDescending(y => y.CreatedOn)
                            .FirstOrDefault();

                        await AddAndSendDeviceHistory(udid, new DeviceHistory()
                        {
                            CreatedOn = DateTime.UtcNow,
                                IsOnline = false,
                                CurrentDeviceLocationLatitude = lastHistoryByDevice?.CurrentDeviceLocationLatitude ?? deviceFromDb.Latitude,
                                CurrentDeviceLocationLongitude = lastHistoryByDevice?.CurrentDeviceLocationLongitude ?? deviceFromDb.Longitude,
                                DeviceLocationLatitude = deviceFromDb.Latitude,
                                DeviceLocationLongitude = deviceFromDb.Longitude,
                                DeviceRadius = deviceFromDb.Radius,
                                LoggedInUserId = lastHistoryByDevice?.LoggedInUserId ?? null,
                                DeviceId = deviceFromDb.Id,
                                CompanyId = deviceFromDb.CompanyId,
                                IsInLocation = lastHistoryByDevice?.IsInLocation ?? true
                        });

                        _logger.LogDebug("Before Send Notification in DeviceHub");
                        await _notificationSenderExtention.SendAlertOnOffLinePrepareByExpirationTime(SelectFunc.Offline, udid);

                        await _rediscache.KeyDeleteAsync(udidPrefix);
                    }

                    await _rediscache.KeyDeleteAsync(key);

                    _logger.LogInformation($"Finish OnDisconnectedAsync: {Context.ConnectionId}, udid: {udid}", Context.ConnectionId, udid);
                }
                else
                {
                    _logger.LogDebug("Redis key not found OnDisconnectedAsync");
                }

                _memoryCache.Remove(Context.ConnectionId);

                await base.OnDisconnectedAsync(ex);

            }
            catch (RedisConnectionException exm)
            {
                RedisConnection.ForceReconnect();
                _logger.LogError($"Error RedisConnectionException ContextID: {Context.ConnectionId} EXC: {exm.Message} {exm.StackTrace} {exm.Source}");
                return;
            }
            catch (Exception exm)
            {
                _logger.LogError($"Error OnDisconnectedAsync ContextID: {Context.ConnectionId} EXC: {exm.Message} {exm.StackTrace} {exm.Source}");
            }
        }

        public async Task UpdateLocation(DeviceUpdateLocationModel device)
        {
            try
            {
                var key = KeyPrefix + Context.ConnectionId;

                if (await _rediscache.KeyExistsAsync(key))
                {
                    string udid = await _rediscache.StringGetAsync(key);

                    _logger.LogInformation($"Start UpdateLocation {Context.ConnectionId}, udid: {udid}", Context.ConnectionId, udid);

                    var deviceFromDb = await _deviceService.GetDeviceByUdidAsync(udid);
                    var lastHistoryByDevice = (await _deviceHistoryRepository.GetAsync(x => x.DeviceId == deviceFromDb.Id))
                        .OrderByDescending(y => y.CreatedOn)
                        .FirstOrDefault();

                    bool calcIsLocation = CalculateDistanceForDevice.DeviceIsInLocation(
                        deviceFromDb.Latitude,
                        deviceFromDb.Longitude,
                        device.Lat,
                        device.Long,
                        deviceFromDb.Radius);

                    _logger.LogInformation($"location changed devHub: calc {calcIsLocation.ToString()}, lastcurr: " +
                        $"{lastHistoryByDevice.CurrentDeviceLocationLatitude} {lastHistoryByDevice.CurrentDeviceLocationLongitude} newCoord" +
                        $"{device.Lat} {device.Long}");

                    if ((calcIsLocation != lastHistoryByDevice.IsInLocation) || (!calcIsLocation))
                    {
                        await AddAndSendDeviceHistory(udid, new DeviceHistory()
                        {
                            CreatedOn = DateTime.UtcNow,
                                IsOnline = true,
                                CurrentDeviceLocationLatitude = device.Lat,
                                CurrentDeviceLocationLongitude = device.Long,
                                DeviceLocationLatitude = deviceFromDb.Latitude,
                                DeviceLocationLongitude = deviceFromDb.Longitude,
                                DeviceRadius = deviceFromDb.Radius,
                                LoggedInUserId = lastHistoryByDevice?.LoggedInUserId ?? null,
                                DeviceId = deviceFromDb.Id,
                                CompanyId = deviceFromDb.CompanyId,
                                IsInLocation = calcIsLocation
                        });

                        await _notificationSenderExtention.NotificationForChangeLocation(udid, calcIsLocation);
                    }

                    _logger.LogInformation($"Finish UpdateLocation {Context.ConnectionId}, udid: {udid}", Context.ConnectionId, udid);
                }
                else
                {
                    _logger.LogWarning("Device not found in UpdateLocation");
                }
            }
            catch (RedisConnectionException exm)
            {
                RedisConnection.ForceReconnect();
                _logger.LogError($"Error RedisConnectionException ContextID: {Context.ConnectionId} EXC: {exm.Message} {exm.StackTrace} {exm.Source}");
                return;
            }
            catch (ObjectDisposedException ex)
            {
                RedisConnection.ForceReconnect();
                _logger.LogCritical("Cannot force reconnect to redis cache!!", ex);
            }
        }

        public async Task UpdateSalesRep(DeviceUpdateSalesRepModel device)
        {
            try
            {
                var key = KeyPrefix + Context.ConnectionId;

                if (await _rediscache.KeyExistsAsync(key))
                {
                    var udid = await _rediscache.StringGetAsync(key);

                    _logger.LogInformation($"Start UpdateSalesRep {Context.ConnectionId}, udid: {udid}", Context.ConnectionId, udid);

                    var deviceFromDb = await _deviceService.GetDeviceByUdidAsync(udid);

                    var lastHistoryByDevice = (await _deviceHistoryRepository.GetAsync(x => x.DeviceId == deviceFromDb.Id))
                        .OrderByDescending(y => y.CreatedOn)
                        .FirstOrDefault();

                    if (lastHistoryByDevice?.LoggedInUserId != device.SalesRepId)
                    {
                        await AddAndSendDeviceHistory(udid, new DeviceHistory()
                        {
                            CreatedOn = DateTime.UtcNow,
                                IsOnline = true,
                                CurrentDeviceLocationLatitude = lastHistoryByDevice?.CurrentDeviceLocationLatitude ?? deviceFromDb.Latitude,
                                CurrentDeviceLocationLongitude = lastHistoryByDevice?.CurrentDeviceLocationLongitude ?? deviceFromDb.Longitude,
                                DeviceLocationLatitude = deviceFromDb.Latitude,
                                DeviceLocationLongitude = deviceFromDb.Longitude,
                                DeviceRadius = deviceFromDb.Radius,
                                LoggedInUserId = device.SalesRepId,
                                DeviceId = deviceFromDb.Id,
                                CompanyId = deviceFromDb.CompanyId,
                                IsInLocation = lastHistoryByDevice?.IsInLocation ?? true
                        });
                    }

                    _logger.LogInformation($"Finish UpdateSalesRep {Context.ConnectionId}, udid: {udid}", Context.ConnectionId, udid);
                }
                else
                {
                    _logger.LogInformation("Device not found in UpdateSalesRep");
                }
            }
            catch (RedisConnectionException exm)
            {
                RedisConnection.ForceReconnect();
                _logger.LogError($"Error RedisConnectionException ContextID: {Context.ConnectionId} EXC: {exm.Message} {exm.StackTrace} {exm.Source}");
                return;
            }
            catch (ObjectDisposedException ex)
            {
                RedisConnection.ForceReconnect();
                _logger.LogCritical("Cannot force reconnect to redis cache!!", ex);
            }
        }

        public void PingFromClientSide(object x)
        {
            _logger.LogInformation($"PingFromClientSide {Context.ConnectionId}", Context.ConnectionId);
            try
            {
                if (_memoryCache.TryGetValue(Context.ConnectionId, out _))
                {
                    _memoryCache.Remove(Context.ConnectionId);
                }

                _memoryCache.Set(Context.ConnectionId, string.Empty, GetCacheEntryOptions());
            }
            catch (Exception ex)
            {
                _logger.LogError($"PingFromClientSide call error:{Context.ConnectionId}, message: {ex.Message}", Context.ConnectionId, ex.Message);
            }
        }

        private MemoryCacheEntryOptions GetCacheEntryOptions()
        {
            MemoryCacheEntryOptions cacheExpirationOptions = new MemoryCacheEntryOptions();
            cacheExpirationOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(35);
            cacheExpirationOptions.RegisterPostEvictionCallback(CallbackForDeleteClient);
            return cacheExpirationOptions;
        }

        private void CallbackForDeleteClient(object key, object value, EvictionReason reason, object state)
        {
            if (reason == EvictionReason.Expired)
            {
                _logger.LogDebug($"CallbackForDeleteClient call expire");
                _badDisconnectSocketService.DisconnectClient(key.ToString());
            }
        }

        private async Task AddAndSendDeviceHistory(string udid, DeviceHistory history)
        {
            await _deviceHistoryRepository.AddAsync(history);
            await _webHubContext.Clients.Group(udid).SendAsync("DeviceUpdated", JsonConvert.SerializeObject(history));
        }

        private async Task AddAndSendDeviceHistoryToAll(DeviceHistory history)
        {
            await _deviceHistoryRepository.AddAsync(history);
            await _webHubContext.Clients.All.SendAsync("DeviceAdded", JsonConvert.SerializeObject(history));
        }

        private DeviceHistory CreateDeviceHistoryOnConnect(Device deviceFromDb, DeviceHistory lastHistoryByDevice)
        {

            return new DeviceHistory()
            {
                CreatedOn = DateTime.UtcNow,
                    IsOnline = true,
                    CurrentDeviceLocationLatitude = lastHistoryByDevice?.CurrentDeviceLocationLatitude ?? deviceFromDb.Latitude,
                    CurrentDeviceLocationLongitude = lastHistoryByDevice?.CurrentDeviceLocationLongitude ?? deviceFromDb.Longitude,
                    DeviceLocationLatitude = deviceFromDb.Latitude,
                    DeviceLocationLongitude = deviceFromDb.Longitude,
                    DeviceRadius = deviceFromDb.Radius,
                    LoggedInUserId = lastHistoryByDevice?.LoggedInUserId ?? null,
                    DeviceId = deviceFromDb.Id,
                    CompanyId = deviceFromDb.CompanyId,
                    IsInLocation = lastHistoryByDevice?.IsInLocation ?? true
            };
        }

    }
}
