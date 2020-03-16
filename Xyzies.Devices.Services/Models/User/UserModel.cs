using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Xyzies.Devices.Services.Models.User
{
    public class UserModel
    {
        [JsonProperty("ObjectId")]
        public Guid Id { get; set; }

        public int? CompanyId { get; set; }

        public string DisplayName { get; set; }

        public string GivenName { get; set; }

        public string Surname { get; set; }

        public IEnumerable<string> Scopes { get; set; }

        public string Role { get; set; }

        public string Email { get; set; }
    }
}
