using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xyzies.Devices.Data;

namespace Xyzies.Devices.Tests
{
    public static class DbContextExtensions
    {
        public static void ClearContext(this DeviceContext context)
        {
            var entities = context.ChangeTracker.Entries().Select(x => x.Entity);
            context.RemoveRange(entities);
            context.SaveChanges();
        }
    }
}
