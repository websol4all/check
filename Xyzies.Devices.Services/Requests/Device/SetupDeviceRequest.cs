using System.ComponentModel.DataAnnotations;

namespace Xyzies.Devices.Services.Requests.Device
{
    /// <summary>
    /// Create device request
    /// </summary>
    public class CreateDeviceRequest : BaseDeviceRequest
    {
        /// <summary>
        /// Udid
        /// </summary>
        [Required]
        public string Udid { get; set; }
    }
}
