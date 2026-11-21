#region Purpose
// Nuru's named benchmarks for the shared `dev benchmark` contract (kanban task 578).
// `dev benchmark list` prints them; `dev benchmark run <name> --base <ref>` compares
// the working tree against the base ref in the same run.
#endregion
#region Design
// Both use DevCli's ready-made benchmarks, so Nuru only names paths:
//
// - aot-startup: publishes benchmarks/aot-benchmarks/bench-nuru with PublishAot and
//   times one cold invocation per sample. It replaces tests/scripts/
//   benchmark-aot-invocation.cs and its stored absolute baseline
//   tests/benchmarks/aot-invocation-baseline.json, both deleted.
// - operation-dispatch: builds benchmarks/operation-dispatch/operation-dispatch-overhead.cs
//   and reads mediator-per-call-ns, the in-process cost of one mediator-dispatched call.
//
// benchmarks/timewarp-nuru-benchmarks (BenchmarkDotNet, Nuru against other CLI
// frameworks) and benchmarks/aot-benchmarks/run-benchmark.sh (hyperfine across
// frameworks) stay manual comparisons; they answer "how does Nuru rank", not "did this
// change make Nuru faster".
#endregion

namespace DevCli;

internal static partial class DevBenchmarks
{
  static partial void AddRepoBenchmarks(List<IDevBenchmark> benchmarks)
  {
    benchmarks.Add
    (
      new ProcessInvocationBenchmark
      (
        name: "aot-startup",
        description: "Cold start of an AOT-published Nuru app (bench-nuru), one process per sample",
        projectPath: "benchmarks/aot-benchmarks/bench-nuru/bench-nuru.csproj",
        executableName: "bench-nuru",
        arguments: ["--str", "hello", "-i", "13", "-b"]
      )
    );
    benchmarks.Add
    (
      new ReportedMetricBenchmark
      (
        name: "operation-dispatch",
        description: "In-process cost of one mediator-dispatched Nuru call (operation-dispatch-overhead.cs)",
        unit: "ns",
        path: "benchmarks/operation-dispatch/operation-dispatch-overhead.cs",
        assemblyName: "operation-dispatch-overhead",
        metricKey: "mediator-per-call-ns"
      )
    );
  }
}
