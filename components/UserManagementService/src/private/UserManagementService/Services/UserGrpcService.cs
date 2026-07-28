using Grpc.Core;
using Shared.Common.Validators;
using Shared.Protos.UserManagement;
using RepoProto = Shared.Protos.UserRepository;

namespace UserManagementService.Services;

public sealed class UserGrpcService(
    RepoProto.UserRepositoryGrpcService.UserRepositoryGrpcServiceClient repositoryClient,
    IValidator<CreateUserRequest> createUserValidator,
    IValidator<UpdateUserRequest> updateUserValidator,
    ILogger<UserGrpcService> logger)
    : UserManagementGrpcService.UserManagementGrpcServiceBase
{
    public override async Task<UserReply> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        logger.LogInformation(
            "Received CreateUser request: FirstName={FirstName}, LastName={LastName}, Email={Email}",
            request.FirstName, request.LastName, request.Email);

        EnsureValid(createUserValidator.Validate(request));

        var emailExists = await repositoryClient.EmailExistsAsync(
            new RepoProto.EmailExistsRequest { Email = request.Email },
            cancellationToken: context.CancellationToken);

        if (emailExists.Exists)
        {
            throw new RpcException(new Status(StatusCode.AlreadyExists, $"A user with email '{request.Email}' already exists."));
        }

        var created = await repositoryClient.CreateUserAsync(
            new RepoProto.CreateUserRequest { FirstName = request.FirstName, LastName = request.LastName, Email = request.Email },
            cancellationToken: context.CancellationToken);

        logger.LogInformation(
            "Returning CreateUser response: UserId={UserId}, FirstName={FirstName}, LastName={LastName}, Email={Email}",
            created.User.UserId, created.User.FirstName, created.User.LastName, created.User.Email);

        return new UserReply { User = created.User };
    }

    public override async Task<UserReply> GetUserById(GetUserByIdRequest request, ServerCallContext context)
    {
        logger.LogInformation("Received GetUserById request: UserId={UserId}", request.UserId);

        EnsureValidUserId(request.UserId);

        var reply = await repositoryClient.GetUserByIdAsync(
            new RepoProto.GetUserByIdRequest { UserId = request.UserId },
            cancellationToken: context.CancellationToken);

        logger.LogInformation(
            "Returning GetUserById response: UserId={UserId}, FirstName={FirstName}, LastName={LastName}, Email={Email}",
            reply.User.UserId, reply.User.FirstName, reply.User.LastName, reply.User.Email);

        return new UserReply { User = reply.User };
    }

    public override async Task<UserReply> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        logger.LogInformation(
            "Received UpdateUser request: UserId={UserId}, FirstName={FirstName}, LastName={LastName}, Email={Email}",
            request.UserId, request.FirstName, request.LastName, request.Email);

        EnsureValid(updateUserValidator.Validate(request));

        var current = await repositoryClient.GetUserByIdAsync(
            new RepoProto.GetUserByIdRequest { UserId = request.UserId },
            cancellationToken: context.CancellationToken);

        if (!string.Equals(current.User.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await repositoryClient.EmailExistsAsync(
                new RepoProto.EmailExistsRequest { Email = request.Email },
                cancellationToken: context.CancellationToken);

            if (emailExists.Exists)
            {
                throw new RpcException(new Status(StatusCode.AlreadyExists, $"A user with email '{request.Email}' already exists."));
            }
        }

        var updated = await repositoryClient.UpdateUserAsync(
            new RepoProto.UpdateUserRequest
            {
                UserId = request.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
            },
            cancellationToken: context.CancellationToken);

        logger.LogInformation(
            "Returning UpdateUser response: UserId={UserId}, FirstName={FirstName}, LastName={LastName}, Email={Email}",
            updated.User.UserId, updated.User.FirstName, updated.User.LastName, updated.User.Email);

        return new UserReply { User = updated.User };
    }

    public override async Task<DeleteUserReply> DeleteUser(DeleteUserRequest request, ServerCallContext context)
    {
        logger.LogInformation("Received DeleteUser request: UserId={UserId}", request.UserId);

        EnsureValidUserId(request.UserId);

        var reply = await repositoryClient.DeleteUserAsync(
            new RepoProto.DeleteUserRequest { UserId = request.UserId },
            cancellationToken: context.CancellationToken);

        logger.LogInformation("Returning DeleteUser response: UserId={UserId}, Success={Success}", request.UserId, reply.Success);

        return new DeleteUserReply { Success = reply.Success };
    }

    public override async Task<SearchUsersReply> SearchUsers(SearchUsersRequest request, ServerCallContext context)
    {
        logger.LogInformation("Received SearchUsers request: Name={Name}", request.Name);

        var reply = await repositoryClient.SearchUsersAsync(
            new RepoProto.SearchUsersRequest { Name = request.Name },
            cancellationToken: context.CancellationToken);

        var result = new SearchUsersReply();
        result.Users.AddRange(reply.Users);

        logger.LogInformation("Returning SearchUsers response: {UserCount} user(s) found", result.Users.Count);

        return result;
    }

    private static void EnsureValidUserId(string userId)
    {
        if (!Guid.TryParse(userId, out _))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"'{userId}' is not a valid user id."));
        }
    }

    private static void EnsureValid(ValidationOutcome outcome)
    {
        if (!outcome.IsValid)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, string.Join(" ", outcome.Errors)));
        }
    }
}
