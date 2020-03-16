using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xyzies.Devices.Services.Models.DeviceModels;
using Xyzies.Devices.Services.Requests.Device;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Services.Models.DeviceHistory;
using Xyzies.Devices.Services.Models;
using Xyzies.Devices.Data.Common;

namespace Xyzies.Devices.Services.Service.Interfaces
{
    public interface IDeviceService
    {
        /// <summary>
        /// Create device
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<Guid> Create(CreateDeviceRequest request, string token);

        /// <summary>
        /// Setup device
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<Guid> Setup(SetupDeviceRequest request);

        /// <summary>
        /// Update device
        /// </summary>
        /// <param name="request"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task Update(BaseDeviceRequest request, Guid id, string token);

        /// <summary>
        /// Change isDate column on true with device history
        /// </summary>
        /// <param name="id"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task Delete(Guid id, string token);

        /// Get devices list
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<LazyLoadedResult<DeviceModel>> GetAll(FilteringModel filter, LazyLoadParameters lazyLoadFilters, Sorting sorting, string token);

        /// <summary>
        /// Get device by Id
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task<DeviceModel> GetById(string token, Guid id);

        /// <summary>
        /// Get device by Udid
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<Device> GetDeviceByUdidAsync(string Udid);

        Task<DevicePhonesModel> GetDevicePhonesByUdidAsync(string Udid, string token);
    }
}
