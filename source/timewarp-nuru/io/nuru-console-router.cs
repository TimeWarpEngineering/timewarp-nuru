#region Purpose
// Sends Console.Out/Error writes made inside a call with CaptureConsole to that call's terminal.
#endregion

#region Design
// Swapping Console.Out per call cannot work with concurrent calls: the writer is process-wide. Instead the
// router is installed once as Console.Out and Console.Error (wrapping whatever writers were there) and picks
// the destination on every write from NuruCallContext.Current: the call's terminal Out/Error when the call
// captures the console, the wrapped writer otherwise. Console.SetOut wraps it in a synchronized writer, so
// writes from concurrent calls do not interleave mid-call. A host that later replaces Console.Out (an MCP
// stdio host restoring its writers) drops the router; the next capturing call installs it again.
#endregion

namespace TimeWarp.Nuru;

#pragma warning disable RS0030 // Routing the process console writers is this type's purpose; see Design.
#pragma warning disable CA2213 // The wrapped writer belongs to the process console, not to the router.
internal sealed class NuruConsoleRouter : TextWriter
{
  private static readonly Lock InstallLock = new();

  private readonly TextWriter Fallback;
  private readonly bool IsError;

  private NuruConsoleRouter(TextWriter fallback, bool isError)
    : base(CultureInfo.InvariantCulture)
  {
    Fallback = fallback;
    IsError = isError;
  }

  public override Encoding Encoding => Fallback.Encoding;

  private TextWriter Target =>
    NuruCallContext.Current is { CaptureConsole: true } context
      ? (IsError ? context.Terminal.Error : context.Terminal.Out)
      : Fallback;

  /// <summary>Installs the router as <see cref="Console.Out"/> and <see cref="Console.Error"/> unless it is already there.</summary>
  public static void EnsureInstalled()
  {
    if (IsRouted(Console.Out) && IsRouted(Console.Error))
      return;

    lock (InstallLock)
    {
      if (!IsRouted(Console.Out))
      {
#pragma warning disable CA2000 // Installed for the life of the process.
        Console.SetOut(new NuruConsoleRouter(Console.Out, isError: false));
        RoutedWriters.AddOrUpdate(Console.Out, Marker);
      }

      if (!IsRouted(Console.Error))
      {
        Console.SetError(new NuruConsoleRouter(Console.Error, isError: true));
#pragma warning restore CA2000
        RoutedWriters.AddOrUpdate(Console.Error, Marker);
      }
    }
  }

  // Console.SetOut wraps the router in a synchronized writer, so the installed writers are remembered.
  private static readonly ConditionalWeakTable<TextWriter, object> RoutedWriters = [];

  private static readonly object Marker = new();

  private static bool IsRouted(TextWriter writer) => writer is NuruConsoleRouter || RoutedWriters.TryGetValue(writer, out _);

  public override void Write(char value) => Target.Write(value);

  public override void Write(string? value) => Target.Write(value);

  public override void Write(char[] buffer, int index, int count) => Target.Write(buffer, index, count);

  public override void Write(ReadOnlySpan<char> buffer) => Target.Write(buffer);

  public override void WriteLine() => Target.WriteLine();

  public override void WriteLine(string? value) => Target.WriteLine(value);

  public override void WriteLine(ReadOnlySpan<char> buffer) => Target.WriteLine(buffer);

  public override Task WriteAsync(char value) => Target.WriteAsync(value);

  public override Task WriteAsync(string? value) => Target.WriteAsync(value);

  public override Task WriteAsync(char[] buffer, int index, int count) => Target.WriteAsync(buffer, index, count);

  public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default) => Target.WriteAsync(buffer, cancellationToken);

  public override Task WriteLineAsync() => Target.WriteLineAsync();

  public override Task WriteLineAsync(string? value) => Target.WriteLineAsync(value);

  public override Task WriteLineAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default) => Target.WriteLineAsync(buffer, cancellationToken);

  public override void Flush() => Target.Flush();

  public override Task FlushAsync() => Target.FlushAsync();
}
#pragma warning restore CA2213
#pragma warning restore RS0030
