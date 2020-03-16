using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xyzies.Devices.Services.Models.Comment
{
    public class Filters
    {
        [JsonProperty(nameof(UsersId))]
        public IEnumerable<string> UsersId { get; set; }

        [JsonProperty(nameof(Role))]
        public IEnumerable<string> Role { get; set; }

    }
}
