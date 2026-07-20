using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using UserManagementService.Interceptors;
using UserManagementService.Tests.Unit.TestUtilities;

namespace UserManagementService.Tests.Unit.Interceptors;

public sealed class ExceptionHandlingInterceptorTests
{
    private readonly ExceptionHandlingInterceptor _sut = new(Mock.Of<ILogger<ExceptionHandlingInterceptor>>());

    [Fact]
    public async Task UnaryServerHandler_WhenNoExceptionThrown_ReturnsResponse()
    {
        var response = await _sut.UnaryServerHandler(
            "request",
            TestServerCallContext.Create(),
            (_, _) => Task.FromResult("response"));

        response.Should().Be("response");
    }

    [Fact]
    public async Task UnaryServerHandler_WhenRpcExceptionThrown_PropagatesUnchanged()
    {
        var act = () => _sut.UnaryServerHandler<string, string>(
            "request",
            TestServerCallContext.Create(),
            (_, _) => throw new RpcException(new Status(StatusCode.AlreadyExists, "duplicate")));

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.AlreadyExists);
    }

    [Fact]
    public async Task UnaryServerHandler_WhenUnexpectedExceptionThrown_ThrowsInternal()
    {
        var act = () => _sut.UnaryServerHandler<string, string>(
            "request",
            TestServerCallContext.Create(),
            (_, _) => throw new InvalidOperationException("boom"));

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.Internal);
    }
}
