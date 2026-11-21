// AOT Benchmark: ConsoleAppFramework v5
// Zero Dependency, Zero Reflection, AOT-first design
// The doc-comment aliases give it the argv every other framework takes: --str hello -i 13 -b (task 501).
using ConsoleAppFramework;

ConsoleApp.Run(args, Commands.Root);

internal static class Commands
{
  /// <summary>Benchmark command.</summary>
  /// <param name="i">-i</param>
  /// <param name="b">-b</param>
  public static void Root(string? str, int i, bool b)
  {
  }
}
