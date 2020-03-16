using System;
using System.Collections.Generic;
using System.Text;

namespace Xyzies.Devices.Tests.Models.Company
{
    public class CreateCompanyModel
    {
        public string CompanyName { get; set; }
        public string Email { get; set; }
        public bool IsEnabled { get; set; }
    }
}
