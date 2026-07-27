using FluentAssertions;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using UserRepositoryService.Interceptors;
using UserRepositoryService.Tests.Unit.TestUtilities;

namespace UserRepositoryService.Tests.Unit.Interceptors;

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
            (_, _) => throw new RpcException(new Status(StatusCode.NotFound, "not found")));

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task UnaryServerHandler_WhenUniqueConstraintViolation_ThrowsAlreadyExists()
    {
        var act = () => _sut.UnaryServerHandler<string, string>(
            "request",
            TestServerCallContext.Create(),
            (_, _) => throw new DbUpdateException("save failed", new Exception("Duplicate entry 'jane@example.com' for key 'UQ_UserInfo_Email'")));

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
