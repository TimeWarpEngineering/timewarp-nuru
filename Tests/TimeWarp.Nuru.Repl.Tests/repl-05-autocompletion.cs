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
  [Input("ver")]
  [Input("st")] 
  [Input("g")]
  public static async Task Should_complete_partial_commands(string partialInput)
  {
    // Arrange
    ArgumentNullException.ThrowIfNull(partialInput);
    
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("version", () => WriteLine("v1.0.0"))
      .AddRoute("status", () => WriteLine("OK"))
      .AddRoute("greet {name}", (string name) => WriteLine($"Hello, {name}!"))
      .AddReplSupport();

    NuruApp app = builder.Build();
    CompletionProvider completionProvider = new(app.TypeConverterRegistry);

    // Act
    WriteLine($"Test: '{partialInput}' should suggest matching commands");
    var context = new CompletionContext(Args: [partialInput], CursorPosition: partialInput.Length, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates = completionProvider.GetCompletions(context, app.Endpoints);
    WriteLine($"  All candidates: {string.Join(", ", candidates.Select(c => c.Value))}");
    WriteLine($"  Candidates count: {candidates.Count()}");

    // Assert - Should find expected command based on partial input
    string expectedCommand = partialInput switch
    {
      "ver" => "version",
      "st" => "status",
      "g" => "greet",
      _ => throw new ArgumentException($"Unknown test input: {partialInput}")
    };

    candidates.ShouldNotBeEmpty($"Should find completions for '{partialInput}'");
    candidates.ShouldContain(c => c.Value == expectedCommand, $"Should contain '{expectedCommand}' for input '{partialInput}'");
    WriteLine();
  }

  /// <summary>
  /// Test parameter completion in REPL context
  /// </summary>
  [Input("greet")]
  [Input("add")]
  [Input("deploy")]
  public static async Task Should_complete_route_parameters(string command)
  {
    // Arrange
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("greet {name}", (string name) => WriteLine($"Hello, {name}!"))
      .AddRoute("add {a:int} {b:int}", (int a, int b) => WriteLine($"{a + b}"))
      .AddRoute("deploy {env} {tag?}", (string env, string? _ = null) => WriteLine($"Deploy to {env}"))
      .AddReplSupport();

    NuruApp app = builder.Build();
    var completionProvider = new CompletionProvider(app.TypeConverterRegistry);

    // Act
    string[] args = command switch
    {
      "greet" => ["greet", ""],
      "add" => ["add", ""],
      "deploy" => ["deploy", "production", ""],
      _ => throw new ArgumentException($"Unknown test command: {command}")
    };

    int cursorPos = args.Sum(a => a.Length) + args.Length - 1; // Account for spaces

    WriteLine($"Test: '{command} ' should suggest parameter completions");
    var context = new CompletionContext(Args: args, CursorPosition: cursorPos, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates = completionProvider.GetCompletions(context, app.Endpoints);
    WriteLine($"  Candidates: {string.Join(", ", candidates.Select(c => c.Value))}");
    WriteLine($"  Candidates count: {candidates.Count()}");

    // Assert
    candidates.ShouldNotBeEmpty($"Should find parameter completions for '{command}' command");
    WriteLine();
  }

  /// <summary>
  /// Test option completion in REPL context
  /// </summary>
  [Input("--")]
  [Input("--v")]
  [Input("--m")]
  public static async Task Should_complete_options(string optionInput)
  {
    // Arrange
    ArgumentNullException.ThrowIfNull(optionInput);
    
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("build --config {mode} --verbose,-v", (string mode, bool _) => WriteLine($"Building {mode}"))
      .AddRoute("deploy {env} --message,-m {msg?}", (string _, string? _ = null) => WriteLine("Deploying"))
      .AddRoute("git commit --message,-m {message} --amend", (string _, bool _) => WriteLine("Committing"))
      .AddReplSupport();

    NuruApp app = builder.Build();
    var completionProvider = new CompletionProvider(app.TypeConverterRegistry);

    // Act
    WriteLine($"Test: '{optionInput}' should suggest matching options");
    var context = new CompletionContext(Args: [optionInput], CursorPosition: optionInput.Length, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates = completionProvider.GetCompletions(context, app.Endpoints);
    WriteLine($"  All candidates: {string.Join(", ", candidates.Select(c => c.Value))}");
    WriteLine($"  Candidates count: {candidates.Count()}");

    // Assert based on input type
    switch (optionInput)
    {
      case "--":
        candidates.ShouldNotBeEmpty("Should find multiple options for '--'");
        candidates.ShouldContain(c => c.Value.Contains("config"), "Should contain --config option");
        candidates.ShouldContain(c => c.Value.Contains("verbose"), "Should contain --verbose option");
        candidates.ShouldContain(c => c.Value.Contains("message"), "Should contain --message option");
        break;
        
      case "--v":
        candidates.ShouldNotBeEmpty("Should find verbose option for '--v'");
        candidates.ShouldContain(c => c.Value.Contains("verbose"), "Should contain --verbose option");
        break;
        
      case "--m":
        candidates.ShouldNotBeEmpty("Should find message options for '--m'");
        candidates.ShouldContain(c => c.Value.Contains("message"), "Should contain --message option");
        break;
        
      default:
        throw new ArgumentException($"Unknown test input: {optionInput}");
    }
     WriteLine();
  }

  /// <summary>
  /// Test complex multi-word route completion
  /// </summary>
  [Input("git")]
  [Input("git ")]
  [Input("git add ")]
  [Input("docker run --")]
  public static async Task Should_complete_complex_routes(string complexInput)
  {
    // Arrange
    ArgumentNullException.ThrowIfNull(complexInput);
    
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("git status", () => WriteLine("Git status"))
      .AddRoute("git add {path}", (string path) => WriteLine($"Adding {path}"))
      .AddRoute("git commit --message,-m {message}", (string _, bool _) => WriteLine("Committing"))
      .AddRoute("docker run --image,-i {image} --detach,-d", (string image, bool _) => WriteLine($"Running {image}"))
      .AddReplSupport();

    NuruApp app = builder.Build();
    var completionProvider = new CompletionProvider(app.TypeConverterRegistry);

    // Act
    string[] args = complexInput switch
    {
      "git" => ["git"],
      "git " => ["git", ""],
      "git add " => ["git", "add", ""],
      "docker run --" => ["docker", "run", "--"],
      _ => throw new ArgumentException($"Unknown test input: {complexInput}")
    };

    int cursorPos = args.Sum(a => a.Length) + args.Length - 1; // Account for spaces

    WriteLine($"Test: '{complexInput}' should complete appropriately");
    var context = new CompletionContext(Args: args, CursorPosition: cursorPos, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates = completionProvider.GetCompletions(context, app.Endpoints);
    WriteLine($"  All candidates: {string.Join(", ", candidates.Select(c => c.Value))}");
    WriteLine($"  Candidates count: {candidates.Count()}");

    // Assert based on input type
    switch (complexInput)
    {
      case "git":
        candidates.ShouldNotBeEmpty("Should find git subcommands");
        candidates.ShouldContain(c => c.Value == "status", "Should contain 'status'");
        candidates.ShouldContain(c => c.Value == "add", "Should contain 'add'");
        candidates.ShouldContain(c => c.Value == "commit", "Should contain 'commit'");
        break;
        
      case "git ":
        candidates.ShouldNotBeEmpty("Should find git subcommands");
        candidates.ShouldContain(c => c.Value == "status", "Should contain 'status'");
        break;
        
      case "git add ":
        candidates.ShouldNotBeEmpty("Should find parameter completions for 'git add'");
        break;
        
      case "docker run --":
        candidates.ShouldNotBeEmpty("Should find docker options");
        candidates.ShouldContain(c => c.Value.Contains("image"), "Should contain --image option");
        candidates.ShouldContain(c => c.Value.Contains("detach"), "Should contain --detach option");
        break;
        
      default:
        throw new ArgumentException($"Unknown test input: {complexInput}");
    }
    WriteLine();
  }

  /// <summary>
  /// Test typed parameter completion (int, double, bool, etc.)
  /// </summary>
  [Input("add")]
  [Input("scale")]
  [Input("enable")]
  public static async Task Should_complete_typed_parameters(string command)
  {
    // Arrange
    ArgumentNullException.ThrowIfNull(command);
    
    NuruAppBuilder builder = new NuruAppBuilder()
      .AddRoute("add {a:int} {b:int}", (int a, int b) => WriteLine($"{a + b}"))
      .AddRoute("scale {factor:double}", (double factor) => WriteLine($"Scaling {factor}"))
      .AddRoute("enable {flag:bool}", (bool flag) => WriteLine($"Enabled: {flag}"))
      .AddRoute("delay {ms:int}", (int _) => WriteLine("Delay command"))
      .AddReplSupport();

    NuruApp app = builder.Build();
    var completionProvider = new CompletionProvider(app.TypeConverterRegistry);

    // Act
    WriteLine($"Test: '{command} ' should suggest {command} parameter completions");
    var context = new CompletionContext(Args: [command, ""], CursorPosition: command.Length + 1, Endpoints: app.Endpoints);
    IEnumerable<CompletionCandidate> candidates = completionProvider.GetCompletions(context, app.Endpoints);
    WriteLine($"  Candidates: {string.Join(", ", candidates.Select(c => c.Value))}");
    WriteLine($"  Candidates count: {candidates.Count()}");

    // Assert
    candidates.ShouldNotBeEmpty($"Should find parameter completions for '{command}' command");
    WriteLine();
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
    hasCompleteRoute.ShouldBeTrue();

    if (!hasCompleteRoute)
      throw new InvalidOperationException("__complete route not found in REPL integration");

    await Task.CompletedTask;
  }
}