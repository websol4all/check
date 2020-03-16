using System;
using Xyzies.Devices.Data.Common;
using Xyzies.Devices.Data.Core;
using Xyzies.Devices.Data.Entity;

namespace Xyzies.Devices.Data.Repository.Behaviour
{
    public interface ICommentRepository : IRepository<Guid, Comment>, ILazyLoadRepository<Comment, LazyLoadParameters>
    {

    }

    
}
