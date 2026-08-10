#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using System.IO.Compression;
using global::DevCli;

/// <summary>
/// NupkgLayoutCheck matrix (kanban 461): the package layout gate that keeps
/// hollow nupkgs (missing build-task payload) from shipping.
/// </summary>
[TestTag("DevCli")]
public class NupkgLayoutCheckTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<NupkgLayoutCheckTests>();

  private static string MakeZip(params string[] entries)
  {
    string dir = Directory.CreateTempSubdirectory("nupkg-layout-test-").FullName;
    string path = Path.Combine(dir, "test.nupkg");
    using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create))
    {
      foreach (string e in entries)
      {
        zip.CreateEntry(e);
      }
    }

    return path;
  }

  public static async Task All_required_present_returns_empty()
  {
    string zip = MakeZip("build/TimeWarp.Nuru.targets", "build/net10.0/TimeWarp.Nuru.Build.dll", "lib/net10.0/TimeWarp.Nuru.dll");
    try
    {
      IReadOnlyList<string> missing = NupkgLayoutCheck.FindMissing(zip, ["build/TimeWarp.Nuru.targets", "build/net10.0/TimeWarp.Nuru.Build.dll"]);
      missing.Count.ShouldBe(0);
    }
    finally { Directory.Delete(Path.GetDirectoryName(zip)!, recursive: true); }

    await Task.CompletedTask;
  }

  public static async Task Missing_payload_is_reported_by_name()
  {
    string zip = MakeZip("build/TimeWarp.Nuru.targets", "lib/net10.0/TimeWarp.Nuru.dll");
    try
    {
      IReadOnlyList<string> missing = NupkgLayoutCheck.FindMissing(zip, ["build/TimeWarp.Nuru.targets", "build/net10.0/TimeWarp.Nuru.Build.dll"]);
      missing.Count.ShouldBe(1);
      missing[0].ShouldBe("build/net10.0/TimeWarp.Nuru.Build.dll");
    }
    finally { Directory.Delete(Path.GetDirectoryName(zip)!, recursive: true); }

    await Task.CompletedTask;
  }

  public static async Task Comparison_is_ordinal_case_sensitive()
  {
    string zip = MakeZip("build/net10.0/timewarp.nuru.build.dll");
    try
    {
      IReadOnlyList<string> missing = NupkgLayoutCheck.FindMissing(zip, ["build/net10.0/TimeWarp.Nuru.Build.dll"]);
      missing.Count.ShouldBe(1);
    }
    finally { Directory.Delete(Path.GetDirectoryName(zip)!, recursive: true); }

    await Task.CompletedTask;
  }

  public static async Task Empty_required_list_passes_any_package()
  {
    string zip = MakeZip("anything.txt");
    try
    {
      NupkgLayoutCheck.FindMissing(zip, []).Count.ShouldBe(0);
    }
    finally { Directory.Delete(Path.GetDirectoryName(zip)!, recursive: true); }

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
