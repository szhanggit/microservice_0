using Grpc.Core;

namespace UserManagementService.Tests.Unit.TestUtilities;

/// <summary>
/// Builds fake AsyncUnaryCall instances for mocking the generated gRPC client's virtual RPC methods.
/// </summary>
internal static class AsyncUnaryCallFactory
{
    public static AsyncUnaryCall<TResponse> Create<TResponse>(TResponse response) =>
        new(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });

    public static AsyncUnaryCall<TResponse> CreateFaulted<TResponse>(RpcException exception) =>
        new(
            Task.FromException<TResponse>(exception),
            Task.FromException<Metadata>(exception),
            () => exception.Status,
            () => new Metadata(),
            () => { });
}
