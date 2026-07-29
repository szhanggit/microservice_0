using Grpc.Core;
using Shared.Common.Exceptions;
using Shared.Common.Validators;
using Shared.Contracts.Requests;
using Shared.Contracts.Responses;
using ManagementProto = Shared.Protos.UserManagement;

namespace UserManagementGateway.Services;

public sealed class UserService(
    ManagementProto.UserManagementGrpcService.UserManagementGrpcServiceClient managementClient,
    IValidator<CreateUserRequest> createUserValidator,
    IValidator<UpdateUserRequest> updateUserValidator,
    ILogger<UserService> logger)
    : IUserService
{
    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received CreateUser request: FirstName={FirstName}, LastName={LastName}, Email={Email}",
            request.FirstName, request.LastName, request.Email);

        EnsureValid(createUserValidator.Validate(request));

        try
        {
            var reply = await managementClient.CreateUserAsync(
                new ManagementProto.CreateUserRequest { FirstName = request.FirstName, LastName = request.LastName, Email = request.Email },
                cancellationToken: cancellationToken);

            var response = MapToResponse(reply.User);

            logger.LogInformation(
                "Returning CreateUser response: UserId={UserId}, FirstName={FirstName}, LastName={LastName}, Email={Email}",
                response.UserId, response.FirstName, response.LastName, response.Email);

            return response;
        }
        catch (RpcException ex)
        {
            throw Translate(ex, request.Email);
        }
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received GetUserById request: UserId={UserId}", userId);

        try
        {
            var reply = await managementClient.GetUserByIdAsync(
                new ManagementProto.GetUserByIdRequest { UserId = userId.ToString() },
                cancellationToken: cancellationToken);

            var response = MapToResponse(reply.User);

            logger.LogInformation(
                "Returning GetUserById response: UserId={UserId}, FirstName={FirstName}, LastName={LastName}, Email={Email}",
                response.UserId, response.FirstName, response.LastName, response.Email);

            return response;
        }
        catch (RpcException ex)
        {
            throw Translate(ex);
        }
    }

    public async Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received UpdateUser request: UserId={UserId}, FirstName={FirstName}, LastName={LastName}, Email={Email}",
            userId, request.FirstName, request.LastName, request.Email);

        EnsureValid(updateUserValidator.Validate(request));

        try
        {
            var reply = await managementClient.UpdateUserAsync(
                new ManagementProto.UpdateUserRequest
                {
                    UserId = userId.ToString(),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    Email = request.Email,
                },
                cancellationToken: cancellationToken);

            var response = MapToResponse(reply.User);

            logger.LogInformation(
                "Returning UpdateUser response: UserId={UserId}, FirstName={FirstName}, LastName={LastName}, Email={Email}",
                response.UserId, response.FirstName, response.LastName, response.Email);

            return response;
        }
        catch (RpcException ex)
        {
            throw Translate(ex, request.Email);
        }
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received DeleteUser request: UserId={UserId}", userId);

        try
        {
            await managementClient.DeleteUserAsync(
                new ManagementProto.DeleteUserRequest { UserId = userId.ToString() },
                cancellationToken: cancellationToken);

            logger.LogInformation("Returning DeleteUser response: UserId={UserId}, Success=true", userId);
        }
        catch (RpcException ex)
        {
            throw Translate(ex);
        }
    }

    public async Task<UserListResponse> SearchUsersAsync(string? name, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received SearchUsers request: Name={Name}", name);

        try
        {
            var reply = await managementClient.SearchUsersAsync(
                new ManagementProto.SearchUsersRequest { Name = name ?? string.Empty },
                cancellationToken: cancellationToken);

            var response = new UserListResponse(reply.Users.Select(MapToResponse).ToList());

            logger.LogInformation("Returning SearchUsers response: {UserCount} user(s) found", response.Users.Count);

            return response;
        }
        catch (RpcException ex)
        {
            throw Translate(ex);
        }
    }

    private static void EnsureValid(ValidationOutcome outcome)
    {
        if (!outcome.IsValid)
        {
            throw new ValidationException(outcome.Errors);
        }
    }

    private static Exception Translate(RpcException ex, string? email = null) => ex.StatusCode switch
    {
        StatusCode.NotFound => new NotFoundException(ex.Status.Detail),
        StatusCode.AlreadyExists => new DuplicateEmailException(email ?? ex.Status.Detail),
        StatusCode.InvalidArgument => new ValidationException(new[] { ex.Status.Detail }),
        _ => ex,
    };

    private static UserResponse MapToResponse(Shared.Protos.Common.UserProto user) =>
        new(Guid.Parse(user.UserId), user.FirstName, user.LastName, user.Email);
}
