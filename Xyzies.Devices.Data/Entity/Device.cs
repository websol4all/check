using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xyzies.Devices.Data.Core;

namespace Xyzies.Devices.Data.Entity
{
    public class Device : BaseEntity<Guid>
    {
        public Device()
        {
            DeviceHistory = new List<DeviceHistory>();
        }

        [Required]
        public string Udid { get; set; }

        public string HexnodeUdid { get; set; }

        public string DeviceName { get; set; }

        public string Phone { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public bool IsPending { get; set; }

        [Range(0, double.MaxValue)]
        public double Radius { get; set; }

        [Range(0, int.MaxValue)]
        public int CompanyId { get; set; }

        public Guid BranchId { get; set; }

        public DateTime CreatedOn { get; set; }

        [JsonIgnore]
        public virtual ICollection<DeviceHistory> DeviceHistory { get; set; }
    }
}
