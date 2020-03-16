using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xyzies.Devices.Services.Service.Interfaces;

namespace Xyzies.Devices.Services.Service.BackGroundWorkerService
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly ILogger _logger;
        public QueuedHostedService(
            IBackgroundTaskQueue taskQueue,
            ILoggerFactory loggerFactory)
        {

            TaskQueue = taskQueue;
            _logger = loggerFactory.CreateLogger<QueuedHostedService>();
        }

        public IBackgroundTaskQueue TaskQueue { get; }

        protected async override Task ExecuteAsync(
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Queued Hosted Service is starting.");

            while (!cancellationToken.IsCancellationRequested)
            {
                var workItem = await TaskQueue.DequeueAsync(cancellationToken);
                try
                {
                    await workItem.WorkMethod.Invoke(cancellationToken);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        $"Error occurred executing {ex.Message}, {ex.StackTrace}", ex.Message, ex.StackTrace);
                }
            }

            _logger.LogInformation("Queued Hosted Service is stopping.");
        }
    }
}
