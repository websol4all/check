using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Data.Core;
using Xyzies.Devices.Data.Entity;

namespace Xyzies.Devices.Data.Repository.Behaviour
{
    public interface IDeviceRepository : IRepository<Guid, Device>
    {
        Task<IQueryable<Device>> GetWithoutIncludeAsync(Expression<Func<Device, bool>> predicate);
    }
}
