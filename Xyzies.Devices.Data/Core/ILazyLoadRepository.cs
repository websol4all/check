using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xyzies.Devices.Data.Common;

namespace Xyzies.Devices.Data.Repository.Behaviour
{
    public interface ILazyLoadRepository<TEntity, TFilter>
        where TFilter : class
        where TEntity : class
    {
        Task<LazyLoadedResult<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate, TFilter filters = null);
    }
}
