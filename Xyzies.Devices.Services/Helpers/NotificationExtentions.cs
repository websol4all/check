using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xyzies.Devices.Data.Repository.Behaviour;
using Xyzies.Devices.Services.Common.Enums;
using Xyzies.Devices.Services.Models;
using Xyzies.Devices.Services.Models.Comment;
using Xyzies.Devices.Services.Models.DeviceNotification;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Services.Helpers
{
    public static class NotificationExtentions
    {
        public static Func<CancellationToken, Task> GetNotificationMethod(DeviceNotificationModel devNotModel, IServiceScopeFactory _serviceScopeFactory)
        {
            return async (CancellationToken) => {
                using (var scope = _serviceScopeFactory.CreateScope())
                    await SendAlertByOnlineOfflineOutOfLocationDevice(scope, devNotModel);
            };
        }

        public async static Task SendAlertByOnlineOfflineOutOfLocationDevice(IServiceScope scope, DeviceNotificationModel deviceModel)
        {
            var deviceService = scope.ServiceProvider.GetService<IDeviceService>();
            var deviceHistoryRepository = scope.ServiceProvider.GetService<IDeviceHistoryRepository>();
            var httpService = scope.ServiceProvider.GetService<IHttpService>();
            var loggerServive = scope.ServiceProvider.GetService<ILogger<NotificationSender>>();
            DateTime previousHeartBeat = default;

            var deviceFromDb = await deviceService.GetDeviceByUdidAsync(deviceModel.Udid);

            loggerServive.LogInformation($"Prepare notification for {deviceModel.Udid} ,  Type notification: {deviceModel.FuncType.ToString()}",
                deviceModel.Udid, deviceModel.FuncType.ToString());

            if (deviceModel.FuncType == SelectFunc.Online)
            {
                previousHeartBeat = (await deviceHistoryRepository.GetAsync(x => x.DeviceId == deviceFromDb.Id))
                    .OrderByDescending(x => x.CreatedOn)
                    .FirstOrDefault(x => x.IsOnline == true)
                    .CreatedOn;
            }
            else
            {
                previousHeartBeat = DateTime.UtcNow;
            }

            #region Get Branch

            var branch = (await httpService.GetBranchesTrustedAsync()).Find(x => x.Id == deviceFromDb.BranchId);
            #endregion

            #region Get User Role

            var companyModel = (await httpService.GetCompaniesForTrustedAsync()).Find(x => x.Id == deviceFromDb.CompanyId);
            Filters filter = new Filters();
            filter.UsersId = companyModel?.UserId;
            filter.Role = new string[] { "supervisor" };

            string queryUsersByRole = JsonConvert.SerializeObject(filter);

            var users = await httpService.GetUsersByIdTrustedAsync(queryUsersByRole);
            #endregion
            TimeZoneInfo timeZoneInfo;
            try
            {
                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Mountain Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                loggerServive.LogWarning("Notification Time Zone Exeption");
                timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
            }

            EmailParametersModel model = new EmailParametersModel()
            {
                UDID = deviceModel.Udid,
                EmailsTo = Enumerable.Empty<string>(),//TODO: uncomment in future with users.Select(x => x.Email)/
                Cause = deviceModel.FuncType.ToString().ToLower(),
                Address = branch?.Address,
                Town = branch?.City,
                PostCode = branch?.ZipCode,
                Country = "USA",
                Notes = "",
                LastHeartBeat = TimeZoneInfo.ConvertTimeFromUtc(deviceModel.LastHeartBeat, timeZoneInfo),
                PreviousHeartBeat = TimeZoneInfo.ConvertTimeFromUtc(previousHeartBeat, timeZoneInfo)
            };

            string query = JsonConvert.SerializeObject(model);

            await httpService.PostNotificationEmailAsync(query);
            loggerServive.LogInformation($"Notification for {deviceModel.Udid} sent to service,  Type notification: {deviceModel.FuncType.ToString()}",
                deviceModel.Udid, deviceModel.FuncType.ToString());
        }
    }
}
