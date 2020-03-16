using System;
using System.Collections.Generic;
using System.Text;

namespace Xyzies.Devices.Services.Models
{
    public class FilteringModel
    {
        public FilteringModel()
        {
            TenantIds = new List<Guid>();
        }
        public List<int> CompanyIds { get; set; }
        public List<Guid> BranchIds { get; set; }
        public List<Guid> TenantIds { get; set; }
        public bool? IsOnline { get; set; }
        public string SearchPhrase { get; set; }
    }
}
