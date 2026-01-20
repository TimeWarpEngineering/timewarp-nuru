#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Tests quoted string handling in the REPL, mirroring PSReadLine capabilities.
// Validates that quotes properly preserve spaces and special characters within arguments.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.QuotedStrings
{

[TestTag("REPL")]
[TestTag("PSReadLine")]
[TestTag("QuotedStrings")]
public class QuotedStringTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<QuotedStringTests>();

  // ============================================================================
  // Double-Quoted Strings
  // ============================================================================

  public static async Task Should_parse_double_quoted_string_with_spaces()
  {
    #region Purpose
    // Double-quoted strings preserve spaces within the argument.
    // This mirrors PSReadLine behavior where "a message" becomes a single argument.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("echo \"a message with spaces\"");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("echo {message}")
        .WithHandler((string message) => $"ECHO:'{message}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - quotes stripped, spaces preserved
    terminal.OutputContains("ECHO:'a message with spaces'").ShouldBeTrue(
      "Double-quoted string should be single argument with spaces preserved");
  }

  public static async Task Should_handle_multiple_double_quoted_args()
  {
    #region Purpose
    // Multiple quoted strings on the command line should each be parsed correctly.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("send \"first message\" \"second message\"");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("send {first} {second}")
        .WithHandler((string first, string second) => $"SEND:first='{first}' second='{second}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("first='first message'").ShouldBeTrue("First quoted arg preserved");
    terminal.OutputContains("second='second message'").ShouldBeTrue("Second quoted arg preserved");
  }

  // ============================================================================
  // Single-Quoted Strings
  // ============================================================================

  public static async Task Should_parse_single_quoted_string_with_spaces()
  {
    #region Purpose
    // Single-quoted strings also preserve spaces (alternative to double quotes).
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("echo 'hello world'");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("echo {message}")
        .WithHandler((string message) => $"ECHO:'{message}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("ECHO:'hello world'").ShouldBeTrue(
      "Single-quoted string should be single argument with spaces preserved");
  }

  // ============================================================================
  // Mixed Quoted and Unquoted Arguments
  // ============================================================================

  public static async Task Should_handle_mixed_quoted_and_unquoted()
  {
    #region Purpose
    // Mix of quoted and unquoted arguments should parse correctly.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("send user \"a message here\" urgent");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("send {recipient} {message} {priority}")
        .WithHandler((string recipient, string message, string priority) =>
          $"SEND:to='{recipient}' msg='{message}' pri='{priority}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("to='user'").ShouldBeTrue("Unquoted arg preserved");
    terminal.OutputContains("msg='a message here'").ShouldBeTrue("Quoted arg preserved");
    terminal.OutputContains("pri='urgent'").ShouldBeTrue("Unquoted arg preserved");
  }

  public static async Task Should_handle_quoted_last_argument()
  {
    #region Purpose
    // Quoted string as the last argument.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("send message \"final argument\"");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("send {action} {arg}")
        .WithHandler((string action, string arg) => $"SEND:action='{action}' arg='{arg}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("action='message'").ShouldBeTrue("First arg preserved");
    terminal.OutputContains("arg='final argument'").ShouldBeTrue("Quoted last arg preserved");
  }

  // ============================================================================
  // Quotes Inside Quotes
  // ============================================================================

  public static async Task Should_handle_single_quote_inside_double_quotes()
  {
    #region Purpose
    // Single quotes inside double-quoted strings should be preserved.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("echo \"it's working\"");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("echo {message}")
        .WithHandler((string message) => $"ECHO:'{message}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("ECHO:'it's working'").ShouldBeTrue(
      "Single quote inside double quotes should be preserved");
  }

  public static async Task Should_handle_double_quote_inside_single_quotes()
  {
    #region Purpose
    // Double quotes inside single-quoted strings should be preserved.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("echo 'say \"hello\"'");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("echo {message}")
        .WithHandler((string message) => $"ECHO:'{message}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("ECHO:'say \"hello\"'").ShouldBeTrue(
      "Double quote inside single quotes should be preserved");
  }

  // ============================================================================
  // Escaped Quotes
  // ============================================================================

  public static async Task Should_handle_escaped_double_quote_inside_double_quotes()
  {
    #region Purpose
    // Escaped quotes (\") inside double-quoted strings should result in a literal quote.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("echo \"Hello \\\"World\\\"\"");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("echo {message}")
        .WithHandler((string message) => $"ECHO:'{message}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - escaped quotes become literal quotes
    terminal.OutputContains("ECHO:'Hello \"World\"'").ShouldBeTrue(
      "Escaped quotes should become literal quotes");
  }

  public static async Task Should_handle_escaped_single_quote_inside_single_quotes()
  {
    #region Purpose
    // Escaped single quotes (\') inside single-quoted strings should result in a literal quote.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("echo 'It\\'s working'");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("echo {message}")
        .WithHandler((string message) => $"ECHO:'{message}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("ECHO:'It's working'").ShouldBeTrue(
      "Escaped single quote should become literal single quote");
  }

  // ============================================================================
  // Empty Quoted Strings
  // ============================================================================

  public static async Task Should_handle_empty_double_quoted_string()
  {
    #region Purpose
    // Empty double-quoted strings should result in empty string argument.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("send \"\"");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("send {message}")
        .WithHandler((string message) => $"SEND:empty={message.Length == 0}")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("SEND:empty=True").ShouldBeTrue(
      "Empty quoted string should result in empty string argument");
  }

  public static async Task Should_handle_empty_single_quoted_string()
  {
    #region Purpose
    // Empty single-quoted strings should result in empty string argument.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("send ''");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("send {message}")
        .WithHandler((string message) => $"SEND:empty={message.Length == 0}")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("SEND:empty=True").ShouldBeTrue(
      "Empty single-quoted string should result in empty string argument");
  }

  // ============================================================================
  // Newlines Inside Quotes (Multiline Quoted Strings)
  // ============================================================================

  public static async Task Should_preserve_newline_inside_double_quoted_string()
  {
    #region Purpose
    // Newlines inside double-quoted strings should be preserved as part of the argument.
    // This is PSReadLine behavior - quotes protect content from line-based splitting.
    #endregion
    // Arrange - type echo, Shift+Enter, then quoted string with embedded newline
    using TestTerminal terminal = new();
    terminal.QueueKeys("echo");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);  // Enter multiline mode
    terminal.QueueKeys("\"hello");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);  // Continue inside quotes
    terminal.QueueKeys("world\"");
    terminal.QueueKey(ConsoleKey.Enter);  // Execute
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("echo {message}")
        .WithHandler((string message) => $"ECHO:msg='{message}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - newline preserved inside quotes
    terminal.OutputContains("ECHO:msg='hello").ShouldBeTrue("First part preserved");
    terminal.OutputContains("world'").ShouldBeTrue("Second part preserved (newline between)");
  }

  public static async Task Should_preserve_newline_inside_single_quoted_string()
  {
    #region Purpose
    // Newlines inside single-quoted strings should also be preserved.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("echo");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("'line1");
    terminal.QueueKey(ConsoleKey.Enter, shift: true);
    terminal.QueueKeys("line2'");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("echo {message}")
        .WithHandler((string message) => $"ECHO:msg='{message}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("ECHO:msg='line1").ShouldBeTrue("First part preserved");
    terminal.OutputContains("line2'").ShouldBeTrue("Second part preserved");
  }

  // ============================================================================
  // Escaped Backslashes
  // ============================================================================

  public static async Task Should_handle_escaped_backslash_in_quoted_string()
  {
    #region Purpose
    // Escaped backslashes (\\) inside quoted strings should result in a single backslash.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("echo \"path\\\\to\\\\file\"");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("echo {path}")
        .WithHandler((string path) => $"ECHO:'{path}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - two backslashes become one in the result
    terminal.OutputContains("ECHO:'path\\to\\file'").ShouldBeTrue(
      "Escaped backslashes should become single backslashes");
  }

  // ============================================================================
  // Tabs and Special Whitespace Inside Quotes
  // ============================================================================

  [Skip("PSReadLine does not support literal tab insertion - Tab always triggers completion")]
  public static async Task Should_preserve_tabs_inside_quoted_string()
  {
    #region Purpose
    // PSReadLine behavior: Tab key ALWAYS triggers completion, even inside quotes.
    // Literal tab insertion requires Ctrl+V followed by Tab (verbatim mode).
    // This test is skipped as literal tab via Tab key is not supported.
    #endregion
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("echo \"hello");
    terminal.QueueKey(ConsoleKey.Tab);  // Triggers completion, not literal tab
    terminal.QueueKeys("world\"");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("echo {message}")
        .WithHandler((string message) => $"ECHO:'{message}'")
        .AsCommand()
        .Done()
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("ECHO:'hello\tworld'").ShouldBeTrue(
      "Tab character inside quotes should be preserved");
  }
}
}

// EOF
