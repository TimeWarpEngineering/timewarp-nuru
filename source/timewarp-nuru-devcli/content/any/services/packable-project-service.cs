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
// dropping it from the pack/push/check-version set. Result is sorted by PackageId
// (Ordinal) so downstream output (check-version's package list, pack/push order)
// is deterministic.
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

      if (isPackable && !string.IsNullOrWhiteSpace(packageId))
      {
        packableProjects.Add(new PackableProject(projectPath, packageId));
      }
    }

    return [.. packableProjects.OrderBy(p => p.PackageId, StringComparer.Ordinal)];
  }

  /// <summary>
  /// Parses the stdout of <c>dotnet msbuild -getProperty:IsPackable,PackageId</c>
  /// into (IsPackable, PackageId). Pure function — tolerant of leading non-JSON
  /// noise (log banners before the first '{'), case-insensitive boolean parsing,
  /// and malformed/partial JSON, all of which return <c>(false, null)</c> rather
  /// than throw.
  /// </summary>
  public static (bool IsPackable, string? PackageId) ParseGetPropertyOutput(string stdout)
  {
    ArgumentNullException.ThrowIfNull(stdout);

    int braceIndex = stdout.IndexOf('{', StringComparison.Ordinal);
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
