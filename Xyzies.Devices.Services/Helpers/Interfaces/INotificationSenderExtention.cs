using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Services.Common.Enums;
using Xyzies.Devices.Services.Models;

namespace Xyzies.Devices.Services.Helpers.Interfaces
{
    public interface INotificationSender
    {
        Task SendAlertOnOffLinePrepareByExpirationTime(SelectFunc funcType, string udid);
        
        Task SendAlertInOutlocationPrepareByExpirationTime(SelectFunc funcType, string udid);

        Task NotificationForChangeLocation(string udid, bool calcIsLocation);
    }
}
