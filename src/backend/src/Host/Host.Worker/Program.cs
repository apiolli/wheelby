using AccessControl.Infrastructure;
using Host.Worker;
using Notifications.Infrastructure;
using Shared.Application;
using Wheelby.Infrastructure;

// RF-NOT-09: "docker compose run --rm worker --once" hace una sola pasada de la cola
// de correos y termina. Sin --once, el Worker corre sin parar con sus dos BackgroundService.
const string OnceFlag = "--once";
var runOnce = args.Contains(OnceFlag, StringComparer.OrdinalIgnoreCase);
var hostArgs = args.Where(a => !string.Equals(a, OnceFlag, StringComparison.OrdinalIgnoreCase)).ToArray();

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(hostArgs);

builder.Services.AddSharedApplication();
builder.Services.AddNotifications(builder.Configuration);

// Expiración de solicitudes vencidas. Necesita Wheelby y, para notificar a los
// participantes, solo el directorio de usuarios de AccessControl.
builder.Services.AddWheelbyWorker(builder.Configuration);
builder.Services.AddAccessControlUserDirectory(builder.Configuration);

if (!runOnce)
{
    builder.Services.AddHostedService<EmailDispatchWorker>();
    builder.Services.AddHostedService<ReservationExpiryWorker>();
}

// Las migraciones las aplica solo la API: el Worker nunca migra el esquema.
using var host = builder.Build();

if (!runOnce)
{
    await host.RunAsync();
    return 0;
}

// Una pasada, sin expiración de reservas y sin quedarse corriendo.
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Host.Worker.Once");
try
{
    var (sent, failed) = await host.Services.SendPendingEmailsAsync();
    logger.LogInformation("Pending emails processed. Sent: {Sent}. Failed: {Failed}.", sent, failed);
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "The single email dispatch pass failed.");
    return 1;
}
