using System.Threading;
using System.Threading.Tasks;
using Xyzies.Devices.Services.Models.BackGroundTaskModel;

namespace Xyzies.Devices.Services.Service.Interfaces
{
    public interface IBackgroundTaskQueue
    {
        void QueueBackgroundWorkItem(BackgroundTask workItem);

        Task<BackgroundTask> DequeueAsync(CancellationToken cancellationToken);

    }
}
