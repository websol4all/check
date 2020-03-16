using System;
using System.Collections.Generic;
using System.Text;
using Xyzies.Devices.Services.Models.Company;

namespace Xyzies.Devices.Services.Models.Tenant
{
    public class TenantModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string Phone { get; set; }
    }
}
