#region Purpose
// The `benchmark` route group for the shared dev benchmark contract (kanban task 578).
#endregion
#region Design
// The routes live here, in DevCli, so every TimeWarp repo answers `dev benchmark list`
// and `dev benchmark run <name> --base <ref>` the same way. A repo adds benchmarks
// through DevBenchmarks.AddRepoBenchmarks (dev-benchmarks.cs), never a route of its own.
#endregion

namespace DevCli;

using TimeWarp.Nuru;

/// <summary>Route prefix for <c>dev benchmark list</c> and <c>dev benchmark run</c>.</summary>
[NuruRouteGroup("benchmark")]
internal abstract class BenchmarkGroup;
