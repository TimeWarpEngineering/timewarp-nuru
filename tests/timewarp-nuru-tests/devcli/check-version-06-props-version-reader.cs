#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// PropsVersionReader.Read coverage (kanban task 470-007, parent-470 finding
/// M10): the reader shared by check-version and release must trim the
/// &lt;Version&gt; element (namespaced or plain) and treat a missing, blank, or
/// absent element/file/directory as "no version" rather than "" or whitespace.
/// </summary>
[TestTag("DevCli")]
public class PropsVersionReaderTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PropsVersionReaderTests>();

  public static async Task Whitespace_padded_version_is_trimmed()
  {
    string root = CreateFixtureRoot();

    try
    {
      WriteProps
      (
        root,
        """
        <Project>
          <PropertyGroup>
            <Version>
              1.2.3-beta.4
            </Version>
          </PropertyGroup>
        </Project>
        """
      );

      PropsVersionReader.Read(root).ShouldBe("1.2.3-beta.4");
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    await Task.CompletedTask;
  }

  public static async Task Namespaced_props_version_is_read()
  {
    string root = CreateFixtureRoot();

    try
    {
      WriteProps
      (
        root,
        """
        <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
          <PropertyGroup>
            <Version> 2.0.0 </Version>
          </PropertyGroup>
        </Project>
        """
      );

      PropsVersionReader.Read(root).ShouldBe("2.0.0");
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    await Task.CompletedTask;
  }

  public static async Task Blank_version_yields_null()
  {
    string root = CreateFixtureRoot();

    try
    {
      WriteProps
      (
        root,
        """
        <Project>
          <PropertyGroup>
            <Version>   </Version>
          </PropertyGroup>
        </Project>
        """
      );

      PropsVersionReader.Read(root).ShouldBeNull();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    await Task.CompletedTask;
  }

  public static async Task Missing_version_element_yields_null()
  {
    string root = CreateFixtureRoot();

    try
    {
      WriteProps
      (
        root,
        """
        <Project>
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
          </PropertyGroup>
        </Project>
        """
      );

      PropsVersionReader.Read(root).ShouldBeNull();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    await Task.CompletedTask;
  }

  public static async Task Missing_props_file_yields_null()
  {
    string root = CreateFixtureRoot();

    try
    {
      Directory.CreateDirectory(Path.Combine(root, "source"));

      PropsVersionReader.Read(root).ShouldBeNull();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    await Task.CompletedTask;
  }

  public static async Task Missing_source_dir_yields_null()
  {
    string root = CreateFixtureRoot();

    try
    {
      // Deliberately do NOT create {root}/source.
      PropsVersionReader.Read(root).ShouldBeNull();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    await Task.CompletedTask;
  }

  public static async Task Null_repo_root_yields_null()
  {
    PropsVersionReader.Read(null).ShouldBeNull();
    await Task.CompletedTask;
  }

  private static string CreateFixtureRoot()
  {
    string root = Path.Combine(Path.GetTempPath(), "nuru-470-007-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(root);
    return root;
  }

  private static void WriteProps(string root, string content)
  {
    string sourceDir = Path.Combine(root, "source");
    Directory.CreateDirectory(sourceDir);
    File.WriteAllText(Path.Combine(sourceDir, "Directory.Build.props"), content);
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
