using System.Collections.Generic;

namespace Xyzies.Devices.Services.Models.WebSocket
{
    public class SubscribeDevicesRequest
    {
        public List<string> Udids { get; set; }
    }
}
