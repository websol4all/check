using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using IdentityServiceClient;

using Mapster;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Data.Core.Utils;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Data.Extensions;
using Xyzies.Devices.Data.Repository.Behaviour;
using Xyzies.Devices.Services.Exceptions;
using Xyzies.Devices.Services.Helpers;
using Xyzies.Devices.Services.Helpers.Interfaces;
using Xyzies.Devices.Services.Models;
using Xyzies.Devices.Services.Models.DeviceModels;
using Xyzies.Devices.Services.Models.Tenant;
using Xyzies.Devices.Services.Requests.Device;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Services.Service
{
    public class DeviceService : IDeviceService
    {
        private readonly ILogger<DeviceService> _logger = null;
        private readonly IHttpService _httpService = null;
        private readonly IDeviceRepository _deviceRepository = null;
        private readonly IValidationHelper _validationHelper = null;
        private readonly IDeviceHistoryRepository _deviceHistoryRepository = null;
        private readonly IHubContext<WebHubService> _webHubContext;
        private Dictionary<string, Expression<Func<DeviceModel, object>>> _sortingRules = null;
        private Dictionary<string, Expression<Func<Device, bool>>> _filteringRules = null;
        private readonly INotificationSender _notificationSenderExtention = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="httpService"></param>
        /// <param name="deviceRepository"></param>
        public DeviceService(ILogger<DeviceService> logger,
            IHttpService httpService,
            IDeviceRepository deviceRepository,
            IValidationHelper validationHelper,
            IHubContext<WebHubService> webHubContext,
            IDeviceHistoryRepository deviceHistoryRepository,
            INotificationSender notificationSenderExtention)
        {
            _logger = logger ??
                throw new ArgumentNullException(nameof(logger));
            _httpService = httpService ??
                throw new ArgumentNullException(nameof(httpService));
            _deviceRepository = deviceRepository ??
                throw new ArgumentNullException(nameof(deviceRepository));
            _validationHelper = validationHelper ??
                throw new ArgumentNullException(nameof(validationHelper));
            _webHubContext = webHubContext ??
                throw new ArgumentNullException(nameof(webHubContext));
            _deviceHistoryRepository = deviceHistoryRepository ??
                throw new ArgumentNullException(nameof(deviceHistoryRepository));

            _notificationSenderExtention = notificationSenderExtention ??
                throw new ArgumentNullException(nameof(notificationSenderExtention));

            FillSortingRules();
        }

        /// <inheritdoc />
        public async Task<Guid> Create(CreateDeviceRequest request, string token)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            var devices = await _deviceRepository.GetAsync();
            if (devices.Any(x => x.Udid == request.Udid))
            {
                throw new ApplicationException($"Device with udid: {request.Udid} already exist");
            }

            var scopeArray = new [] { Const.Permissions.Device.AdminCreate };
            var correctCompanyId = await _validationHelper.GetCompanyIdByPermission(token, scopeArray, request.CompanyId);
            if (!correctCompanyId.HasValue)
            {
                throw new ArgumentException("Invalid companyId", nameof(request.CompanyId));
            }
            request.CompanyId = correctCompanyId.Value;

            await _validationHelper.ValidateCompanyAndBranch(request.CompanyId, request.BranchId, token);

            var device = request.Adapt<Device>();
            device.CreatedOn = DateTime.UtcNow;
            return await _deviceRepository.AddAsync(device);
        }

        public async Task<Guid> Setup(SetupDeviceRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (await _deviceRepository.HasAsync(x => x.Udid == request.Udid))
            {
                throw new ApplicationException($"Device with udid: {request.Udid} already exist");
            }
            var device = request.Adapt<Device>();
            device.CreatedOn = DateTime.UtcNow;
            device.IsPending = true;
            return await _deviceRepository.AddAsync(device);
        }

        /// <inheritdoc />
        public async Task<LazyLoadedResult<DeviceModel>> GetAll(FilteringModel filter, LazyLoadParameters lazyLoadFilters, Sorting sorting, string token)
        {
            var resultLazy = new LazyLoadedResult<DeviceModel>();
            var deviceList = new List<DeviceModel>();
            var companyId = await _validationHelper.GetCompanyIdByPermission(token, new [] { Const.Permissions.Device.AdminRead });
            List<TenantFullModel> tenants = new List<TenantFullModel>();
            if (companyId != null)
            {
                filter.CompanyIds = new List<int> { companyId.Value };
                var tenant = await _httpService.GetTenantSingleByCompanyId(companyId.Value, token);
                tenants.Add(tenant);
            }
            else
            {
                if (filter.TenantIds.Any())
                {
                    var tenantIdsString = filter.TenantIds.Select(x => x.ToString()).ToList();
                    tenants = await _httpService.GetTenantsByIds(token, GetQuery("TenantIds", tenantIdsString));
                    var companyIds = tenants.SelectMany(x => x.Companies).Select(x => x.Id).ToList();
                    filter.CompanyIds = filter.CompanyIds != null ? companyIds.Count > 0 ? filter.CompanyIds.Intersect(companyIds).ToList() :
                        null : companyIds;
                }
                else
                {
                    tenants = await _httpService.GetTenantsByIds(token);
                }
            }
            var devices = await _deviceRepository.GetWithoutIncludeAsync(GetFilterExpression(filter));
            var devicesHistories = await _deviceHistoryRepository.GetAsync();

            if (!devices.Any())
            {
                return resultLazy;
            }

            var compIds = (from device in devices select device.CompanyId.ToString()).ToList();

            var branchIds = (from device in devices select device.BranchId.ToString()).ToList();

            var deviceAndLastHistoryQuery = (from device in devices
                                            from history in devicesHistories.Where(x => x.DeviceId == device.Id).OrderByDescending(x => x.CreatedOn).Take(1)
                                            select new
                                            {
                                                Device = device,
                                                LastHistory = history
                                            });

            var usersIds = deviceAndLastHistoryQuery.Where(x => x.LastHistory.LoggedInUserId.HasValue).Select(x => x.LastHistory.LoggedInUserId.ToString().ToLower()).Distinct().ToList();

            //var companies = await _httpService.GetCompaniesByIds(GetQuery("CompanyIds", compIds), token);
            var companyTenant = tenants.SelectMany(tenant => tenant.Companies, (tenant, company) => new
            {
                Company = company,
                    Tenant = tenant
            });
            var branches = await _httpService.GetBranchesByIds(GetQuery("BranchIds", branchIds), token);
            var users = await _httpService.GetUsersByIdsAsync(token, GetQuery("UsersId", usersIds));

            var fdeviceList = (from deviceInfo in deviceAndLastHistoryQuery
                               join company in companyTenant on deviceInfo.Device.CompanyId equals company.Company.Id into _c
                               from company in _c.DefaultIfEmpty()
                               join branch in branches on deviceInfo.Device.BranchId equals branch.Id into _b
                               from branch in _b.DefaultIfEmpty()
                               join user in users on deviceInfo.LastHistory.LoggedInUserId equals user.Id into _u
                               from user in _u.DefaultIfEmpty()
                               select new DeviceModel
                               {
                                   Id = deviceInfo.Device.Id,
                                   Udid = deviceInfo.Device.Udid,
                                   CreatedOn = deviceInfo.Device.CreatedOn,
                                   HexnodeUdid = deviceInfo.Device.HexnodeUdid,
                                   DeviceName = deviceInfo.Device.DeviceName,
                                   IsPending = deviceInfo.Device.IsPending,
                                   Latitude = deviceInfo.Device.Latitude,
                                   Longitude = deviceInfo.Device.Longitude,
                                   Radius = deviceInfo.Device.Radius,
                                   IsDeleted = deviceInfo.Device.IsDeleted,
                                   IsOnline = deviceInfo.LastHistory.IsOnline,
                                   IsInLocation = deviceInfo.LastHistory.IsInLocation,
                                   StatusSince = deviceInfo.LastHistory.CreatedOn,
                                   Branch = branch,
                                   Company = company.Company,
                                   Tenant = company.Tenant.Adapt<TenantModel>(),
                                   LoggedInAs = user,
                                   Phone = deviceInfo.Device.Phone,
                                   CurrentDeviceLocationLatitude = deviceInfo.LastHistory.CurrentDeviceLocationLatitude,
                                   CurrentDeviceLocationLongitude = deviceInfo.LastHistory.CurrentDeviceLocationLongitude,
                               });

            resultLazy = GetSorted(fdeviceList, sorting).GetPart(lazyLoadFilters);

            return resultLazy;
        }

        public async Task<DeviceModel> GetById(string token, Guid id)
        {
            var device = await _deviceRepository.GetByAsync(x => x.Id == id);
            var companyId = await _validationHelper.GetCompanyIdByPermission(token, new [] { Const.Permissions.Device.AdminRead });
            if (companyId != null && device.CompanyId != companyId)
            {
                throw new AccessException();
            }
            var result = device.Adapt<DeviceModel>();
            result.Branch = await _httpService.GetBranchById(device.BranchId, token);
            result.Company = await _httpService.GetCompanyById(device.CompanyId, token);
            return result;
        }

        /// <inheritdoc />
        public async Task Update(BaseDeviceRequest request, Guid id, string token)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            var device = await _deviceRepository.GetAsync(id);
            if (device == null)
            {
                throw new KeyNotFoundException($"Device with id: {id} not found");
            }

            var scopeArray = new [] { Const.Permissions.Device.AdminUpdate };
            var correctCompanyId = await _validationHelper.GetCompanyIdByPermission(token, scopeArray, request.CompanyId);
            if (!correctCompanyId.HasValue)
            {
                throw new ArgumentException("Invalid companyId", nameof(request.CompanyId));
            }
            request.CompanyId = correctCompanyId.Value;

            await _validationHelper.ValidateCompanyAndBranch(request.CompanyId, request.BranchId, token);

            device.CompanyId = request.CompanyId;
            device.DeviceName = request.DeviceName;
            device.BranchId = request.BranchId;
            device.Latitude = request.Latitude;
            device.Longitude = request.Longitude;
            device.Radius = request.Radius;
            device.HexnodeUdid = request.HexnodeUdid;
            device.Phone = request.Phone;
            device.IsPending = false;

            await _deviceRepository.UpdateAsync(device);

            await AddHistoryIfLocationChanged(request, device.Id);
        }

        /// <inheritdoc />
        public async Task<Device> GetDeviceByUdidAsync(string Udid)
        {
            if (string.IsNullOrWhiteSpace(Udid))
            {
                throw new ArgumentNullException(nameof(Udid));
            }

            return await _deviceRepository.GetByAsync(x => x.Udid == Udid);

        }

        /// <inheritdoc />
        public async Task Delete(Guid id, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            var device = await _deviceRepository.GetAsync(id);
            if (device == null)
            {
                throw new KeyNotFoundException($"Device with id: {id} not found");
            }

            var scopeArray = new [] { Const.Permissions.Device.AdminDelete };
            var companyId = await _validationHelper.GetCompanyIdByPermission(token, scopeArray, device.CompanyId);

            if (companyId != device.CompanyId)
            {
                throw new AccessException();
            }

            device.IsDeleted = true;
            await _deviceRepository.UpdateAsync(device);
        }

        public async Task<DevicePhonesModel> GetDevicePhonesByUdidAsync(string Udid, string token)
        {
            if (string.IsNullOrWhiteSpace(Udid))
            {
                throw new ArgumentNullException(nameof(Udid));
            }
            var device = await _deviceRepository.GetByAsync(x => x.Udid == Udid);
            var user = await _httpService.GetCurrentUser(token);
            var tenant = await _httpService.GetTenantSingleByCompanyId(user.CompanyId.Value, token);
            if (device == null)
            {
                throw new KeyNotFoundException();
            }

            return new DevicePhonesModel()
            {
                DevicePhoneNumber = device.Phone,
                    TenantPhoneNumber = tenant?.Phone
            };
        }

        #region Private helpers

        private Expression<Func<Device, bool>> GetFilterExpression(FilteringModel filter)
        {
            FillFiltering(filter);
            Expression<Func<Device, bool>> expression = (Device device) => device.IsDeleted == false;
            foreach (var prop in filter.GetType().GetProperties())
            {
                if (prop.GetValue(filter) != null)
                {
                    expression = expression.AND(_filteringRules.GetValueOrDefault(prop.Name.ToLower()) ?? expression);
                }
            }
            return expression;
        }

        private IQueryable<DeviceModel> GetSorted(IQueryable<DeviceModel> devices, Sorting sorting)
        {
            Expression<Func<DeviceModel, object>> sortFunc = x => x.CreatedOn;
            if (!string.IsNullOrWhiteSpace(sorting.SortBy))
            {
                sortFunc = _sortingRules.GetValueOrDefault(sorting.SortBy.ToLower()) ?? sortFunc;
            }
            return sorting.IsAscending ?
                devices.OrderBy(sortFunc) : devices.OrderByDescending(sortFunc);
        }

        private string GetQuery(string param, IEnumerable<string> values)
        {
            return $"{param}={string.Join($"&{param}=", values.Distinct())}";
        }

        private void FillSortingRules()
        {
            _sortingRules = new Dictionary<string, Expression<Func<DeviceModel, object>>>();
            _sortingRules.Add(nameof(DeviceModel.Id).ToLower(), x => x.Id);
            _sortingRules.Add(nameof(DeviceModel.Udid).ToLower(), x => x.Udid);
            _sortingRules.Add(nameof(DeviceModel.StatusSince).ToLower(), x => x.StatusSince);
            _sortingRules.Add(nameof(DeviceModel.IsOnline).ToLower(), x => x.Type);
            _sortingRules.Add(nameof(DeviceModel.IsInLocation).ToLower(), x => x.IsInLocation);
            _sortingRules.Add(nameof(DeviceModel.Branch.BranchName).ToLower(), x => x.Branch != null ? x.Branch.BranchName : default);
            _sortingRules.Add(nameof(DeviceModel.Company.CompanyName).ToLower(), x => x.Company != null ? x.Company.CompanyName : default);
            _sortingRules.Add(nameof(DeviceModel.Tenant.Name).ToLower(), x => x.Tenant != null ? x.Tenant.Name : default);
            _sortingRules.Add(nameof(DeviceModel.LoggedInAs.DisplayName).ToLower(), x => x.LoggedInAs != null ? x.LoggedInAs.DisplayName : default);
            _sortingRules.Add(nameof(DeviceModel.DeviceName).ToLower(), x => x.DeviceName);
        }

        private void FillFiltering(FilteringModel filter)
        {
            _filteringRules = new Dictionary<string, Expression<Func<Device, bool>>>();
            Expression<Func<Device, bool>> quickSearchFunc = x => !string.IsNullOrEmpty(x.DeviceName) ? x.DeviceName.ToLower().Contains(filter.SearchPhrase.ToLower()) ||
                x.Udid.ToLower().Contains(filter.SearchPhrase.ToLower()) ||
                x.Id.ToString().ToLower().Contains(filter.SearchPhrase.ToLower()) :
                x.Udid.ToLower().Contains(filter.SearchPhrase.ToLower()) ||
                x.Id.ToString().ToLower().Contains(filter.SearchPhrase.ToLower());
            _filteringRules.Add(nameof(FilteringModel.CompanyIds).ToLower(), (Device device) => filter.CompanyIds.Contains(device.CompanyId));
            _filteringRules.Add(nameof(FilteringModel.IsOnline).ToLower(), (Device device) => device.DeviceHistory.Any() &&
                device.DeviceHistory.OrderByDescending(x => x.CreatedOn).Take(1).All(x => x.IsOnline == filter.IsOnline));
            _filteringRules.Add(nameof(FilteringModel.BranchIds).ToLower(), (Device device) => filter.BranchIds.Contains(device.BranchId));
            _filteringRules.Add(nameof(FilteringModel.SearchPhrase).ToLower(), quickSearchFunc);
        }

        private async Task AddHistoryIfLocationChanged(BaseDeviceRequest request, Guid Id)
        {
            var deviceFromDb = await _deviceRepository.GetByAsync(x => x.Id == Id);
            var lastDeviceHistory = (await _deviceHistoryRepository.GetAsync(x=>x.DeviceId == deviceFromDb.Id))
                .OrderByDescending(y => y.CreatedOn)
                .FirstOrDefault();

            if (lastDeviceHistory != null)
            {
                bool calcIsLocation = CalculateDistanceForDevice.DeviceIsInLocation(
                    lastDeviceHistory.CurrentDeviceLocationLatitude,
                    lastDeviceHistory.CurrentDeviceLocationLongitude,
                    request.Latitude,
                    request.Longitude,
                    request.Radius);

                await AddAndSendDeviceHistory(request.UDID, new DeviceHistory()
                {
                    CreatedOn = DateTime.UtcNow,
                        IsOnline = lastDeviceHistory.IsOnline,
                        CurrentDeviceLocationLatitude = lastDeviceHistory.CurrentDeviceLocationLatitude,
                        CurrentDeviceLocationLongitude = lastDeviceHistory.CurrentDeviceLocationLongitude,
                        DeviceLocationLatitude = request.Latitude,
                        DeviceLocationLongitude = request.Longitude,
                        DeviceRadius = request.Radius,
                        LoggedInUserId = lastDeviceHistory?.LoggedInUserId ?? null,
                        DeviceId = Id,
                        CompanyId = request.CompanyId,
                        IsInLocation = calcIsLocation
                });

                DeviceUpdateLocationModel device = new DeviceUpdateLocationModel()
                {
                    Lat = lastDeviceHistory.CurrentDeviceLocationLatitude,
                    Long = lastDeviceHistory.CurrentDeviceLocationLongitude
                };

                _logger.LogError("Before Send Notification in DeviceService");
                await _notificationSenderExtention.NotificationForChangeLocation(request.UDID, calcIsLocation);
            }

        }

        private async Task AddAndSendDeviceHistory(string udid, DeviceHistory deviceHistory)
        {
            var gui = await _deviceHistoryRepository.AddAsync(deviceHistory);
            await _webHubContext.Clients.Group(udid).SendAsync("DeviceUpdated", JsonConvert.SerializeObject(deviceHistory));
        }

        #endregion
    }
}
