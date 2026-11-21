#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// In-process cost of Nuru's own startup work (kanban task 501), without process and
// runtime startup. `dev benchmark run startup-first-call` and `startup-warm-call` read it.
#endregion
#region Design
// first-call-us: CreateBuilder, Map, Build and one RunAsync, timed from the first Nuru
// call in a fresh JIT process. It includes JIT of the generated code, which is what a
// non-AOT CLI pays on every launch. The route and argv are the cross-framework ones
// (benchmarks/aot-benchmarks/bench-nuru): `--str hello -i 13 -b`.
// warm-call-ns and warm-call-bytes: the same build and run in a loop after warm-up,
// per call. Allocation is GC.GetTotalAllocatedBytes(precise) so async hops are counted.
// Each line is `key=value` for DevCli's ReportedMetricBenchmark.
#endregion

using System.Diagnostics;
using System.Globalization;
using TimeWarp.Nuru;

const int Warmup = 200;
const int Iterations = 5000;
string[] arguments = ["--str", "hello", "-i", "13", "-b"];

long start = Stopwatch.GetTimestamp();
int firstCode = await StartupBench.BuildAndRunAsync(arguments);
long firstTicks = Stopwatch.GetTimestamp() - start;
if (firstCode != 0)
{
  throw new InvalidOperationException($"First call exited {firstCode}.");
}

for (int index = 0; index < Warmup; index++)
{
  await StartupBench.BuildAndRunAsync(arguments);
}

long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
long loopStart = Stopwatch.GetTimestamp();
for (int index = 0; index < Iterations; index++)
{
  int code = await StartupBench.BuildAndRunAsync(arguments);
  if (code != 0)
  {
    throw new InvalidOperationException($"Warm call exited {code}.");
  }
}

long loopTicks = Stopwatch.GetTimestamp() - loopStart;
long allocated = GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;

double firstMicroseconds = firstTicks * 1_000_000.0 / Stopwatch.Frequency;
double warmNanoseconds = loopTicks * 1_000_000_000.0 / Stopwatch.Frequency / Iterations;
Console.WriteLine($"first-call-us={firstMicroseconds.ToString("F1", CultureInfo.InvariantCulture)}");
Console.WriteLine($"warm-call-ns={warmNanoseconds.ToString("F1", CultureInfo.InvariantCulture)}");
Console.WriteLine($"warm-call-bytes={(allocated / Iterations).ToString(CultureInfo.InvariantCulture)}");
return 0;

internal static class StartupBench
{
  // One call site, so the generator intercepts Build() and RunAsync() once.
  public static Task<int> BuildAndRunAsync(string[] arguments)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .Map("--str {str} -i {i:int} -b")
        .WithHandler((string str, int i) => { })
        .AsQuery()
        .Done()
      .Build();

    return app.RunAsync(arguments);
  }
}
