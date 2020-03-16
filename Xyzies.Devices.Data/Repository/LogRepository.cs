using System;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Data.Repository.Behaviour;

namespace Xyzies.Devices.Data.Repository
{
    public class LogRepository : EfCoreBaseRepository<Guid, Log>, ILogRepository
    {
        public LogRepository(DeviceContext dbContext) : base(dbContext)
        {
        }
    }
}
