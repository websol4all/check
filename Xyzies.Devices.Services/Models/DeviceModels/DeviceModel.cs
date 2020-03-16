using Newtonsoft.Json;
using System;
using Xyzies.Devices.Services.Common.Enums;
using Xyzies.Devices.Services.Models.Branch;
using Xyzies.Devices.Services.Models.Company;
using Xyzies.Devices.Services.Models.Tenant;
using Xyzies.Devices.Services.Models.User;

namespace Xyzies.Devices.Services.Models.DeviceModels
{
    public class DeviceModel
    {
        public Guid Id { get; set; }

        /// <summary>
        /// VDID
        /// </summary>
        public string Udid { get; set; }

        public string HexnodeUdid { get; set; }

        public string DeviceName { get; set; }

        public bool IsOnline { get; set; }

        public DateTime? StatusSince { get; set; }

        public bool IsInLocation { get; set; }

        public bool IsPending { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Radius { get; set; }

        public UserModel LoggedInAs { get; set; }

        public CompanyModel Company { get; set; }

        public BranchModel Branch { get; set; }

        public DateTime CreatedOn { get; set; }

        public string Phone { get; set; }

        public bool IsDeleted { get; set; }

        public TenantModel Tenant { get; set; }

        public double? CurrentDeviceLocationLatitude { get; set; }

        public double? CurrentDeviceLocationLongitude { get; set; }

        [JsonIgnore]
        public DeviceStatusType Type {
            get
            {
                if (IsPending)
                    return DeviceStatusType.Waiting;
                else if (IsOnline)
                    return DeviceStatusType.Online;
                else 
                    return DeviceStatusType.Offline;
            }
        }
    }
}
