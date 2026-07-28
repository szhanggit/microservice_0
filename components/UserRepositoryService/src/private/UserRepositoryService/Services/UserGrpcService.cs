using Grpc.Core;
using Shared.Protos.UserRepository;
using UserRepositoryService.Entities;
using UserRepositoryService.Repositories;

namespace UserRepositoryService.Services;

public sealed class UserGrpcService(IUserRepository userRepository, ILogger<UserGrpcService> logger)
    : UserRepositoryGrpcService.UserRepositoryGrpcServiceBase
{
    public override async Task<UserReply> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        RequireNotBlank(request.FirstName, nameof(request.FirstName));
        RequireNotBlank(request.LastName, nameof(request.LastName));
        RequireNotBlank(request.Email, nameof(request.Email));

        if (await userRepository.EmailExistsAsync(request.Email, context.CancellationToken))
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, $"A user with email '{request.Email}' already exists."));
        }

        var user = new UserInfo
        {
            UserId = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
        };

        await userRepository.AddAsync(user, context.CancellationToken);

        logger.LogInformation(
            "Saved UserInfo record {UserId}: FirstName={FirstName}, LastName={LastName}, Email={Email}",
            user.UserId, user.FirstName, user.LastName, user.Email);

        return MapToReply(user);
    }

    public override async Task<UserReply> GetUserById(GetUserByIdRequest request, ServerCallContext context)
    {
        var userId = ParseUserId(request.UserId);

        var user = await userRepository.GetByIdAsync(userId, context.CancellationToken);
        if (user is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"User with id '{userId}' was not found."));
        }

        return MapToReply(user);
    }

    public override async Task<UserReply> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        var userId = ParseUserId(request.UserId);
        RequireNotBlank(request.FirstName, nameof(request.FirstName));
        RequireNotBlank(request.LastName, nameof(request.LastName));
        RequireNotBlank(request.Email, nameof(request.Email));

        var updated = await userRepository.UpdateAsync(userId, request.FirstName, request.LastName, request.Email, context.CancellationToken);
        if (updated is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"User with id '{userId}' was not found."));
        }

        logger.LogInformation(
            "Saved UserInfo record {UserId}: FirstName={FirstName}, LastName={LastName}, Email={Email}",
            updated.UserId, updated.FirstName, updated.LastName, updated.Email);

        return MapToReply(updated);
    }

    public override async Task<DeleteUserReply> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        var userId = ParseUserId(request.UserId);

        var deleted = await userRepository.DeleteAsync(userId, context.CancellationToken);
        if (!deleted)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"User with id '{userId}' was not found."));
        }

        logger.LogInformation("Deleted user {UserId}", userId);

        return new DeleteUserReply { Success = true };
    }

    public override async Task<SearchUsersReply> SearchUsers(SearchUsersRequest request, ServerCallContext context)
    {
        var users = await userRepository.SearchByNameAsync(request.Name, context.CancellationToken);

        var reply = new SearchUsersReply();
        reply.Users.AddRange(users.Select(MapToProto));

        return reply;
    }

    public override async Task<EmailExistsReply> EmailExists(EmailExistsRequest request, ServerCallContext context)
    {
        var exists = await userRepository.EmailExistsAsync(request.Email, context.CancellationToken);
        return new EmailExistsReply { Exists = exists };
    }

    private static void RequireNotBlank(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{fieldName}' is required."));
        }
    }

    private static Guid ParseUserId(string userId)
    {
        if (!Guid.TryParse(userId, out var parsed))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{userId}' is not a valid user id."));
        }

        return parsed;
    }

    private static UserReply MapToReply(UserInfo user) => new() { User = MapToProto(user) };

    private static Shared.Protos.Common.UserProto MapToProto(UserInfo user) => new()
    {
        UserId = user.UserId.ToString(),
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
    };
}
