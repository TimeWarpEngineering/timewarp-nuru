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
// Per project: `dotnet msbuild <path> -nologo -getProperty:IsPackable,PackageId`
// (~0.4s/project, no build/restore needed) emits a single JSON object on stdout;
// ParseGetPropertyOutput is a pure function over that stdout so the parsing logic
// is unit-testable without invoking MSBuild. A nonzero exit is a hard failure
// (misconfigured/broken csproj) — throw naming the project rather than silently
// dropping it from the pack/push/check-version set.
//
// Fail-loud guards (round-1 review, kanban task 458-004, finding #1): a project
// silently dropped from the derived set would ship an incomplete release with a
// "SUCCEEDED" banner — worse than a hard failure at derivation time.
// - JSON anchor: locate `"Properties"` and walk backward to the nearest
//   preceding `{` (only whitespace allowed between them), NOT the first '{' in
//   stdout — a log line containing a stray brace before the real payload
//   (e.g. `warning XY{123}: ...`) must not be mistaken for the JSON start.
//   Handles both msbuild's actual pretty-printed shape (`{\n  "Properties"`)
//   and a hypothetical compact shape (`{"Properties"`).
// - IsPackable=true with a null/blank PackageId is a configuration error, not
//   "not packable" — throws naming the project instead of silently excluding it.
// - ValidateDerivedSet throws on a duplicate PackageId across two projects
//   (would otherwise collapse in a HashSet or overwrite a .nupkg on push) and
//   is where the final PackageId-ordinal sort happens, so downstream output
//   (check-version's package list, pack/push order) is deterministic.
#endregion

namespace DevCli;

using System.Text.Json;

public sealed class PackableProjectService : IPackableProjectService
{
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

    List<PackableProject> packableProjects = [];

    foreach (string projectPath in Directory.EnumerateFiles(sourceDir, "*.csproj", SearchOption.AllDirectories))
    {
      if (IsBuildOutputPath(projectPath))
      {
        continue;
      }

      CommandOutput output = await Shell.Builder("dotnet")
        .WithArguments("msbuild", projectPath, "-nologo", "-getProperty:IsPackable,PackageId")
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (output.ExitCode != 0)
      {
        throw new InvalidOperationException($"Failed to evaluate MSBuild properties for '{projectPath}' (exit code {output.ExitCode}): {output.Stderr}");
      }

      (bool isPackable, string? packageId) = ParseGetPropertyOutput(output.Stdout);

      if (isPackable)
      {
        if (string.IsNullOrWhiteSpace(packageId))
        {
          throw new InvalidOperationException($"Project '{projectPath}' is IsPackable but evaluates to an empty PackageId — fix the project or mark it IsPackable=false.");
        }

        packableProjects.Add(new PackableProject(projectPath, packageId));
      }
    }

    return ValidateDerivedSet(packableProjects);
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
  /// into (IsPackable, PackageId). Pure function — tolerant of leading non-JSON
  /// noise (log banners before the JSON payload, even noise containing its own
  /// brace), case-insensitive boolean parsing, and malformed/partial JSON, all
  /// of which return <c>(false, null)</c> rather than throw.
  /// </summary>
  public static (bool IsPackable, string? PackageId) ParseGetPropertyOutput(string stdout)
  {
    ArgumentNullException.ThrowIfNull(stdout);

    // Anchor on "Properties" and walk backward to the nearest preceding '{'
    // (only whitespace allowed in between) rather than the first '{' in
    // stdout — a log line with a stray brace before the real payload must not
    // be mistaken for the JSON start. Handles both msbuild's actual
    // pretty-printed shape (`{\n  "Properties"`) and a compact shape
    // (`{"Properties"`).
    int propertiesIndex = stdout.IndexOf("\"Properties\"", StringComparison.Ordinal);
    if (propertiesIndex < 0)
    {
      return (false, null);
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
      return (false, null);
    }

    string json = stdout[braceIndex..];

    MsBuildEvaluationOutput? parsed;
    try
    {
      parsed = JsonSerializer.Deserialize(json, DevCliJsonContext.Default.MsBuildEvaluationOutput);
    }
    catch (JsonException)
    {
      return (false, null);
    }

    if (parsed?.Properties is null)
    {
      return (false, null);
    }

    bool isPackable = parsed.Properties.TryGetValue("IsPackable", out string? isPackableRaw)
      && bool.TryParse(isPackableRaw, out bool parsedIsPackable)
      && parsedIsPackable;

    string? packageId = parsed.Properties.TryGetValue("PackageId", out string? packageIdRaw)
      ? packageIdRaw
      : null;

    return (isPackable, packageId);
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
public sealed class MsBuildEvaluationOutput
{
  public Dictionary<string, string>? Properties { get; init; }
}
