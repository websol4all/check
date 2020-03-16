using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Services.Models.DeviceHistory;

namespace Xyzies.Devices.Services.Service.Interfaces
{
    public interface IDeviceHistoryService
    {
        /// <summary>
        /// Get history by device Id
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<LazyLoadedResult<DeviceHistoryModel>> GetHistoryByDeviceId(string token, Guid deviceId, LazyLoadParameters filters = null);
    }
}
