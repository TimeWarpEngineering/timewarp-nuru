#region Purpose
// Pure statistics for `dev benchmark run` (kanban task 578): the median and MAD of each
// side, the head/base ratio, the noise band, and the win / noise / regression verdict.
#endregion
#region Design
// Medians and the median absolute deviation (MAD), not means and standard deviations:
// one descheduled run on a shared CI runner must not move the answer.
//
//   spread(side) = 1.4826 * MAD / median          robust relative spread (a CV that
//                                                 equals stddev/mean for normal data)
//   ratio        = median(head) / median(base)    below 1 is faster, above 1 slower
//   noiseBand    = max(noiseFloor,
//                      3 * 1.2533 * sqrt(spread(base)^2 / n(base)
//                                      + spread(head)^2 / n(head)))
//
// 1.2533 * spread / sqrt(n) is the relative standard error of a median, so the band is
// three standard errors of the relative difference between the two medians. The floor
// (default 2%) keeps two very quiet sides from calling a 0.5% change a win.
//
//   ratio < 1 - noiseBand  -> win
//   ratio > 1 + noiseBand  -> regression
//   otherwise              -> noise
//
// Both sides are measured in the same run on the same machine, so the ratio cancels
// the machine; no stored absolute number is involved.
#endregion

namespace DevCli;

/// <summary>The verdict of one <c>dev benchmark run</c>.</summary>
internal enum BenchmarkVerdict
{
  /// <summary>Head is faster than base by more than the noise band.</summary>
  Win,

  /// <summary>The difference is inside the noise band.</summary>
  Noise,

  /// <summary>Head is slower than base by more than the noise band.</summary>
  Regression
}

/// <summary>Median, MAD and sample count of one side.</summary>
internal readonly record struct BenchmarkSideStatistics(double Median, double Mad, int Samples)
{
  /// <summary>1.4826 * MAD / median: the robust relative spread.</summary>
  public double RelativeSpread => Median == 0 ? 0 : BenchmarkComparison.MadToSigma * Mad / Median;
}

/// <summary>The comparison of a head side against a base side.</summary>
internal sealed record BenchmarkComparison
(
  BenchmarkSideStatistics Base,
  BenchmarkSideStatistics Head,
  double Ratio,
  double NoiseBand,
  BenchmarkVerdict Verdict
)
{
  /// <summary>The default lower bound of the noise band: 2%.</summary>
  public const double DefaultNoiseFloor = 0.02;

  /// <summary>Scales a MAD to a standard deviation for normal data.</summary>
  public const double MadToSigma = 1.4826;

  /// <summary>The standard error of a median is this times the standard error of a mean.</summary>
  public const double MedianStandardErrorFactor = 1.2533;

  /// <summary>How many standard errors wide the noise band is.</summary>
  public const double StandardErrors = 3.0;

  /// <summary>Compares head samples against base samples.</summary>
  /// <exception cref="ArgumentException">A side has no samples, a sample is negative or not finite, or the floor is out of range.</exception>
  /// <exception cref="InvalidOperationException">The base median is zero, so there is no ratio.</exception>
  public static BenchmarkComparison Compare
  (
    IReadOnlyList<double> baseSamples,
    IReadOnlyList<double> headSamples,
    double noiseFloor = DefaultNoiseFloor
  )
  {
    ArgumentNullException.ThrowIfNull(baseSamples);
    ArgumentNullException.ThrowIfNull(headSamples);
    if (double.IsNaN(noiseFloor) || noiseFloor < 0 || noiseFloor >= 1)
    {
      throw new ArgumentException("The noise floor must be at least 0 and below 1.", nameof(noiseFloor));
    }

    BenchmarkSideStatistics baseStatistics = Summarize(baseSamples, nameof(baseSamples));
    BenchmarkSideStatistics headStatistics = Summarize(headSamples, nameof(headSamples));
    if (baseStatistics.Median == 0)
    {
      throw new InvalidOperationException("The base median is 0, so head/base has no ratio. Measure a cost that is above zero.");
    }

    double ratio = headStatistics.Median / baseStatistics.Median;
    double standardError = MedianStandardErrorFactor * Math.Sqrt
    (
      (Square(baseStatistics.RelativeSpread) / baseStatistics.Samples) +
      (Square(headStatistics.RelativeSpread) / headStatistics.Samples)
    );
    double noiseBand = Math.Max(noiseFloor, StandardErrors * standardError);
    return new BenchmarkComparison(baseStatistics, headStatistics, ratio, noiseBand, Classify(ratio, noiseBand));
  }

  /// <summary>Win below 1 - band, regression above 1 + band, noise otherwise.</summary>
  public static BenchmarkVerdict Classify(double ratio, double noiseBand)
  {
    if (ratio < 1 - noiseBand)
    {
      return BenchmarkVerdict.Win;
    }

    return ratio > 1 + noiseBand ? BenchmarkVerdict.Regression : BenchmarkVerdict.Noise;
  }

  /// <summary>The verdict as the contract spells it: <c>win</c>, <c>noise</c>, or <c>regression</c>.</summary>
  public static string Format(BenchmarkVerdict verdict) => verdict switch
  {
    BenchmarkVerdict.Win => "win",
    BenchmarkVerdict.Noise => "noise",
    BenchmarkVerdict.Regression => "regression",
    _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null)
  };

  /// <summary>The median of <paramref name="values"/>; the mean of the middle two for an even count.</summary>
  public static double Median(IReadOnlyList<double> values)
  {
    ArgumentNullException.ThrowIfNull(values);
    if (values.Count == 0)
    {
      throw new ArgumentException("There are no values.", nameof(values));
    }

    double[] sorted = [.. values];
    Array.Sort(sorted);
    int middle = sorted.Length / 2;
    return sorted.Length % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
  }

  /// <summary>The median absolute deviation from the median.</summary>
  public static double MedianAbsoluteDeviation(IReadOnlyList<double> values)
  {
    double median = Median(values);
    return Median([.. values.Select(value => Math.Abs(value - median))]);
  }

  private static BenchmarkSideStatistics Summarize(IReadOnlyList<double> samples, string parameterName)
  {
    if (samples.Count == 0)
    {
      throw new ArgumentException("A side has no samples.", parameterName);
    }

    if (samples.Any(sample => double.IsNaN(sample) || double.IsInfinity(sample) || sample < 0))
    {
      throw new ArgumentException("A sample is negative or not a finite number.", parameterName);
    }

    return new BenchmarkSideStatistics(Median(samples), MedianAbsoluteDeviation(samples), samples.Count);
  }

  private static double Square(double value) => value * value;
}
