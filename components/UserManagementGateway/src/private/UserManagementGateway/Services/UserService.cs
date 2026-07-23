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
    IValidator<UpdateUserRequest> updateUserValidator)
    : IUserService
{
    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        EnsureValid(createUserValidator.Validate(request));

        try
        {
            var reply = await managementClient.CreateUserAsync(
                new ManagementProto.CreateUserRequest { FirstName = request.FirstName, LastName = request.LastName, Email = request.Email },
                cancellationToken: cancellationToken);

            return MapToResponse(reply.User);
        }
        catch (RpcException ex)
        {
            throw Translate(ex, request.Email);
        }
    }

    public async Task<UserResponse> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            var reply = await managementClient.GetUserByIdAsync(
                new ManagementProto.GetUserByIdRequest { UserId = userId.ToString() },
                cancellationToken: cancellationToken);

            return MapToResponse(reply.User);
        }
        catch (RpcException ex)
        {
            throw Translate(ex);
        }
    }

    public async Task<UserResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken)
    {
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

            return MapToResponse(reply.User);
        }
        catch (RpcException ex)
        {
            throw Translate(ex, request.Email);
        }
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await managementClient.DeleteUserAsync(
                new ManagementProto.DeleteUserRequest { UserId = userId.ToString() },
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex)
        {
            throw Translate(ex);
        }
    }

    public async Task<UserListResponse> SearchUsersAsync(string? name, CancellationToken cancellationToken)
    {
        try
        {
            var reply = await managementClient.SearchUsersAsync(
                new ManagementProto.SearchUsersRequest { Name = name ?? string.Empty },
                cancellationToken: cancellationToken);

            return new UserListResponse(reply.Users.Select(MapToResponse).ToList());
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
