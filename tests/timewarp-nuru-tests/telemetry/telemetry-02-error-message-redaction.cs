#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 470-015: TelemetryBehavior (and emitter twin) must not export Exception.Message.
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

    public static async Task Should_keep_emitter_catch_twin_free_of_error_message()
    {
      // EmitTelemetryCatch is the generated twin of TelemetryBehavior; assert source parity
      // (method may not yet be wired into the interceptor).
      string repoRoot = FindRepoRoot();
      string emitterPath = Path.Combine(
        repoRoot,
        "source",
        "timewarp-nuru-analyzers",
        "generators",
        "emitters",
        "telemetry-emitter.cs");

      File.Exists(emitterPath).ShouldBeTrue();
      string source = await File.ReadAllTextAsync(emitterPath);

      int catchStart = source.IndexOf("EmitTelemetryCatch", StringComparison.Ordinal);
      catchStart.ShouldBeGreaterThan(0);
      string catchRegion = source[catchStart..];
      int nextMethod = catchRegion.IndexOf("EmitTelemetryFlush", StringComparison.Ordinal);
      if (nextMethod > 0)
        catchRegion = catchRegion[..nextMethod];

      catchRegion.ShouldContain("error.type");
      catchRegion.Contains("error.message").ShouldBeFalse();
      catchRegion.Contains("__telemetryEx.Message").ShouldBeFalse();
      catchRegion.ShouldContain("ActivityStatusCode.Error);");
    }

    private static string FindRepoRoot()
    {
      string? dir = AppContext.BaseDirectory;
      while (dir is not null)
      {
        if (File.Exists(Path.Combine(dir, "Directory.Build.props"))
            && Directory.Exists(Path.Combine(dir, "source", "timewarp-nuru")))
        {
          return dir;
        }

        dir = Directory.GetParent(dir)?.FullName;
      }

      dir = Directory.GetCurrentDirectory();
      while (dir is not null)
      {
        if (File.Exists(Path.Combine(dir, "Directory.Build.props"))
            && Directory.Exists(Path.Combine(dir, "source", "timewarp-nuru")))
        {
          return dir;
        }

        dir = Directory.GetParent(dir)?.FullName;
      }

      throw new InvalidOperationException("Could not locate repo root from test process.");
    }
  }
}
