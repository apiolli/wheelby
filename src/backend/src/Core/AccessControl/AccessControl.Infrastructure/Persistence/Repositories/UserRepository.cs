using AccessControl.Application.Abstractions;
using AccessControl.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Exceptions;

namespace AccessControl.Infrastructure.Persistence;

internal sealed class UserRepository(AccessControlDbContext db) : IUserRepository
{
    public Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken)
        => db.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        => db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken)
        => db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
        => await db.Users.AddAsync(user, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("El correo ya está registrado.");
        }
    }
}