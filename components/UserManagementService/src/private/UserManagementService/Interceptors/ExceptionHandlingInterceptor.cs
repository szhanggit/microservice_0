using Grpc.Core;
using Grpc.Core.Interceptors;

namespace UserManagementService.Interceptors;

public sealed class ExceptionHandlingInterceptor(ILogger<ExceptionHandlingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            // Already carries a meaningful status (including ones propagated from UserRepositoryService).
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception while processing {Method}", context.Method);
            throw new RpcException(new Status(StatusCode.Internal, "An unexpected error occurred."));
        }
    }
}
