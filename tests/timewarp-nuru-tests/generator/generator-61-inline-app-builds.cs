#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Every Build() call gets its own capabilities catalog, whatever statement it sits in (task 508-008).
// Before the fix, the generator gave every Build() in a method the first app of that method,
// and it skipped loop, if, using and lock bodies, expression lambdas and expression-bodied members.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.InlineAppBuilds
{

[TestTag("Generator")]
public class InlineAppBuildTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<InlineAppBuildTests>();

  public static async Task Should_attach_its_own_catalog_to_each_of_three_apps_in_one_method()
  {
    // Arrange
    NuruApp first = NuruApp.CreateBuilder()
      .Map("iab-first").WithHandler(() => "first").AsQuery().Done()
      .Build();

    NuruApp second = NuruApp.CreateBuilder()
      .Map("iab-second {name}").WithHandler((string name) => name).AsQuery().Done()
      .Map("iab-second-extra").WithHandler(() => "extra").AsCommand().Done()
      .Build();

    List<NuruApp> looped = [];
    foreach (string _ in new[] { "a", "b" })
    {
      NuruApp app = NuruApp.CreateBuilder()
        .Map("iab-looped").WithHandler(() => "looped").AsQuery().Done()
        .Build();
      looped.Add(app);
    }

    // Act + Assert
    Patterns(first).ShouldBe(["iab-first"]);
    Patterns(second).ShouldBe(["iab-second {name}", "iab-second-extra"]);
    looped.Count.ShouldBe(2);
    foreach (NuruApp app in looped)
      Patterns(app).ShouldBe(["iab-looped"]);

    await Task.CompletedTask;
  }

  public static async Task Should_dispatch_each_inline_app_to_its_own_routes()
  {
    // Arrange
    using TestTerminal firstTerminal = new();
    NuruApp first = NuruApp.CreateBuilder()
      .UseTerminal(firstTerminal)
      .Map("iab-run").WithHandler(() => "from-first").AsQuery().Done()
      .Build();

    using TestTerminal secondTerminal = new();
    NuruApp second = NuruApp.CreateBuilder()
      .UseTerminal(secondTerminal)
      .Map("iab-run").WithHandler(() => "from-second").AsQuery().Done()
      .Build();

    // Act
    int firstExit = await first.RunAsync(["iab-run"]);
    int secondExit = await second.RunAsync(["iab-run"]);

    // Assert
    firstExit.ShouldBe(0);
    secondExit.ShouldBe(0);
    firstTerminal.Output.ShouldContain("from-first");
    firstTerminal.Output.ShouldNotContain("from-second");
    secondTerminal.Output.ShouldContain("from-second");
    secondTerminal.Output.ShouldNotContain("from-first");
  }

  public static async Task Should_attach_a_catalog_inside_if_using_and_lock_bodies()
  {
    // Arrange
    NuruApp? inIf = null;
    if (Environment.ProcessId > 0)
    {
      inIf = NuruApp.CreateBuilder()
        .Map("iab-if").WithHandler(() => "if").AsQuery().Done()
        .Build();
    }

    NuruApp inUsing;
    using (TestTerminal terminal = new())
    {
      inUsing = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("iab-using").WithHandler(() => "using").AsQuery().Done()
        .Build();
    }

    NuruApp inLock;
    lock (LockObject)
    {
      inLock = NuruApp.CreateBuilder()
        .Map("iab-lock").WithHandler(() => "lock").AsQuery().Done()
        .Build();
    }

    NuruApp inFor = null!;
    for (int i = 0; i < 1; i++)
    {
      inFor = NuruApp.CreateBuilder()
        .Map("iab-for").WithHandler(() => "for").AsQuery().Done()
        .Build();
    }

    // Act + Assert
    inIf.ShouldNotBeNull();
    Patterns(inIf).ShouldBe(["iab-if"]);
    Patterns(inUsing).ShouldBe(["iab-using"]);
    Patterns(inLock).ShouldBe(["iab-lock"]);
    Patterns(inFor).ShouldBe(["iab-for"]);

    await Task.CompletedTask;
  }

  public static async Task Should_attach_a_catalog_in_lambdas_and_local_functions()
  {
    // Arrange
    Func<NuruApp> expressionLambda = () => NuruApp.CreateBuilder()
      .Map("iab-expression-lambda").WithHandler(() => "x").AsQuery().Done()
      .Build();

    Func<NuruApp> blockLambda = () =>
    {
      NuruApp app = NuruApp.CreateBuilder()
        .Map("iab-block-lambda").WithHandler(() => "x").AsQuery().Done()
        .Build();
      return app;
    };

    // Act + Assert
    Patterns(expressionLambda()).ShouldBe(["iab-expression-lambda"]);
    Patterns(blockLambda()).ShouldBe(["iab-block-lambda"]);
    Patterns(LocalBlock()).ShouldBe(["iab-local-block"]);
    Patterns(LocalExpression()).ShouldBe(["iab-local-expression"]);
    Patterns(Argument(NuruApp.CreateBuilder()
      .Map("iab-argument").WithHandler(() => "x").AsQuery().Done()
      .Build())).ShouldBe(["iab-argument"]);

    await Task.CompletedTask;

    static NuruApp LocalBlock()
    {
      return NuruApp.CreateBuilder()
        .Map("iab-local-block").WithHandler(() => "x").AsQuery().Done()
        .Build();
    }

    static NuruApp LocalExpression() => NuruApp.CreateBuilder()
      .Map("iab-local-expression").WithHandler(() => "x").AsQuery().Done()
      .Build();
  }

  public static async Task Should_attach_a_catalog_in_expression_bodied_members()
  {
    // Act + Assert
    Patterns(ExpressionBodiedMethod()).ShouldBe(["iab-expression-method"]);
    Patterns(ExpressionBodiedProperty).ShouldBe(["iab-expression-property"]);

    await Task.CompletedTask;
  }

  private static readonly object LockObject = new();

  private static NuruApp ExpressionBodiedMethod() => NuruApp.CreateBuilder()
    .Map("iab-expression-method").WithHandler(() => "x").AsQuery().Done()
    .Build();

  private static NuruApp ExpressionBodiedProperty => NuruApp.CreateBuilder()
    .Map("iab-expression-property").WithHandler(() => "x").AsQuery().Done()
    .Build();

  private static NuruApp Argument(NuruApp app) => app;

  // Built-in routes (--help, --version, ...) are not in the catalog; user routes are, in order.
  private static string[] Patterns(NuruApp app) => [.. app.Capabilities.Endpoints.Select(e => e.Pattern)];
}

}
