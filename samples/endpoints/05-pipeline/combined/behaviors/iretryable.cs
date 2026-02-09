// ═══════════════════════════════════════════════════════════════════════════════
// RETRY FILTER INTERFACE
// ═══════════════════════════════════════════════════════════════════════════════
// Marker interface for retry filtering.

namespace PipelineCombined.Behaviors;

public interface IRetryable
{
  int MaxRetries { get; }
}
