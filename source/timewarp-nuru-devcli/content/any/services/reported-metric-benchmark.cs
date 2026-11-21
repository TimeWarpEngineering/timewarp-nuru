#region Purpose
// A ready-made IDevBenchmark (kanban task 578): build a project or runfile from each
// tree, run it once per sample, and read the sample from a `key=value` line it prints.
// For in-process measurements (nanoseconds per call, allocations) that the program
// computes itself.
#endregion
#region Design
// PrepareAsync runs (through DotNet.Build) `dotnet build <tree>/<path> -c Release -o <tree output>`; a
// file-based app (.cs) and a .csproj both work. MeasureAsync runs
// `dotnet <output>/<assembly>.dll <arguments>` and parses the first stdout line that
// starts with `<MetricKey>=` as an invariant-culture double. A non-zero exit, a missing
// line or a value that does not parse throws.
//
// GeneratePackageOnBuild=false: a referenced packable project would otherwise try to
// pack from its default bin/ path, which -o leaves empty.
//
// Running the built dll directly, not `dotnet run`, keeps the build out of every sample.
#endregion

namespace DevCli;

using System.Globalization;

/// <summary>Builds a program from each tree and reads one <c>key=value</c> metric per run.</summary>
internal sealed class ReportedMetricBenchmark : IDevBenchmark
{
  /// <param name="name">The benchmark name.</param>
  /// <param name="description">What the benchmark measures.</param>
  /// <param name="unit">The metric's unit. Lower is better.</param>
  /// <param name="path">The runfile (.cs) or project to build, relative to a tree root (forward slashes).</param>
  /// <param name="assemblyName">The built assembly name without <c>.dll</c>.</param>
  /// <param name="metricKey">The key of the <c>key=value</c> stdout line that holds the sample.</param>
  /// <param name="arguments">The arguments for every run.</param>
  public ReportedMetricBenchmark
  (
    string name,
    string description,
    string unit,
    string path,
    string assemblyName,
    string metricKey,
    IReadOnlyList<string>? arguments = null
  )
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentException.ThrowIfNullOrWhiteSpace(unit);
    ArgumentException.ThrowIfNullOrWhiteSpace(path);
    ArgumentException.ThrowIfNullOrWhiteSpace(assemblyName);
    ArgumentException.ThrowIfNullOrWhiteSpace(metricKey);
    Name = name;
    Description = description ?? string.Empty;
    Unit = unit;
    ProgramPath = path;
    AssemblyName = assemblyName;
    MetricKey = metricKey;
    Arguments = arguments ?? [];
  }

  public string Name { get; }

  public string Description { get; }

  public string Unit { get; }

  /// <summary>The runfile or project to build, relative to a tree root.</summary>
  public string ProgramPath { get; }

  /// <summary>The built assembly name without <c>.dll</c>.</summary>
  public string AssemblyName { get; }

  /// <summary>The key of the stdout line that holds the sample.</summary>
  public string MetricKey { get; }

  /// <summary>The arguments for every run.</summary>
  public IReadOnlyList<string> Arguments { get; }

  public async ValueTask<IDevBenchmarkSide> PrepareAsync(DevBenchmarkTree tree, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(tree);
    string program = Path.Combine(tree.Root, ProgramPath.Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(program))
    {
      throw new InvalidOperationException($"{Name}: {ProgramPath} does not exist in the {tree.Label} tree ({tree.Root}).");
    }

    CommandOutput build = await DotNet.Build(program)
      .WithConfiguration("Release")
      .WithOutput(tree.OutputDirectory)
      .WithProperty("GeneratePackageOnBuild", "false")
      .WithWorkingDirectory(tree.Root)
      .WithNoValidation()
      .CaptureAsync(cancellationToken)
      .ConfigureAwait(false);

    if (build.ExitCode != 0)
    {
      throw new InvalidOperationException
      (
        $"{Name}: dotnet build failed in the {tree.Label} tree (exit {build.ExitCode}).\n{build.Stdout}\n{build.Stderr}".TrimEnd()
      );
    }

    string assembly = Path.Combine(tree.OutputDirectory, AssemblyName + ".dll");
    if (!File.Exists(assembly))
    {
      throw new InvalidOperationException($"{Name}: build succeeded but {assembly} is missing.");
    }

    return new Side(this, assembly);
  }

  /// <summary>The value of the first <c>key=value</c> line for <paramref name="key"/>, or null.</summary>
  public static double? ParseMetric(string output, string key)
  {
    ArgumentNullException.ThrowIfNull(output);
    ArgumentException.ThrowIfNullOrWhiteSpace(key);
    string prefix = key + "=";
    foreach (string line in output.Split('\n'))
    {
      string trimmed = line.Trim();
      if (trimmed.StartsWith(prefix, StringComparison.Ordinal))
      {
        return double.TryParse(trimmed[prefix.Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
          ? value
          : null;
      }
    }

    return null;
  }

  private sealed class Side(ReportedMetricBenchmark benchmark, string assembly) : IDevBenchmarkSide
  {
    public async ValueTask<double> MeasureAsync(CancellationToken cancellationToken)
    {
      CommandOutput result = await DotNet.Builder()
        .WithArguments([assembly, .. benchmark.Arguments])
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (result.ExitCode != 0)
      {
        throw new InvalidOperationException($"{benchmark.Name}: {assembly} exited {result.ExitCode}. {result.Stderr.Trim()}");
      }

      return ParseMetric(result.Stdout, benchmark.MetricKey)
        ?? throw new InvalidOperationException($"{benchmark.Name}: no '{benchmark.MetricKey}=<number>' line in the output of {assembly}.");
    }
  }
}
