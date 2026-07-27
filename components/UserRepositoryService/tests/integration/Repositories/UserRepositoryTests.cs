using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using UserRepositoryService.Entities;
using UserRepositoryService.Persistence;
using UserRepositoryService.Repositories;

namespace UserRepositoryService.Tests.Integration.Repositories;

/// <summary>
/// Runs against a real SQLite in-memory database (rather than the EF Core InMemory provider) so that
/// relational behavior we depend on - the unique index on Email - is actually enforced.
/// </summary>
public sealed class UserRepositoryTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private UserRepositoryDbContext _dbContext = null!;
    private UserRepository _sut = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<UserRepositoryDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new UserRepositoryDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();

        _sut = new UserRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_ReturnsSameUser()
    {
        var user = new UserInfo { UserId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };

        await _sut.AddAsync(user, CancellationToken.None);
        var fetched = await _sut.GetByIdAsync(user.UserId, CancellationToken.None);

        fetched.Should().BeEquivalentTo(user);
    }

    [Fact]
    public async Task AddAsync_WithDuplicateEmail_ThrowsDbUpdateException()
    {
        var email = "jane@example.com";
        await _sut.AddAsync(new UserInfo { UserId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", Email = email }, CancellationToken.None);

        var act = () => _sut.AddAsync(new UserInfo { UserId = Guid.NewGuid(), FirstName = "Other", LastName = "Person", Email = email }, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        exception.Which.InnerException!.Message.Should().Contain("UNIQUE constraint failed");
    }

    [Fact]
    public async Task UpdateAsync_WhenUserExists_UpdatesFields()
    {
        var user = new UserInfo { UserId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };
        await _sut.AddAsync(user, CancellationToken.None);

        var updated = await _sut.UpdateAsync(user.UserId, "Janet", "Doe", "janet@example.com", CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.FirstName.Should().Be("Janet");
        updated.Email.Should().Be("janet@example.com");
    }

    [Fact]
    public async Task UpdateAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        var updated = await _sut.UpdateAsync(Guid.NewGuid(), "Janet", "Doe", "janet@example.com", CancellationToken.None);

        updated.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenUserExists_RemovesUserAndReturnsTrue()
    {
        var user = new UserInfo { UserId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };
        await _sut.AddAsync(user, CancellationToken.None);

        var deleted = await _sut.DeleteAsync(user.UserId, CancellationToken.None);
        var fetched = await _sut.GetByIdAsync(user.UserId, CancellationToken.None);

        deleted.Should().BeTrue();
        fetched.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenUserDoesNotExist_ReturnsFalse()
    {
        var deleted = await _sut.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task SearchByNameAsync_ReturnsUsersMatchingFirstOrLastName()
    {
        await _sut.AddAsync(new UserInfo { UserId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" }, CancellationToken.None);
        await _sut.AddAsync(new UserInfo { UserId = Guid.NewGuid(), FirstName = "John", LastName = "Doe", Email = "john@example.com" }, CancellationToken.None);
        await _sut.AddAsync(new UserInfo { UserId = Guid.NewGuid(), FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" }, CancellationToken.None);

        var results = await _sut.SearchByNameAsync("Doe", CancellationToken.None);

        results.Should().HaveCount(2);
        results.Should().OnlyContain(u => u.LastName == "Doe");
    }

    [Fact]
    public async Task SearchByNameAsync_WithNoNameFilter_ReturnsAllUsers()
    {
        await _sut.AddAsync(new UserInfo { UserId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" }, CancellationToken.None);
        await _sut.AddAsync(new UserInfo { UserId = Guid.NewGuid(), FirstName = "Alice", LastName = "Smith", Email = "alice@example.com" }, CancellationToken.None);

        var results = await _sut.SearchByNameAsync(null, CancellationToken.None);

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task EmailExistsAsync_ReturnsTrueForExistingEmail_FalseOtherwise()
    {
        await _sut.AddAsync(new UserInfo { UserId = Guid.NewGuid(), FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" }, CancellationToken.None);

        var exists = await _sut.EmailExistsAsync("jane@example.com", CancellationToken.None);
        var doesNotExist = await _sut.EmailExistsAsync("nobody@example.com", CancellationToken.None);

        exists.Should().BeTrue();
        doesNotExist.Should().BeFalse();
    }
}
