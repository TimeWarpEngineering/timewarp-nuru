#region Purpose
// Implementation of IPackableProjectService: derives the packable project set by
// asking MSBuild itself (dotnet msbuild -getProperty:IsPackable,PackageId) rather
// than parsing csproj/props XML — IsPackable is a two-level props default flip
// (root Directory.Build.props=false -> source/Directory.Build.props=true ->
// per-csproj override) and PackageId often derives from AssemblyName with no
// explicit override (timewarp-nuru.csproj has neither), so only real MSBuild
// evaluation gives a correct answer (kanban task 458-004, decision D1).
#endregion
#region Design
// Enumerates *.csproj under {repoRoot}/source (recursive), skipping any path with
// an /obj/ or /bin/ segment (generated/staged csproj copies must not be evaluated).
// Per project: `dotnet msbuild <path> -nologo -nodeReuse:false
// -getProperty:IsPackable,PackageId` (no build/restore). One invocation takes
// on the order of 10s, not a fraction of a second. On Roslynk (24 projects)
// that is minutes with no output until the end: 258s for `dev check-version`,
// 255s for the scan alone (median 10.6s/project). The same 24 evaluations
// finish in 11s when run together, bounded by Environment.ProcessorCount,
// with the same per-project stdout. -nodeReuse:false is free at that size
// (11s either way, identical answers) and keeps the processes off one shared
// MSBuild node. Each result is stored at the project's index in the
// enumerated list, then ValidateDerivedSet sorts by PackageId, so finish
// order never changes the derived set or which path a duplicate-PackageId
// error names first. Before the first evaluation, the injected ITerminal
// writes one error line, `Evaluating N projects for IsPackable…`, and only
// when there is at least one project. Stdout stays the command result. A
// test supplies its own ITerminal and reads the line there. The static
// Terminal is not used, so the line is not stuck on the process stderr.
// One evaluation failure is rethrown unchanged. More than one become a
// single InvalidOperationException whose inner exception is an
// AggregateException of the originals.
// A generated multi-project traversal is not used. On Roslynk it failed in
// 10s (MSB4057: the injected target does not exist in the project). Injecting
// a target also depends on Microsoft.Common.targets, so a bare project that
// -getProperty still evaluates (the blank-PackageId guard) would fail or be
// dropped. Task 458-004 D1 forbids that.
// ParseGetPropertyOutput is a pure function over that stdout so the parsing logic
// is unit-testable without invoking MSBuild. A nonzero exit is a hard failure
// (misconfigured/broken csproj) — throw naming the project rather than silently
// dropping it from the pack/push/check-version set.
//
// Fail-loud guards (round-1 review, kanban task 458-004, finding #1; 470 M23):
// a project silently dropped from the derived set would ship an incomplete
// release with a "SUCCEEDED" banner — worse than a hard failure at derivation
// time.
// - JSON anchor: locate `"Properties"` and walk backward to the nearest
//   preceding `{` (only whitespace allowed between them), NOT the first '{' in
//   stdout — a log line containing a stray brace before the real payload
//   (e.g. `warning XY{123}: ...`) must not be mistaken for the JSON start.
//   Handles both msbuild's actual pretty-printed shape (`{\n  "Properties"`)
//   and a hypothetical compact shape (`{"Properties"`).
// - Exit 0 with unparseable/missing Properties JSON throws naming the project
//   (TryParseGetPropertyOutput false) — same posture as a nonzero exit; must
//   not be treated as IsPackable=false (470 M23).
// - IsPackable=true with a null/blank PackageId is a configuration error, not
//   "not packable" — throws naming the project instead of silently excluding it.
// - ValidateDerivedSet throws on a duplicate PackageId across two projects
//   (would otherwise collapse in a HashSet or overwrite a .nupkg on push) and
//   is where the final PackageId-ordinal sort happens, so downstream output
//   (check-version's package list, pack/push order) is deterministic.
#endregion

namespace DevCli;

using System.Runtime.ExceptionServices;
using System.Text.Json;
using TimeWarp.Terminal;

internal sealed class PackableProjectService : IPackableProjectService
{
  private readonly ITerminal Terminal;

  /// <summary>
  /// Creates the service. The progress line goes to <paramref name="terminal"/>
  /// so a test terminal can observe it and stdout stays the command result.
  /// </summary>
  public PackableProjectService(ITerminal terminal)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    Terminal = terminal;
  }

  /// <inheritdoc />
  public async ValueTask<IReadOnlyList<PackableProject>> GetPackableProjectsAsync
  (
    string repoRoot,
    CancellationToken cancellationToken = default
  )
  {
    ArgumentNullException.ThrowIfNull(repoRoot);

    string sourceDir = Path.Combine(repoRoot, "source");
    if (!Directory.Exists(sourceDir))
    {
      return [];
    }

    List<string> projectPaths = [];
    foreach (string projectPath in Directory.EnumerateFiles(sourceDir, "*.csproj", SearchOption.AllDirectories))
    {
      if (!IsBuildOutputPath(projectPath))
      {
        projectPaths.Add(projectPath);
      }
    }

    return await EvaluateProjectsAsync
    (
      projectPaths,
      EvaluateWithMsBuildAsync,
      line => Terminal.WriteErrorLine(line),
      cancellationToken
    ).ConfigureAwait(false);
  }

  /// <summary>
  /// Evaluates <paramref name="projectPaths"/> with bounded parallelism and
  /// returns the packable subset sorted by PackageId. <paramref name="evaluate"/>
  /// is the per-project MSBuild call in production and a stub in tests. One
  /// failure is rethrown with its original message. More than one throw
  /// <see cref="InvalidOperationException"/> naming every failure, with those
  /// exceptions kept in an inner <see cref="AggregateException"/>. Finish order
  /// does not change the list <see cref="ValidateDerivedSet"/> sees.
  /// <paramref name="reportProgress"/> receives one line before evaluation when
  /// there is at least one project. Production passes the injected
  /// <see cref="ITerminal"/> error writer so stdout stays the command result.
  /// </summary>
  internal static async ValueTask<IReadOnlyList<PackableProject>> EvaluateProjectsAsync
  (
    IReadOnlyList<string> projectPaths,
    Func<string, CancellationToken, Task<CommandOutput>> evaluate,
    Action<string> reportProgress,
    CancellationToken cancellationToken,
    int? maxDegreeOfParallelism = null
  )
  {
    ArgumentNullException.ThrowIfNull(projectPaths);
    ArgumentNullException.ThrowIfNull(evaluate);
    ArgumentNullException.ThrowIfNull(reportProgress);
    if (maxDegreeOfParallelism is < 1)
    {
      throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism), maxDegreeOfParallelism, "Parallelism must be at least 1.");
    }

    if (projectPaths.Count == 0)
    {
      return ValidateDerivedSet([]);
    }

    int degree = maxDegreeOfParallelism ?? Math.Max(1, Environment.ProcessorCount);
    string projectsWord = projectPaths.Count == 1 ? "project" : "projects";
    reportProgress($"Evaluating {projectPaths.Count} {projectsWord} for IsPackable…");

    PackableProject?[] slots = new PackableProject?[projectPaths.Count];
    Exception?[] errors = new Exception?[projectPaths.Count];
    ParallelOptions options = new()
    {
      MaxDegreeOfParallelism = degree,
      CancellationToken = cancellationToken
    };

    await Parallel.ForEachAsync
    (
      Enumerable.Range(0, projectPaths.Count),
      options,
      async (index, iterationToken) =>
      {
        try
        {
          string projectPath = projectPaths[index];
          CommandOutput output = await evaluate(projectPath, iterationToken).ConfigureAwait(false);
          slots[index] = ToPackableProject(projectPath, output);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
          // Finish the other projects so one broken csproj does not hide the next.
          // Parallel.ForEachAsync reports a single failure and cancels the rest.
          errors[index] = exception;
        }
      }
    ).ConfigureAwait(false);

    List<Exception> failures = [];
    foreach (Exception? error in errors)
    {
      if (error is not null)
      {
        failures.Add(error);
      }
    }

    if (failures.Count == 1)
    {
      ExceptionDispatchInfo.Capture(failures[0]).Throw();
    }

    if (failures.Count > 1)
    {
      throw new InvalidOperationException
      (
        "Failed to evaluate MSBuild properties for multiple projects: "
        + string.Join("; ", failures.Select(failure => failure.Message)),
        new AggregateException(failures)
      );
    }

    List<PackableProject> packableProjects = [];
    foreach (PackableProject? slot in slots)
    {
      if (slot is not null)
      {
        packableProjects.Add(slot);
      }
    }

    return ValidateDerivedSet(packableProjects);
  }

  private static Task<CommandOutput> EvaluateWithMsBuildAsync(string projectPath, CancellationToken cancellationToken)
  {
    return Shell.Builder("dotnet")
      .WithArguments("msbuild", projectPath, "-nologo", "-nodeReuse:false", "-getProperty:IsPackable,PackageId")
      .WithNoValidation()
      .CaptureAsync(cancellationToken);
  }

  private static PackableProject? ToPackableProject(string projectPath, CommandOutput output)
  {
    if (output.ExitCode != 0)
    {
      throw new InvalidOperationException($"Failed to evaluate MSBuild properties for '{projectPath}' (exit code {output.ExitCode}): {output.Stderr}");
    }

    if (!TryParseGetPropertyOutput(output.Stdout, out bool isPackable, out string? packageId))
    {
      throw new InvalidOperationException($"Failed to parse MSBuild -getProperty output for '{projectPath}' (exit code 0 but Properties JSON was missing or unparseable).");
    }

    if (!isPackable)
    {
      return null;
    }

    if (string.IsNullOrWhiteSpace(packageId))
    {
      throw new InvalidOperationException($"Project '{projectPath}' is IsPackable but evaluates to an empty PackageId — fix the project or mark it IsPackable=false.");
    }

    return new PackableProject(projectPath, packageId);
  }

  /// <summary>
  /// Validates a derived packable-project list — throws on a duplicate
  /// PackageId across two different projects — and returns it sorted by
  /// PackageId (Ordinal). Extracted as a pure static so the duplicate-ID
  /// guard is unit-testable without invoking MSBuild.
  /// </summary>
  public static IReadOnlyList<PackableProject> ValidateDerivedSet(IReadOnlyList<PackableProject> projects)
  {
    ArgumentNullException.ThrowIfNull(projects);

    Dictionary<string, string> firstProjectPathByPackageId = new(StringComparer.Ordinal);

    foreach (PackableProject project in projects)
    {
      if (firstProjectPathByPackageId.TryGetValue(project.PackageId, out string? firstProjectPath))
      {
        throw new InvalidOperationException($"Duplicate PackageId '{project.PackageId}' derived from both '{firstProjectPath}' and '{project.ProjectPath}' — packable projects must have unique PackageIds.");
      }

      firstProjectPathByPackageId[project.PackageId] = project.ProjectPath;
    }

    return [.. projects.OrderBy(p => p.PackageId, StringComparer.Ordinal)];
  }

  /// <summary>
  /// Parses the stdout of <c>dotnet msbuild -getProperty:IsPackable,PackageId</c>
  /// into (IsPackable, PackageId). Pure convenience wrapper over
  /// <see cref="TryParseGetPropertyOutput"/> — returns <c>(false, null)</c>
  /// when the Properties JSON cannot be located or deserialized. Callers that
  /// must fail-loud on unparseable exit-0 output (derivation) should use
  /// <see cref="TryParseGetPropertyOutput"/> and throw when it returns false.
  /// </summary>
  public static (bool IsPackable, string? PackageId) ParseGetPropertyOutput(string stdout)
  {
    return TryParseGetPropertyOutput(stdout, out bool isPackable, out string? packageId)
      ? (isPackable, packageId)
      : (false, null);
  }

  /// <summary>
  /// Tries to parse the stdout of <c>dotnet msbuild -getProperty:IsPackable,PackageId</c>.
  /// Returns <c>false</c> when the Properties JSON cannot be located or
  /// deserialized (missing marker, no preceding brace, JsonException, or null
  /// Properties). Returns <c>true</c> with IsPackable/PackageId when a Properties
  /// object was obtained — including IsPackable=false. Tolerant of leading
  /// non-JSON noise (log banners before the JSON payload, even noise containing
  /// its own brace) and case-insensitive boolean parsing.
  /// </summary>
  public static bool TryParseGetPropertyOutput
  (
    string stdout,
    out bool isPackable,
    out string? packageId
  )
  {
    ArgumentNullException.ThrowIfNull(stdout);

    isPackable = false;
    packageId = null;

    // Anchor on "Properties" and walk backward to the nearest preceding '{'
    // (only whitespace allowed in between) rather than the first '{' in
    // stdout — a log line with a stray brace before the real payload must not
    // be mistaken for the JSON start. Handles both msbuild's actual
    // pretty-printed shape (`{\n  "Properties"`) and a compact shape
    // (`{"Properties"`).
    int propertiesIndex = stdout.IndexOf("\"Properties\"", StringComparison.Ordinal);
    if (propertiesIndex < 0)
    {
      return false;
    }

    int braceIndex = -1;
    for (int i = propertiesIndex - 1; i >= 0; i--)
    {
      char c = stdout[i];
      if (char.IsWhiteSpace(c))
      {
        continue;
      }

      if (c == '{')
      {
        braceIndex = i;
      }

      break;
    }

    if (braceIndex < 0)
    {
      return false;
    }

    string json = stdout[braceIndex..];

    MsBuildEvaluationOutput? parsed;
    try
    {
      parsed = JsonSerializer.Deserialize(json, DevCliJsonContext.Default.MsBuildEvaluationOutput);
    }
    catch (JsonException)
    {
      return false;
    }

    if (parsed?.Properties is null)
    {
      return false;
    }

    isPackable = parsed.Properties.TryGetValue("IsPackable", out string? isPackableRaw)
      && bool.TryParse(isPackableRaw, out bool parsedIsPackable)
      && parsedIsPackable;

    packageId = parsed.Properties.TryGetValue("PackageId", out string? packageIdRaw)
      ? packageIdRaw
      : null;

    return true;
  }

  private static bool IsBuildOutputPath(string path)
  {
    string normalized = path.Replace('\\', '/');
    return normalized.Contains("/obj/", StringComparison.Ordinal) || normalized.Contains("/bin/", StringComparison.Ordinal);
  }
}

/// <summary>
/// Shape of <c>dotnet msbuild -getProperty:...</c> JSON stdout:
/// <c>{"Properties":{"IsPackable":"true","PackageId":"TimeWarp.Nuru"}}</c>.
/// </summary>
internal sealed class MsBuildEvaluationOutput
{
  public Dictionary<string, string>? Properties { get; init; }
}
