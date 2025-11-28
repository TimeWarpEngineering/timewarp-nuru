#!/usr/bin/dotnet --
#:package TimeWarp.Amuru
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

using System.Diagnostics;
using System.Globalization;
using System.Text;
using TimeWarp.Amuru;
using TimeWarp.Nuru;

NuruAppBuilder builder = new();

builder.MapDefault(() => RunBenchmark(100), "Run AOT invocation benchmark with default 100 runs");

builder.Map(
  "--runs {count:int}",
  (int count) => RunBenchmark(count),
  "Run AOT invocation benchmark with specified number of runs"
);

builder.Map(
  "--help",
  async () =>
  {
    await Console.Out.WriteLineAsync("AOT Command Invocation Benchmark");
    await Console.Out.WriteLineAsync("================================");
    await Console.Out.WriteLineAsync();
    await Console.Out.WriteLineAsync("Measures the cold-start performance of AOT-compiled Nuru applications.");
    await Console.Out.WriteLineAsync("This establishes a baseline for Task 029 (EnableDynamicCompletion) performance.");
    await Console.Out.WriteLineAsync();
    await Console.Out.WriteLineAsync("Usage:");
    await Console.Out.WriteLineAsync("  benchmark-aot-invocation.cs [--runs COUNT]");
    await Console.Out.WriteLineAsync();
    await Console.Out.WriteLineAsync("Options:");
    await Console.Out.WriteLineAsync("  --runs COUNT    Number of invocations to measure (default: 100)");
    await Console.Out.WriteLineAsync();
    await Console.Out.WriteLineAsync("Target: <100ms average (Task 029 requirement for dynamic completion)");
    return 0;
  }
);

NuruApp app = builder.Build();
return await app.RunAsync(args);

async Task<int> RunBenchmark(int runs)
{
  string workingDir = Directory.GetCurrentDirectory();
  string executable = Path.GetFullPath(
    Path.Combine(workingDir, "../../Samples/ShellCompletionExample/publish/ShellCompletionExample")
  );

  if (!File.Exists(executable))
  {
    await Console.Error.WriteLineAsync($"ERROR: Executable not found: {executable}");
    await Console.Error.WriteLineAsync("Build it with:");
    await Console.Error.WriteLineAsync("  cd Samples/ShellCompletionExample");
    await Console.Error.WriteLineAsync("  dotnet publish ShellCompletionExample.cs -c Release -r linux-x64 -p:PublishAot=true -o ./publish");
    return 1;
  }

  WriteLine("AOT Command Invocation Benchmark");
  WriteLine("=================================");
  WriteLine($"Executable: {Path.GetFileName(executable)}");
  WriteLine("Command: status");
  WriteLine($"Runs: {runs}");
  WriteLine($"Size: {new FileInfo(executable).Length / 1024.0:F0} KB");
  WriteLine();

  List<double> timings = [];

  for (int i = 0; i < runs; i++)
  {
    Stopwatch sw = Stopwatch.StartNew();
    CommandOutput result = await Shell.Builder(executable)
      .WithArguments("status")
      .WithNoValidation()
      .CaptureAsync();
    sw.Stop();

    if (result.Success)
    {
      timings.Add(sw.Elapsed.TotalMilliseconds);
    }
    else
    {
      await Console.Error.WriteLineAsync($"ERROR: Run {i + 1} failed with exit code {result.ExitCode}");
      await Console.Error.WriteLineAsync($"Stderr: {result.Stderr}");
      return 1;
    }
  }

  // Calculate statistics
  timings.Sort();
  double min = timings[0];
  double max = timings[^1];
  double avg = timings.Average();
  double median = timings[timings.Count / 2];
  double p95 = timings[(int)(timings.Count * 0.95)];
  double p99 = timings[(int)(timings.Count * 0.99)];
  double sumSquaredDiff = 0;
  foreach (double timing in timings)
  {
    sumSquaredDiff += Math.Pow(timing - avg, 2);
  }

  double stdDev = Math.Sqrt(sumSquaredDiff / timings.Count);

  WriteLine("Results:");
  WriteLine($"  Min:    {min,6:F2} ms");
  WriteLine($"  Avg:    {avg,6:F2} ms");
  WriteLine($"  Median: {median,6:F2} ms");
  WriteLine($"  p95:    {p95,6:F2} ms");
  WriteLine($"  p99:    {p99,6:F2} ms");
  WriteLine($"  Max:    {max,6:F2} ms");
  WriteLine($"  StdDev: {stdDev,6:F2} ms");
  WriteLine();

  const double targetMs = 100.0;
  bool passed = avg < targetMs;

  WriteLine($"Target: <{targetMs}ms (Task 029 requirement)");
  WriteLine($"Status: {(passed ? "✅ PASS" : "❌ FAIL")}");

  if (passed)
  {
    double margin = targetMs - avg;
    WriteLine($"Margin: {margin:F2}ms under target ({margin / targetMs * 100:F1}% headroom)");
  }
  else
  {
    double overrun = avg - targetMs;
    WriteLine($"Overrun: {overrun:F2}ms over target ({overrun / targetMs * 100:F1}% slower)");
  }

  // Save baseline to JSON
  string baselineDir = Path.GetFullPath(Path.Combine(workingDir, "../benchmarks"));
  Directory.CreateDirectory(baselineDir);
  string baselineFile = Path.Combine(baselineDir, "aot-invocation-baseline.json");

  // Build JSON manually to avoid AOT serialization issues
  StringBuilder json = new();
  json.AppendLine("{");
  json.AppendLine(CultureInfo.InvariantCulture, $"  \"baseline_date\": \"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)}\",");
  json.AppendLine(CultureInfo.InvariantCulture, $"  \"commit\": \"{GetGitCommit()}\",");
  json.AppendLine(CultureInfo.InvariantCulture, $"  \"executable\": \"{Path.GetFileName(executable)}\",");
  json.AppendLine("  \"platform\": \"linux-x64\",");
  json.AppendLine("  \"scenarios\": {");
  json.AppendLine("    \"status_command\": {");
  json.AppendLine(CultureInfo.InvariantCulture, $"      \"runs\": {runs},");
  json.AppendLine(CultureInfo.InvariantCulture, $"      \"min_ms\": {Math.Round(min, 2)},");
  json.AppendLine(CultureInfo.InvariantCulture, $"      \"avg_ms\": {Math.Round(avg, 2)},");
  json.AppendLine(CultureInfo.InvariantCulture, $"      \"median_ms\": {Math.Round(median, 2)},");
  json.AppendLine(CultureInfo.InvariantCulture, $"      \"p95_ms\": {Math.Round(p95, 2)},");
  json.AppendLine(CultureInfo.InvariantCulture, $"      \"p99_ms\": {Math.Round(p99, 2)},");
  json.AppendLine(CultureInfo.InvariantCulture, $"      \"max_ms\": {Math.Round(max, 2)},");
  json.AppendLine(CultureInfo.InvariantCulture, $"      \"stddev_ms\": {Math.Round(stdDev, 2)},");
  json.AppendLine(CultureInfo.InvariantCulture, $"      \"target_ms\": {targetMs},");
  json.AppendLine(CultureInfo.InvariantCulture, $"      \"passed\": {passed.ToString().ToLowerInvariant()}");
  json.AppendLine("    }");
  json.AppendLine("  }");
  json.AppendLine("}");

  await File.WriteAllTextAsync(baselineFile, json.ToString());

  WriteLine();
  WriteLine($"Baseline saved to: {baselineFile}");

  return passed ? 0 : 1;
}

string GetGitCommit()
{
  try
  {
    CommandOutput result = Shell.Builder("git")
      .WithArguments("rev-parse", "--short", "HEAD")
      .WithNoValidation()
      .CaptureAsync()
      .GetAwaiter()
      .GetResult();
    return result.Success ? result.Stdout.Trim() : "unknown";
  }
  catch
  {
    return "unknown";
  }
}
