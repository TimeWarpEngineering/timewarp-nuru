namespace TimeWarp.Nuru.Daemon;

/// <summary>
/// The only place this package touches <see cref="Console"/>: redirecting the process writers.
/// </summary>
/// <remarks>
/// <see cref="Console"/> is banned in favor of <see cref="ITerminal"/> (BannedSymbols.txt). A detached server
/// is the exception: it must not write to inherited streams, so it points them at nothing for its whole life.
/// Calls never swap the console; each call's console writes go to its own terminal (NuruCallContext).
/// </remarks>
#pragma warning disable RS0030 // Console redirection is this type's purpose; see remarks.
internal static class ConsoleRedirect
{
  /// <summary>Points <see cref="Console.Out"/> and <see cref="Console.Error"/> at the given writers.</summary>
  /// <returns>The previous writers, for <see cref="Restore"/>.</returns>
  public static (TextWriter Out, TextWriter Error) Redirect(TextWriter output, TextWriter error)
  {
    (TextWriter Out, TextWriter Error) previous = (Console.Out, Console.Error);
    Console.SetOut(output);
    Console.SetError(error);
    return previous;
  }

  /// <summary>Restores writers returned by <see cref="Redirect"/>.</summary>
  public static void Restore((TextWriter Out, TextWriter Error) previous)
  {
    Console.SetOut(previous.Out);
    Console.SetError(previous.Error);
  }
}
#pragma warning restore RS0030
