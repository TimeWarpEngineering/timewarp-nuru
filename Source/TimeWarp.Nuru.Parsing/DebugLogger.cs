namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Simple debug logger that conditionally outputs based on compilation context.
/// </summary>
internal static class ParserConsole
{
  /// <summary>
  /// Platform-independent newline constant.
  /// </summary>
  public const string NewLine = "\n";

  /// <summary>
  /// Enable diagnostic output by setting environment variable NURU_DEBUG=true
  /// </summary>
  public static readonly bool EnableDiagnostics =
#if ANALYZER_BUILD
      false; // Analyzers can't read environment variables
#else
        Environment.GetEnvironmentVariable("NURU_DEBUG") == "true";
#endif

  /// <summary>
  /// Writes a debug message if diagnostics are enabled.
  /// </summary>
  public static void WriteLine(string message)
  {
#if !ANALYZER_BUILD
        if (EnableDiagnostics)
        {
            Console.WriteLine(message);
        }
#endif
  }
}
