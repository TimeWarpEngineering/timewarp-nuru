#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// CLI binding errors through the app's problem renderer after UseProblemRendererForErrors() (task 508-018).
// The CI multi-mode compilation must not contain a visible IResponseRenderer<SharedProblemDetails> (it would
// change every problem pair there), so these tests attach the renderer the generator would attach.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing.ProblemErrors
{
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;

[TestTag("Routing")]
public class ProblemErrorsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ProblemErrorsTests>();

  private static string Outline(SharedProblemDetails problem) =>
    $"error={problem.Type}\nerrorMessage={problem.Detail}\nkind={(problem.Extensions.TryGetValue(NuruProblems.KindExtension, out object? kind) ? kind : "")}";

  // Each Build() is its own app; RunAsync is intercepted only beside its builder, so the tests use ExecuteAsync.
  private static NuruApp CreateApp(TestTerminal terminal, bool optIn)
  {
    NuruApp app = optIn ? CreateOptedInApp(terminal) : CreateDefaultApp(terminal);
    app.ProblemRenderer = (_, problem) => Outline(problem);
    return app;
  }

  private static NuruApp CreateOptedInApp(TestTerminal terminal)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .UseProblemRendererForErrors()
      .Map("pe-delay {ms:int}").WithHandler((int ms) => $"ms:{ms}").AsCommand().Done()
      .Map("pe-greet {name}").WithHandler((string name) => $"hi {name}").AsQuery().Done()
      .Map<PeJsonRequiredQuery>()
      .Build();
    return app;
  }

  private static NuruApp CreateDefaultApp(TestTerminal terminal)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("pe-delay {ms:int}").WithHandler((int ms) => $"ms:{ms}").AsCommand().Done()
      .Map("pe-greet {name}").WithHandler((string name) => $"hi {name}").AsQuery().Done()
      .Build();
    return app;
  }

  public static async Task Should_map_a_bind_error_to_an_invalid_problem()
  {
    NuruBindError error = new(NuruBindErrorKind.MissingValue, "name", "Error: Missing required value 'name' for 'pe-greet {name}'.");

    SharedProblemDetails problem = NuruProblems.FromBindError(error);

    problem.Type.ShouldBe(NuruProblems.InvalidType);
    problem.Status.ShouldBe(400);
    problem.Detail.ShouldBe("Missing required value 'name' for 'pe-greet {name}'.");
    problem.Extensions[NuruProblems.KindExtension].ShouldBe("MissingValue");
    problem.Extensions[NuruProblems.KeyExtension].ShouldBe("name");

    SharedProblemDetails faulted = NuruProblems.FromException(new InvalidOperationException("boom"));
    faulted.Type.ShouldBe(NuruProblems.FaultedType);
    faulted.Status.ShouldBe(500);
    faulted.Detail.ShouldBe("boom");
    await Task.CompletedTask;
  }

  public static async Task Should_render_an_argv_conversion_error_when_opted_in()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal, optIn: true);
    app.ProblemRendererForErrors.ShouldBeTrue();

    int exitCode = await app.ExecuteAsync(["pe-delay", "abc"]);

    exitCode.ShouldBe(1);
    terminal.ErrorOutput.ShouldContain("error=Invalid");
    terminal.ErrorOutput.ShouldContain("errorMessage=Invalid value 'abc' for parameter 'ms'");
    terminal.ErrorOutput.ShouldContain("kind=InvalidValue");
    terminal.Output.ShouldNotContain("Error:");
  }

  public static async Task Should_render_a_json_args_bind_failure_when_opted_in()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal, optIn: true);

    int exitCode = await app.ExecuteAsync(["pe-greet", "--json-args", """{"name":"Ada","nope":1}"""]);

    exitCode.ShouldBe(1);
    terminal.ErrorOutput.ShouldContain("error=Invalid");
    terminal.ErrorOutput.ShouldContain("errorMessage=Unknown key 'nope'");
    terminal.ErrorOutput.ShouldContain("kind=UnknownKey");
    terminal.ErrorOutput.ShouldNotContain("Error: Unknown key");
  }

  public static async Task Should_render_a_json_args_input_error_when_opted_in()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal, optIn: true);

    int exitCode = await app.ExecuteAsync(["pe-greet", "--json-args", "[1]"]);

    exitCode.ShouldNotBe(0);
    terminal.ErrorOutput.ShouldContain("error=Invalid");
    terminal.ErrorOutput.ShouldContain("errorMessage=--json-args value must be -, @path, or a JSON object.");
  }

  public static async Task Should_render_argv_errors_for_a_json_member_when_opted_in()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal, optIn: true);

    int argv = await app.ExecuteAsync(["pe-jsonreq", "--Body", "nope"]);
    argv.ShouldBe(1);
    terminal.ErrorOutput.ShouldContain("error=Invalid");
    terminal.ErrorOutput.ShouldContain("errorMessage=--Body is JSON. Pass it with --json-args.");
    terminal.ErrorOutput.ShouldContain("kind=InvalidValue");
    terminal.ErrorOutput.ShouldNotContain("Error: --Body");

    terminal.Clear();
    int missing = await app.ExecuteAsync(["pe-jsonreq"]);
    missing.ShouldBe(1);
    terminal.ErrorOutput.ShouldContain("error=Invalid");
    terminal.ErrorOutput.ShouldContain("errorMessage=pass --Body via --json-args.");
    terminal.ErrorOutput.ShouldContain("kind=MissingValue");
    terminal.ErrorOutput.ShouldNotContain("Error: pass");
  }

  public static async Task Should_render_an_argument_error_with_its_kind()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal, optIn: true);

    app.WriteArgumentError("Error: Missing value for option '--file'. Expected: FileInfo", NuruBindErrorKind.MissingValue, "--file");

    terminal.ErrorOutput.ShouldContain("errorMessage=Missing value for option '--file'. Expected: FileInfo");
    terminal.ErrorOutput.ShouldContain("kind=MissingValue");
    terminal.Output.ShouldNotContain("Error:");
    await Task.CompletedTask;
  }

  public static async Task Should_keep_plain_errors_without_the_opt_in()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal, optIn: false);
    app.ProblemRendererForErrors.ShouldBeFalse();

    int argv = await app.ExecuteAsync(["pe-delay", "abc"]);
    argv.ShouldBe(1);
    terminal.Output.ShouldContain("Error: Invalid value 'abc' for parameter 'ms'");
    terminal.ErrorOutput.ShouldNotContain("error=Invalid");

    terminal.Clear();
    int json = await app.ExecuteAsync(["pe-greet", "--json-args", """{"name":"Ada","nope":1}"""]);
    json.ShouldBe(1);
    terminal.ErrorOutput.ShouldContain("Error: Unknown key 'nope'");
    terminal.ErrorOutput.ShouldNotContain("error=Invalid");
  }

  public static async Task Should_keep_plain_errors_without_a_problem_renderer()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal, optIn: true);
    app.ProblemRenderer = null;

    app.TryRenderProblem(NuruProblems.Invalid("Error: x"), out string? text).ShouldBeFalse();
    text.ShouldBeNull();

    int exitCode = await app.ExecuteAsync(["pe-delay", "abc"]);
    exitCode.ShouldBe(1);
    terminal.Output.ShouldContain("Error: Invalid value 'abc' for parameter 'ms'");
  }
}

public sealed class PeBody
{
  public string Name { get; set; } = "";
}

[Operation("pe-jsonreq")]
public sealed class PeJsonRequiredQuery : IQuery<string>
{
  public required PeBody Body { get; set; }

  public sealed class Handler : IQueryHandler<PeJsonRequiredQuery, string>
  {
    public Task<string> Handle(PeJsonRequiredQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      return Task.FromResult("body:" + query.Body.Name);
    }
  }
}
}
