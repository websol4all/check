using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyzies.Devices.Services.Helpers
{
    internal class Queries
    {
        internal static string GetQuery(string param, IEnumerable<string> values)
        {
            return $"{param}={string.Join($"&{param}=", values.Distinct())}";
        }
    }
}
