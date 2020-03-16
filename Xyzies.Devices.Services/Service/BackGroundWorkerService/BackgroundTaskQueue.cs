using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xyzies.Devices.Services.Models.BackGroundTaskModel;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Services.Service.BackGroundWorkerService
{
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private ConcurrentQueue<BackgroundTask> _workItems =
            new ConcurrentQueue<BackgroundTask>();

        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public void QueueBackgroundWorkItem(BackgroundTask workItem)
        {
            if (workItem == null)
            {
                throw new ArgumentNullException(nameof(workItem));
            }

            _workItems.Enqueue(workItem);
            _signal.Release();
        }

        public async Task<BackgroundTask> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            _workItems.TryDequeue(out var workItem);

            return workItem;
        }
    }
}
