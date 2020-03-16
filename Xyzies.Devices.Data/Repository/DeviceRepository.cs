using System;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Data.Repository.Behaviour;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Data.Extensions;

namespace Xyzies.Devices.Data.Repository
{
    public class DeviceRepository : EfCoreBaseRepository<Guid, Device>, IDeviceRepository
    {
        public DeviceRepository(DeviceContext dbContext) : base(dbContext)
        {

        }

        public override async Task<IQueryable<Device>> GetAsync(Expression<Func<Device, bool>> predicate) =>
            await Task.FromResult(Data
                .Include(x => x.DeviceHistory)
                .Where(predicate));

        public async Task<IQueryable<Device>> GetWithoutIncludeAsync(Expression<Func<Device, bool>> predicate) =>
           await Task.FromResult(Data
               .Where(predicate));
    }
}
