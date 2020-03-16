using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Xyzies.Devices.Services.Service
{
    public class BadDisconnectSocketService
    {

        private readonly ILogger<BadDisconnectSocketService> _logger = null;

        readonly HashSet<string> PendingConnections = new HashSet<string>();
        readonly object PendingConnectionsLock = new object();

        public BadDisconnectSocketService(ILogger<BadDisconnectSocketService> logger)
        {
            _logger = logger ??
               throw new ArgumentNullException(nameof(logger));
        }

        public void DisconnectClient(string ConnectionId)
        {
            if (!PendingConnections.Contains(ConnectionId))
            {
                lock (PendingConnectionsLock)
                {
                    PendingConnections.Add(ConnectionId);                    
                }
            }
        }

        public void InitConnectionMonitoring(HubCallerContext Context)
        {
            var feature = Context.Features.Get<IConnectionHeartbeatFeature>();

            feature.OnHeartbeat(state =>
            {
                if (PendingConnections.Contains(Context.ConnectionId))
                {
                    try
                    {
                        Context.Abort();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Abort exception {ex.Message}, {ex.StackTrace}, {ex.Source}");
                    }
                    _logger.LogInformation($"Abort disconnect call");

                    lock (PendingConnectionsLock)
                    {
                        PendingConnections.Remove(Context.ConnectionId);
                    }
                }

            }, Context.ConnectionId);
        }
    }
}
