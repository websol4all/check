using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Services.Models.Comment;

namespace Xyzies.Devices.Services.Service.Interfaces
{
    public interface ICommentService
    {
        /// <summary>
        /// Get list of comments by device Id
        /// </summary>
        /// <param name="token"></param>
        /// <param name="deviceId"></param>
        /// <returns>CommentModel</returns>
        Task<LazyLoadedResult<CommentModel>> GetAllByDeviceIdAsync(string token, Guid deviceId, LazyLoadParameters filters = null);

        /// <summary>
        /// Create comment for device
        /// </summary>
        /// <param name="token"></param>
        /// <param name="deviceId"></param>
        /// <param name="comment"></param>
        /// <returns>Guid</returns>
        Task<Guid> CreateAsync(string token, Guid deviceId, string comment);

    }
}
