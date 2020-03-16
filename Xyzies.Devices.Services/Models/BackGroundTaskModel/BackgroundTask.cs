using System;
using System.Threading;
using System.Threading.Tasks;

namespace Xyzies.Devices.Services.Models.BackGroundTaskModel
{
    public class BackgroundTask
    {
        public Guid Id { get; set; }

        public Func<CancellationToken, Task> WorkMethod { get; set; }
    }
}
