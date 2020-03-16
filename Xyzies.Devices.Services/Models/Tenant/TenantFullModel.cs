using System;
using System.Collections.Generic;
using System.Text;
using Xyzies.Devices.Services.Models.Company;

namespace Xyzies.Devices.Services.Models.Tenant
{
    public class TenantFullModel : TenantModel
    {
        public List<CompanyModel> Companies { get; set; }
    }
}
