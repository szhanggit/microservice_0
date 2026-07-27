namespace Shared.Logging.Correlation;

public interface ICorrelationIdAccessor
{
    string CorrelationId { get; set; }
}
