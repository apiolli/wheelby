using AccessControl.Infrastructure;
using Host.Worker;
using Notifications.Infrastructure;
using Shared.Application;
using Wheelby.Infrastructure;

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

builder.Services.AddSharedApplication();
builder.Services.AddNotifications(builder.Configuration);
builder.Services.AddHostedService<EmailDispatchWorker>();

// Expiración de solicitudes vencidas. Necesita Wheelby y, para avisar por correo
// a los participantes, solo el directorio de usuarios de AccessControl.
builder.Services.AddWheelbyWorker(builder.Configuration);
builder.Services.AddAccessControlUserDirectory(builder.Configuration);
builder.Services.AddHostedService<ReservationExpiryWorker>();

// Las migraciones las aplica solo la API: el Worker nunca migra el esquema.
using var host = builder.Build();
await host.RunAsync();
