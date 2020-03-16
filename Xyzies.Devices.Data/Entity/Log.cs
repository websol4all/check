using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Xyzies.Devices.Data.Core;

namespace Xyzies.Devices.Data.Entity
{
    public class Log : BaseEntity<Guid>
    {
        [Required]
        public DateTime CreateOn { get; set; }

        [Required]
        public string Message { get; set; }

        public string Status { get; set; }
    }
}
