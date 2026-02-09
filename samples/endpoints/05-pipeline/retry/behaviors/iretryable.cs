// ═══════════════════════════════════════════════════════════════════════════════
// RETRY FILTER INTERFACE
// ═══════════════════════════════════════════════════════════════════════════════
// Marker interface for commands that support retry.

namespace PipelineRetry.Behaviors;

public interface IRetryable
{
  int MaxRetries { get; }
}
