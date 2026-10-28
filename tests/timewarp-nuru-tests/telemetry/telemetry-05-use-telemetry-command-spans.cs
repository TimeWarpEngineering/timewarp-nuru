#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 552: UseTelemetry() on its own emits one command span per run. The generator registers
// TelemetryBehavior as the outermost behavior; an app that also adds it gets one span, not two;
// the span nests under the caller's Activity.Current. OTLP export of the span is telemetry-04.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Telemetry
{
  using System.Collections.Concurrent;
  using System.Diagnostics;

  [TestTag("Telemetry")]
  public class UseTelemetryCommandSpanTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<UseTelemetryCommandSpanTests>();

    public static async Task Should_emit_one_ok_span_per_command_from_UseTelemetry_alone()
    {
      using Tel05SpanRecorder recorder = new();
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseTelemetry()
        .Map("tel05-ok")
          .WithHandler(Tel05Handlers.Ok)
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["tel05-ok"]);

      exitCode.ShouldBe(0);
      Activity[] spans = recorder.Named("tel05-ok");
      spans.Length.ShouldBe(1);
      Activity span = spans[0];
      span.Source.Name.ShouldBe("TimeWarp.Nuru.Behavior");
      span.Kind.ShouldBe(ActivityKind.Internal);
      span.Status.ShouldBe(ActivityStatusCode.Ok);
      span.GetTagItem("command.name").ShouldBe("tel05-ok");
      span.GetTagItem("command.type").ShouldNotBeNull();
    }

    public static async Task Should_emit_error_span_with_error_type_on_failure()
    {
      using Tel05SpanRecorder recorder = new();
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseTelemetry()
        .Map("tel05-fail")
          .WithHandler(Tel05Handlers.Fail)
          .AsCommand()
          .Done()
        .Build();

      try
      {
        int exitCode = await app.RunAsync(["tel05-fail"]);
        exitCode.ShouldNotBe(0);
      }
      catch (InvalidOperationException)
      {
        // Either outcome is fine here: the span is what this test checks.
      }

      Activity[] spans = recorder.Named("tel05-fail");
      spans.Length.ShouldBe(1);
      Activity span = spans[0];
      span.Status.ShouldBe(ActivityStatusCode.Error);
      span.StatusDescription.ShouldBeNull();
      span.GetTagItem("error.type").ShouldBe(nameof(InvalidOperationException));
      span.GetTagItem("error.message").ShouldBeNull();
    }

    public static async Task Should_emit_one_span_when_app_also_adds_TelemetryBehavior()
    {
      using Tel05SpanRecorder recorder = new();
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseTelemetry()
        .AddBehavior(typeof(TelemetryBehavior))
        .Map("tel05-dup")
          .WithHandler(Tel05Handlers.Ok)
          .AsQuery()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["tel05-dup"]);

      exitCode.ShouldBe(0);
      recorder.Named("tel05-dup").Length.ShouldBe(1);
    }

    public static async Task Should_parent_command_span_under_ambient_activity()
    {
      using Tel05SpanRecorder recorder = new();
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseTelemetry()
        .Map("tel05-child")
          .WithHandler(Tel05Handlers.Ok)
          .AsQuery()
          .Done()
        .Build();

      using (Activity? host = Tel05SpanRecorder.HostSource.StartActivity("tel05-host"))
      {
        host.ShouldNotBeNull();
        int exitCode = await app.RunAsync(["tel05-child"]);
        exitCode.ShouldBe(0);

        Activity[] spans = recorder.Named("tel05-child");
        spans.Length.ShouldBe(1);
        spans[0].TraceId.ShouldBe(host!.TraceId);
        spans[0].ParentSpanId.ShouldBe(host.SpanId);
      }
    }
    public static async Task Should_compile_and_span_short_only_flag_route()
    {
      using Tel05SpanRecorder recorder = new();
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseTelemetry()
        .Map("tel05-build -v")
          .WithHandler((bool v) => v ? "verbose" : "quiet")
          .AsCommand()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["tel05-build", "-v"]);

      exitCode.ShouldBe(0);
      terminal.OutputContains("verbose").ShouldBeTrue();
      recorder.Named("tel05-build -v").Length.ShouldBe(1);
    }

    public static async Task Should_compile_and_span_several_short_only_flags()
    {
      using Tel05SpanRecorder recorder = new();
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseTelemetry()
        .Map("tel05-docker run -i -t")
          .WithHandler((bool i, bool t) => $"i={i} t={t}")
          .AsCommand()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["tel05-docker", "run", "-i", "-t"]);

      exitCode.ShouldBe(0);
      terminal.OutputContains("i=True t=True").ShouldBeTrue();
      recorder.Named("tel05-docker run -i -t").Length.ShouldBe(1);
    }

    public static async Task Should_compile_and_span_short_and_camel_case_value_options()
    {
      using Tel05SpanRecorder recorder = new();
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .UseTelemetry()
        .Map("tel05-commit -m {message} --target-name {targetName?}")
          .WithHandler((string message, string? targetName) => $"{message}/{targetName}")
          .AsCommand()
          .Done()
        .Build();

      int exitCode = await app.RunAsync(["tel05-commit", "-m", "hi", "--target-name", "main"]);

      exitCode.ShouldBe(0);
      terminal.OutputContains("hi/main").ShouldBeTrue();
      recorder.Named("tel05-commit -m {message} --target-name {targetName?}").Length.ShouldBe(1);
    }
  }

  public static class Tel05Handlers
  {
    public static string Ok() => "ok";

    public static void Fail() => throw new InvalidOperationException("tel05 secret argv");
  }

  /// <summary>
  /// Records every stopped span from the command source and a test host source.
  /// </summary>
  public sealed class Tel05SpanRecorder : IDisposable
  {
    public static readonly ActivitySource HostSource = new("TimeWarp.Nuru.Tests.Tel05Host");

    private readonly ConcurrentQueue<Activity> Stopped = new();
    private readonly ActivityListener Listener;

    public Tel05SpanRecorder()
    {
      Listener = new ActivityListener
      {
        ShouldListenTo = source => source.Name is "TimeWarp.Nuru.Behavior" or "TimeWarp.Nuru.Tests.Tel05Host",
        Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
        ActivityStopped = Stopped.Enqueue
      };
      ActivitySource.AddActivityListener(Listener);
    }

    public Activity[] Named(string name) => [.. Stopped.Where(a => a.DisplayName == name)];

    public void Dispose() => Listener.Dispose();
  }
}
