using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Data.Entity;
using Xyzies.Devices.Data.Extensions;
using System.Linq;

namespace Xyzies.Devices.Data.Repository.Behaviour
{
    public class CommentRepository : EfCoreBaseRepository<Guid, Comment>, ICommentRepository
    {
        public CommentRepository(DeviceContext dbContext) : base(dbContext)
        {
        }

        public async Task<LazyLoadedResult<Comment>> GetAllAsync(Expression<Func<Comment, bool>> predicate, LazyLoadParameters filters) =>
            await Task.FromResult(Data.Where(predicate).OrderByDescending(x => x.CreateOn).GetPart(filters));
    }
}