using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Xyzies.Devices.Tests.Models.User
{
    public class TokenModel
    {
        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }
}
