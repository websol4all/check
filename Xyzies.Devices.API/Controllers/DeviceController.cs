using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading.Tasks;
using Xyzies.Devices.Services.Requests.Device;
using Xyzies.Devices.Services.Service.Interfaces;
using IdentityServiceClient.Filters;
using System.Collections.Generic;
using IdentityServiceClient;
using Xyzies.Devices.Services.Exceptions;
using Xyzies.Devices.Services.Models.DeviceModels;
using Xyzies.Devices.Services.Models.DeviceHistory;
using Xyzies.Devices.Services.Models;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.API.Models;

namespace Xyzies.Devices.API.Controllers
{
    /// <summary>
    /// Device controller
    /// </summary>
    [ApiController]
    [Route("devices")]
    [Authorize]
    public class DeviceController : BaseController
    {
        private readonly ILogger<DeviceController> _logger = null;
        private readonly IDeviceService _deviceService = null;

        /// <summary>
        /// Device constructor
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="deviceService"></param>
        public DeviceController(ILogger<DeviceController> logger,
            IDeviceService deviceService)
        {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _deviceService = deviceService ??
                throw new ArgumentNullException(nameof(deviceService));
        }

        /// <summary>
        /// Create device
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [AccessFilter(Const.Permissions.Device.Create)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created  /* 201 */)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest /* 400 */)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized /* 401 */)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden /* 403 */)]
        [SwaggerOperation(Tags = new[] { "Device API" })]
        public async Task<IActionResult> Post([FromBody] CreateDeviceRequest request)
        {
            try
            {
                var deviceId = await _deviceService.Create(request, Token);
                return Created(HttpContext.Request.GetEncodedUrl(), deviceId);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Create device
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("setup")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created  /* 201 */)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest /* 400 */)]
        [SwaggerOperation(Tags = new[] { "Device API" })]
        public async Task<IActionResult> PendingPost([FromBody] SetupDeviceRequest request)
        {
            try
            {
                var deviceId = await _deviceService.Setup(request);
                return Created(HttpContext.Request.GetEncodedUrl(), deviceId);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Update device
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [AccessFilter(Const.Permissions.Device.Update)]
        [ProducesResponseType(typeof(NoContentResult), StatusCodes.Status204NoContent  /* 204 */)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest /* 400 */)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized /* 401 */)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden /* 403 */)]
        [ProducesResponseType(typeof(NotFoundResult), StatusCodes.Status404NotFound /* 404 */)]
        [SwaggerOperation(Tags = new[] { "Device API" })]
        public async Task<IActionResult> Put(Guid id, [FromBody] BaseDeviceRequest request)
        {
            try
            {
                await _deviceService.Update(request, id, Token);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Device PUT Error: {ex.Message} : {ex.StackTrace} : {ex.Source}", ex.Message, ex.StackTrace, ex.Source);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Update device
        /// </summary>
        /// <param name="id"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [AccessFilter(Const.Permissions.Device.Delete)]
        [ProducesResponseType(typeof(OkResult), StatusCodes.Status200OK  /* 200 */)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized /* 401 */)]
        [ProducesResponseType(typeof(ForbidResult), StatusCodes.Status403Forbidden /* 403 */)]
        [ProducesResponseType(typeof(NotFoundResult), StatusCodes.Status404NotFound /* 404 */)]
        [SwaggerOperation(Tags = new[] { "Device API" })]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _deviceService.Delete(id, Token);
                return Ok();
            }
            catch (AccessException ex)
            {
                return new ContentResult { StatusCode = 403, Content = ex.Message };
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Get devices list
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="lazyLoadFilters"></param>
        /// <param name="sorting"></param>
        /// <returns></returns>
        [HttpGet]
        [AccessFilter(Const.Permissions.Device.Read)]
        [ProducesResponseType(typeof(LazyLoadedResult<DeviceModel>), StatusCodes.Status200OK  /* 200 */)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest /* 400 */)]
        [ProducesResponseType(typeof(ForbidResult), StatusCodes.Status403Forbidden /* 403 */)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized /* 401 */)]
        [SwaggerOperation(Tags = new[] { "Device API" })]
        public async Task<IActionResult> GetAll([FromQuery]FilteringModel filter, [FromQuery]LazyLoadParameters lazyLoadFilters, [FromQuery] Sorting sorting)
        {
            try
            {
                var devices = await _deviceService.GetAll(filter, lazyLoadFilters, sorting, Token);
                return Ok(devices);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest($"{ex.ParamName} ---- {ex.Message}");
            }
            catch (ApplicationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Get device by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [AccessFilter(Const.Permissions.Device.Read)]
        [ProducesResponseType(typeof(DeviceModel), StatusCodes.Status200OK  /* 200 */)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest /* 400 */)]
        [ProducesResponseType(typeof(ForbidResult), StatusCodes.Status403Forbidden /* 403 */)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized /* 401 */)]
        [SwaggerOperation(Tags = new[] { "Device API" })]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                var devices = await _deviceService.GetById(Token, id);
                return Ok(devices);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (AccessException ex)
            {
                return new ContentResult { StatusCode = 403, Content = ex.Message };
            }
        }

        /// <summary>
        /// Get Phones By UDID
        /// </summary>
        /// <param name="udid"></param>
        /// <returns></returns>
        [HttpGet("phones/{udid}")]
        [ProducesResponseType(typeof(DevicePhonesModel), StatusCodes.Status200OK  /* 200 */)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest /* 400 */)]
        [ProducesResponseType(typeof(ForbidResult), StatusCodes.Status403Forbidden /* 403 */)]
        [ProducesResponseType(typeof(NotFoundResult), StatusCodes.Status404NotFound /* 404 */)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized /* 401 */)]
        [SwaggerOperation(Tags = new[] { "Device API" })]
        public async Task<IActionResult> GetPhonesByUDID([FromRoute]string udid)
        {
            try
            {
                var devicePhones = await _deviceService.GetDevicePhonesByUdidAsync(udid, Token);

                return Ok(devicePhones);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (AccessException ex)
            {
                return new ContentResult { StatusCode = 403, Content = ex.Message };
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
