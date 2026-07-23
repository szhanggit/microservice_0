using Microsoft.EntityFrameworkCore;
using UserRepositoryService.Entities;
using UserRepositoryService.Persistence;

namespace UserRepositoryService.Repositories;

public sealed class UserRepository(UserRepositoryDbContext dbContext) : IUserRepository
{
    public Task<UserInfo?> GetByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

    public async Task<IReadOnlyCollection<UserInfo>> SearchByNameAsync(string? name, CancellationToken cancellationToken)
    {
        var query = dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(u =>
                EF.Functions.Like(u.FirstName, $"%{name}%") ||
                EF.Functions.Like(u.LastName, $"%{name}%"));
        }

        return await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        dbContext.Users.AsNoTracking().AnyAsync(u => u.Email == email, cancellationToken);

    public async Task AddAsync(UserInfo user, CancellationToken cancellationToken)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserInfo?> UpdateAsync(Guid userId, string firstName, string lastName, string email, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.Email = email;

        await dbContext.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
