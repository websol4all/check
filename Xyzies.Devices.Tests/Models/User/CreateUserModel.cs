using System;
using System.Collections.Generic;
using System.Text;

namespace Xyzies.Devices.Tests.Models.User
{
    public class CreateUserModel
    {
        public bool AccountEnabled { get; set; } = true;

        public string CreationType { get; set; } = "LocalAccount";

        public string DisplayName { get; set; }

        public string GivenName { get; set; }

        public string Surname { get; set; }

        public string Role { get; set; }

        public Guid StatusId { get; set; }

        public PasswordProfileModel PasswordProfile { get; set; }

        public List<SignInName> SignInNames { get; set; }

        public int? CompanyId { get; set; }

        public Guid? BranchId { get; set; }
    }
}
