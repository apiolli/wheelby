using AccessControl.Domain.Sessions;
using AccessControl.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Persistence;

internal sealed class AccessControlDbContext(DbContextOptions<AccessControlDbContext> options)
    : DbContext(options)
{
    public const string Schema = "access_control";

    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccessControlDbContext).Assembly);
    }
}