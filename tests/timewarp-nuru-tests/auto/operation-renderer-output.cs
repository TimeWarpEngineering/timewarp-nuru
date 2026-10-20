#!/usr/bin/env -S dotnet --
#pragma warning disable CA1062 // Handlers trust the mediator request instance

// Verbatim renderer output (UseVerbatimRendererOutput) and IResponseRenderer<SharedProblemDetails> (task 508-011).
// The problem renderers here take a constructor argument, so the generator never picks them as the one visible
// renderer. They reach an app only through its own ConfigureServices, so other apps in the CI multi-mode
// compilation keep the default problem lines.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Auto.RendererOutput
{
#if !JARIBU_MULTI
using Microsoft.Extensions.DependencyInjection;
#endif
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;

[TestTag("Auto")]
public class OperationRendererOutputTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OperationRendererOutputTests>();

  public static async Task Should_add_a_newline_after_renderer_text_by_default()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<T511VerbatimQuery>()
      .Build();

    int exitCode = await app.RunAsync(["t511verbatim"]);

    exitCode.ShouldBe(0);
    terminal.Output.ShouldStartWith(T511TextRenderer.Text);
    terminal.Output.ShouldEndWith("\n");
  }

  public static async Task Should_write_renderer_text_verbatim_when_opted_in()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .UseVerbatimRendererOutput()
      .Map<T511VerbatimQuery>()
      .Build();

    int exitCode = await app.RunAsync(["t511verbatim"]);

    exitCode.ShouldBe(0);
    terminal.Output.ShouldBe(T511TextRenderer.Text);
  }

  public static async Task Should_write_renderer_text_verbatim_on_the_json_args_path()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .UseVerbatimRendererOutput()
      .Map<T511VerbatimQuery>()
      .Build();

    int exitCode = await app.RunAsync(["t511verbatim", "--json-args", "{}"]);

    exitCode.ShouldBe(0);
    terminal.Output.ShouldBe(T511TextRenderer.Text);
  }

  public static async Task Should_render_verbatim_through_render_async()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .UseVerbatimRendererOutput()
      .Map<T511VerbatimQuery>()
      .Build();

    app.TryBind("t511verbatim", default, out NuruBoundRequest? request, out NuruBindError? error).ShouldBeTrue(error?.Message);
    object? result = await app.InvokeAsync(request);
    using TestTerminal render = new();
    int exitCode = await app.RenderAsync(request, result, render);

    exitCode.ShouldBe(0);
    render.Output.ShouldBe(T511TextRenderer.Text);
  }

  public static async Task Should_keep_the_output_strategy_newline_in_verbatim_mode()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .UseVerbatimRendererOutput()
      .Map<T511PlainQuery>()
      .Build();

    int exitCode = await app.RunAsync(["t511plain"]);

    exitCode.ShouldBe(0);
    terminal.Output.ShouldStartWith("plain");
    terminal.Output.ShouldEndWith("\n");
  }

  public static async Task Should_write_a_registered_problem_renderer_to_stderr_and_keep_the_status()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .ConfigureServices(static services =>
      {
        services.AddSingleton<T511ProblemPrefix>();
        services.AddSingleton<IResponseRenderer<SharedProblemDetails>, T511ProblemRenderer>();
      })
      .Map<T511ProblemQuery>()
      .Build();

    int exitCode = await app.RunAsync(["t511problem"]);

    exitCode.ShouldBe(17);
    terminal.ErrorOutput.ShouldStartWith("error=Bad input status=17");
    terminal.ErrorOutput.ShouldEndWith("\n");
    terminal.ErrorContains("https://example.test/t511").ShouldBeFalse();
    terminal.Output.ShouldBeEmpty();
  }

  public static async Task Should_write_a_problem_renderer_verbatim_when_opted_in()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .UseVerbatimRendererOutput()
      .ConfigureServices(static services =>
      {
        services.AddSingleton<T511ProblemPrefix>();
        services.AddSingleton<IResponseRenderer<SharedProblemDetails>, T511ProblemRenderer>();
      })
      .Map<T511ProblemQuery>()
      .Build();

    int exitCode = await app.RunAsync(["t511problem"]);

    exitCode.ShouldBe(17);
    terminal.ErrorOutput.ShouldBe("error=Bad input status=17");
  }

  public static async Task Should_exit_1_with_a_problem_renderer_when_the_status_is_out_of_range()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .ConfigureServices(static services =>
      {
        services.AddSingleton<T511ProblemPrefix>();
        services.AddSingleton<IResponseRenderer<SharedProblemDetails>, T511ProblemRenderer>();
      })
      .Map<T511BadStatusQuery>()
      .Build();

    int exitCode = await app.RunAsync(["t511badstatus"]);

    exitCode.ShouldBe(1);
    terminal.ErrorOutput.ShouldStartWith("error=Too big status=999");
  }

  public static async Task Should_use_a_runtime_di_problem_renderer()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .UseMicrosoftDependencyInjection()
      .UseVerbatimRendererOutput()
      // Runtime DI copies this lambda into generated code, so the names are fully qualified.
      .ConfigureServices(static services =>
      {
        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::TimeWarp.Nuru.Tests.Auto.RendererOutput.T511ProblemPrefix>(services);
        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::TimeWarp.Contracts.IResponseRenderer<global::TimeWarp.Foundation.Types.SharedProblemDetails>, global::TimeWarp.Nuru.Tests.Auto.RendererOutput.T511ProblemRenderer>(services);
      })
      .Map<T511ProblemQuery>()
      .Build();

    int exitCode = await app.RunAsync(["t511problem"]);

    exitCode.ShouldBe(17);
    terminal.ErrorOutput.ShouldBe("error=Bad input status=17");
  }

  public static async Task Should_keep_the_default_problem_lines_without_a_problem_renderer()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .UseVerbatimRendererOutput()
      .Map<T511ProblemQuery>()
      .Build();

    int exitCode = await app.RunAsync(["t511problem"]);

    exitCode.ShouldBe(17);
    string[] lines = terminal.ErrorOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
    lines.ShouldBe(["https://example.test/t511 Bad input", "too small"]);
  }
}

public sealed class T511VerbatimValue
{
  public string Text { get; set; } = "";
}

public sealed class T511TextRenderer : IResponseRenderer<T511VerbatimValue>
{
  // Ends without a newline so a CLI newline would show up in the output.
  public const string Text = "line one\nline two";

  public string Render(T511VerbatimValue response) => response.Text;
}

[Operation("t511verbatim")]
public sealed class T511VerbatimQuery : IQuery<T511VerbatimValue>
{
  public sealed class Handler : IQueryHandler<T511VerbatimQuery, T511VerbatimValue>
  {
    public Task<T511VerbatimValue> Handle(T511VerbatimQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult(new T511VerbatimValue { Text = T511TextRenderer.Text });
    }
  }
}

[Operation("t511plain")]
public sealed class T511PlainQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<T511PlainQuery, string>
  {
    public Task<string> Handle(T511PlainQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult("plain");
    }
  }
}

/// <summary>
/// Constructor dependency of <see cref="T511ProblemRenderer"/>. It keeps that renderer from being
/// the visible parameterless renderer for every problem pair in the compilation.
/// </summary>
public sealed class T511ProblemPrefix
{
  public string Value { get; } = "error=";
}

public sealed class T511ProblemRenderer : IResponseRenderer<SharedProblemDetails>
{
  private readonly T511ProblemPrefix Prefix;

  public T511ProblemRenderer(T511ProblemPrefix prefix)
  {
    Prefix = prefix;
  }

  public string Render(SharedProblemDetails response) =>
    $"{Prefix.Value}{response.Title} status={response.Status}";
}

[Operation("t511problem")]
public sealed class T511ProblemQuery : IQuery<OneOf<string, SharedProblemDetails>>
{
  public sealed class Handler : IQueryHandler<T511ProblemQuery, OneOf<string, SharedProblemDetails>>
  {
    public Task<OneOf<string, SharedProblemDetails>> Handle(T511ProblemQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      OneOf<string, SharedProblemDetails> problem = new SharedProblemDetails
      {
        Type = "https://example.test/t511",
        Title = "Bad input",
        Status = 17,
        Detail = "too small"
      };
      return Task.FromResult(problem);
    }
  }
}

[Operation("t511badstatus")]
public sealed class T511BadStatusQuery : IQuery<OneOf<string, SharedProblemDetails>>
{
  public sealed class Handler : IQueryHandler<T511BadStatusQuery, OneOf<string, SharedProblemDetails>>
  {
    public Task<OneOf<string, SharedProblemDetails>> Handle(T511BadStatusQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      OneOf<string, SharedProblemDetails> problem = new SharedProblemDetails
      {
        Type = "https://example.test/t511",
        Title = "Too big",
        Status = 999
      };
      return Task.FromResult(problem);
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Auto.RendererOutput
