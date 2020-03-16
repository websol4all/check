using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xyzies.Devices.Services.Models.WebSocket;

namespace Xyzies.Devices.Services.Service
{
    public class WebHubService : Hub
    {
        private static readonly ConcurrentDictionary<string, List<string>> _connectionGroupNames = new ConcurrentDictionary<string, List<string>>();
        private readonly ILogger<WebHubService> _logger = null;

        public static ConcurrentDictionary<string, List<string>> ConnectionGroupNames => _connectionGroupNames;

        public WebHubService(ILogger<WebHubService> logger)
        {
            _logger = logger ??
               throw new ArgumentNullException(nameof(logger));
        }

        public async Task SubscribeDevicesUpdates(SubscribeDevicesRequest request)
        {
            try
            {
                _logger.LogInformation($"Client Request Subscribe, Client ID: {Context.ConnectionId}, UDIDs: {request.Udids}", Context.ConnectionId, request.Udids);

                await LeaveAllGroups(Context.ConnectionId);
                await JoinGroups(Context.ConnectionId, request.Udids);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unxpected error in WebHubService Subscribe method, message", ex.Message, ex);
                _connectionGroupNames.TryRemove(Context.ConnectionId, out _);
            }

        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            _logger.LogInformation($"Client Connected ID: {Context.ConnectionId}", Context.ConnectionId);
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            await base.OnDisconnectedAsync(ex);
            _connectionGroupNames.TryRemove(Context.ConnectionId, out _);

            _logger.LogInformation($"Client Disconnected ID: {Context.ConnectionId}, Ex:{ex.Message}", Context.ConnectionId, ex?.Message);
        }

        #region Helpers

        private async Task LeaveAllGroups(string connectionId)
        {
            _connectionGroupNames.TryGetValue(connectionId, out List<string> groupNames);

            if (groupNames?.Count > 0)
            {
                foreach (var name in groupNames)
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, name);
                }
                _connectionGroupNames.TryRemove(Context.ConnectionId, out _);
            }
        }
        private async Task JoinGroups(string connectionId, List<string> groupsNames)
        {
            if (groupsNames?.Count > 0)
            {
                foreach (var name in groupsNames)
                {
                    await Groups.AddToGroupAsync(connectionId, name);
                    _logger.LogInformation($"Client subscribed group {name}", name);
                }

                if (!_connectionGroupNames.TryAdd(connectionId, groupsNames))
                {
                    _logger.LogError($"Cannot insert groups names to groupNames list, ConnectionID: {connectionId}", connectionId);
                    throw new InvalidOperationException("Cannot insert groups names to groupNames list");
                }
            }
        }

        #endregion
    }
}
