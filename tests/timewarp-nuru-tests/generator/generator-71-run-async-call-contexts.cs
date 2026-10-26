#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 547: RunAsync() was intercepted only when the call sat in a statement shape the DSL
// interpreter walks (await x; var y = await x; return await x;). Other ordinary C# compiled, then
// threw "RunAsync was not intercepted" at runtime, for example (await app.RunAsync(args)).ShouldBe(0).
// Each test here runs RunAsync() in one such context and checks the handler ran. Review round 1
// added a local reassigned to a second app and app?.RunAsync(...).
#endregion

#region Design
// Runtime tests, multi-included in the CI assembly: an unintercepted call throws, so a passing test
// proves the generator emitted an interceptor for that call site. Every app uses Map("...") with a
// gen71-prefixed route so no other test file's endpoints reach it.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.Gen71RunAsyncCallContexts
{

[TestTag("Generator")]
public class RunAsyncCallContextTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RunAsyncCallContextTests>();

  public static async Task Should_intercept_parenthesized_await_used_as_receiver()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("gen71-work").WithHandler(() => "gen71 parenthesized").AsCommand().Done()
      .Build();

    // Act + Assert
    (await app.RunAsync(["gen71-work"])).ShouldBe(0);
    terminal.OutputContains("gen71 parenthesized").ShouldBeTrue();
  }

  public static async Task Should_intercept_await_passed_as_method_argument()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("gen71-work").WithHandler(() => "gen71 argument").AsCommand().Done()
      .Build();

    // Act
    List<int> exitCodes = [];
    exitCodes.Add(await app.RunAsync(["gen71-work"]));

    // Assert
    exitCodes.ShouldBe([0]);
    terminal.OutputContains("gen71 argument").ShouldBeTrue();
  }

  public static async Task Should_intercept_both_ternary_branches()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("gen71-yes").WithHandler(() => "gen71 yes").AsCommand().Done()
      .Map("gen71-no").WithHandler(() => "gen71 no").AsCommand().Done()
      .Build();

    // Act
    int yes = Environment.ProcessId > 0 ? await app.RunAsync(["gen71-yes"]) : await app.RunAsync(["gen71-no"]);
    int no = Environment.ProcessId < 0 ? await app.RunAsync(["gen71-yes"]) : await app.RunAsync(["gen71-no"]);

    // Assert
    yes.ShouldBe(0);
    no.ShouldBe(0);
    terminal.OutputContains("gen71 yes").ShouldBeTrue();
    terminal.OutputContains("gen71 no").ShouldBeTrue();
  }

  public static async Task Should_intercept_configure_await_false()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("gen71-work").WithHandler(() => "gen71 configure-await").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["gen71-work"]).ConfigureAwait(false);
    (await app.RunAsync(["gen71-work"]).ConfigureAwait(false)).ShouldBe(0);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("gen71 configure-await").ShouldBeTrue();
  }

  public static async Task Should_intercept_return_await()
  {
    // Arrange
    using TestTerminal terminal = new();

    // Act
    int exitCode = await RunReturnAwait(terminal);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("gen71 return").ShouldBeTrue();
  }

  public static async Task Should_intercept_expression_bodied_member()
  {
    // Arrange
    using TestTerminal terminal = new();

    // Act
    int exitCode = await RunExpressionBodied(terminal);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("gen71 expression-bodied").ShouldBeTrue();
  }

  public static async Task Should_intercept_inside_a_lambda_and_through_null_forgiving()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp? app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("gen71-work").WithHandler(() => "gen71 lambda").AsCommand().Done()
      .Build();

    Func<Task<int>> run = () => app!.RunAsync(["gen71-work"]);

    // Act
    int exitCode = await run();

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("gen71 lambda").ShouldBeTrue();
  }

  public static async Task Should_intercept_parenthesized_build_chain()
  {
    // Arrange
    using TestTerminal terminal = new();

    // Act + Assert
    (await NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("gen71-work").WithHandler(() => "gen71 chain").AsCommand().Done()
      .Build()
      .RunAsync(["gen71-work"])).ShouldBe(0);
    terminal.OutputContains("gen71 chain").ShouldBeTrue();
  }

  public static async Task Should_bind_each_call_to_the_app_the_local_holds_at_that_point()
  {
    // Review round 1 M1: a call bound through the local's final value landed on the second app too,
    // which emitted two interceptors for one call site and failed the build.
    // Arrange
    using TestTerminal first = new();
    using TestTerminal second = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(first)
      .Map("gen71-first").WithHandler(() => "gen71 first app").AsCommand().Done()
      .Build();

    // Act: statement form and an expression form on each app
    int firstStatement = await app.RunAsync(["gen71-first"]);
    (await app.RunAsync(["gen71-first"])).ShouldBe(0);

    app = NuruApp.CreateBuilder()
      .UseTerminal(second)
      .Map("gen71-second").WithHandler(() => "gen71 second app").AsCommand().Done()
      .Build();

    int secondStatement = await app.RunAsync(["gen71-second"]);
    (await app.RunAsync(["gen71-second"])).ShouldBe(0);

    // Assert
    firstStatement.ShouldBe(0);
    secondStatement.ShouldBe(0);
    first.OutputContains("gen71 first app").ShouldBeTrue();
    second.OutputContains("gen71 second app").ShouldBeTrue();
  }

  public static async Task Should_intercept_conditional_access()
  {
    // Review round 1 M2: app?.RunAsync(...) is a member binding, not a member access
    // Arrange
    using TestTerminal terminal = new();
    NuruApp? app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("gen71-work").WithHandler(() => "gen71 conditional").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await (app?.RunAsync(["gen71-work"]) ?? Task.FromResult(1));

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("gen71 conditional").ShouldBeTrue();
  }

  private static async Task<int> RunReturnAwait(TestTerminal terminal)
  {
    // An earlier await keeps the return await from being redundant (RCS1174)
    await Task.Yield();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("gen71-work").WithHandler(() => "gen71 return").AsCommand().Done()
      .Build();

    return await app.RunAsync(["gen71-work"]);
  }

  private static Task<int> RunExpressionBodied(TestTerminal terminal) =>
    NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("gen71-work").WithHandler(() => "gen71 expression-bodied").AsCommand().Done()
      .Build()
      .RunAsync(["gen71-work"]);
}

}
