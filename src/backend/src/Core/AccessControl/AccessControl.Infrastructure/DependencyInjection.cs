using AccessControl.Application.Abstractions;
using AccessControl.Application.Features.Register;
using AccessControl.Infrastructure.Persistence;
using AccessControl.Infrastructure.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AccessControl.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace AccessControl.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAccessControl(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión ConnectionStrings__Postgres.");

        if (string.IsNullOrWhiteSpace(configuration[$"{AppOptions.SectionName}:ActivationUrl"]))
            throw new InvalidOperationException("Falta la variable App__ActivationUrl.");

        var applicationAssembly = typeof(RegisterUserCommand).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        services.AddDbContext<AccessControlDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AccessControlDbContext.Schema)));

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<IOneTimeTokenGenerator, OneTimeTokenGenerator>();

        services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
        services.AddSingleton<IActivationLinkBuilder, ActivationLinkBuilder>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IUserReadStore, UserReadStore>();

        // JWT
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => o.Key.Length >= 32, "Jwt:Key debe tener al menos 32 caracteres.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Falta Jwt:Issuer.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Falta Jwt:Audience.")
            .Validate(o => o.ExpirationMinutes > 0, "Jwt:ExpirationMinutes debe ser mayor que cero.")
            .ValidateOnStart();

        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());
        services.AddScoped<ICurrentSession>(sp => sp.GetRequiredService<CurrentUser>());

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.ConfigureOptions<ConfigureJwtBearerOptions>();
        services.AddAuthorization();

        return services;
    }

    // Genera las migraciones al arrancar
    public static async Task ApplyAccessControlMigrationsAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessControlDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}