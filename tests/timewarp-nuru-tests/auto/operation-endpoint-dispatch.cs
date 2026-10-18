#!/usr/bin/env -S dotnet --
#:package Microsoft.Extensions.Http
#pragma warning disable CA1062 // Handlers trust the mediator request instance
#pragma warning disable CA1805 // `= false` is the flag initializer this task derives

#if !JARIBU_MULTI
[assembly: TimeWarp.Mediator.MediatorBehavior(typeof(TimeWarp.Nuru.Tests.Auto.T508DispatchProbe<,>))]
return await RunAllTests();
#else
[assembly: TimeWarp.Mediator.MediatorBehavior(typeof(TimeWarp.Nuru.Tests.Auto.T508DispatchProbe<,>))]
#endif

namespace TimeWarp.Nuru.Tests.Auto
{
#if !JARIBU_MULTI
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
#endif
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
      .Map<T508TraceQuery>()
      .Build();

    int exitCode = await app.RunAsync(["t508trace"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("traced").ShouldBeTrue();
    T508Trace.Hits.ShouldBe(1);
  }

  public static async Task Should_run_mediator_behaviors_on_the_mediator_path()
  {
    T508DispatchCounts.Direct = 0;
    using TestTerminal mediatorTerminal = new();
    MediatorApp = NuruApp.CreateBuilder()
      .UseTerminal(mediatorTerminal)
      .Map<T508DirectQuery>()
      .Build();

    int mediator = await MediatorApp.RunAsync(["t508direct"]);
    mediator.ShouldBe(0);
    mediatorTerminal.OutputContains("direct:ok").ShouldBeTrue();
    T508DispatchCounts.Direct.ShouldBe(1);
  }

  public static async Task Should_skip_mediator_behaviors_when_direct_invoke_is_set()
  {
    T508DispatchCounts.Direct = 0;
    using TestTerminal directTerminal = new();
    DirectApp = NuruApp.CreateBuilder()
      .UseTerminal(directTerminal)
      .UseDirectInvoke()
      .Map<T508DirectQuery>()
      .Build();

    int direct = await DirectApp.RunAsync(["t508direct"]);
    direct.ShouldBe(0);
    directTerminal.OutputContains("direct:ok").ShouldBeTrue();
    T508DispatchCounts.Direct.ShouldBe(0);
  }

  public static async Task Should_keep_an_external_handler_on_the_mediator_when_direct_invoke_is_set()
  {
    T508DispatchCounts.External = 0;
    using TestTerminal terminal = new();
    ExternalApp = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .UseDirectInvoke()
      .Map<T508DirectExternalCommand>()
      .Build();

    int exitCode = await ExternalApp.RunAsync(["t508directexternal", "--Name", "ada"]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("ext:ada").ShouldBeTrue();
    T508DispatchCounts.External.ShouldBe(1);
  }

  // [NuruRoute] endpoints are collected for every app in the CI multi-mode compilation.
  // A handler dependency that only one builder registers fails NURU050 on the other apps.
#if !JARIBU_MULTI
  public static async Task Should_construct_a_typed_http_client_on_the_mediator_path()
  {
    T508HttpProbe.Constructions = 0;
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .ConfigureServices(static services => services.AddHttpClient<IT508HttpProbe, T508HttpProbe>())
      .Map<T508HttpQuery>()
      .Build();

    int exitCode = await app.RunAsync(["t508http"]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("http:ok").ShouldBeTrue();
    T508HttpProbe.Constructions.ShouldBeGreaterThan(0);
  }
#endif

  // RunAsync must share a field with Build(). The generator records intercept sites from
  // the method that contains Build(), or from later calls on the field that Build() assigns.
  private static NuruApp MediatorApp = null!;
  private static NuruApp DirectApp = null!;
  private static NuruApp ExternalApp = null!;

  public static async Task Should_render_with_a_response_renderer_before_json()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<T508RenderQuery>()
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
      .Map<T508ProblemQuery>()
      .Build();

    int problem = await app.RunAsync(["t508problem", "--Fail"]);
    problem.ShouldBe(17);
    string firstErrorLine = terminal.ErrorOutput
      .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)[0];
    firstErrorLine.ShouldBe("https://example.test/t508 Bad input");
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

/// <summary>
/// Counts mediator pipeline entries for the two dispatch probes in this file.
/// </summary>
public static class T508DispatchCounts
{
  public static int Direct { get; set; }

  public static int External { get; set; }
}

/// <summary>
/// Assembly pipeline behavior. It counts the two probe requests and forwards every other request,
/// because this attribute applies to the whole multi-mode test compilation.
/// </summary>
public sealed class T508DispatchProbe<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
  where TRequest : notnull
{
  public Task<TResponse> Handle
  (
    TRequest request,
    RequestHandlerDelegate<TResponse> next,
    CancellationToken cancellationToken
  )
  {
    if (request is T508DirectQuery)
    {
      T508DispatchCounts.Direct++;
    }
    else if (request is T508DirectExternalCommand)
    {
      T508DispatchCounts.External++;
    }

    return next(cancellationToken);
  }
}

[Operation("t508directexternal")]
public sealed class T508DirectExternalCommand : ICommand<string>
{
  public string Name { get; set; } = "world";
}

public sealed class T508DirectExternalHandler : ICommandHandler<T508DirectExternalCommand, string>
{
  public Task<string> Handle(T508DirectExternalCommand command, CancellationToken cancellationToken)
  {
    _ = cancellationToken;
    return Task.FromResult("ext:" + command.Name);
  }
}

#if !JARIBU_MULTI
public interface IT508HttpProbe
{
  string Value { get; }
}

public sealed class T508HttpProbe : IT508HttpProbe
{
  public static int Constructions { get; set; }

  public T508HttpProbe(HttpClient httpClient)
  {
    _ = httpClient;
    Constructions++;
  }

  public string Value => "ok";
}

[NuruRoute("t508http")]
public sealed class T508HttpQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<T508HttpQuery, string>
  {
    private readonly IT508HttpProbe Probe;

    public Handler(IT508HttpProbe probe)
    {
      Probe = probe;
    }

    public Task<string> Handle(T508HttpQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult("http:" + Probe.Value);
    }
  }
}
#endif

} // namespace TimeWarp.Nuru.Tests.Auto
