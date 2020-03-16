using IdentityServiceClient;
using Mapster;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Data.Extensions;
using Xyzies.Devices.Data.Repository.Behaviour;
using Xyzies.Devices.Services.Exceptions;
using Xyzies.Devices.Services.Helpers.Interfaces;
using Xyzies.Devices.Services.Models.DeviceHistory;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Services.Service
{
    public class DeviceHistoryService : IDeviceHistoryService
    {
        private readonly ILogger<DeviceHistoryService> _logger = null;
        private readonly IDeviceRepository _deviceRepository = null;
        private readonly IValidationHelper _validationHelper = null;
        private readonly IDeviceHistoryRepository _deviceHistoryRepository = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="httpService"></param>
        /// <param name="deviceRepository"></param>
        public DeviceHistoryService(ILogger<DeviceHistoryService> logger,
            IDeviceRepository deviceRepository,
            IValidationHelper validationHelper,
            IDeviceHistoryRepository deviceHistoryRepository)
        {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _deviceRepository = deviceRepository ??
                throw new ArgumentNullException(nameof(deviceRepository));
            _validationHelper = validationHelper ??
                throw new ArgumentNullException(nameof(validationHelper));
            _deviceHistoryRepository = deviceHistoryRepository ??
                throw new ArgumentNullException(nameof(deviceHistoryRepository));
        }

        /// <inheritdoc />
        public async Task<LazyLoadedResult<DeviceHistoryModel>> GetHistoryByDeviceId(string token, Guid deviceId, LazyLoadParameters filters = null)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            var device = await _deviceRepository.GetAsync(deviceId);
            if (device == null)
            {
                throw new KeyNotFoundException($"Device with id: {deviceId} not found");
            }

            IQueryable<DeviceHistory> deviceHistory = await _deviceHistoryRepository.GetAsync(x => x.DeviceId == device.Id);

            var scopeArray = new[] { Const.Permissions.History.AdminRead };
            var companyId = await _validationHelper.GetCompanyIdByPermission(token, scopeArray);

            if (companyId.HasValue && companyId != device.CompanyId)
            {
                throw new AccessException();
            }
            else if (companyId == device.CompanyId)
            {
                deviceHistory = deviceHistory.Where(x => x.CompanyId == device.CompanyId);
            }

            return deviceHistory.OrderByDescending(x=>x.CreatedOn).GetPart(filters).Adapt<LazyLoadedResult<DeviceHistoryModel>>();
        }
    }
}
