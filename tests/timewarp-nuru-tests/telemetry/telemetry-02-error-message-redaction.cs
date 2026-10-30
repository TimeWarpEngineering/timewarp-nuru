#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 470-015: TelemetryBehavior must not export Exception.Message. Task 552 removed the
// emitter twin: UseTelemetry() now gets its spans from TelemetryBehavior (see telemetry-05).
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Telemetry
{
  using System.Diagnostics;

  [TestTag("Telemetry")]
  public class ErrorMessageRedactionTests
  {
    internal const string SecretInMessage = "secret-token-xyz argv=/tmp/passwd";

    [ModuleInitializer]
    internal static void Register() => RegisterTests<ErrorMessageRedactionTests>();

    public static async Task Should_record_error_type_without_error_message_or_status_detail()
    {
      Activity? captured = null;

      using ActivityListener listener = new()
      {
        ShouldListenTo = source => source.Name == "TimeWarp.Nuru.Behavior",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        ActivityStopped = activity => captured = activity
      };
      ActivitySource.AddActivityListener(listener);

      TelemetryBehavior behavior = new();
      BehaviorContext context = new()
      {
        CommandName = "tel02-fail",
        CommandTypeName = nameof(ErrorMessageRedactionTests),
        CancellationToken = CancellationToken.None
      };

      InvalidOperationException? thrown = null;
      try
      {
        await behavior.HandleAsync(context, static () =>
          throw new InvalidOperationException(SecretInMessage));
      }
      catch (InvalidOperationException ex)
      {
        thrown = ex;
      }

      thrown.ShouldNotBeNull();
      thrown!.Message.ShouldBe(SecretInMessage);
      captured.ShouldNotBeNull();
      captured!.Status.ShouldBe(ActivityStatusCode.Error);
      captured.StatusDescription.ShouldBeNull();
      captured.GetTagItem("error.type").ShouldBe(nameof(InvalidOperationException));
      captured.GetTagItem("error.message").ShouldBeNull();
    }
  }
}
