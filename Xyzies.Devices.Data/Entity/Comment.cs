using System;
using System.ComponentModel.DataAnnotations.Schema;
using Xyzies.Devices.Data.Core;

namespace Xyzies.Devices.Data.Entity
{
    public class Comment: BaseEntity<Guid>
    {
        public DateTime CreateOn { get; set; }

        public Guid UserId { get; set; }

        public string UserName { get; set; }

        public string Message { get; set; }

        public Guid DeviceId { get; set; }

        [ForeignKey(nameof(DeviceId))]
        public virtual Device Device { get; set; }

    }
}
