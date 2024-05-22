using Hikaria.Core.WebAPI.Managers;

namespace Hikaria.Core.WebAPI.BackgroundServices
{
    public class LiveLobbyWatchdog : BackgroundService
    {
        private readonly ILogger<LiveLobbyWatchdog> _logger;
        private Timer _timer;

        public LiveLobbyWatchdog(ILogger<LiveLobbyWatchdog> logger)
        {
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            await base.StopAsync(cancellationToken);
        }

        private void DoWork(object state)
        {
            LiveLobbyManager.CheckLobbiesAlive();
        }

        public override void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
