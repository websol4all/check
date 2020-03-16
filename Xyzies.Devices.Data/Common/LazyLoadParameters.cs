using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Xyzies.Devices.Data.Common
{
    public class LazyLoadParameters
    {
        /// <summary>
        /// Items count to skip
        /// </summary>
        [Range(0, int.MaxValue)]
        public int? Offset { get; set; }

        /// <summary>
        /// Limit to returned items
        /// </summary>
        [Range(1, int.MaxValue)]
        public int? Limit { get; set; }
    }
}
