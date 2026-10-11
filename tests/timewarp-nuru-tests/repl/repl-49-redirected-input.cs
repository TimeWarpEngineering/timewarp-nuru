#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Test REPL line-mode fallback when stdin is redirected (kanban 493)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.RedirectedInput
{
  [TestTag("REPL")]
  public class RedirectedInputTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<RedirectedInputTests>();

    public static async Task Should_execute_piped_lines_then_exit_at_eof()
    {
      // Arrange
      using KeyReadFailingTerminal terminal = new("greet x\nstatus\n", inputRedirected: true);

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("greet {name}")
          .WithHandler((string name) => $"Hello, {name}!")
          .AsCommand()
          .Done()
        .Map("status")
          .WithHandler(() => "all-good")
          .AsCommand()
          .Done()
        .AddRepl(options => options.EnableArrowHistory = true)
        .Build();

      // Act
      int exitCode = await app.RunAsync(["--interactive"]);

      // Assert
      terminal.OutputContains("Hello, x!").ShouldBeTrue("First piped command should run");
      terminal.OutputContains("all-good").ShouldBeTrue("Second piped command should run");
      exitCode.ShouldBe(0);
    }

    public static async Task Should_run_exit_command_from_piped_input()
    {
      // Arrange
      using KeyReadFailingTerminal terminal = new("greet y\nexit\n", inputRedirected: true);

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("greet {name}")
          .WithHandler((string name) => $"Hello, {name}!")
          .AsCommand()
          .Done()
        .AddRepl(options => options.EnableArrowHistory = true)
        .Build();

      // Act
      int exitCode = await app.RunAsync(["--interactive"]);

      // Assert
      terminal.OutputContains("Hello, y!").ShouldBeTrue("Piped command should run");
      terminal.OutputContains("Goodbye!").ShouldBeTrue("exit should end the session with the goodbye message");
      exitCode.ShouldBe(0);
    }

    public static async Task Should_not_throw_when_arrow_history_disabled_and_redirected()
    {
      // Arrange
      using KeyReadFailingTerminal terminal = new("greet z\nexit\n", inputRedirected: true);

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("greet {name}")
          .WithHandler((string name) => $"Hello, {name}!")
          .AsCommand()
          .Done()
        .AddRepl(options => options.EnableArrowHistory = false)
        .Build();

      // Act
      int exitCode = await app.RunAsync(["--interactive"]);

      // Assert
      terminal.OutputContains("Hello, z!").ShouldBeTrue("Piped command should run");
      exitCode.ShouldBe(0);
    }
    public static async Task Should_report_one_stderr_line_when_console_input_is_unusable()
    {
      // Arrange - not flagged redirected, but ReadKey throws like a console-less host
      using KeyReadFailingTerminal terminal = new("greet w\n", inputRedirected: false);

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("greet {name}")
          .WithHandler((string name) => $"Hello, {name}!")
          .AsCommand()
          .Done()
        .AddRepl(options => options.EnableArrowHistory = true)
        .Build();

      // Act - must not throw
      int exitCode = await app.RunAsync(["--interactive"]);

      // Assert
      terminal.ErrorContains("interactive input is unavailable")
        .ShouldBeTrue("Unusable console should produce one stderr line instead of an exception");
      terminal.OutputContains("Hello, w!").ShouldBeFalse("No command should run");
      exitCode.ShouldBe(1);
    }

    public static async Task Should_return_one_from_run_repl_async_when_console_input_is_unusable()
    {
      // Arrange
      using KeyReadFailingTerminal terminal = new("greet w\n", inputRedirected: false);

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("greet {name}")
          .WithHandler((string name) => $"Hello, {name}!")
          .AsCommand()
          .Done()
        .AddRepl(options => options.EnableArrowHistory = true)
        .Build();

      // Act
      int exitCode = await app.RunReplAsync();

      // Assert
      exitCode.ShouldBe(1);
    }
  }

  /// <summary>
  /// Wraps TestTerminal but throws from ReadKey, like Console.ReadKey does when stdin is redirected.
  /// </summary>
  internal sealed class KeyReadFailingTerminal : ITerminal, IDisposable
  {
    private readonly TestTerminal Inner;
    private readonly bool InputRedirected;

    public KeyReadFailingTerminal(string input, bool inputRedirected)
    {
      Inner = new TestTerminal(input);
      InputRedirected = inputRedirected;
    }

    public bool OutputContains(string text) => Inner.OutputContains(text);
    public bool ErrorContains(string text) => Inner.ErrorContains(text);
    public void Dispose() => Inner.Dispose();

    public ConsoleKeyInfo ReadKey() => throw new InvalidOperationException("Cannot read keys when console input has been redirected.");
    public ConsoleKeyInfo ReadKey(bool intercept) => ReadKey();
    public bool IsInputRedirected => InputRedirected;

    public int CursorLeft { get => Inner.CursorLeft; set => Inner.CursorLeft = value; }
    public int CursorTop { get => Inner.CursorTop; set => Inner.CursorTop = value; }
    public bool CursorVisible { get => Inner.CursorVisible; set => Inner.CursorVisible = value; }
    public int WindowWidth => Inner.WindowWidth;
    public int WindowHeight => Inner.WindowHeight;
    public int BufferWidth => Inner.BufferWidth;
    public int BufferHeight => Inner.BufferHeight;
    public bool IsInteractive => Inner.IsInteractive;
    public bool SupportsColor => Inner.SupportsColor;
    public bool SupportsHyperlinks => Inner.SupportsHyperlinks;
    public ConsoleColor ForegroundColor { get => Inner.ForegroundColor; set => Inner.ForegroundColor = value; }
    public ConsoleColor BackgroundColor { get => Inner.BackgroundColor; set => Inner.BackgroundColor = value; }
    public bool TreatControlCAsInput { get => Inner.TreatControlCAsInput; set => Inner.TreatControlCAsInput = value; }
    public string Title { get => Inner.Title; set => Inner.Title = value; }
    public bool KeyAvailable => Inner.KeyAvailable;
    public event ConsoleCancelEventHandler? CancelKeyPress { add => Inner.CancelKeyPress += value; remove => Inner.CancelKeyPress -= value; }
    public System.Text.Encoding InputEncoding { get => Inner.InputEncoding; set => Inner.InputEncoding = value; }
    public System.Text.Encoding OutputEncoding { get => Inner.OutputEncoding; set => Inner.OutputEncoding = value; }
    public bool IsOutputRedirected => Inner.IsOutputRedirected;
    public bool IsErrorRedirected => Inner.IsErrorRedirected;
    public TextReader In => Inner.In;
    public TextWriter Out => Inner.Out;
    public TextWriter Error => Inner.Error;

    public ITerminal Write(string message) { Inner.Write(message); return this; }
    public ITerminal WriteLine(string? message = null) { Inner.WriteLine(message); return this; }
    public ITerminal WriteErrorLine(string? message = null) { Inner.WriteErrorLine(message); return this; }
    IConsole IConsole.Write(string message) => Write(message);
    IConsole IConsole.WriteLine(string? message) => WriteLine(message);
    IConsole IConsole.WriteErrorLine(string? message) => WriteErrorLine(message);
    public Task WriteLineAsync(string? message = null) => Inner.WriteLineAsync(message);
    public Task WriteErrorLineAsync(string? message = null) => Inner.WriteErrorLineAsync(message);
    public string? ReadLine() => Inner.ReadLine();
    public int Read() => Inner.Read();
    public Stream OpenStandardInput() => Inner.OpenStandardInput();
    public Stream OpenStandardOutput() => Inner.OpenStandardOutput();
    public Stream OpenStandardError() => Inner.OpenStandardError();
    public void SetIn(TextReader reader) => Inner.SetIn(reader);
    public void SetOut(TextWriter writer) => Inner.SetOut(writer);
    public void SetError(TextWriter writer) => Inner.SetError(writer);
    public void SetCursorPosition(int left, int top) => Inner.SetCursorPosition(left, top);
    public (int Left, int Top) GetCursorPosition() => Inner.GetCursorPosition();
    public void ResetColor() => Inner.ResetColor();
    public void Clear() => Inner.Clear();
    public void Beep() => Inner.Beep();
    public void Beep(int frequency, int duration) => Inner.Beep(frequency, duration);
  }
}
