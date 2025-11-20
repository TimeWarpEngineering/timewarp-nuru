#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using TimeWarp.Nuru.Completion;
using TimeWarp.Nuru.Parsing;
using static System.Console;

return await RunTests<ReplAutocompletionTests>(clearCache: true);

[TestTag("REPL")]
[ClearRunfileCache]
public class ReplAutocompletionTests
{
  /// <summary>
  /// Test REPL creates CompletionProvider with proper dependencies
  /// </summary>
  public static async Task Should_create_completion_provider_in_repl()
  {
    // Arrange
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("version", () => WriteLine("v1.0.0"))
      .AddRoute("greet {name}", (string name) => WriteLine($"Hello, {name}!"))
      .AddReplSupport();

    NuruApp app = builder.Build();

    // Act - Create REPL components to test completion provider creation
    using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    ReplMode replMode = new(app, new ReplOptions { PersistHistory = false }, loggerFactory);

    // Assert - REPL should have completion capabilities
    app.TypeConverterRegistry.ShouldNotBeNull();
    app.Endpoints.Count.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test command completion in REPL context
  /// </summary>
  public static async Task Should_complete_partial_commands()
  {
    // Arrange
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("version", () => WriteLine("v1.0.0"))
      .AddRoute("status", () => WriteLine("OK"))
      .AddRoute("greet {name}", (string name) => WriteLine($"Hello, {name}!"))
      .AddReplSupport();

    NuruApp app = builder.Build();
    CompletionProvider completionProvider = new(app.TypeConverterRegistry);

    // Test 1: 'ver' should suggest 'version'
    WriteLine("Test 1: 'ver' should suggest 'version'");
    CompletionContext context1 = new(Args: ["ver"], CursorPosition: 3, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates1 = completionProvider.GetCompletions(context1, app.Endpoints);
    CompletionCandidate? versionCandidate = candidates1.FirstOrDefault(c => c.Value == "version");
    WriteLine($"  Expected: version - Actual: {versionCandidate?.Value ?? "none"}");
    WriteLine(versionCandidate?.Value == "version" ? "PASS" : "FAIL");
    WriteLine();

    // Test 2: 'st' should suggest 'status'
    WriteLine("Test 2: 'st' should suggest 'status'");
    CompletionContext context2 = new(Args: ["st"], CursorPosition: 2, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates2 = completionProvider.GetCompletions(context2, app.Endpoints);
    CompletionCandidate? statusCandidate = candidates2.FirstOrDefault(c => c.Value == "status");
    WriteLine($"  Expected: status - Actual: {statusCandidate?.Value ?? "none"}");
    WriteLine(statusCandidate?.Value == "status" ? "PASS" : "FAIL");
    WriteLine();

    // Test 3: 'g' should suggest 'greet'
    WriteLine("Test 3: 'g' should suggest 'greet'");
    CompletionContext context3 = new(Args: ["g"], CursorPosition: 1, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates3 = completionProvider.GetCompletions(context3, app.Endpoints);
    CompletionCandidate? greetCandidate = candidates3.FirstOrDefault(c => c.Value == "greet");
    WriteLine($"  Expected: greet - Actual: {greetCandidate?.Value ?? "none"}");
    WriteLine(greetCandidate?.Value == "greet" ? "PASS" : "FAIL");
    WriteLine();

    // Summary
    bool allPassed = versionCandidate?.Value == "version" &&
                     statusCandidate?.Value == "status" &&
                     greetCandidate?.Value == "greet";

    WriteLine($"=== Command Completion: {(allPassed ? "ALL TESTS PASSED" : "SOME TESTS FAILED")} ===");

    if (!allPassed)
      throw new InvalidOperationException("Command completion tests failed");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test parameter completion in REPL context
  /// </summary>
  public static async Task Should_complete_route_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("greet {name}", (string name) => WriteLine($"Hello, {name}!"))
      .AddRoute("add {a:int} {b:int}", (int a, int b) => WriteLine($"{a + b}"))
      .AddRoute("deploy {env} {tag?}", (string env, string? _ = null) => WriteLine($"Deploy to {env}"))
      .AddReplSupport();

    NuruApp app = builder.Build();
    var completionProvider = new CompletionProvider(app.TypeConverterRegistry);

    // Test 1: 'greet ' should suggest parameter completions
    WriteLine("Test 1: 'greet ' should suggest parameter completions");
    CompletionContext context1 = new(Args: ["greet", ""], CursorPosition: 6, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates1 = completionProvider.GetCompletions(context1, app.Endpoints);
    WriteLine($"  Candidates: {string.Join(", ", candidates1.Select(c => c.Value))}");
    WriteLine(candidates1.Any() ? "PASS" : "FAIL");
    WriteLine();

    // Test 2: 'add ' should suggest parameter completions for int type
    WriteLine("Test 2: 'add ' should suggest parameter completions");
    CompletionContext context2 = new(Args: ["add", ""], CursorPosition: 4, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates2 = completionProvider.GetCompletions(context2, app.Endpoints);
    WriteLine($"  Candidates: {string.Join(", ", candidates2.Select(c => c.Value))}");
    WriteLine(candidates2.Any() ? "PASS" : "FAIL");
    WriteLine();

    // Test 3: 'deploy production ' should suggest optional parameter
    WriteLine("Test 3: 'deploy production ' should suggest optional parameter");
    CompletionContext context3 = new(Args: ["deploy", "production", ""], CursorPosition: 18, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates3 = completionProvider.GetCompletions(context3, app.Endpoints);
    WriteLine($"  Candidates: {string.Join(", ", candidates3.Select(c => c.Value))}");
    WriteLine(candidates3.Any() ? "PASS" : "FAIL");
    WriteLine();

    // All tests should generate some candidates for parameters
    bool allPassed = candidates1.Any() && candidates2.Any() && candidates3.Any();

    WriteLine($"=== Parameter Completion: {(allPassed ? "ALL TESTS PASSED" : "SOME TESTS FAILED")} ===");

    if (!allPassed)
      throw new InvalidOperationException("Parameter completion tests failed");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test option completion in REPL context
  /// </summary>
  public static async Task Should_complete_options()
  {
    // Arrange
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("build --config {mode} --verbose,-v", (string mode, bool _) => WriteLine($"Building {mode}"))
      .AddRoute("deploy {env} --message,-m {msg?}", (string _, string? _ = null) => WriteLine("Deploying"))
      .AddRoute("git commit --message,-m {message} --amend", (string _, bool _) => WriteLine("Committing"))
      .AddReplSupport();

    NuruApp app = builder.Build();
    var completionProvider = new CompletionProvider(app.TypeConverterRegistry);

    // Test 1: '--' should suggest available options
    WriteLine("Test 1: '--' should suggest available options");
    CompletionContext context1 = new(Args: ["--"], CursorPosition: 2, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates1 = completionProvider.GetCompletions(context1, app.Endpoints);
    var candidates1List = candidates1.ToList();
    bool hasConfig = candidates1List.Any(c => c.Value.Contains("config"));
    bool hasVerbose = candidates1List.Any(c => c.Value.Contains("verbose"));
    bool hasMessage = candidates1List.Any(c => c.Value.Contains("message"));
    WriteLine($"  Options found: config={hasConfig}, verbose={hasVerbose}, message={hasMessage}");
    WriteLine(hasConfig && hasVerbose && hasMessage ? "PASS" : "FAIL");
    WriteLine();

    // Test 2: '--v' should suggest --verbose
    WriteLine("Test 2: '--v' should suggest --verbose");
    CompletionContext context2 = new(Args: ["--v"], CursorPosition: 3, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates2 = completionProvider.GetCompletions(context2, app.Endpoints);
    CompletionCandidate? verboseCandidate = candidates2.FirstOrDefault(c => c.Value.Contains("verbose"));
    WriteLine($"  Expected: --verbose - Actual: {verboseCandidate?.Value ?? "none"}");
    WriteLine(verboseCandidate?.Value?.Contains("verbose") == true ? "PASS" : "FAIL");
    WriteLine();

    // Test 3: '--m' should suggest --message options
    WriteLine("Test 3: '--m' should suggest --message options");
    CompletionContext context3 = new(Args: ["--m"], CursorPosition: 3, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates3 = completionProvider.GetCompletions(context3, app.Endpoints);
    var messageCandidates = candidates3.Where(c => c.Value.Contains("message")).ToList();
    WriteLine($"  Message options: {string.Join(", ", messageCandidates.Select(c => c.Value))}");
    WriteLine(messageCandidates.Count != 0 ? "PASS" : "FAIL");
    WriteLine();

    // Summary
    bool allPassed = hasConfig && hasVerbose && hasMessage &&
                     verboseCandidate?.Value?.Contains("verbose") == true &&
                     messageCandidates.Count > 0;

    WriteLine($"=== Option Completion: {(allPassed ? "ALL TESTS PASSED" : "SOME TESTS FAILED")} ===");

    if (!allPassed)
      throw new InvalidOperationException("Option completion tests failed");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test complex multi-word route completion
  /// </summary>
  public static async Task Should_complete_complex_routes()
  {
    // Arrange
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("git status", () => WriteLine("Git status"))
      .AddRoute("git add {path}", (string path) => WriteLine($"Adding {path}"))
      .AddRoute("git commit --message,-m {message}", (string _) => WriteLine("Committing"))
      .AddRoute("docker run --image,-i {image} --detach,-d", (string image, bool _) => WriteLine($"Running {image}"))
      .AddReplSupport();

    NuruApp app = builder.Build();
    var completionProvider = new CompletionProvider(app.TypeConverterRegistry);

    // Test 1: 'git' should suggest git subcommands
    WriteLine("Test 1: 'git' should suggest git subcommands");
    CompletionContext context1 = new(Args: ["git"], CursorPosition: 3, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates1 = completionProvider.GetCompletions(context1, app.Endpoints);
    var candidates1List = candidates1.ToList();
    bool hasStatus = candidates1List.Any(c => c.Value == "status");
    bool hasAdd = candidates1List.Any(c => c.Value == "add");
    bool hasCommit = candidates1List.Any(c => c.Value == "commit");
    WriteLine($"  Git subcommands: status={hasStatus}, add={hasAdd}, commit={hasCommit}");
    WriteLine(hasStatus && hasAdd && hasCommit ? "PASS" : "FAIL");
    WriteLine();

    // Test 2: 'git ' should suggest subcommands
    WriteLine("Test 2: 'git ' should suggest subcommands");
    CompletionContext context2 = new(Args: ["git", ""], CursorPosition: 4, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates2 = completionProvider.GetCompletions(context2, app.Endpoints);
    CompletionCandidate? statusCandidate2 = candidates2.FirstOrDefault(c => c.Value == "status");
    WriteLine($"  Expected: status - Actual: {statusCandidate2?.Value ?? "none"}");
    WriteLine(statusCandidate2?.Value == "status" ? "PASS" : "FAIL");
    WriteLine();

    // Test 3: 'git add ' should suggest parameter completion
    WriteLine("Test 3: 'git add ' should suggest parameter completion");
    CompletionContext context3 = new(Args: ["git", "add", ""], CursorPosition: 8, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates3 = completionProvider.GetCompletions(context3, app.Endpoints);
    WriteLine($"  Parameter candidates: {string.Join(", ", candidates3.Select(c => c.Value))}");
    WriteLine(candidates3.Any() ? "PASS" : "FAIL");
    WriteLine();

    // Test 4: 'docker run --' should suggest docker options
    WriteLine("Test 4: 'docker run --' should suggest docker options");
    CompletionContext context4 = new(Args: ["docker", "run", "--"], CursorPosition: 12, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates4 = completionProvider.GetCompletions(context4, app.Endpoints);
    var candidates4List = candidates4.ToList();
    bool hasImage = candidates4List.Any(c => c.Value.Contains("image"));
    bool hasDetach = candidates4List.Any(c => c.Value.Contains("detach"));
    WriteLine($"  Docker options: image={hasImage}, detach={hasDetach}");
    WriteLine(hasImage && hasDetach ? "PASS" : "FAIL");
    WriteLine();

    // Summary
    bool allPassed = hasStatus && hasAdd && hasCommit &&
                     statusCandidate2?.Value == "status" &&
                     candidates3.Any() &&
                     hasImage && hasDetach;

    WriteLine($"=== Complex Route Completion: {(allPassed ? "ALL TESTS PASSED" : "SOME TESTS FAILED")} ===");

    if (!allPassed)
      throw new InvalidOperationException("Complex route completion tests failed");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test typed parameter completion (int, double, bool, etc.)
  /// </summary>
  public static async Task Should_complete_typed_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("add {a:int} {b:int}", (int a, int b) => WriteLine($"{a + b}"))
      .AddRoute("scale {factor:double}", (double factor) => WriteLine($"Scaling {factor}"))
      .AddRoute("enable {flag:bool}", (bool flag) => WriteLine($"Enabled: {flag}"))
      .AddRoute("delay {ms:int}", (int _) => { WriteLine("Delay command"); })
      .AddReplSupport();

    NuruApp app = builder.Build();
    var completionProvider = new CompletionProvider(app.TypeConverterRegistry);

    // Test 1: 'add ' should suggest int parameter completions
    WriteLine("Test 1: 'add ' should suggest int parameter completions");
    CompletionContext context1 = new(Args: ["add", ""], CursorPosition: 4, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates1 = completionProvider.GetCompletions(context1, app.Endpoints);
    WriteLine($"  Int candidates: {string.Join(", ", candidates1.Select(c => c.Value))}");
    WriteLine(candidates1.Any() ? "PASS" : "FAIL");
    WriteLine();

    // Test 2: 'scale ' should suggest double parameter completions
    WriteLine("Test 2: 'scale ' should suggest double parameter completions");
    CompletionContext context2 = new(Args: ["scale", ""], CursorPosition: 6, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates2 = completionProvider.GetCompletions(context2, app.Endpoints);
    WriteLine($"  Double candidates: {string.Join(", ", candidates2.Select(c => c.Value))}");
    WriteLine(candidates2.Any() ? "PASS" : "FAIL");
    WriteLine();

    // Test 3: 'enable ' should suggest bool parameter completions
    WriteLine("Test 3: 'enable ' should suggest bool parameter completions");
    CompletionContext context3 = new(Args: ["enable", ""], CursorPosition: 7, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates3 = completionProvider.GetCompletions(context3, app.Endpoints);
    WriteLine($"  Bool candidates: {string.Join(", ", candidates3.Select(c => c.Value))}");
    WriteLine(candidates3.Any() ? "PASS" : "FAIL");
    WriteLine();

    // All typed parameters should generate some candidates
    bool allPassed = candidates1.Any() && candidates2.Any() && candidates3.Any();

    WriteLine($"=== Typed Parameter Completion: {(allPassed ? "ALL TESTS PASSED" : "SOME TESTS FAILED")} ===");

    if (!allPassed)
      throw new InvalidOperationException("Typed parameter completion tests failed");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test REPL completion integration using __complete command pattern
  /// </summary>
  public static async Task Should_integrate_completion_with_repl_commands()
  {
    // Arrange
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("version", () => WriteLine("v1.0.0"))
      .AddRoute("help", () => WriteLine("Help!"))
      .AddRoute("greet {name}", (string name) => WriteLine($"Hello, {name}!"))
      .AddReplSupport();

    // Enable dynamic completion to register __complete route
    builder.EnableDynamicCompletion();

    NuruApp app = builder.Build();

    // Act & Assert - Test __complete route exists
    bool hasCompleteRoute = app.Endpoints.Any(e =>
      e.CompiledRoute.PositionalMatchers.Count > 0 &&
      e.CompiledRoute.PositionalMatchers[0] is LiteralMatcher literal &&
      literal.Value == "__complete");

    WriteLine($"__complete route registered: {hasCompleteRoute}");
    WriteLine(hasCompleteRoute ? "PASS" : "FAIL");

    if (!hasCompleteRoute)
      throw new InvalidOperationException("__complete route not found in REPL integration");

    await Task.CompletedTask;
  }
}