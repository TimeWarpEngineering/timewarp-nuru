#region Purpose
// The extension point for `dev benchmark` (kanban task 578): one named benchmark a repo
// offers, and the prepared side (base or head) that takes one sample at a time.
#endregion
#region Design
// A repo implements IDevBenchmark in its own tools/dev-cli and registers it in
// DevBenchmarks.AddRepoBenchmarks (dev-benchmarks.cs). The shared endpoints own the
// `benchmark list` and `benchmark run` routes, so a repo never defines them.
//
// One benchmark definition serves both sides. `dev benchmark run --base <ref>` checks
// the base ref out into a temporary git worktree and calls PrepareAsync twice: once
// with that tree and once with the current working tree. The definition is the head's
// code, so it must build from any tree that has the paths it names; a base that lacks
// them fails PrepareAsync with a message saying so.
//
// A sample is a cost: lower is better (a wall time, nanoseconds per call, bytes).
// DevBenchmarkRunner interleaves base and head samples and compares medians; the
// benchmark never compares anything itself and never reads a stored number.
#endregion

namespace DevCli;

/// <summary>
/// One source tree a benchmark side is built from.
/// </summary>
/// <param name="Label"><c>base</c> or <c>head</c>.</param>
/// <param name="Root">The tree's repository root: the temporary base worktree, or the current working tree.</param>
/// <param name="OutputDirectory">An empty directory, outside <paramref name="Root"/>'s sources, for this side's build output.</param>
internal sealed record DevBenchmarkTree(string Label, string Root, string OutputDirectory);

/// <summary>
/// One named benchmark that <c>dev benchmark</c> lists and runs.
/// </summary>
internal interface IDevBenchmark
{
  /// <summary>The name <c>dev benchmark list</c> prints and <c>dev benchmark run</c> takes. Lower-case kebab-case.</summary>
  string Name { get; }

  /// <summary>One line saying what the benchmark measures.</summary>
  string Description { get; }

  /// <summary>The unit of one sample, for example <c>ms</c> or <c>ns</c>. Lower is better.</summary>
  string Unit { get; }

  /// <summary>
  /// Builds the benchmark from <paramref name="tree"/> and returns the side that takes samples.
  /// Throw <see cref="InvalidOperationException"/> with a full message when the tree cannot be built.
  /// </summary>
  ValueTask<IDevBenchmarkSide> PrepareAsync(DevBenchmarkTree tree, CancellationToken cancellationToken);
}

/// <summary>
/// A built benchmark side. Each call takes one sample.
/// </summary>
internal interface IDevBenchmarkSide
{
  /// <summary>Takes one sample, in the benchmark's <see cref="IDevBenchmark.Unit"/>. Lower is better.</summary>
  ValueTask<double> MeasureAsync(CancellationToken cancellationToken);
}
