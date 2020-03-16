using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Xyzies.Devices.Data.Core;

namespace Xyzies.Devices.Data.Entity
{
    public class DeviceHistory : BaseEntity<Guid>
    {
        public DateTime CreatedOn { get; set; }

        public int CompanyId { get; set; }

        public bool IsOnline { get; set; }

        public bool IsInLocation { get; set; }

        public double CurrentDeviceLocationLatitude { get; set; }

        public double CurrentDeviceLocationLongitude { get; set; }

        public double DeviceLocationLatitude { get; set; }

        public double DeviceLocationLongitude { get; set; }

        [Range(0, double.MaxValue)]
        public double DeviceRadius { get; set; }

        public Guid? LoggedInUserId { get; set; }

        public Guid DeviceId { get; set; }

        [ForeignKey(nameof(DeviceId))]
        public virtual Device Device { get; set; }
    }
}
