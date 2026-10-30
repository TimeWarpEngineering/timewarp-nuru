#region Purpose
// Child spans for each dev workflow stage, on a source UseTelemetry already exports.
#endregion

#region Design
// dev workflow calls clean, build, and the other handlers in-process, so they
// never pass through the route dispatcher and would not get command spans.
// These activities use ActivitySource "TimeWarp.Nuru" because the generated
// provider subscribes only to that name and "TimeWarp.Nuru.Behavior".
// The span name is the step. ci.mode and ci.stage are the only stage tags.
// A pipeline abort records error.type and status Error, never a message,
// an API key, a token, or argv. ProcessorCount is set on the workflow span
// (Activity.Current when the handler starts), which is the pipeline span.
#endregion

namespace DevCli;

internal static class WorkflowStageSpan
{
  internal const string SourceName = "TimeWarp.Nuru";
  internal const string PipelineAbort = "PipelineAbort";
  internal const string PackageLayoutGate = "PackageLayoutGate";

  private static readonly ActivitySource Source = new(SourceName, "1.0.0");

  internal static void TagPipeline(Activity? pipeline)
  {
    pipeline?.SetTag("ProcessorCount", Environment.ProcessorCount);
  }

  internal static Scope Begin(string mode, string stage, string stepName)
  {
    Activity? activity = Source.StartActivity(stepName, ActivityKind.Internal);
    activity?.SetTag("ci.mode", mode);
    activity?.SetTag("ci.stage", stage);
    return new Scope(activity);
  }

  internal readonly record struct Result(bool Continue, string ErrorType)
  {
    internal static Result Done { get; } = new(true, "");

    internal static Result Stop(string errorType)
    {
      ArgumentException.ThrowIfNullOrEmpty(errorType);
      return new Result(false, errorType);
    }
  }

  internal sealed class Scope : IDisposable
  {
    private readonly Activity? Activity;
    private bool Settled;

    internal Scope(Activity? activity)
    {
      Activity = activity;
    }

    internal void Succeed()
    {
      Activity?.SetStatus(ActivityStatusCode.Ok);
      Settled = true;
    }

    internal void Fail(string errorType)
    {
      ArgumentException.ThrowIfNullOrEmpty(errorType);
      Activity?.SetStatus(ActivityStatusCode.Error);
      Activity?.SetTag("error.type", errorType);
      Settled = true;
    }

    public void Dispose()
    {
      if (!Settled && Activity is not null)
      {
        Activity.SetStatus(ActivityStatusCode.Error);
        Activity.SetTag("error.type", "Exception");
      }

      Activity?.Dispose();
    }
  }
}
