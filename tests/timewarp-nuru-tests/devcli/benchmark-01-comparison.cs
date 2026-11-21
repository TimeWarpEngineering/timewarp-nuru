#!/usr/bin/env -S dotnet --

#region Purpose
// Pins BenchmarkComparison, the statistics behind `dev benchmark run` (kanban task 578):
// median and MAD, the head/base ratio, the noise band (floor and spread-driven), the
// win / noise / regression boundaries, and the refusals.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>BenchmarkComparison: the pure numbers behind the verdict.</summary>
[TestTag("DevCli")]
public class BenchmarkComparisonTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<BenchmarkComparisonTests>();

  public static async Task Median_takes_the_middle_value_or_the_mean_of_the_middle_two()
  {
    BenchmarkComparison.Median([5, 1, 3]).ShouldBe(3);
    BenchmarkComparison.Median([4, 1, 3, 2]).ShouldBe(2.5);
    BenchmarkComparison.Median([7]).ShouldBe(7);

    await Task.CompletedTask;
  }

  public static async Task Mad_is_the_median_of_absolute_deviations_from_the_median()
  {
    // Median 3; deviations 2, 1, 0, 1, 97 -> median 1. One outlier does not move it.
    BenchmarkComparison.MedianAbsoluteDeviation([1, 2, 3, 4, 100]).ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Identical_quiet_sides_are_noise_at_the_floor()
  {
    double[] samples = [10, 10, 10, 10, 10];

    BenchmarkComparison comparison = BenchmarkComparison.Compare(samples, samples);

    comparison.Ratio.ShouldBe(1);
    comparison.NoiseBand.ShouldBe(BenchmarkComparison.DefaultNoiseFloor);
    comparison.Verdict.ShouldBe(BenchmarkVerdict.Noise);
    comparison.Base.Samples.ShouldBe(5);
    comparison.Head.Median.ShouldBe(10);

    await Task.CompletedTask;
  }

  public static async Task Head_faster_beyond_the_band_is_a_win()
  {
    BenchmarkComparison comparison = BenchmarkComparison.Compare([10, 10, 10], [9, 9, 9]);

    comparison.Ratio.ShouldBe(0.9, 1e-12);
    comparison.Verdict.ShouldBe(BenchmarkVerdict.Win);
    BenchmarkComparison.Format(comparison.Verdict).ShouldBe("win");

    await Task.CompletedTask;
  }

  public static async Task Head_slower_beyond_the_band_is_a_regression()
  {
    BenchmarkComparison comparison = BenchmarkComparison.Compare([10, 10, 10], [11, 11, 11]);

    comparison.Ratio.ShouldBe(1.1, 1e-12);
    comparison.Verdict.ShouldBe(BenchmarkVerdict.Regression);
    BenchmarkComparison.Format(comparison.Verdict).ShouldBe("regression");

    await Task.CompletedTask;
  }

  public static async Task Spread_widens_the_band_past_the_floor()
  {
    // Base median 100, MAD 10 -> relative spread 0.14826; head is identical.
    // band = 3 * 1.2533 * sqrt(2 * 0.14826^2 / 5) ~= 0.3527.
    double[] samples = [80, 90, 100, 110, 120];

    BenchmarkComparison comparison = BenchmarkComparison.Compare(samples, samples);

    double spread = BenchmarkComparison.MadToSigma * 10 / 100;
    double expected = BenchmarkComparison.StandardErrors * BenchmarkComparison.MedianStandardErrorFactor *
      Math.Sqrt(2 * spread * spread / 5);
    comparison.NoiseBand.ShouldBe(expected, 1e-12);
    comparison.NoiseBand.ShouldBeGreaterThan(BenchmarkComparison.DefaultNoiseFloor);

    // A 10% slowdown sits inside that wide band, so it is noise, not a regression.
    BenchmarkComparison slower = BenchmarkComparison.Compare(samples, [88, 99, 110, 121, 132]);
    slower.Ratio.ShouldBe(1.1, 1e-12);
    slower.Verdict.ShouldBe(BenchmarkVerdict.Noise);

    await Task.CompletedTask;
  }

  public static async Task Classify_is_noise_on_the_band_edge_and_outside_it_beyond()
  {
    BenchmarkComparison.Classify(0.95, 0.05).ShouldBe(BenchmarkVerdict.Noise);
    BenchmarkComparison.Classify(0.949, 0.05).ShouldBe(BenchmarkVerdict.Win);
    BenchmarkComparison.Classify(1.05, 0.05).ShouldBe(BenchmarkVerdict.Noise);
    BenchmarkComparison.Classify(1.051, 0.05).ShouldBe(BenchmarkVerdict.Regression);
    BenchmarkComparison.Format(BenchmarkVerdict.Noise).ShouldBe("noise");

    await Task.CompletedTask;
  }

  public static async Task Noise_floor_is_configurable()
  {
    BenchmarkComparison.Compare([10, 10, 10], [10.3, 10.3, 10.3], noiseFloor: 0.02).Verdict
      .ShouldBe(BenchmarkVerdict.Regression);
    BenchmarkComparison.Compare([10, 10, 10], [10.3, 10.3, 10.3], noiseFloor: 0.05).Verdict
      .ShouldBe(BenchmarkVerdict.Noise);

    await Task.CompletedTask;
  }

  public static async Task Refuses_empty_negative_and_zero_base_input()
  {
    Should.Throw<ArgumentException>(() => BenchmarkComparison.Compare([], [1]));
    Should.Throw<ArgumentException>(() => BenchmarkComparison.Compare([1], []));
    Should.Throw<ArgumentException>(() => BenchmarkComparison.Compare([1, -1, 1], [1]));
    Should.Throw<ArgumentException>(() => BenchmarkComparison.Compare([1, double.NaN, 1], [1]));
    Should.Throw<ArgumentException>(() => BenchmarkComparison.Compare([1], [1], noiseFloor: 1));
    Should.Throw<InvalidOperationException>(() => BenchmarkComparison.Compare([0, 0, 0], [1]));

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
