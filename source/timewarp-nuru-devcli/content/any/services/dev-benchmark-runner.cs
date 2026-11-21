#region Purpose
// Runs one benchmark's base and head sides in the same process and compares them
// (kanban task 578). The engine behind `dev benchmark run`.
#endregion
#region Design
// Takes the benchmark and both trees as parameters so tests drive it with a fake
// benchmark; BenchmarkRunCommand builds the trees (BenchmarkBaseCheckout for base, the
// working tree for head).
//
// Both sides are prepared first (base, then head), so a build failure stops the run
// before any sample is taken. Samples are interleaved in rounds: even rounds measure
// base then head, odd rounds head then base. Alternating the order cancels drift
// (thermal throttling, a noisy neighbour starting mid-run) and any first-in-a-pair
// advantage. Warmup rounds run the same way and are discarded.
//
// Progress lines go to the caller's callback (stderr in the command) so stdout stays
// the one JSON result.
#endregion

namespace DevCli;

using System.Globalization;

/// <summary>Sample counts and the noise floor for one <c>dev benchmark run</c>.</summary>
internal sealed record DevBenchmarkRunOptions
(
  int Samples = DevBenchmarkRunOptions.DefaultSamples,
  int Warmup = DevBenchmarkRunOptions.DefaultWarmup,
  double NoiseFloor = BenchmarkComparison.DefaultNoiseFloor
)
{
  /// <summary>Samples kept per side by default.</summary>
  public const int DefaultSamples = 30;

  /// <summary>Discarded warmup samples per side by default.</summary>
  public const int DefaultWarmup = 3;
}

/// <summary>Prepares both sides of a benchmark, takes interleaved samples, and compares them.</summary>
internal static class DevBenchmarkRunner
{
  public static async Task<BenchmarkComparison> RunAsync
  (
    IDevBenchmark benchmark,
    DevBenchmarkTree baseTree,
    DevBenchmarkTree headTree,
    DevBenchmarkRunOptions options,
    Action<string> progress,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(benchmark);
    ArgumentNullException.ThrowIfNull(baseTree);
    ArgumentNullException.ThrowIfNull(headTree);
    ArgumentNullException.ThrowIfNull(options);
    ArgumentNullException.ThrowIfNull(progress);
    if (options.Samples < 1)
    {
      throw new ArgumentException("Take at least one sample per side.", nameof(options));
    }

    if (options.Warmup < 0)
    {
      throw new ArgumentException("Warmup cannot be negative.", nameof(options));
    }

    progress($"Preparing {benchmark.Name} at base ({baseTree.Root})...");
    IDevBenchmarkSide baseSide = await benchmark.PrepareAsync(baseTree, cancellationToken).ConfigureAwait(false);
    progress($"Preparing {benchmark.Name} at head ({headTree.Root})...");
    IDevBenchmarkSide headSide = await benchmark.PrepareAsync(headTree, cancellationToken).ConfigureAwait(false);

    if (options.Warmup > 0)
    {
      progress(string.Create(CultureInfo.InvariantCulture, $"Warmup: {options.Warmup} round(s), discarded"));
      await MeasureRoundsAsync(baseSide, headSide, options.Warmup, cancellationToken).ConfigureAwait(false);
    }

    progress(string.Create(CultureInfo.InvariantCulture, $"Measuring: {options.Samples} interleaved round(s)"));
    (List<double> baseSamples, List<double> headSamples) = await MeasureRoundsAsync(baseSide, headSide, options.Samples, cancellationToken)
      .ConfigureAwait(false);

    return BenchmarkComparison.Compare(baseSamples, headSamples, options.NoiseFloor);
  }

  private static async Task<(List<double> Base, List<double> Head)> MeasureRoundsAsync
  (
    IDevBenchmarkSide baseSide,
    IDevBenchmarkSide headSide,
    int rounds,
    CancellationToken cancellationToken
  )
  {
    List<double> baseSamples = [];
    List<double> headSamples = [];
    for (int round = 0; round < rounds; round++)
    {
      if (round % 2 == 0)
      {
        baseSamples.Add(await baseSide.MeasureAsync(cancellationToken).ConfigureAwait(false));
        headSamples.Add(await headSide.MeasureAsync(cancellationToken).ConfigureAwait(false));
      }
      else
      {
        headSamples.Add(await headSide.MeasureAsync(cancellationToken).ConfigureAwait(false));
        baseSamples.Add(await baseSide.MeasureAsync(cancellationToken).ConfigureAwait(false));
      }
    }

    return (baseSamples, headSamples);
  }
}
