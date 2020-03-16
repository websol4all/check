using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace Xyzies.Devices.Services.Models.DeviceHistory
{
    public class DeviceHistoryModel
    {

        public Guid Id { get; set; }

        public DateTime CreatedOn { get; set; }

        public bool IsOnline { get; set; }

        public bool IsInLocation { get; set; }

        public double CurrentDeviceLocationLatitude { get; set; }

        public double CurrentDeviceLocationLongitude { get; set; }

        public double DeviceLocationLatitude { get; set; }

        public double DeviceLocationLongitude { get; set; }

        [JsonIgnore]
        public int CompanyId { get; set; }
    }
}
