using Documents.Application.Abstractions;
using Documents.Application.Common;
using Documents.Application.Features.UploadDocument;
using Documents.Infrastructure.Persistence;
using Documents.Infrastructure.Storage;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Documents.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDocuments(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión ConnectionStrings__Postgres.");

        var applicationAssembly = typeof(UploadDocumentCommand).Assembly;
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly, includeInternalTypes: true);

        services.Configure<DocumentsOptions>(configuration.GetSection(DocumentsOptions.SectionName));

        services.AddDbContext<DocumentsDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", DocumentsDbContext.Schema)));

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentReadStore, DocumentReadStore>();
        services.AddScoped<IDocumentsUnitOfWork, DocumentsUnitOfWork>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        return services;
    }

    // Aplica las migraciones de Documents al arrancar.
    public static async Task ApplyDocumentsMigrationsAsync(
        this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}
