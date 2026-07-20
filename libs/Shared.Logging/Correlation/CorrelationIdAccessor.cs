namespace Shared.Logging.Correlation;

public sealed class CorrelationIdAccessor : ICorrelationIdAccessor
{
    public string CorrelationId { get; set; } = string.Empty;
}
