using Microsoft.EntityFrameworkCore;
using Permissions.Domain.PermissionGrants;
using Permissions.Domain.PermissionRequests;

namespace Permissions.Infrastructure.Persistence;

internal sealed class PermissionsDbContext(DbContextOptions<PermissionsDbContext> options)
    : DbContext(options)
{
    public const string Schema = "permissions";

    public DbSet<PermissionRequest> PermissionRequests => Set<PermissionRequest>();
    public DbSet<PermissionGrant> PermissionGrants => Set<PermissionGrant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PermissionsDbContext).Assembly);
    }
}
