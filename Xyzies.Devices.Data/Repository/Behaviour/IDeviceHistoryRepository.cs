﻿using System;
using Xyzies.Devices.Data.Core;
using Xyzies.Devices.Data.Entity;

namespace Xyzies.Devices.Data.Repository.Behaviour
{
    public interface IDeviceHistoryRepository : IRepository<Guid, DeviceHistory>
    {
    }
}
