#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Wrap-aware single-line redraw and cursor mapping (Task 454-019 / M17).

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.WrappedLineRedraw
{

[TestTag("REPL")]
public class WrappedLineLayoutTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<WrappedLineLayoutTests>();

  public static async Task Occupied_row_count_is_at_least_one_for_empty_display()
  {
    WrappedLineLayout.OccupiedRowCount(0, 80).ShouldBe(1);
    WrappedLineLayout.OccupiedRowCount(-1, 80).ShouldBe(1);
    await Task.CompletedTask;
  }

  public static async Task Occupied_row_count_uses_ceiling_division()
  {
    WrappedLineLayout.OccupiedRowCount(1, 80).ShouldBe(1);
    WrappedLineLayout.OccupiedRowCount(80, 80).ShouldBe(1);
    WrappedLineLayout.OccupiedRowCount(81, 80).ShouldBe(2);
    WrappedLineLayout.OccupiedRowCount(160, 80).ShouldBe(2);
    WrappedLineLayout.OccupiedRowCount(161, 80).ShouldBe(3);
    await Task.CompletedTask;
  }

  public static async Task Occupied_row_count_treats_non_positive_width_as_one()
  {
    WrappedLineLayout.OccupiedRowCount(5, 0).ShouldBe(5);
    WrappedLineLayout.OccupiedRowCount(5, -3).ShouldBe(5);
    await Task.CompletedTask;
  }

  public static async Task Map_cursor_splits_visual_index_into_column_and_row_offset()
  {
    WrappedLineLayout.MapCursor(0, 20).ShouldBe((0, 0));
    WrappedLineLayout.MapCursor(19, 20).ShouldBe((19, 0));
    WrappedLineLayout.MapCursor(20, 20).ShouldBe((0, 1));
    WrappedLineLayout.MapCursor(27, 20).ShouldBe((7, 1));
    WrappedLineLayout.MapCursor(-4, 20).ShouldBe((0, 0));
    await Task.CompletedTask;
  }

  public static async Task Start_row_subtracts_last_cursor_row_offset()
  {
    WrappedLineLayout.StartRow(currentTop: 5, lastCursorVisualIndex: 2, windowWidth: 80).ShouldBe(5);
    WrappedLineLayout.StartRow(currentTop: 1, lastCursorVisualIndex: 20, windowWidth: 20).ShouldBe(0);
    WrappedLineLayout.StartRow(currentTop: 0, lastCursorVisualIndex: 40, windowWidth: 20).ShouldBe(0);
    await Task.CompletedTask;
  }

  public static async Task Rows_to_clear_uses_the_larger_occupancy()
  {
    WrappedLineLayout.RowsToClear(previousDisplayLength: 80, nextDisplayLength: 40, windowWidth: 80).ShouldBe(1);
    WrappedLineLayout.RowsToClear(previousDisplayLength: 81, nextDisplayLength: 40, windowWidth: 80).ShouldBe(2);
    WrappedLineLayout.RowsToClear(previousDisplayLength: 40, nextDisplayLength: 161, windowWidth: 80).ShouldBe(3);
    await Task.CompletedTask;
  }
}

[TestTag("REPL")]
public class WrappedLineRedrawTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<WrappedLineRedrawTests>();

  private sealed class EmptyRouteProvider : IReplRouteProvider
  {
    public IReadOnlyList<string> GetCommandPrefixes() => [];
    public IEnumerable<CompletionCandidate> GetCompletions(string[] args, bool hasTrailingSpace) => [];
    public bool IsKnownCommand(string token) => false;
  }

  private sealed class PrefixSRouteProvider : IReplRouteProvider
  {
    public IReadOnlyList<string> GetCommandPrefixes() => ["status", "start"];

    public IEnumerable<CompletionCandidate> GetCompletions(string[] args, bool hasTrailingSpace)
    {
      string prefix = args.Length == 0 ? string.Empty : args[^1];
      if (!prefix.StartsWith('s'))
        yield break;

      yield return new CompletionCandidate("status", null, CompletionType.Command);
      yield return new CompletionCandidate("start", null, CompletionType.Command);
    }

    public bool IsKnownCommand(string token) =>
      token.Equals("status", StringComparison.OrdinalIgnoreCase) ||
      token.Equals("start", StringComparison.OrdinalIgnoreCase);
  }

  private static ReplConsoleReader CreateReader(TestTerminal terminal, IReplRouteProvider? routeProvider = null)
  {
    ReplOptions options = new()
    {
      Prompt = "> ",
      EnableColors = false,
      PersistHistory = false
    };

    return new(
      history: [],
      routeProvider: routeProvider ?? new EmptyRouteProvider(),
      replOptions: options,
      loggerFactory: null,
      terminal: terminal);
  }

  public static async Task Cursor_at_end_of_wrapped_line_maps_beyond_the_first_row()
  {
    using TestTerminal terminal = new();
    terminal.WindowWidth = 20;
    terminal.QueueKeys(new string('a', 25));
    terminal.QueueKey(ConsoleKey.Enter);

    ReplConsoleReader reader = CreateReader(terminal);
    string? line = await reader.ReadLineAsync("> ");

    line.ShouldBe(new string('a', 25));
    // prompt 2 + 25 chars = visual 27 on width 20 → column 7, row offset 1
    terminal.CursorLeft.ShouldBe(7);
    terminal.CursorTop.ShouldBe(1);
  }

  public static async Task Cursor_at_exact_window_width_is_positioned_not_skipped()
  {
    using TestTerminal terminal = new();
    terminal.WindowWidth = 20;
    terminal.QueueKeys(new string('a', 18));
    terminal.QueueKey(ConsoleKey.Enter);

    ReplConsoleReader reader = CreateReader(terminal);
    string? line = await reader.ReadLineAsync("> ");

    line.ShouldBe(new string('a', 18));
    // prompt 2 + 18 = visual 20 → column 0 of the next row (old code skipped this)
    terminal.CursorLeft.ShouldBe(0);
    terminal.CursorTop.ShouldBe(1);
  }

  public static async Task Home_after_wrap_returns_cursor_to_prompt_row()
  {
    using TestTerminal terminal = new();
    terminal.WindowWidth = 20;
    terminal.QueueKeys(new string('a', 25));
    terminal.QueueKey(ConsoleKey.Home);
    terminal.QueueKey(ConsoleKey.Enter);

    ReplConsoleReader reader = CreateReader(terminal);
    string? line = await reader.ReadLineAsync("> ");

    line.ShouldBe(new string('a', 25));
    terminal.CursorLeft.ShouldBe(2);
    terminal.CursorTop.ShouldBe(0);
  }

  public static async Task End_after_home_on_wrapped_line_restores_end_coordinates()
  {
    using TestTerminal terminal = new();
    terminal.WindowWidth = 20;
    terminal.QueueKeys(new string('a', 25));
    terminal.QueueKey(ConsoleKey.Home);
    terminal.QueueKey(ConsoleKey.End);
    terminal.QueueKey(ConsoleKey.Enter);

    ReplConsoleReader reader = CreateReader(terminal);
    string? line = await reader.ReadLineAsync("> ");

    line.ShouldBe(new string('a', 25));
    terminal.CursorLeft.ShouldBe(7);
    terminal.CursorTop.ShouldBe(1);
  }

  public static async Task Shrinking_below_window_width_moves_cursor_back_to_the_prompt_row()
  {
    using TestTerminal terminal = new();
    terminal.WindowWidth = 20;
    terminal.QueueKeys(new string('a', 25));
    for (int i = 0; i < 10; i++)
      terminal.QueueKey(ConsoleKey.Backspace);

    terminal.QueueKey(ConsoleKey.Enter);

    ReplConsoleReader reader = CreateReader(terminal);
    string? line = await reader.ReadLineAsync("> ");

    line.ShouldBe(new string('a', 15));
    // prompt 2 + 15 = visual 17 on width 20 → still row 0
    terminal.CursorLeft.ShouldBe(17);
    terminal.CursorTop.ShouldBe(0);
  }

  public static async Task Escape_after_wrap_clears_input_and_parks_cursor_after_the_prompt()
  {
    using TestTerminal terminal = new();
    terminal.WindowWidth = 20;
    terminal.QueueKeys(new string('a', 25));
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueKey(ConsoleKey.Enter);

    ReplConsoleReader reader = CreateReader(terminal);
    string? line = await reader.ReadLineAsync("> ");

    line.ShouldBe(string.Empty);
    terminal.CursorLeft.ShouldBe(2);
    terminal.CursorTop.ShouldBe(0);
  }

  public static async Task Narrow_window_long_input_then_backspace_still_executes_shortened_command()
  {
    using TestTerminal terminal = new();
    terminal.WindowWidth = 20;
    terminal.QueueKeys("echo " + new string('x', 30));
    for (int i = 0; i < 26; i++)
      terminal.QueueKey(ConsoleKey.Backspace);

    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("echo {text}")
        .WithHandler((string text) => $"ECHO:{text}")
        .AsCommand()
        .Done()
      .AddRepl(options =>
      {
        options.EnableColors = false;
        options.PersistHistory = false;
        options.Prompt = "> ";
      })
      .Build();

    await app.RunAsync(["--interactive"]);

    terminal.OutputContains("ECHO:xxxx").ShouldBeTrue("shortened wrapped input must still execute");
  }

  public static async Task Home_after_alt_equals_on_wrapped_line_parks_at_prompt_column()
  {
    using TestTerminal terminal = new();
    terminal.WindowWidth = 20;
    terminal.QueueKeys("s" + new string('a', 24));
    terminal.QueueKey(ConsoleKey.Oem7, alt: true);
    terminal.QueueKey(ConsoleKey.Home);
    terminal.QueueKey(ConsoleKey.Enter);

    ReplConsoleReader reader = CreateReader(terminal, new PrefixSRouteProvider());
    string? line = await reader.ReadLineAsync("> ");

    line.ShouldBe("s" + new string('a', 24));
    terminal.OutputContains("Available completions").ShouldBeTrue();
    terminal.CursorLeft.ShouldBe(2);
  }

  public static async Task Home_after_alt_equals_on_non_wrapped_line_parks_at_prompt_column()
  {
    using TestTerminal terminal = new();
    terminal.WindowWidth = 80;
    terminal.QueueKeys("s");
    terminal.QueueKey(ConsoleKey.Oem7, alt: true);
    terminal.QueueKey(ConsoleKey.Home);
    terminal.QueueKey(ConsoleKey.Enter);

    ReplConsoleReader reader = CreateReader(terminal, new PrefixSRouteProvider());
    string? line = await reader.ReadLineAsync("> ");

    line.ShouldBe("s");
    terminal.OutputContains("Available completions").ShouldBeTrue();
    terminal.CursorLeft.ShouldBe(2);
  }
}

}
