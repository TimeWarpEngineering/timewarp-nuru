#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// NuruApp.TryBind / InvokeAsync / RenderAsync: the public JSON binder and invoke (task 508-003).
// Parity cases run the same JSON through --json-args and through the public API and compare
// the text each produces.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing.JsonBindInvoke
{
using System.Text.Json;
using OneOf;
using TimeWarp.Foundation.Types;

[TestTag("Routing")]
public class JsonBindInvokeTests
{
  public enum BindEnv
  {
    Dev,
    Staging,
    Prod
  }

  [ModuleInitializer]
  internal static void Register() => RegisterTests<JsonBindInvokeTests>();

  // One app per case keeps the CLI's route selection to the single route under test.
  private static NuruApp ParityApp(TestTerminal terminal)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("jbi-deploy {env}").WithHandler((string env) => $"pos:{env}").AsCommand().Done()
      .Map("jbi-count --count {count:int}").WithHandler((int count) => count * 2).AsQuery().Done()
      .Map("jbi-pick --env {env}").WithHandler((BindEnv env) => $"env:{env}").AsCommand().Done()
      .Map("jbi-docker run --env {e}*").WithHandler((string[] e) => $"e:[{string.Join(",", e)}]").AsCommand().Done()
      .Map("jbi-run {*args}").WithHandler((string[] args) => $"args:[{string.Join(",", args)}]").AsCommand().Done()
      .Map("jbi-promote {env} now").WithHandler((string env) => $"promoted:{env}").AsCommand().Done()
      .Map("jbi-git {repo} push").WithHandler((string repo) => $"push:{repo}").AsCommand().Done()
      .Map("jbi-tag {name} --note {note?}").WithHandler((string name, string? note) => $"tag:{name}|note:{note ?? "<null>"}").AsCommand().Done()
      .Map("jbi-flag --force").WithHandler((bool force) => $"force:{force}").AsCommand().Done()
      .Map("jbi-sum --n {n:int}*").WithHandler((int[] n) => n.Sum()).AsQuery().Done()
      .Map("jbi-exec -- {*rest}").WithHandler((string[] rest) => $"rest:[{string.Join(",", rest)}]").AsCommand().Done()
      .Map<JbiShipCommand>()
      .Build();
    return app;
  }

  public static async Task Should_bind_and_report_like_json_args_for_every_binding_case()
  {

    // RunAsync is intercepted on an app built in the calling method, so this app is built here.
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("jbi-deploy {env}").WithHandler((string env) => $"pos:{env}").AsCommand().Done()
      .Map("jbi-count --count {count:int}").WithHandler((int count) => count * 2).AsQuery().Done()
      .Map("jbi-pick --env {env}").WithHandler((BindEnv env) => $"env:{env}").AsCommand().Done()
      .Map("jbi-docker run --env {e}*").WithHandler((string[] e) => $"e:[{string.Join(",", e)}]").AsCommand().Done()
      .Map("jbi-run {*args}").WithHandler((string[] args) => $"args:[{string.Join(",", args)}]").AsCommand().Done()
      .Map("jbi-promote {env} now").WithHandler((string env) => $"promoted:{env}").AsCommand().Done()
      .Map("jbi-git {repo} push").WithHandler((string repo) => $"push:{repo}").AsCommand().Done()
      .Map("jbi-tag {name} --note {note?}").WithHandler((string name, string? note) => $"tag:{name}|note:{note ?? "<null>"}").AsCommand().Done()
      .Map("jbi-flag --force").WithHandler((bool force) => $"force:{force}").AsCommand().Done()
      .Map("jbi-sum --n {n:int}*").WithHandler((int[] n) => n.Sum()).AsQuery().Done()
      .Map("jbi-exec -- {*rest}").WithHandler((string[] rest) => $"rest:[{string.Join(",", rest)}]").AsCommand().Done()
      .Map<JbiShipCommand>()
      .Build();

    // The interpreter intercepts RunAsync only in block-level statements, so the cases are unrolled.
    int cli;
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-deploy {env}"), "--json-args", """{"env":"prod"}"""]);
    await AssertParity(app, terminal, cli, "jbi-deploy {env}", """{"env":"prod"}""", true, "pos:prod");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-deploy {env}"), "--json-args", """{"env":"dev","env":"prod"}"""]);
    await AssertParity(app, terminal, cli, "jbi-deploy {env}", """{"env":"dev","env":"prod"}""", true, "pos:prod");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-deploy {env}"), "--json-args", "{}"]);
    await AssertParity(app, terminal, cli, "jbi-deploy {env}", "{}", false, "Missing required value 'env'");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-deploy {env}"), "--json-args", """{"env":"prod","nope":1}"""]);
    await AssertParity(app, terminal, cli, "jbi-deploy {env}", """{"env":"prod","nope":1}""", false, "Unknown key 'nope'");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-deploy {env}"), "--json-args", """{"env":"prod","Env":"x"}"""]);
    await AssertParity(app, terminal, cli, "jbi-deploy {env}", """{"env":"prod","Env":"x"}""", false, "Unknown key 'Env'");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-deploy {env}"), "--json-args", """{"env":5}"""]);
    await AssertParity(app, terminal, cli, "jbi-deploy {env}", """{"env":5}""", false, "wrong JSON type");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-deploy {env}"), "--json-args", """{"env":null}"""]);
    await AssertParity(app, terminal, cli, "jbi-deploy {env}", """{"env":null}""", false, "wrong JSON type");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-count --count {count:int}"), "--json-args", """{"count":12}"""]);
    await AssertParity(app, terminal, cli, "jbi-count --count {count:int}", """{"count":12}""", true, "24");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-count --count {count:int}"), "--json-args", """{"count":"12"}"""]);
    await AssertParity(app, terminal, cli, "jbi-count --count {count:int}", """{"count":"12"}""", false, "Expected int");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-count --count {count:int}"), "--json-args", """{"count":1.5}"""]);
    await AssertParity(app, terminal, cli, "jbi-count --count {count:int}", """{"count":1.5}""", false, "wrong JSON type");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-count --count {count:int}"), "--json-args", """{"count":99999999999}"""]);
    await AssertParity(app, terminal, cli, "jbi-count --count {count:int}", """{"count":99999999999}""", false, "wrong JSON type");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-count --count {count:int}"), "--json-args", "{}"]);
    await AssertParity(app, terminal, cli, "jbi-count --count {count:int}", "{}", false, "Missing required value 'count'");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-pick --env {env}"), "--json-args", """{"env":"prod"}"""]);
    await AssertParity(app, terminal, cli, "jbi-pick --env {env}", """{"env":"prod"}""", true, "env:Prod");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-pick --env {env}"), "--json-args", """{"env":"nope"}"""]);
    await AssertParity(app, terminal, cli, "jbi-pick --env {env}", """{"env":"nope"}""", false, "wrong JSON type");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-docker run --env {e}*"), "--json-args", """{"env":["A","B"]}"""]);
    await AssertParity(app, terminal, cli, "jbi-docker run --env {e}*", """{"env":["A","B"]}""", true, "e:[A,B]");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-docker run --env {e}*"), "--json-args", "{}"]);
    await AssertParity(app, terminal, cli, "jbi-docker run --env {e}*", "{}", false, "Missing required value 'env'");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-docker run --env {e}*"), "--json-args", """{"env":"A"}"""]);
    await AssertParity(app, terminal, cli, "jbi-docker run --env {e}*", """{"env":"A"}""", false, "Expected string array");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-docker run --env {e}*"), "--json-args", """{"env":[1]}"""]);
    await AssertParity(app, terminal, cli, "jbi-docker run --env {e}*", """{"env":[1]}""", false, "wrong JSON type");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-run {*args}"), "--json-args", """{"args":["one","two"]}"""]);
    await AssertParity(app, terminal, cli, "jbi-run {*args}", """{"args":["one","two"]}""", true, "args:[one,two]");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-run {*args}"), "--json-args", """{"args":"one"}"""]);
    await AssertParity(app, terminal, cli, "jbi-run {*args}", """{"args":"one"}""", false, "wrong JSON type");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-promote {env} now"), "--json-args", """{"env":"prod"}"""]);
    await AssertParity(app, terminal, cli, "jbi-promote {env} now", """{"env":"prod"}""", true, "promoted:prod");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-promote {env} now"), "--json-args", "{}"]);
    await AssertParity(app, terminal, cli, "jbi-promote {env} now", "{}", false, "Missing required value 'env'");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-git {repo} push"), "--json-args", """{"repo":"r1"}"""]);
    await AssertParity(app, terminal, cli, "jbi-git {repo} push", """{"repo":"r1"}""", true, "push:r1");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-tag {name} --note {note?}"), "--json-args", """{"name":"v1"}"""]);
    await AssertParity(app, terminal, cli, "jbi-tag {name} --note {note?}", """{"name":"v1"}""", true, "tag:v1|note:<null>");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-tag {name} --note {note?}"), "--json-args", """{"name":"v1","note":null}"""]);
    await AssertParity(app, terminal, cli, "jbi-tag {name} --note {note?}", """{"name":"v1","note":null}""", true, "tag:v1|note:<null>");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-tag {name} --note {note?}"), "--json-args", """{"name":"v1","note":"x"}"""]);
    await AssertParity(app, terminal, cli, "jbi-tag {name} --note {note?}", """{"name":"v1","note":"x"}""", true, "tag:v1|note:x");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-flag --force"), "--json-args", """{"force":true}"""]);
    await AssertParity(app, terminal, cli, "jbi-flag --force", """{"force":true}""", true, "force:True");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-flag --force"), "--json-args", "{}"]);
    await AssertParity(app, terminal, cli, "jbi-flag --force", "{}", true, "force:False");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-flag --force"), "--json-args", """{"force":"yes"}"""]);
    await AssertParity(app, terminal, cli, "jbi-flag --force", """{"force":"yes"}""", false, "Expected bool");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-sum --n {n:int}*"), "--json-args", """{"n":[1,2,3]}"""]);
    await AssertParity(app, terminal, cli, "jbi-sum --n {n:int}*", """{"n":[1,2,3]}""", true, "6");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-sum --n {n:int}*"), "--json-args", """{"n":[1,"2"]}"""]);
    await AssertParity(app, terminal, cli, "jbi-sum --n {n:int}*", """{"n":[1,"2"]}""", false, "wrong JSON type");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-exec -- {*rest}"), "--json-args", """{"rest":["a","--b"]}"""]);
    await AssertParity(app, terminal, cli, "jbi-exec -- {*rest}", """{"rest":["a","--b"]}""", true, "rest:[a,--b]");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-ship"), "--json-args", """{"env":"prod"}"""]);
    await AssertParity(app, terminal, cli, "jbi-ship", """{"env":"prod"}""", true, "env:prod|title:untitled|retries:3");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-ship"), "--json-args", """{"retries":5,"title":"t"}"""]);
    await AssertParity(app, terminal, cli, "jbi-ship", """{"retries":5,"title":"t"}""", true, "env:dev|title:t|retries:5");
    terminal.ClearOutput();
    cli = await app.RunAsync([.. LiteralWords("jbi-ship"), "--json-args", """{"retries":"5"}"""]);
    await AssertParity(app, terminal, cli, "jbi-ship", """{"retries":"5"}""", false, "Expected int");
  }

  public static async Task Should_return_a_structured_error_with_the_key()
  {
    using TestTerminal terminal = new();
    NuruApp app = ParityApp(terminal);

    Bind(app, "jbi-deploy {env}", """{"env":"prod","nope":1}""").ShouldBe((NuruBindErrorKind.UnknownKey, "nope"));
    Bind(app, "jbi-count --count {count:int}", """{"count":"12"}""").ShouldBe((NuruBindErrorKind.WrongType, "count"));
    Bind(app, "jbi-deploy {env}", "{}").ShouldBe((NuruBindErrorKind.MissingValue, "env"));
    Bind(app, "jbi-deploy", "{}").ShouldBe((NuruBindErrorKind.UnknownRoute, (string?)null));
    Bind(app, "jbi-deploy {env}", "[1]").ShouldBe((NuruBindErrorKind.NotAnObject, (string?)null));

    await Task.CompletedTask;
  }

  public static async Task Should_treat_a_default_json_element_as_no_arguments()
  {
    using TestTerminal terminal = new();
    NuruApp app = ParityApp(terminal);

    app.TryBind("jbi-flag --force", default, out NuruBoundRequest? request, out NuruBindError? error).ShouldBeTrue(error?.Message);
    request.Route.ShouldBe("jbi-flag --force");
    (await app.InvokeAsync(request)).ShouldBe("force:False");
  }

  public static async Task Should_return_the_typed_result_without_writing_it()
  {
    using TestTerminal terminal = new();
    NuruApp app = ParityApp(terminal);

    using JsonDocument doc = JsonDocument.Parse("""{"count":21}""");
    app.TryBind("jbi-count --count {count:int}", doc.RootElement, out NuruBoundRequest? request, out _).ShouldBeTrue();
    object? result = await app.InvokeAsync(request);

    result.ShouldBeOfType<int>();
    result.ShouldBe(42);
    terminal.Output.ShouldBeEmpty();

    using TestTerminal render = new();
    int exitCode = await app.RenderAsync(request, result, render);
    exitCode.ShouldBe(0);
    render.Output.Trim().ShouldBe("42");
  }

  public static async Task Should_bind_on_an_app_that_never_runs()
  {
    NuruApp app = NuruApp.CreateBuilder()
      .Map("jbi-only {name}").WithHandler((string name) => $"hi:{name}").AsQuery().Done()
      .Build();

    using JsonDocument doc = JsonDocument.Parse("""{"name":"ada"}""");
    app.TryBind("jbi-only {name}", doc.RootElement, out NuruBoundRequest? request, out NuruBindError? error).ShouldBeTrue(error?.Message);
    (await app.InvokeAsync(request)).ShouldBe("hi:ada");
  }

  public static async Task Should_not_start_the_repl_on_an_app_that_starts_it_when_empty()
  {
    // InvokeAsync runs route execution with empty argv; that must not count as "no arguments".
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options => options.AutoStartWhenEmpty = true)
      .Map("jbi-repl {name}").WithHandler((string name) => $"repl:{name}").AsQuery().Done()
      .Build();

    using JsonDocument doc = JsonDocument.Parse("""{"name":"ada"}""");
    app.TryBind("jbi-repl {name}", doc.RootElement, out NuruBoundRequest? request, out NuruBindError? error).ShouldBeTrue(error?.Message);
    object? result = await app.InvokeAsync(request);

    result.ShouldBe("repl:ada");
    terminal.Output.ShouldBeEmpty();
  }

  public static async Task Should_run_nuru_behaviors_around_the_handler()
  {
    JbiTrace.Hits = 0;
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddBehavior(typeof(JbiTraceBehavior))
      .Map<JbiTracedQuery>()
      .Build();

    using JsonDocument doc = JsonDocument.Parse("""{"name":"ada"}""");
    app.TryBind("jbi-traced", doc.RootElement, out NuruBoundRequest? request, out NuruBindError? error).ShouldBeTrue(error?.Message);
    object? result = await app.InvokeAsync(request);

    result.ShouldBe("traced:ada");
    JbiTrace.Hits.ShouldBe(1);
  }

  public static async Task Should_return_the_problem_pair_and_render_it_like_the_cli()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<JbiProblemQuery>()
      .Build();

    using JsonDocument doc = JsonDocument.Parse("""{"fail":true}""");
    app.TryBind("jbi-problem", doc.RootElement, out NuruBoundRequest? request, out NuruBindError? error).ShouldBeTrue(error?.Message);
    object? result = await app.InvokeAsync(request);

    result.ShouldBeOfType<OneOf<string, SharedProblemDetails>>();
    ((OneOf<string, SharedProblemDetails>)result).IsT1.ShouldBeTrue();
    terminal.ErrorOutput.ShouldBeEmpty();

    using TestTerminal render = new();
    int exitCode = await app.RenderAsync(request, result, render);
    exitCode.ShouldBe(17);
    render.ErrorContains("https://example.test/jbi Bad input").ShouldBeTrue();
    render.ErrorContains("too small").ShouldBeTrue();
  }

  public static async Task Should_render_a_complex_result_as_json()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("jbi-list").WithHandler(() => new List<string> { "a", "b" }).AsQuery().Done()
      .Build();

    app.TryBind("jbi-list", default, out NuruBoundRequest? request, out _).ShouldBeTrue();
    object? result = await app.InvokeAsync(request);
    result.ShouldBeOfType<List<string>>();

    using TestTerminal render = new();
    await app.RenderAsync(request, result, render);
    render.OutputContains("\"a\"").ShouldBeTrue();
    render.OutputContains("\"b\"").ShouldBeTrue();
  }

  public static async Task Should_reject_a_request_bound_by_another_app()
  {
    using TestTerminal terminal = new();
    NuruApp first = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("jbi-first").WithHandler(() => "first").AsQuery().Done()
      .Build();
    NuruApp second = SecondApp(terminal);

    first.TryBind("jbi-first", default, out NuruBoundRequest? request, out _).ShouldBeTrue();
    second.TryBind("jbi-first", default, out _, out NuruBindError? error).ShouldBeFalse();
    error.Kind.ShouldBe(NuruBindErrorKind.UnknownRoute);

    await Should.ThrowAsync<ArgumentException>(() => second.InvokeAsync(request));
  }

  // Its own method: the interpreter models one app per method.
  private static NuruApp SecondApp(TestTerminal terminal)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("jbi-second").WithHandler(() => "second").AsQuery().Done()
      .Build();
    return app;
  }

  private static (NuruBindErrorKind Kind, string? Key) Bind(NuruApp app, string pattern, string json)
  {
    using JsonDocument doc = JsonDocument.Parse(json);
    app.TryBind(pattern, doc.RootElement, out _, out NuruBindError? error).ShouldBeFalse();
    return (error.Kind, error.Key);
  }

  // The route's literal words: the argv TryBind binds with.
  private static string[] LiteralWords(string pattern) =>
    [.. pattern.Split(' ').Where(w => !w.StartsWith('{') && !(w.StartsWith('-') && w != "--"))];

  // Compares the --json-args run just made on terminal with the same JSON through TryBind and InvokeAsync.
  private static async Task AssertParity(NuruApp app, TestTerminal terminal, int cliExitCode, string pattern, string json, bool bound, string expect)
  {
    (bool Ok, string Text) cli = cliExitCode == 0 ? (true, terminal.Output) : (false, terminal.ErrorOutput.Trim());
    (bool Ok, string Text) api = await ViaPublicApi(app, terminal, pattern, json);

    cli.Ok.ShouldBe(bound, $"--json-args {pattern} {json}: {cli.Text}");
    api.Ok.ShouldBe(bound, $"TryBind {pattern} {json}: {api.Text}");
    api.Text.ShouldBe(cli.Text, $"{pattern} {json}");
    api.Text.ShouldContain(expect, Case.Sensitive, $"{pattern} {json}");
  }

  private static async Task<(bool Ok, string Text)> ViaPublicApi(NuruApp app, TestTerminal terminal, string pattern, string json)
  {
    using JsonDocument doc = JsonDocument.Parse(json);
    if (!app.TryBind(pattern, doc.RootElement, out NuruBoundRequest? request, out NuruBindError? error))
      return (false, error.Message);

    // Handler side effects go to the app's terminal; the rendered result to its own.
    terminal.ClearOutput();
    object? result = await app.InvokeAsync(request);
    using TestTerminal render = new();
    int exitCode = await app.RenderAsync(request, result, render);
    exitCode.ShouldBe(0);
    return (true, terminal.Output + render.Output);
  }
}

public static class JbiTrace
{
  public static int Hits { get; set; }
}

public sealed class JbiTraceBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(proceed);
    if (context.CommandName == "jbi-traced")
      JbiTrace.Hits++;

    await proceed();
  }
}

[NuruRoute("jbi-traced")]
public sealed class JbiTracedQuery : IQuery<string>
{
  [Option("name", Description = "Name to trace")]
  public string Name { get; set; } = "";

  public sealed class Handler : IQueryHandler<JbiTracedQuery, string>
  {
    public Task<string> Handle(JbiTracedQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      return Task.FromResult($"traced:{query.Name}");
    }
  }
}

[NuruRoute("jbi-ship")]
public sealed class JbiShipCommand : ICommand<Unit>
{
  [Option("env", Description = "Target environment")]
  public string Env { get; set; } = "dev";

  [Option("title", Description = "Display title")]
  public string Title { get; set; } = "untitled";

  [Option("retries", Description = "Retry count")]
  public int Retries { get; set; } = 3;

  internal sealed class Handler(ITerminal terminal) : ICommandHandler<JbiShipCommand, Unit>
  {
    public async Task<Unit> Handle(JbiShipCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      cancellationToken.ThrowIfCancellationRequested();
      await terminal.WriteLineAsync($"env:{command.Env}|title:{command.Title}|retries:{command.Retries}").ConfigureAwait(false);
      return default;
    }
  }
}

[NuruRoute("jbi-problem")]
public sealed class JbiProblemQuery : IQuery<OneOf<string, SharedProblemDetails>>
{
  [Option("fail", Description = "Return a problem")]
  public bool Fail { get; set; }

  public sealed class Handler : IQueryHandler<JbiProblemQuery, OneOf<string, SharedProblemDetails>>
  {
    public Task<OneOf<string, SharedProblemDetails>> Handle(JbiProblemQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      if (query.Fail)
      {
        OneOf<string, SharedProblemDetails> problem = new SharedProblemDetails
        {
          Type = "https://example.test/jbi",
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
}
