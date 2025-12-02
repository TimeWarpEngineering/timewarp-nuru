#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

using TimeWarp.Nuru;

return await RunTests<ShouldIgnoreCommandTests>(clearCache: true);

[TestTag("REPL")]
[ClearRunfileCache]
public class ShouldIgnoreCommandTests
{
  // Shared helper instance for all tests
  private static ReplSessionHelper? Helper;

  public static async Task Setup()
  {
    Helper = new ReplSessionHelper();
    await Task.CompletedTask;
  }

  public static async Task CleanUp()
  {
    Helper = null;
    await Task.CompletedTask;
  }

  public static async Task Should_block_common_secrets_with_default_patterns()
  {
    // Arrange
    ReplOptions options = new();
    List<string> testCommands =
    [
    "login --password secret123",
      "set apikey=ABC",
      "deploy --token xyz",
      "export SECRET_KEY=value",
      "configure --credential admin"
    ];

    // Act & Assert
    foreach (string cmd in testCommands)
    {
      bool isBlocked = Helper!.ShouldIgnoreCommand(cmd, options);
      isBlocked.ShouldBeTrue($"Command should be blocked: {cmd}");
    }

    await Task.CompletedTask;
  }

  [Input("PASSWORD=123")]
  [Input("Password=456")]
  [Input("password=789")]
  [Input("PaSsWoRd=000")]
  public static async Task Should_match_case_insensitive_patterns(string caseVariant)
  {
    // Arrange
    ReplOptions options = new();

    // Act
    bool isBlocked = Helper!.ShouldIgnoreCommand(caseVariant, options);

    // Assert
    isBlocked.ShouldBeTrue($"Case variant should be blocked: {caseVariant}");

    await Task.CompletedTask;
  }

  public static async Task Should_match_wildcard_asterisk_patterns()
  {
    // Arrange
    ReplOptions options = new() { HistoryIgnorePatterns = ["*secret*"] };
    string[] shouldMatch = ["secret", "mysecret", "secretvalue", "has_secret_in_middle"];
    string[] shouldNotMatch = ["secure", "secrecy", "sacred"];

    // Act & Assert - Should match
    foreach (string cmd in shouldMatch)
    {
      bool isBlocked = Helper!.ShouldIgnoreCommand(cmd, options);
      isBlocked.ShouldBeTrue($"Should match '*secret*': {cmd}");
    }

    // Act & Assert - Should not match
    foreach (string cmd in shouldNotMatch)
    {
      bool isBlocked = Helper!.ShouldIgnoreCommand(cmd, options);
      isBlocked.ShouldBeFalse($"Should NOT match '*secret*': {cmd}");
    }

    await Task.CompletedTask;
  }

  public static async Task Should_match_wildcard_question_mark_patterns()
  {
    // Arrange
    ReplOptions options = new() { HistoryIgnorePatterns = ["log?n"] };
    string[] shouldMatch = ["login", "log1n", "logXn", "loggn"];  // ? matches any single char including 'g'
    string[] shouldNotMatch = ["logiin", "lon", "log123n", "logn"];  // These don't have exactly one char between log and n

    // Act & Assert - Should match
    foreach (string cmd in shouldMatch)
    {
      bool isBlocked = Helper!.ShouldIgnoreCommand(cmd, options);
      isBlocked.ShouldBeTrue($"Should match 'log?n': {cmd}");
    }

    // Act & Assert - Should not match
    foreach (string cmd in shouldNotMatch)
    {
      bool isBlocked = Helper!.ShouldIgnoreCommand(cmd, options);
      isBlocked.ShouldBeFalse($"Should NOT match 'log?n': {cmd}");
    }

    await Task.CompletedTask;
  }

  public static async Task Should_filter_with_custom_patterns()
  {
    // Arrange
    ReplOptions options = new()
    {
      HistoryIgnorePatterns = ["deploy prod*", "*staging*", "test-*-command"]
    };

    string[] shouldBlock =
    [
    "deploy production",
      "deploy prod --force",
      "test staging env",
      "staging-server",
      "test-abc-command"
    ];

    string[] shouldAllow =
    [
    "deploy dev",
      "production deploy",  // doesn't start with "deploy prod"
      "test",
      "command-test"
    ];

    // Act & Assert - Should block
    foreach (string cmd in shouldBlock)
    {
      bool isBlocked = Helper!.ShouldIgnoreCommand(cmd, options);
      isBlocked.ShouldBeTrue($"Should block: {cmd}");
    }

    // Act & Assert - Should allow
    foreach (string cmd in shouldAllow)
    {
      bool isBlocked = Helper!.ShouldIgnoreCommand(cmd, options);
      isBlocked.ShouldBeFalse($"Should allow: {cmd}");
    }

    await Task.CompletedTask;
  }

  public static async Task Should_allow_all_commands_with_null_or_empty_patterns()
  {
    // Arrange & Act - Test null patterns
    ReplOptions nullOptions = new() { HistoryIgnorePatterns = null };
    bool nullBlocks = Helper!.ShouldIgnoreCommand("password=123", nullOptions);

    // Arrange & Act - Test empty patterns
    ReplOptions emptyOptions = new() { HistoryIgnorePatterns = [] };
    bool emptyBlocks = Helper!.ShouldIgnoreCommand("secret=456", emptyOptions);

    // Assert
    nullBlocks.ShouldBeFalse("Null patterns should allow all commands");
    emptyBlocks.ShouldBeFalse("Empty patterns should allow all commands");

    await Task.CompletedTask;
  }

  public static async Task Should_escape_special_regex_characters()
  {
    // Arrange
    ReplOptions options = new() { HistoryIgnorePatterns = ["login.*", "test[123]", "end$"] };

    // These should match exactly (special chars are escaped)
    string[] shouldMatchExact = ["login.*", "test[123]", "end$"];

    // These should NOT match (regex chars are literal)
    string[] shouldNotMatch = ["loginABC", "test1", "test2", "test3", "end", "endX"];

    // Act & Assert - Should match exactly
    foreach (string cmd in shouldMatchExact)
    {
      bool isBlocked = Helper!.ShouldIgnoreCommand(cmd, options);
      isBlocked.ShouldBeTrue($"Should match exactly: {cmd}");
    }

    // Act & Assert - Should not match
    foreach (string cmd in shouldNotMatch)
    {
      bool isBlocked = Helper!.ShouldIgnoreCommand(cmd, options);
      isBlocked.ShouldBeFalse($"Should NOT match: {cmd}");
    }

    await Task.CompletedTask;
  }

  [Timeout(5000)] // 5 second timeout
  public static async Task Should_perform_pattern_matching_efficiently()
  {
    // Arrange
    ReplOptions options = new()
    {
      HistoryIgnorePatterns =
    [
        "*password*", "*secret*", "*token*", "*apikey*", "*credential*",
          "deploy prod*", "test staging*", "backup *", "restore *"
    ]
    };

    // Act
    Stopwatch stopwatch = Stopwatch.StartNew();
    for (int i = 0; i < 1000; i++)
    {
      _ = Helper!.ShouldIgnoreCommand($"command{i}", options);
      _ = Helper!.ShouldIgnoreCommand($"password{i}", options);
    }

    stopwatch.Stop();

    // Assert
    double perCheck = stopwatch.ElapsedMilliseconds / 2000.0;
    perCheck.ShouldBeLessThan(1.0, $"Performance too slow: {perCheck:F3}ms per check");

    await Task.CompletedTask;
  }

  public static async Task Should_match_combined_wildcard_patterns()
  {
    // Arrange
    ReplOptions options = new() { HistoryIgnorePatterns = ["deploy?prod*", "*-secret-*"] };

    string[] shouldMatch =
    [
    "deploy-production",
      "deploy_prod",
      "deploy1prod_server",
      "test-secret-value",
      "my-secret-key"
    ];

    string[] shouldNotMatch =
    [
    "deploy-dev",
      "deployproduction",  // Missing single char between deploy and prod
      "secret-value",      // Missing first dash
      "test-secure-value"
    ];

    // Act & Assert - Should match
    foreach (string cmd in shouldMatch)
    {
      bool isBlocked = Helper!.ShouldIgnoreCommand(cmd, options);
      isBlocked.ShouldBeTrue($"Should match pattern: {cmd}");
    }

    // Act & Assert - Should not match
    foreach (string cmd in shouldNotMatch)
    {
      bool isBlocked = Helper!.ShouldIgnoreCommand(cmd, options);
      isBlocked.ShouldBeFalse($"Should NOT match pattern: {cmd}");
    }

    await Task.CompletedTask;
  }

  public static async Task Should_have_correct_default_patterns()
  {
    // Arrange
    ReplOptions defaultOptions = new();
    string[] expectedDefaults = ["*password*", "*secret*", "*token*", "*apikey*", "*credential*", "clear-history"];

    // Act & Assert
    defaultOptions.HistoryIgnorePatterns.ShouldNotBeNull("Default patterns should not be null");

    foreach (string pattern in expectedDefaults)
    {
      defaultOptions.HistoryIgnorePatterns.ShouldContain(pattern,
          $"Default patterns should contain: {pattern}");
    }

    await Task.CompletedTask;
  }

  public static async Task Should_block_clear_history_command()
  {
    // Arrange - clear-history should never be added to history
    // This prevents confusing behavior where clear-history appears after clearing
    ReplOptions options = new();

    // Act
    bool isBlocked = Helper!.ShouldIgnoreCommand("clear-history", options);

    // Assert
    isBlocked.ShouldBeTrue("clear-history command should be blocked from history");

    await Task.CompletedTask;
  }
}

// Helper class to test ReplSession's ShouldIgnoreCommand method
internal sealed class ReplSessionHelper
{
  private readonly NuruCoreApp App;
  private readonly ILoggerFactory LoggerFactoryInstance;

  public ReplSessionHelper()
  {
    App = new NuruAppBuilder().Build();
    LoggerFactoryInstance = LoggerFactory.Create(_ => { });
  }

  public bool ShouldIgnoreCommand(string command, ReplOptions options)
  {
    ReplHistory replHistory = new(options, App.Terminal);
    return replHistory.ShouldIgnore(command);
  }
}