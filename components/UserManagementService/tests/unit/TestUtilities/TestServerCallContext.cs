using Grpc.Core;

namespace UserManagementService.Tests.Unit.TestUtilities;

/// <summary>
/// Minimal hand-rolled ServerCallContext for unit tests, avoiding a dependency on the
/// legacy native-gRPC "Grpc.Core.Testing" package.
/// </summary>
internal sealed class TestServerCallContext : ServerCallContext
{
    private readonly CancellationToken _cancellationToken;

    private TestServerCallContext(CancellationToken cancellationToken)
    {
        _cancellationToken = cancellationToken;
    }

    protected override string MethodCore => "TestMethod";

    protected override string HostCore => "localhost";

    protected override string PeerCore => "test-peer";

    protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);

    protected override Metadata RequestHeadersCore { get; } = new();

    protected override CancellationToken CancellationTokenCore => _cancellationToken;

    protected override Metadata ResponseTrailersCore { get; } = new();

    protected override Status StatusCore { get; set; }

    protected override WriteOptions? WriteOptionsCore { get; set; }

    protected override AuthContext AuthContextCore { get; } = new(string.Empty, new Dictionary<string, List<AuthProperty>>());

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotSupportedException();

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;

    public static ServerCallContext Create(CancellationToken cancellationToken = default) =>
        new TestServerCallContext(cancellationToken);
}
