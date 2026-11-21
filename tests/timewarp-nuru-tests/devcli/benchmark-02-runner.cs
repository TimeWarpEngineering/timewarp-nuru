#!/usr/bin/env -S dotnet --

#region Purpose
// Pins DevBenchmarkRunner and the `dev benchmark` output (kanban task 578): both sides
// prepared before any sample, warmup discarded, base and head interleaved with the order
// alternating per round, the JSON result shape, the list line and JSON, the empty
// registry, and ReportedMetricBenchmark's key=value parsing.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using System.Text.Json;
using global::DevCli;

/// <summary>DevBenchmarkRunner driven with a fake benchmark; the result and list formats.</summary>
[TestTag("DevCli")]
public class DevBenchmarkRunnerTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DevBenchmarkRunnerTests>();

  private static readonly DevBenchmarkTree BaseTree = new("base", "/base-tree", "/out/base");
  private static readonly DevBenchmarkTree HeadTree = new("head", "/head-tree", "/out/head");

  public static async Task Empty_registry_has_no_benchmarks()
  {
    // This test compilation implements no AddRepoBenchmarks, like a repo outside the perf sweep.
    DevBenchmarks.All().Count.ShouldBe(0);
    DevBenchmarkResult.ListToJson(DevBenchmarks.All()).ShouldBe("[]");

    await Task.CompletedTask;
  }

  public static async Task Find_matches_names_case_insensitively()
  {
    FakeBenchmark benchmark = new([1], [1]);

    DevBenchmarks.Find([benchmark], "FAKE").ShouldBeSameAs(benchmark);
    DevBenchmarks.Find([benchmark], "other").ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Prepares_both_sides_before_sampling_and_interleaves_alternating_order()
  {
    FakeBenchmark benchmark = new([10, 10, 10, 10, 10, 10], [9, 9, 9, 9, 9, 9]);
    List<string> progress = [];

    BenchmarkComparison comparison = await DevBenchmarkRunner.RunAsync
    (
      benchmark,
      BaseTree,
      HeadTree,
      new DevBenchmarkRunOptions(Samples: 4, Warmup: 2),
      progress.Add,
      CancellationToken.None
    );

    benchmark.Calls.Take(2).ShouldBe(["prepare base", "prepare head"]);
    // Warmup rounds 0-1, then measured rounds 0-3: even rounds base first, odd rounds head first.
    benchmark.Calls.Skip(2).ShouldBe
    ([
      "base", "head", "head", "base",
      "base", "head", "head", "base", "base", "head", "head", "base"
    ]);
    comparison.Base.Samples.ShouldBe(4);
    comparison.Head.Samples.ShouldBe(4);
    comparison.Verdict.ShouldBe(BenchmarkVerdict.Win);
    progress.ShouldContain(line => line.Contains("Preparing fake at base", StringComparison.Ordinal));
    progress.ShouldContain(line => line.Contains("Measuring: 4 interleaved round(s)", StringComparison.Ordinal));
  }

  public static async Task Warmup_samples_are_discarded()
  {
    // The first two samples of each side are huge; with Warmup 2 they never reach the statistics.
    FakeBenchmark benchmark = new([1000, 1000, 10, 10, 10], [1000, 1000, 10, 10, 10]);

    BenchmarkComparison comparison = await DevBenchmarkRunner.RunAsync
    (
      benchmark, BaseTree, HeadTree, new DevBenchmarkRunOptions(Samples: 3, Warmup: 2), _ => { }, CancellationToken.None
    );

    comparison.Base.Median.ShouldBe(10);
    comparison.Head.Median.ShouldBe(10);
    comparison.Verdict.ShouldBe(BenchmarkVerdict.Noise);
  }

  public static async Task A_prepare_failure_stops_the_run_before_any_sample()
  {
    FakeBenchmark benchmark = new([10], [10]) { FailPrepareFor = "head" };

    await Should.ThrowAsync<InvalidOperationException>
    (
      async () => await DevBenchmarkRunner.RunAsync
      (
        benchmark, BaseTree, HeadTree, new DevBenchmarkRunOptions(Samples: 1, Warmup: 0), _ => { }, CancellationToken.None
      )
    );

    benchmark.Calls.ShouldBe(["prepare base", "prepare head"]);
  }

  public static async Task Rejects_zero_samples_and_negative_warmup()
  {
    FakeBenchmark benchmark = new([10], [10]);

    await Should.ThrowAsync<ArgumentException>
    (
      async () => await DevBenchmarkRunner.RunAsync
      (
        benchmark, BaseTree, HeadTree, new DevBenchmarkRunOptions(Samples: 0), _ => { }, CancellationToken.None
      )
    );
    await Should.ThrowAsync<ArgumentException>
    (
      async () => await DevBenchmarkRunner.RunAsync
      (
        benchmark, BaseTree, HeadTree, new DevBenchmarkRunOptions(Samples: 1, Warmup: -1), _ => { }, CancellationToken.None
      )
    );
  }

  public static async Task Result_json_carries_the_contract_fields()
  {
    BenchmarkComparison comparison = BenchmarkComparison.Compare([10, 10, 10], [11, 11, 11]);
    DevBenchmarkResult result = new
    (
      "aot-startup",
      "ms",
      new DevBenchmarkRevision("origin/master", "aaaa", Dirty: false),
      new DevBenchmarkRevision("HEAD", "bbbb", Dirty: true),
      comparison
    );

    using JsonDocument document = JsonDocument.Parse(result.ToJson());
    JsonElement root = document.RootElement;

    root.GetProperty("schema").GetString().ShouldBe(DevBenchmarkResult.Schema);
    root.GetProperty("benchmark").GetString().ShouldBe("aot-startup");
    root.GetProperty("unit").GetString().ShouldBe("ms");
    root.GetProperty("base").GetProperty("ref").GetString().ShouldBe("origin/master");
    root.GetProperty("base").GetProperty("commit").GetString().ShouldBe("aaaa");
    root.GetProperty("base").GetProperty("dirty").GetBoolean().ShouldBeFalse();
    root.GetProperty("base").GetProperty("median").GetDouble().ShouldBe(10);
    root.GetProperty("base").GetProperty("samples").GetInt32().ShouldBe(3);
    root.GetProperty("head").GetProperty("dirty").GetBoolean().ShouldBeTrue();
    root.GetProperty("head").GetProperty("mad").GetDouble().ShouldBe(0);
    root.GetProperty("ratio").GetDouble().ShouldBe(1.1);
    root.GetProperty("noiseBand").GetDouble().ShouldBe(0.02);
    root.GetProperty("verdict").GetString().ShouldBe("regression");

    result.ToSummaryLine().ShouldBe
    (
      "aot-startup: base 10 ms, head 11 ms, head/base 1.1000 (noise band ±2.0%) -> regression"
    );

    await Task.CompletedTask;
  }

  public static async Task List_prints_name_tab_unit_and_description_and_json()
  {
    FakeBenchmark benchmark = new([1], [1]);

    DevBenchmarks.FormatListLine(benchmark).ShouldBe("fake\t[ms] A fake benchmark");

    using JsonDocument document = JsonDocument.Parse(DevBenchmarkResult.ListToJson([benchmark]));
    JsonElement entry = document.RootElement[0];
    entry.GetProperty("name").GetString().ShouldBe("fake");
    entry.GetProperty("description").GetString().ShouldBe("A fake benchmark");
    entry.GetProperty("unit").GetString().ShouldBe("ms");

    await Task.CompletedTask;
  }

  public static async Task Reported_metric_reads_the_first_matching_key_value_line()
  {
    string output = "warmup=50\nmediator-per-call-ns=5938\r\ndirect-per-call-ns=5193\nmediator-per-call-ns=1\n";

    ReportedMetricBenchmark.ParseMetric(output, "mediator-per-call-ns").ShouldBe(5938);
    ReportedMetricBenchmark.ParseMetric(output, "direct-per-call-ns").ShouldBe(5193);
    ReportedMetricBenchmark.ParseMetric(output, "missing").ShouldBeNull();
    ReportedMetricBenchmark.ParseMetric("x=not-a-number", "x").ShouldBeNull();
    ReportedMetricBenchmark.ParseMetric("x=1.5", "x").ShouldBe(1.5);

    await Task.CompletedTask;
  }
}

/// <summary>Records every prepare and measure call; returns queued samples per side.</summary>
internal sealed class FakeBenchmark(double[] baseSamples, double[] headSamples) : IDevBenchmark
{
  public string Name => "fake";

  public string Description => "A fake benchmark";

  public string Unit => "ms";

  public string? FailPrepareFor { get; init; }

  public List<string> Calls { get; } = [];

  public ValueTask<IDevBenchmarkSide> PrepareAsync(DevBenchmarkTree tree, CancellationToken cancellationToken)
  {
    Calls.Add($"prepare {tree.Label}");
    if (tree.Label == FailPrepareFor)
    {
      throw new InvalidOperationException($"cannot build {tree.Label}");
    }

    Queue<double> samples = new(tree.Label == "base" ? baseSamples : headSamples);
    return ValueTask.FromResult<IDevBenchmarkSide>(new FakeSide(tree.Label, samples, Calls));
  }

  private sealed class FakeSide(string label, Queue<double> samples, List<string> calls) : IDevBenchmarkSide
  {
    public ValueTask<double> MeasureAsync(CancellationToken cancellationToken)
    {
      calls.Add(label);
      return ValueTask.FromResult(samples.Count > 1 ? samples.Dequeue() : samples.Peek());
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
