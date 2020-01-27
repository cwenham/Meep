using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Meep
{
    public class PipelineService : BackgroundService
    {
        private Bootstrapper Bootstrapper { get; set; }

        private LaunchOptions Options { get; set; }

        private CancellationTokenSource tokenSource = new CancellationTokenSource();

        public PipelineService(Bootstrapper bootstrapper, LaunchOptions options)
        {
            this.Bootstrapper = bootstrapper;
            this.Options = options;
            this.Bootstrapper.PipelineRefreshed += Bootstrapper_PipelineRefreshed;
        }

        private void Bootstrapper_PipelineRefreshed(object sender, PipelineRefreshEventArgs e)
        {
            // Stop the current pipeline, which will make ExecuteAsync() loop and restart with the new pipeline.
            tokenSource.Cancel();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Bootstrapper.Start(tokenSource.Token);

                await Task.Run(() =>
                {
                    tokenSource.Token.WaitHandle.WaitOne();
                });                
            }
            
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            tokenSource.Cancel();

            await base.StopAsync(stoppingToken);
        }
    }
}
