#region Purpose
// The registry `dev benchmark list` and `dev benchmark run` read from (kanban task 578).
#endregion
#region Design
// Same shape as DevAuditChecks (dev-audit-checks.cs): a partial method, not DI or
// reflection. DevCli is a source-only package, so this class compiles into the
// consuming dev CLI's own assembly. A repo with benchmarks adds, in its tools/dev-cli:
//
//   internal static partial class DevBenchmarks
//   {
//     static partial void AddRepoBenchmarks(List<IDevBenchmark> benchmarks) =>
//       benchmarks.Add(new MyBenchmark());
//   }
//
// A repo without benchmarks writes nothing and the call compiles away; `dev benchmark
// list` then prints nothing, which means the repo does not take part in a perf sweep.
#endregion

namespace DevCli;

/// <summary>
/// The repo's <see cref="IDevBenchmark"/> list, in the order the repo adds them.
/// </summary>
internal static partial class DevBenchmarks
{
  /// <summary>Returns every benchmark the repo registered in <c>AddRepoBenchmarks</c>.</summary>
  public static IReadOnlyList<IDevBenchmark> All()
  {
    List<IDevBenchmark> benchmarks = [];
    AddRepoBenchmarks(benchmarks);
    return benchmarks;
  }

  /// <summary>Finds a registered benchmark by name (ordinal, case-insensitive), or null.</summary>
  public static IDevBenchmark? Find(IReadOnlyList<IDevBenchmark> benchmarks, string name)
  {
    ArgumentNullException.ThrowIfNull(benchmarks);
    return benchmarks.FirstOrDefault(benchmark => string.Equals(benchmark.Name, name, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>One <c>dev benchmark list</c> line: the name, a tab, the unit in brackets, the description.</summary>
  public static string FormatListLine(IDevBenchmark benchmark)
  {
    ArgumentNullException.ThrowIfNull(benchmark);
    return $"{benchmark.Name}\t[{benchmark.Unit}] {benchmark.Description}";
  }

  /// <summary>Implemented by the repo's own tools/dev-cli to add its benchmarks in list order.</summary>
  static partial void AddRepoBenchmarks(List<IDevBenchmark> benchmarks);
}
