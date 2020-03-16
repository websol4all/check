using System;
using System.Collections.Generic;
using System.Text;
using Xyzies.Devices.Services.Models.User;

namespace Xyzies.Devices.Tests.Models.User
{
    public class UserModelTest : UserModel
    {
        public string Role { get; set; }

        public string Email { get; set; }
    }
}
