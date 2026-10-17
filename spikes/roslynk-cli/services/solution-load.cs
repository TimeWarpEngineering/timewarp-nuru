#region Purpose
// Waits for Roslynk's first solution load and reports a fault before a tool runs.
#endregion

#region Design
// GetOrAddAsync completes on Ready and on Faulted. A faulted model has no snapshot, and the tool
// method then answers Indexing. The CLI already waited, so it reports the load exception instead.
#endregion

namespace RoslynkNuru.Services;

using Morris.Roslynk.Infrastructure.Lifecycle;

internal static class SolutionLoad
{
  public static async Task<string?> FaultAfterReadyAsync(InstanceRegistry registry, string solutionPath)
  {
    RoslynInstance instance = await registry.GetOrAddAsync(solutionPath).ConfigureAwait(false);
    return instance.CurrentModel.FaultMessage;
  }
}
