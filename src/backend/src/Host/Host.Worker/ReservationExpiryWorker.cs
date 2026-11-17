using MediatR;
using Wheelby.Application.Features.ExpirePendingReservations;

namespace Host.Worker;

// Expira las solicitudes Pending cuyo inicio ya llegó (Confirm exige utcNow <= StartDate,
// así que quedarían colgadas). Cada pasada procesa lotes hasta vaciar lo pendiente.
// Una falla se registra y se reintenta en la siguiente pasada; nunca tumba el proceso.
internal sealed class ReservationExpiryWorker(
    IServiceProvider services,
    IConfiguration configuration,
    ILogger<ReservationExpiryWorker> logger) : BackgroundService
{
    public const int DefaultIntervalSeconds = 300;
    public const int BatchSize = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(10,
            configuration.GetValue<int?>("Worker:ReservationExpiryIntervalSeconds") ?? DefaultIntervalSeconds));
        logger.LogInformation("Reservation expiry worker started. Interval: {Seconds} s.", interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                int expired, total = 0;
                do
                {
                    // Un scope por lote: DbContext nuevo y sin entidades acumuladas.
                    using var scope = services.CreateScope();
                    var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                    expired = await sender.Send(new ExpirePendingReservationsCommand(BatchSize), stoppingToken);
                    total += expired;
                } while (expired == BatchSize && !stoppingToken.IsCancellationRequested);

                if (total > 0)
                    logger.LogInformation("Expired {Count} pending reservation(s).", total);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "The reservation expiry pass failed. It will be retried in the next cycle.");
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

        logger.LogInformation("Reservation expiry worker stopped.");
    }
}
