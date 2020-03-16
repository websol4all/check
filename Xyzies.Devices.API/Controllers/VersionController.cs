using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Xyzies.Devices.API.Options;

namespace Xyzies.Devices.API.Controllers
{
    [ApiController, Route("api/version")]
    public class VersionController : ControllerBase
    {
        private readonly string _serviceName;
        private readonly string _version;
        private readonly string _buildNumber;

        public VersionController(IOptionsMonitor<AssemblyOptions> optionsMonitor)
        {
            _serviceName = optionsMonitor?.CurrentValue?.Name ??
                 throw new ArgumentNullException(nameof(VersionController));
            _version = optionsMonitor?.CurrentValue?.Version ??
                 throw new ArgumentNullException(nameof(VersionController));
            _buildNumber = optionsMonitor?.CurrentValue?.BuildNumber ??
                 throw new ArgumentNullException(nameof(VersionController));
        }

        [HttpGet, HttpHead]
        public async Task<IActionResult> Get()
        {
            bool isHttp20 = Request.Protocol == "HTTP/2";

            return Ok(new
            {
                ServiceName = _serviceName,
                ServiceVersion = _version,
                BuildNumber = _buildNumber,
                ReleaseDate = DateTime.Now.ToShortDateString(),
                UUseHttp2 = isHttp20
            });
        }
    }
}