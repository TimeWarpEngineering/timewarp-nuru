#region Purpose
// The ITerminal generated DI injects: it forwards every member to the running call's terminal.
#endregion

#region Design
// Services are built once (singletons) or before a call starts, so the terminal they hold must not be a
// particular call's terminal. This one reads NuruApp.Terminal on every member, which is the current
// NuruCallContext's terminal during a call and the app's own terminal otherwise. Events subscribe on the
// terminal that is current when they are added.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// An <see cref="ITerminal"/> that writes to the running call's terminal (<see cref="NuruCallContext"/>)
/// and to the app's terminal outside a call. Generated code injects it for <see cref="ITerminal"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class NuruCallTerminal : ITerminal
{
  private readonly NuruApp App;

  /// <summary>Creates a terminal that follows <paramref name="app"/>'s current call.</summary>
  /// <param name="app">The app whose terminal it forwards to.</param>
  public NuruCallTerminal(NuruApp app)
  {
    ArgumentNullException.ThrowIfNull(app);
    App = app;
  }

  private ITerminal Target => App.Terminal;

  /// <inheritdoc />
  public Encoding InputEncoding { get => Target.InputEncoding; set => Target.InputEncoding = value; }

  /// <inheritdoc />
  public Encoding OutputEncoding { get => Target.OutputEncoding; set => Target.OutputEncoding = value; }

  /// <inheritdoc />
  public bool IsInputRedirected => Target.IsInputRedirected;

  /// <inheritdoc />
  public bool IsOutputRedirected => Target.IsOutputRedirected;

  /// <inheritdoc />
  public bool IsErrorRedirected => Target.IsErrorRedirected;

  /// <inheritdoc />
  public TextReader In => Target.In;

  /// <inheritdoc />
  public TextWriter Out => Target.Out;

  /// <inheritdoc />
  public TextWriter Error => Target.Error;

  /// <inheritdoc />
  public int CursorLeft { get => Target.CursorLeft; set => Target.CursorLeft = value; }

  /// <inheritdoc />
  public int CursorTop { get => Target.CursorTop; set => Target.CursorTop = value; }

  /// <inheritdoc />
  public bool CursorVisible { get => Target.CursorVisible; set => Target.CursorVisible = value; }

  /// <inheritdoc />
  public int WindowWidth => Target.WindowWidth;

  /// <inheritdoc />
  public int WindowHeight => Target.WindowHeight;

  /// <inheritdoc />
  public int BufferWidth => Target.BufferWidth;

  /// <inheritdoc />
  public int BufferHeight => Target.BufferHeight;

  /// <inheritdoc />
  public bool IsInteractive => Target.IsInteractive;

  /// <inheritdoc />
  public bool SupportsColor => Target.SupportsColor;

  /// <inheritdoc />
  public bool SupportsHyperlinks => Target.SupportsHyperlinks;

  /// <inheritdoc />
  public ConsoleColor ForegroundColor { get => Target.ForegroundColor; set => Target.ForegroundColor = value; }

  /// <inheritdoc />
  public ConsoleColor BackgroundColor { get => Target.BackgroundColor; set => Target.BackgroundColor = value; }

  /// <inheritdoc />
  public bool TreatControlCAsInput { get => Target.TreatControlCAsInput; set => Target.TreatControlCAsInput = value; }

  /// <inheritdoc />
  public string Title { get => Target.Title; set => Target.Title = value; }

  /// <inheritdoc />
  public bool KeyAvailable => Target.KeyAvailable;

  /// <inheritdoc />
  public event ConsoleCancelEventHandler? CancelKeyPress
  {
    add => Target.CancelKeyPress += value;
    remove => Target.CancelKeyPress -= value;
  }

  /// <inheritdoc />
  public ITerminal Write(string message)
  {
    Target.Write(message);
    return this;
  }

  /// <inheritdoc />
  public ITerminal WriteLine(string? message = null)
  {
    Target.WriteLine(message);
    return this;
  }

  /// <inheritdoc />
  public ITerminal WriteErrorLine(string? message = null)
  {
    Target.WriteErrorLine(message);
    return this;
  }

  IConsole IConsole.Write(string message) => Write(message);

  IConsole IConsole.WriteLine(string? message) => WriteLine(message);

  IConsole IConsole.WriteErrorLine(string? message) => WriteErrorLine(message);

  /// <inheritdoc />
  public Task WriteLineAsync(string? message = null) => Target.WriteLineAsync(message);

  /// <inheritdoc />
  public Task WriteErrorLineAsync(string? message = null) => Target.WriteErrorLineAsync(message);

  /// <inheritdoc />
  public string? ReadLine() => Target.ReadLine();

  /// <inheritdoc />
  public int Read() => Target.Read();

  /// <inheritdoc />
  public ConsoleKeyInfo ReadKey() => Target.ReadKey();

  /// <inheritdoc />
  public ConsoleKeyInfo ReadKey(bool intercept) => Target.ReadKey(intercept);

  /// <inheritdoc />
  public Stream OpenStandardInput() => Target.OpenStandardInput();

  /// <inheritdoc />
  public Stream OpenStandardOutput() => Target.OpenStandardOutput();

  /// <inheritdoc />
  public Stream OpenStandardError() => Target.OpenStandardError();

  /// <inheritdoc />
  public void SetIn(TextReader reader) => Target.SetIn(reader);

  /// <inheritdoc />
  public void SetOut(TextWriter writer) => Target.SetOut(writer);

  /// <inheritdoc />
  public void SetError(TextWriter writer) => Target.SetError(writer);

  /// <inheritdoc />
  public void SetCursorPosition(int left, int top) => Target.SetCursorPosition(left, top);

  /// <inheritdoc />
  public (int Left, int Top) GetCursorPosition() => Target.GetCursorPosition();

  /// <inheritdoc />
  public void ResetColor() => Target.ResetColor();

  /// <inheritdoc />
  public void Clear() => Target.Clear();

  /// <inheritdoc />
  public void Beep() => Target.Beep();

  /// <inheritdoc />
  public void Beep(int frequency, int duration) => Target.Beep(frequency, duration);
}
