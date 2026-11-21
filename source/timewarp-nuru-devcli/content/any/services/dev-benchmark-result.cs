#region Purpose
// The machine-readable result of `dev benchmark run` (kanban task 578) and its JSON form,
// plus the JSON form of `dev benchmark list --json`.
#endregion
#region Design
// Written with Utf8JsonWriter, not a serializer context: the shape is small and fixed,
// and this stays AOT- and trim-safe in every consuming dev CLI without a
// JsonSerializerContext of its own. Property names are camelCase. Numbers are rounded
// to 4 decimal places. The schema id is "dev-benchmark/v1"; a breaking shape change
// bumps it.
//
// {
//   "schema": "dev-benchmark/v1",
//   "benchmark": "aot-startup",
//   "unit": "ms",
//   "base": { "ref": "origin/master", "commit": "<sha>", "dirty": false,
//             "median": 7.1, "mad": 0.2, "samples": 30 },
//   "head": { "ref": "HEAD", "commit": "<sha>", "dirty": true, ... },
//   "ratio": 0.9718,
//   "noiseBand": 0.0312,
//   "verdict": "noise"
// }
#endregion

namespace DevCli;

using System.Globalization;
using System.Text;
using System.Text.Json;

/// <summary>Which code one side ran: the ref the caller named, its commit, and uncommitted changes.</summary>
internal sealed record DevBenchmarkRevision(string Ref, string Commit, bool Dirty);

/// <summary>The result of one <c>dev benchmark run</c>.</summary>
internal sealed record DevBenchmarkResult
(
  string Benchmark,
  string Unit,
  DevBenchmarkRevision Base,
  DevBenchmarkRevision Head,
  BenchmarkComparison Comparison
)
{
  /// <summary>The schema id written into every result.</summary>
  public const string Schema = "dev-benchmark/v1";

  /// <summary>The result as an indented JSON object.</summary>
  public string ToJson()
  {
    using MemoryStream stream = new();
    using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
    {
      writer.WriteStartObject();
      writer.WriteString("schema", Schema);
      writer.WriteString("benchmark", Benchmark);
      writer.WriteString("unit", Unit);
      WriteSide(writer, "base", Base, Comparison.Base);
      WriteSide(writer, "head", Head, Comparison.Head);
      writer.WriteNumber("ratio", Round(Comparison.Ratio));
      writer.WriteNumber("noiseBand", Round(Comparison.NoiseBand));
      writer.WriteString("verdict", BenchmarkComparison.Format(Comparison.Verdict));
      writer.WriteEndObject();
    }

    return Encoding.UTF8.GetString(stream.ToArray());
  }

  /// <summary>One human line for stderr: the medians, the ratio, the band and the verdict.</summary>
  public string ToSummaryLine()
  {
    string medians = string.Create
    (
      CultureInfo.InvariantCulture,
      $"{Benchmark}: base {Comparison.Base.Median:0.###} {Unit}, head {Comparison.Head.Median:0.###} {Unit}"
    );
    string ratio = string.Create
    (
      CultureInfo.InvariantCulture,
      $"head/base {Comparison.Ratio:0.0000} (noise band ±{Comparison.NoiseBand * 100:0.0}%)"
    );
    return $"{medians}, {ratio} -> {BenchmarkComparison.Format(Comparison.Verdict)}";
  }

  /// <summary><c>dev benchmark list --json</c>: an array of name, description and unit.</summary>
  public static string ListToJson(IReadOnlyList<IDevBenchmark> benchmarks)
  {
    ArgumentNullException.ThrowIfNull(benchmarks);
    using MemoryStream stream = new();
    using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
    {
      writer.WriteStartArray();
      foreach (IDevBenchmark benchmark in benchmarks)
      {
        writer.WriteStartObject();
        writer.WriteString("name", benchmark.Name);
        writer.WriteString("description", benchmark.Description);
        writer.WriteString("unit", benchmark.Unit);
        writer.WriteEndObject();
      }

      writer.WriteEndArray();
    }

    return Encoding.UTF8.GetString(stream.ToArray());
  }

  private static void WriteSide(Utf8JsonWriter writer, string name, DevBenchmarkRevision revision, BenchmarkSideStatistics statistics)
  {
    writer.WriteStartObject(name);
    writer.WriteString("ref", revision.Ref);
    writer.WriteString("commit", revision.Commit);
    writer.WriteBoolean("dirty", revision.Dirty);
    writer.WriteNumber("median", Round(statistics.Median));
    writer.WriteNumber("mad", Round(statistics.Mad));
    writer.WriteNumber("samples", statistics.Samples);
    writer.WriteEndObject();
  }

  private static double Round(double value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
