using Microsoft.EntityFrameworkCore;
using Wheelby.Domain.Reservations;
using Wheelby.Domain.Vehicles;

namespace Wheelby.Infrastructure.Persistence;

internal sealed class WheelbyDbContext(DbContextOptions<WheelbyDbContext> options)
    : DbContext(options)
{
    public const string Schema = "wheelby";

    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WheelbyDbContext).Assembly);
    }
}
