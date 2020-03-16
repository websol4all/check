using System;
using System.Threading.Tasks;
using IdentityServiceClient;
using IdentityServiceClient.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using Xyzies.Devices.API.Models;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Services.Exceptions;
using Xyzies.Devices.Services.Models.Comment;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.API.Controllers
{
    [ApiController]
    [Route("devices/{deviceId}/comments")]
    [Authorize]
    public class DeviceCommentsController : BaseController
    {
        private readonly ILogger<DeviceCommentsController> _logger = null;
        private readonly ICommentService _commentService = null;

        public DeviceCommentsController(ILogger<DeviceCommentsController> logger,
            ICommentService commentService)
        {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _commentService = commentService ??
                throw new ArgumentNullException(nameof(commentService));
        }

        /// <summary>
        /// Get list of comments by device Id
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="filters"></param>
        /// <returns></returns>
        [HttpGet]
        [AccessFilter(Const.Permissions.Comment.Read)]
        [ProducesResponseType(typeof(LazyLoadedResult<CommentModel>), StatusCodes.Status200OK  /* 200 */)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest /* 400 */)]
        [ProducesResponseType(typeof(ForbidResult), StatusCodes.Status403Forbidden /* 403 */)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized /* 401 */)]
        [SwaggerOperation(Tags = new[] { "Device Management API" })]
        public async Task<IActionResult> Get(Guid deviceId, [FromQuery]LazyLoadParameters filters)
        {
            try
            {
                var comments = await _commentService.GetAllByDeviceIdAsync(Token, deviceId, filters);
                return Ok(comments);
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
            catch (AccessException ex)
            {
                _logger.LogError(ex.Message);
                return new ContentResult { StatusCode = 403, Content = ex.Message };
            }
        }

        /// <summary>
        /// Add comment for device
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="commentString"></param>
        /// <returns></returns>
        [HttpPost]
        [AccessFilter(Const.Permissions.Comment.Create)]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created  /* 201 */)]
        [ProducesResponseType(typeof(BadRequestResult), StatusCodes.Status400BadRequest /* 400 */)]
        [ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized /* 401 */)]
        [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden /* 403 */)]
        [SwaggerOperation(Tags = new[] { "Device Management API" })]
        public async Task<IActionResult> Post(Guid deviceId, [FromBody] CommentRequestModel commentString)
        {
            try
            {
                var commentId = await _commentService.CreateAsync(Token, deviceId, commentString.Comment);
                return Created(HttpContext.Request.GetEncodedUrl(), commentId);
            }
            catch (ArgumentNullException ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }
    }
}