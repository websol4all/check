using IdentityServiceClient;
using IdentityServiceClient.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Services.Exceptions;
using Xyzies.Devices.Services.Models.DeviceHistory;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.API.Controllers
{
    /// <summary>
    /// Device history controller
    /// </summary>
    [ApiController]
    [Route("devices/{deviceId}/history")]
    [Authorize]
    public class DeviceHistoryController : BaseController
    {
        private readonly ILogger<DeviceHistoryController> _logger = null;
        private readonly IDeviceHistoryService _deviceHistoryService = null;

        /// <summary>
        /// Device history constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="deviceHistoryService"></param>
        public DeviceHistoryController(ILogger<DeviceHistoryController> logger,
            IDeviceHistoryService deviceHistoryService)
        {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _deviceHistoryService = deviceHistoryService ??
                throw new ArgumentNullException(nameof(deviceHistoryService));
        }

        /// <summary>
        /// Get device history by device id
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="filters"></param>
        /// <returns></returns>
        [HttpGet]
        [AccessFilter(Const.Permissions.History.Read)]
        [ProducesResponseType(typeof(LazyLoadedResult<DeviceHistoryModel>), StatusCodes.Status200OK  /* 200 */)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest /* 400 */)]
        [ProducesResponseType(typeof(ForbidResult), StatusCodes.Status403Forbidden /* 403 */)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized /* 401 */)]
        [ProducesResponseType(typeof(NotFoundResult), StatusCodes.Status404NotFound /* 404 */)]
        [SwaggerOperation(Tags = new[] { "Device API" })]
        public async Task<IActionResult> GetHistory(Guid deviceId, [FromQuery]LazyLoadParameters filters)
        {
            try
            {
                var deviceHistory = await _deviceHistoryService.GetHistoryByDeviceId(Token, deviceId, filters);
                _logger.LogInformation($"[GetHistory], deviceId = {deviceId}, status code = {StatusCodes.Status200OK}", deviceId, filters);
                return Ok(deviceHistory);
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogInformation($"[GetHistory], deviceId = {deviceId}, status code = {StatusCodes.Status400BadRequest}, error = ex.Message", deviceId, filters);
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogInformation($"[GetHistory], deviceId = {deviceId}, status code = {StatusCodes.Status404NotFound}, error = ex.Message", deviceId, filters);
                return NotFound(ex.Message);
            }
            catch (AccessException ex)
            {
                _logger.LogInformation($"[GetHistory], deviceId = {deviceId}, status code = {StatusCodes.Status403Forbidden}, error = ex.Message", deviceId, filters);
                return new ContentResult { StatusCode = 403, Content = ex.Message };
            }
        }
    }
}
