using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Data.Repository.Behaviour;

namespace Xyzies.Devices.Data.Repository
{
    public class DeviceHistoryRepository: EfCoreBaseRepository<Guid, DeviceHistory>, IDeviceHistoryRepository
    {
        public DeviceHistoryRepository(DeviceContext dbContext) : base(dbContext)
        {
        }

        /// <inheritdoc />
        public override async Task<IQueryable<DeviceHistory>> GetAsync(Expression<Func<DeviceHistory, bool>> predicate) =>
            await Task.FromResult(Data.Include(x=>x.Device).Where(predicate));
    }
}
