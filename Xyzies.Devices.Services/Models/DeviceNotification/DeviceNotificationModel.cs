using System;
using Xyzies.Devices.Services.Common.Enums;

namespace Xyzies.Devices.Services.Models
{
    [Serializable]
    public class DeviceNotificationModel
    {
        public string Udid { get; set; }

        public SelectFunc FuncType { get; set; }

        public DateTime LastHeartBeat { get; set; }
    }

    [Serializable]
    public class DeviceNotificationKey
    {
        public string Udid { get; set; }

        public SelectFunc FuncType { get; set; }
    }
}
