#region Purpose
// A ready-made IDevBenchmark (kanban task 578): publish one project from each tree and
// time one invocation of the published executable per sample. Cold-start and
// whole-command benchmarks need nothing more than this.
#endregion
#region Design
// PrepareAsync runs (through DotNet.Publish) `dotnet publish <tree>/<project> -c Release -r <host rid> -o
// <tree output>`, with -p:PublishAot=true when PublishAot is set. The host RID comes
// from RuntimeInformation.RuntimeIdentifier, so a project that pins its own
// RuntimeIdentifier is overridden for the machine the run is on.
// GeneratePackageOnBuild=false: a referenced packable project would otherwise try to
// pack from its default bin/ path, which -o leaves empty.
//
// MeasureAsync times one process start to exit with Stopwatch, in milliseconds. A
// non-zero exit throws: a crashing build is not a fast build.
#endregion

namespace DevCli;

using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>Publishes a project from each tree and times one invocation per sample, in milliseconds.</summary>
internal sealed class ProcessInvocationBenchmark : IDevBenchmark
{
  /// <param name="name">The benchmark name.</param>
  /// <param name="description">What the benchmark measures.</param>
  /// <param name="projectPath">The project to publish, relative to a tree root (forward slashes).</param>
  /// <param name="executableName">The published executable's file name without an extension.</param>
  /// <param name="arguments">The arguments for every invocation.</param>
  /// <param name="publishAot">Publish with <c>PublishAot=true</c>.</param>
  public ProcessInvocationBenchmark
  (
    string name,
    string description,
    string projectPath,
    string executableName,
    IReadOnlyList<string> arguments,
    bool publishAot = true
  )
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    ArgumentException.ThrowIfNullOrWhiteSpace(projectPath);
    ArgumentException.ThrowIfNullOrWhiteSpace(executableName);
    ArgumentNullException.ThrowIfNull(arguments);
    Name = name;
    Description = description ?? string.Empty;
    ProjectPath = projectPath;
    ExecutableName = executableName;
    Arguments = arguments;
    PublishAot = publishAot;
  }

  public string Name { get; }

  public string Description { get; }

  public string Unit => "ms";

  /// <summary>The project to publish, relative to a tree root.</summary>
  public string ProjectPath { get; }

  /// <summary>The published executable's file name without an extension.</summary>
  public string ExecutableName { get; }

  /// <summary>The arguments for every invocation.</summary>
  public IReadOnlyList<string> Arguments { get; }

  /// <summary>Publish with <c>PublishAot=true</c>.</summary>
  public bool PublishAot { get; }

  public async ValueTask<IDevBenchmarkSide> PrepareAsync(DevBenchmarkTree tree, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(tree);
    string project = Path.Combine(tree.Root, ProjectPath.Replace('/', Path.DirectorySeparatorChar));
    if (!File.Exists(project))
    {
      throw new InvalidOperationException($"{Name}: {ProjectPath} does not exist in the {tree.Label} tree ({tree.Root}).");
    }

    DotNetPublishBuilder publishBuilder = DotNet.Publish(project)
      .WithConfiguration("Release")
      .WithRuntime(RuntimeInformation.RuntimeIdentifier)
      .WithOutput(tree.OutputDirectory)
      .WithProperty("GeneratePackageOnBuild", "false")
      .WithWorkingDirectory(tree.Root)
      .WithNoValidation();
    if (PublishAot)
    {
      publishBuilder = publishBuilder.WithProperty("PublishAot", "true");
    }

    CommandOutput publish = await publishBuilder.CaptureAsync(cancellationToken).ConfigureAwait(false);

    if (publish.ExitCode != 0)
    {
      throw new InvalidOperationException
      (
        $"{Name}: dotnet publish failed in the {tree.Label} tree (exit {publish.ExitCode}).\n{publish.Stdout}\n{publish.Stderr}".TrimEnd()
      );
    }

    string executable = Path.Combine(tree.OutputDirectory, ExecutableName + (OperatingSystem.IsWindows() ? ".exe" : string.Empty));
    if (!File.Exists(executable))
    {
      throw new InvalidOperationException($"{Name}: publish succeeded but {executable} is missing.");
    }

    return new Side(executable, Arguments, Name);
  }

  private sealed class Side(string executable, IReadOnlyList<string> arguments, string name) : IDevBenchmarkSide
  {
    public async ValueTask<double> MeasureAsync(CancellationToken cancellationToken)
    {
      long start = Stopwatch.GetTimestamp();
      CommandOutput result = await Shell.Builder(executable)
        .WithArguments([.. arguments])
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);
      TimeSpan elapsed = Stopwatch.GetElapsedTime(start);

      if (result.ExitCode != 0)
      {
        throw new InvalidOperationException($"{name}: {executable} exited {result.ExitCode}. {result.Stderr.Trim()}");
      }

      return elapsed.TotalMilliseconds;
    }
  }
}
