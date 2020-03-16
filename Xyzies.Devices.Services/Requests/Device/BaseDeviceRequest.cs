using System;
using System.ComponentModel.DataAnnotations;

namespace Xyzies.Devices.Services.Requests.Device
{
    /// <summary>
    /// Base device request
    /// </summary>
    public class BaseDeviceRequest
    {
        /// <summary>
        /// Device udid
        /// </summary>
        public string UDID { get; set; }

        /// <summary>
        /// Company id
        /// </summary>
        [Range(0, int.MaxValue)]
        public int CompanyId { get; set; }

        /// <summary>
        /// Branch id
        /// </summary>
        public Guid BranchId { get; set; }

        /// <summary>
        /// Hexnode Udid
        /// </summary>
        public string HexnodeUdid { get; set; }

        /// <summary>
        /// Device Name
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// Latitude
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Longitude
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Radius
        /// </summary>
        [Range(0, double.MaxValue)]
        public double Radius { get; set; }

        [Phone]
        public string Phone { get; set; }
    }
}
