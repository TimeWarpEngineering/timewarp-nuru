#!/usr/bin/env -S dotnet --
#pragma warning disable CA1062 // Handlers trust the mediator request instance
#pragma warning disable CA1805 // `= false` is the flag initializer this task derives

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Auto
{
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;

/// <summary>
/// Mediator dispatch, direct-invoke opt-out, renderers, and problem details.
/// </summary>
[TestTag("Auto")]
public class OperationEndpointDispatchTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OperationEndpointDispatchTests>();

  public static async Task Should_run_a_nuru_behavior_around_mediator_dispatch()
  {
    T508Trace.Hits = 0;
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddBehavior(typeof(T508TraceBehavior))
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["t508trace"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("traced").ShouldBeTrue();
    T508Trace.Hits.ShouldBe(1);
  }

  public static async Task Should_match_direct_invoke_and_mediator_results()
  {
    const int Calls = 30;
    (long First, long Loop) mediator = await MeasureMediator(Calls);
    (long First, long Loop) direct = await MeasureDirect(Calls);
    string report =
      $"mediator-first-ms={mediator.First} mediator-{Calls}-ms={mediator.Loop} direct-first-ms={direct.First} direct-{Calls}-ms={direct.Loop}";
    await File.WriteAllTextAsync("/tmp/t508-overhead.txt", report);
  }

  // RunAsync inside a loop is invisible to the block interpreter. Assigning Build() to a
  // field makes the generator scan every call on that field, including the loop.
  private static NuruApp MediatorApp = null!;
  private static NuruApp DirectApp = null!;

  private static async Task<(long First, long Loop)> MeasureMediator(int calls)
  {
    using TestTerminal terminal = new();
    MediatorApp = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    Stopwatch first = Stopwatch.StartNew();
    int code = await MediatorApp.RunAsync(["t508direct"]);
    first.Stop();
    code.ShouldBe(0);
    terminal.OutputContains("direct:ok").ShouldBeTrue();

    Stopwatch loop = Stopwatch.StartNew();
    for (int i = 0; i < calls; i++)
    {
      int loopCode = await MediatorApp.RunAsync(["t508direct"]);
      loopCode.ShouldBe(0);
    }

    loop.Stop();
    return (first.ElapsedMilliseconds, loop.ElapsedMilliseconds);
  }

  private static async Task<(long First, long Loop)> MeasureDirect(int calls)
  {
    using TestTerminal terminal = new();
    DirectApp = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .UseDirectInvoke()
      .DiscoverEndpoints()
      .Build();

    Stopwatch first = Stopwatch.StartNew();
    int code = await DirectApp.RunAsync(["t508direct"]);
    first.Stop();
    code.ShouldBe(0);
    terminal.OutputContains("direct:ok").ShouldBeTrue();

    Stopwatch loop = Stopwatch.StartNew();
    for (int i = 0; i < calls; i++)
    {
      int loopCode = await DirectApp.RunAsync(["t508direct"]);
      loopCode.ShouldBe(0);
    }

    loop.Stop();
    return (first.ElapsedMilliseconds, loop.ElapsedMilliseconds);
  }

  public static async Task Should_render_with_a_response_renderer_before_json()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["t508render", "--Text", "ada"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("rendered:ada").ShouldBeTrue();
    terminal.OutputContains("{").ShouldBeFalse();
  }

  public static async Task Should_write_problem_details_to_stderr_and_use_the_status()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int problem = await app.RunAsync(["t508problem", "--Fail"]);
    problem.ShouldBe(17);
    terminal.ErrorContains("https://example.test/t508 Bad input").ShouldBeTrue();
    terminal.ErrorContains("too small").ShouldBeTrue();

    int ok = await app.RunAsync(["t508problem"]);
    ok.ShouldBe(0);
    terminal.OutputContains("problem:ok").ShouldBeTrue();
  }
}

public static class T508Trace
{
  public static int Hits { get; set; }
}

public sealed class T508TraceBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    if (context.CommandName == "t508trace")
      T508Trace.Hits++;

    await proceed();
  }
}

[Operation("t508trace")]
public sealed class T508TraceQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<T508TraceQuery, string>
  {
    public Task<string> Handle(T508TraceQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult("traced");
    }
  }
}

[NuruRoute("t508direct")]
public sealed class T508DirectQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<T508DirectQuery, string>
  {
    public Task<string> Handle(T508DirectQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult("direct:ok");
    }
  }
}

public sealed class T508RenderValue
{
  public string Text { get; set; } = "";
}

public sealed class T508TextRenderer : IResponseRenderer<T508RenderValue>
{
  public string Render(T508RenderValue response) => "rendered:" + response.Text;
}

[Operation("t508render")]
public sealed class T508RenderQuery : IQuery<T508RenderValue>
{
  public string Text { get; set; } = "";

  public sealed class Handler : IQueryHandler<T508RenderQuery, T508RenderValue>
  {
    public Task<T508RenderValue> Handle(T508RenderQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult(new T508RenderValue { Text = query.Text });
    }
  }
}

[Operation("t508problem")]
public sealed class T508ProblemQuery : IQuery<OneOf<string, SharedProblemDetails>>
{
  public bool Fail { get; set; } = false;

  public sealed class Handler : IQueryHandler<T508ProblemQuery, OneOf<string, SharedProblemDetails>>
  {
    public Task<OneOf<string, SharedProblemDetails>> Handle(T508ProblemQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      if (query.Fail)
      {
        OneOf<string, SharedProblemDetails> problem = new SharedProblemDetails
        {
          Type = "https://example.test/t508",
          Title = "Bad input",
          Status = 17,
          Detail = "too small"
        };
        return Task.FromResult(problem);
      }

      OneOf<string, SharedProblemDetails> ok = "problem:ok";
      return Task.FromResult(ok);
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Auto
