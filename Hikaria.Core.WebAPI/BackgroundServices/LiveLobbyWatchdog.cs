using Hikaria.Core.Contracts;

namespace Hikaria.Core.WebAPI.BackgroundServices
{
    public class LiveLobbyWatchdog : BackgroundService
    {
        private readonly ILogger<LiveLobbyWatchdog> _logger;
        private readonly IServiceProvider _serviceProvider;
        private Timer _timer;

        public LiveLobbyWatchdog(IServiceProvider serviceProvider, ILogger<LiveLobbyWatchdog> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);

            await base.StopAsync(cancellationToken);
        }

        private async void DoWork(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                IRepositoryWrapper repository = scope.ServiceProvider.GetService<IRepositoryWrapper>();
                await repository.LiveLobbies.DeleteExpiredLobbies();
            }
        }

        public override void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
