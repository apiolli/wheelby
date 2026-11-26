using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Abstractions;
using Notifications.Application.Features.EnqueueEmail;
using Notifications.Application.Features.SendPendingEmails;
using Notifications.Application.Options;
using Notifications.Contracts;
using INotificationPublisher = Notifications.Contracts.INotificationPublisher;
using Notifications.Infrastructure.Email;
using Notifications.Infrastructure.Inbox;
using Notifications.Infrastructure.Persistence;

namespace Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotifications(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión ConnectionStrings__Postgres.");

        var applicationAssembly = typeof(EnqueueEmailCommand).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", NotificationsDbContext.Schema)));

        services.AddScoped<IQueuedEmailRepository, QueuedEmailRepository>();
        services.AddScoped<IQueuedEmailReadStore, QueuedEmailReadStore>();
        services.AddScoped<INotificationReadStore, NotificationReadStore>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationsUnitOfWork, NotificationsUnitOfWork>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();

        // RF-NOT-10: Notifications__MaxEmailAttempts, 5 por defecto y mínimo 1.
        services.AddOptions<NotificationOptions>()
            .Bind(configuration.GetSection(NotificationOptions.SectionName))
            .Validate(o => o.MaxEmailAttempts >= 1, "Notifications__MaxEmailAttempts debe ser 1 o mayor.")
            .ValidateOnStart();

        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IEmailSender, MailKitEmailSender>();

        // Correos de cuenta (activación, recuperación): directo a la cola, sin bandeja ni preferencia.
        services.AddScoped<IEmailQueue, EmailQueue>();

        // Motor de notificaciones. Necesita IUserDirectory e IAdministratorDirectory de AccessControl.
        services.AddScoped<INotificationPublisher, NotificationPublisher>();

        return services;
    }

    public static async Task ApplyNotificationsMigrationsAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }

    public static async Task<(int Sent, int Failed)> SendPendingEmailsAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var result = await sender.Send(new SendPendingEmailsCommand(), cancellationToken);
        return (result.Sent, result.Failed);
    }
}