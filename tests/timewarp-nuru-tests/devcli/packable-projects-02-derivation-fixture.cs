#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Temp-root fixture tests for PackableProjectService.GetPackableProjectsAsync
/// (kanban task 458-004). Each test builds a throwaway repo-shaped directory
/// tree under a unique temp path, shells out to the real `dotnet msbuild` (no
/// build/restore needed for -getProperty evaluation), and cleans up in
/// `finally`. Covers the two-level IsPackable default flip (source/Directory
/// .Build.props sets true, individual csproj can override to false) and the
/// AssemblyName-derived PackageId case (MSBuild's own SDK default, not
/// something PackableProjectService computes itself).
/// </summary>
[TestTag("DevCli")]
public class PackableProjectDerivationFixtureTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PackableProjectDerivationFixtureTests>();

  public static async Task Mixed_fixture_derives_expected_packable_set()
  {
    string root = CreateFixtureRoot();

    try
    {
      string sourceDir = Path.Combine(root, "source");
      Directory.CreateDirectory(sourceDir);

      // Two-level default: source/Directory.Build.props flips IsPackable to
      // true for everything under source/ (mirrors the real repo's
      // source/Directory.Build.props).
      await File.WriteAllTextAsync
      (
        Path.Combine(sourceDir, "Directory.Build.props"),
        """
        <Project>
          <PropertyGroup>
            <IsPackable>true</IsPackable>
          </PropertyGroup>
        </Project>
        """,
        CancellationToken.None
      ).ConfigureAwait(false);

      // fake-a: relies on the inherited IsPackable=true default, explicit
      // PackageId -> included.
      await WriteFixtureProjectAsync
      (
        sourceDir,
        "fake-a",
        """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <PackageId>Fake.A</PackageId>
          </PropertyGroup>
        </Project>
        """
      ).ConfigureAwait(false);

      // fake-b: overrides IsPackable to false -> excluded.
      await WriteFixtureProjectAsync
      (
        sourceDir,
        "fake-b",
        """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <IsPackable>false</IsPackable>
            <PackageId>Fake.B</PackageId>
          </PropertyGroup>
        </Project>
        """
      ).ConfigureAwait(false);

      // fake-c: has an AssemblyName but NO explicit PackageId -> included,
      // with the PackageId MSBuild itself derives from AssemblyName (matches
      // timewarp-nuru.csproj in the real repo, which has neither).
      await WriteFixtureProjectAsync
      (
        sourceDir,
        "fake-c",
        """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <AssemblyName>Fake.C</AssemblyName>
          </PropertyGroup>
        </Project>
        """
      ).ConfigureAwait(false);

      PackableProjectService service = new();
      IReadOnlyList<PackableProject> result = await service
        .GetPackableProjectsAsync(root, CancellationToken.None)
        .ConfigureAwait(false);

      result.Select(p => p.PackageId).ShouldBe(["Fake.A", "Fake.C"]);
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }
  }

  public static async Task Root_without_source_directory_returns_empty()
  {
    string root = CreateFixtureRoot();

    try
    {
      // Deliberately do NOT create {root}/source.
      PackableProjectService service = new();
      IReadOnlyList<PackableProject> result = await service
        .GetPackableProjectsAsync(root, CancellationToken.None)
        .ConfigureAwait(false);

      result.ShouldBeEmpty();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }
  }

  private static string CreateFixtureRoot()
  {
    string root = Path.Combine(Path.GetTempPath(), $"nuru-packable-fixture-{Guid.NewGuid():N}");
    Directory.CreateDirectory(root);
    return root;
  }

  private static async Task WriteFixtureProjectAsync(string sourceDir, string projectName, string csprojContent)
  {
    string projectDir = Path.Combine(sourceDir, projectName);
    Directory.CreateDirectory(projectDir);
    await File.WriteAllTextAsync(Path.Combine(projectDir, $"{projectName}.csproj"), csprojContent, CancellationToken.None).ConfigureAwait(false);
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
