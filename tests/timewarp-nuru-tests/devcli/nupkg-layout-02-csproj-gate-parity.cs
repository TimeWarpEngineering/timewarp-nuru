#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using System.Xml.Linq;
using global::DevCli;

/// <summary>
/// Kanban 462: keep NupkgLayoutCheck.NuruRequiredPackageEntries in sync with
/// the explicit Pack="true" None items under build/ and analyzers/ in
/// timewarp-nuru.csproj. Drift either way means the layout gate silently
/// stops covering a packed file (or gates a path the csproj never packs).
/// </summary>
[TestTag("DevCli")]
public class NupkgLayoutCsprojGateParityTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<NupkgLayoutCsprojGateParityTests>();

  public static async Task Csproj_pack_items_match_gate_list_under_build_and_analyzers()
  {
    string csprojPath = Path.Combine(FindRepoRoot(), "source", "timewarp-nuru", "timewarp-nuru.csproj");
    File.Exists(csprojPath).ShouldBeTrue($"Expected csproj at {csprojPath}");

    HashSet<string> fromCsproj = CollectPackedBuildAndAnalyzerEntries(await File.ReadAllTextAsync(csprojPath));
    HashSet<string> fromGate = NupkgLayoutCheck.NuruRequiredPackageEntries
      .Where(IsBuildOrAnalyzerEntry)
      .ToHashSet(StringComparer.Ordinal);

    List<string> onlyInCsproj = [.. fromCsproj.Except(fromGate, StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    List<string> onlyInGate = [.. fromGate.Except(fromCsproj, StringComparer.Ordinal).Order(StringComparer.Ordinal)];

    if (onlyInCsproj.Count > 0 || onlyInGate.Count > 0)
    {
      string message =
        "NuruRequiredPackageEntries drifted from timewarp-nuru.csproj Pack items under build/ and analyzers/."
        + (onlyInCsproj.Count > 0
          ? $" Only in csproj: {string.Join(", ", onlyInCsproj)}."
          : string.Empty)
        + (onlyInGate.Count > 0
          ? $" Only in gate: {string.Join(", ", onlyInGate)}."
          : string.Empty)
        + " Update BOTH (kanban 462).";
      throw new ShouldAssertException(message);
    }

    fromCsproj.Count.ShouldBeGreaterThan(0);
    await Task.CompletedTask;
  }

  public static async Task ResolvePublishedEntry_directory_package_path_appends_file_name()
  {
    NupkgLayoutCheck.ResolvePublishedEntry(
        "../timewarp-nuru-build/bin/$(Configuration)/net10.0/TimeWarp.Nuru.Build.dll",
        "build/net10.0")
      .ShouldBe("build/net10.0/TimeWarp.Nuru.Build.dll");

    NupkgLayoutCheck.ResolvePublishedEntry(
        "../timewarp-nuru-analyzers/bin/$(Configuration)/$(TargetFramework)/TimeWarp.Nuru.Analyzers.dll",
        "analyzers/dotnet/cs")
      .ShouldBe("analyzers/dotnet/cs/TimeWarp.Nuru.Analyzers.dll");

    await Task.CompletedTask;
  }

  public static async Task ResolvePublishedEntry_file_package_path_is_used_as_is()
  {
    NupkgLayoutCheck.ResolvePublishedEntry(
        "../timewarp-nuru-build/build/TimeWarp.Nuru.Build.targets",
        "build/TimeWarp.Nuru.targets")
      .ShouldBe("build/TimeWarp.Nuru.targets");

    await Task.CompletedTask;
  }

  internal static HashSet<string> CollectPackedBuildAndAnalyzerEntries(string csprojXml)
  {
    XDocument doc = XDocument.Parse(csprojXml);
    HashSet<string> entries = new(StringComparer.Ordinal);

    foreach (XElement none in doc.Descendants().Where(e => e.Name.LocalName == "None"))
    {
      string? pack = (string?)none.Attribute("Pack");
      if (!string.Equals(pack, "true", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      string? packagePath = (string?)none.Attribute("PackagePath");
      if (string.IsNullOrWhiteSpace(packagePath))
      {
        continue;
      }

      string normalizedPath = packagePath.Replace('\\', '/').TrimEnd('/');
      if (!IsBuildOrAnalyzerPackagePath(normalizedPath))
      {
        continue;
      }

      string? include = (string?)none.Attribute("Include");
      if (string.IsNullOrWhiteSpace(include))
      {
        continue;
      }

      entries.Add(NupkgLayoutCheck.ResolvePublishedEntry(include, normalizedPath));
    }

    return entries;
  }

  private static bool IsBuildOrAnalyzerPackagePath(string packagePath) =>
    packagePath.Equals("build", StringComparison.Ordinal)
    || packagePath.StartsWith("build/", StringComparison.Ordinal)
    || packagePath.Equals("analyzers", StringComparison.Ordinal)
    || packagePath.StartsWith("analyzers/", StringComparison.Ordinal);

  private static bool IsBuildOrAnalyzerEntry(string entry) =>
    entry.StartsWith("build/", StringComparison.Ordinal)
    || entry.StartsWith("analyzers/", StringComparison.Ordinal);

  private static string FindRepoRoot()
  {
    string? dir = AppContext.BaseDirectory;
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir, "Directory.Build.props"))
          && Directory.Exists(Path.Combine(dir, "source", "timewarp-nuru")))
      {
        return dir;
      }

      dir = Directory.GetParent(dir)?.FullName;
    }

    dir = Directory.GetCurrentDirectory();
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir, "Directory.Build.props"))
          && Directory.Exists(Path.Combine(dir, "source", "timewarp-nuru")))
      {
        return dir;
      }

      dir = Directory.GetParent(dir)?.FullName;
    }

    throw new InvalidOperationException("Could not locate repo root from test process.");
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
