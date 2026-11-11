using Host.Worker;
using Notifications.Infrastructure;
using Shared.Application;

var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

builder.Services.AddSharedApplication();
builder.Services.AddNotifications(builder.Configuration);
builder.Services.AddHostedService<EmailDispatchWorker>();

// Las migraciones las aplica solo la API: el Worker nunca migra el esquema.
using var host = builder.Build();
await host.RunAsync();
