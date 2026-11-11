using Notifications.Infrastructure;

namespace Host.Worker;

// Procesa la cola de correos sin parar: una pasada, una espera y otra pasada.
// Una falla en una pasada (base caída, tabla aún sin crear) se registra y se reintenta;
// nunca tumba el proceso.
internal sealed class EmailDispatchWorker(
    IServiceProvider services,
    IConfiguration configuration,
    ILogger<EmailDispatchWorker> logger) : BackgroundService
{
    public const int DefaultIntervalSeconds = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(ReadIntervalSeconds());
        logger.LogInformation("Email dispatch worker started. Interval: {Seconds} s.", interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var (sent, failed) = await services.SendPendingEmailsAsync(stoppingToken);

                if (sent > 0 || failed > 0)
                    logger.LogInformation(
                        "Pending emails processed. Sent: {Sent}. Failed: {Failed}.", sent, failed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "The email dispatch pass failed. It will be retried in the next cycle.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Email dispatch worker stopped.");
    }

    // Worker:IntervalSeconds, 10 por defecto y mínimo 1.
    private int ReadIntervalSeconds()
    {
        var configured = configuration.GetValue<int?>("Worker:IntervalSeconds") ?? DefaultIntervalSeconds;
        return Math.Max(1, configured);
    }
}
