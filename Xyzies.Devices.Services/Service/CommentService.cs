using Mapster;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Data.Repository.Behaviour;
using Xyzies.Devices.Services.Helpers;
using Xyzies.Devices.Services.Models.Comment;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Services.Service
{
    public class CommentService : ICommentService
    {
        private readonly ILogger<CommentService> _logger = null;
        private readonly IHttpService _httpService = null;
        private readonly ICommentRepository _commentRepository = null;
        private readonly IDeviceRepository _deviceRepository = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="httpService"></param>
        /// <param name="commentRepository"></param>
        public CommentService(ILogger<CommentService> logger,
            IHttpService httpService,
            ICommentRepository commentRepository,
            IDeviceRepository deviceRepository)
        {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _httpService = httpService ??
                throw new ArgumentNullException(nameof(httpService));
            _commentRepository = commentRepository ??
                throw new ArgumentNullException(nameof(commentRepository));
            _deviceRepository = deviceRepository ??
                throw new ArgumentNullException(nameof(deviceRepository));
        }

        /// <inheritdoc />
        public async Task<LazyLoadedResult<CommentModel>> GetAllByDeviceIdAsync(string token, Guid deviceId, LazyLoadParameters filters = null)
        {
            var comments = (await _commentRepository.GetAllAsync(x => x.DeviceId == deviceId, filters)).Adapt<LazyLoadedResult<CommentModel>>();

            Filters filter = new Filters();
            filter.UsersId = comments.Result.Select(x => x.UserId.ToString());

            string query = JsonConvert.SerializeObject(filter);
            var users = await _httpService.GetUsersByIdTrustedAsync(query);

            if (comments.Result.Count() > 0)
            {
                users.ForEach(x => comments.Result.FirstOrDefault(y => y.UserId == x.Id).UserName = x.DisplayName);
            }

            return comments;
        }

        /// <inheritdoc />
        public async Task<Guid> CreateAsync(string token, Guid deviceId, string comment)
        {
           if (string.IsNullOrWhiteSpace(token))
           {
                _logger.LogError("Token is empty");
               throw new ArgumentNullException(nameof(token));
           }

           if (await _deviceRepository.HasAsync(deviceId))
           {
               var user = await _httpService.GetCurrentUser(token);

               return await _commentRepository.AddAsync(new Comment()
               {
                    Message = comment,
                    UserId = user.Id,
                    UserName =$"{user.GivenName} {user.Surname}",
                    CreateOn = DateTime.UtcNow,
                    DeviceId = deviceId
               });
           }
           else
           {
               throw new ArgumentException(nameof(deviceId));
           }
        }
    }
}
