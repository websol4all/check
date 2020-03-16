using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xyzies.Devices.Services.Models
{
    public class Sorting
    {
        public string SortBy { get; set; }
        public string Order { get; set; }

        [JsonIgnore]
        public bool IsAscending => !string.IsNullOrEmpty(Order) && Order.ToLower() == "asc";
    }
}
