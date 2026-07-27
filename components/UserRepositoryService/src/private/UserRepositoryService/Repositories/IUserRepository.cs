using UserRepositoryService.Entities;

namespace UserRepositoryService.Repositories;

public interface IUserRepository
{
    Task<UserInfo?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserInfo>> SearchByNameAsync(string? name, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);

    Task AddAsync(UserInfo user, CancellationToken cancellationToken);

    Task<UserInfo?> UpdateAsync(Guid userId, string firstName, string lastName, string email, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken);
}
