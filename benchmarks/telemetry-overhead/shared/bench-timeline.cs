#region Purpose
// In-process timestamps for the telemetry overhead benchmark (task 520), used to split
// the cost of a run with an endpoint into provider setup, the handler, and the flush tail
// (ForceFlush + the FlushTelemetryAsync grace delay + dispose).
#endregion

#region Design
// Off unless NURU_BENCH_TIMELINE=1, so the hyperfine runs pay one environment read.
// Times are milliseconds since Main started:
//   handler = Main start to handler entry. Generated RunAsync builds the OTLP providers
//             before routing, so on minus off is the provider setup cost.
//   end     = Main start to RunAsync returning. end minus handler is the handler, the
//             output line, and FlushTelemetryAsync (ForceFlush, grace delay, dispose).
// The line goes to stderr so stdout stays the one line the command writes.
#endregion

using System.Diagnostics;
using System.Globalization;

/// <summary>Records and reports benchmark timestamps when NURU_BENCH_TIMELINE=1.</summary>
internal static class BenchTimeline
{
  private static bool Enabled;
  private static long StartTimestamp;
  private static long HandlerTimestamp;
  private static long EndTimestamp;

  /// <summary>Reads the switch and records the Main start time.</summary>
  public static void Start()
  {
    Enabled = Environment.GetEnvironmentVariable("NURU_BENCH_TIMELINE") == "1";
    if (Enabled)
      StartTimestamp = Stopwatch.GetTimestamp();
  }

  /// <summary>Records handler entry.</summary>
  public static void MarkHandler()
  {
    if (Enabled)
      HandlerTimestamp = Stopwatch.GetTimestamp();
  }

  /// <summary>Records the return from RunAsync.</summary>
  public static void MarkEnd()
  {
    if (Enabled)
      EndTimestamp = Stopwatch.GetTimestamp();
  }

  /// <summary>Writes <c>timeline handler=&lt;ms&gt; end=&lt;ms&gt;</c> to stderr.</summary>
  public static void Report()
  {
    if (!Enabled)
      return;

    string handler = Stopwatch.GetElapsedTime(StartTimestamp, HandlerTimestamp).TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture);
    string end = Stopwatch.GetElapsedTime(StartTimestamp, EndTimestamp).TotalMilliseconds.ToString("F3", CultureInfo.InvariantCulture);
    Console.Error.WriteLine($"timeline handler={handler} end={end}");
  }
}
