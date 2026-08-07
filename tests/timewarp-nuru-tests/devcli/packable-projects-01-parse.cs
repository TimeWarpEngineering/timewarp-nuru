#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Pure-parse matrix for PackableProjectService.ParseGetPropertyOutput — the
/// stdout of `dotnet msbuild &lt;csproj&gt; -getProperty:IsPackable,PackageId`
/// (kanban task 458-004). Covers the tolerances the parser must have: leading
/// non-JSON log noise before the first '{', case-insensitive boolean parsing,
/// and malformed/missing shape all resolving to a sensible (false, null)
/// rather than throwing.
/// </summary>
[TestTag("DevCli")]
public class PackableProjectParseTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PackableProjectParseTests>();

  public static async Task Packable_true_with_package_id_parses()
  {
    const string stdout = """
    {
      "Properties": {
        "IsPackable": "true",
        "PackageId": "TimeWarp.Nuru"
      }
    }
    """;

    (bool isPackable, string? packageId) = PackableProjectService.ParseGetPropertyOutput(stdout);

    isPackable.ShouldBeTrue();
    packageId.ShouldBe("TimeWarp.Nuru");

    await Task.CompletedTask;
  }

  public static async Task Packable_false_parses()
  {
    const string stdout = """
    {
      "Properties": {
        "IsPackable": "false",
        "PackageId": "TimeWarp.Nuru.Parsing"
      }
    }
    """;

    (bool isPackable, string? packageId) = PackableProjectService.ParseGetPropertyOutput(stdout);

    isPackable.ShouldBeFalse();
    packageId.ShouldBe("TimeWarp.Nuru.Parsing");

    await Task.CompletedTask;
  }

  public static async Task Capitalized_true_casing_parses_as_packable()
  {
    const string stdout = """
    {
      "Properties": {
        "IsPackable": "True",
        "PackageId": "TimeWarp.Nuru"
      }
    }
    """;

    (bool isPackable, _) = PackableProjectService.ParseGetPropertyOutput(stdout);
    isPackable.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Upper_true_casing_parses_as_packable()
  {
    const string stdout = """
    {
      "Properties": {
        "IsPackable": "TRUE",
        "PackageId": "TimeWarp.Nuru"
      }
    }
    """;

    (bool isPackable, _) = PackableProjectService.ParseGetPropertyOutput(stdout);
    isPackable.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Leading_log_noise_before_json_is_tolerated()
  {
    // dotnet msbuild -nologo should suppress this, but tolerate it anyway —
    // a stray warning line landing on stdout must not break parsing.
    const string stdout = """
    MSBuild version 17.12.0 for .NET
    Some warning line unrelated to the JSON payload
    {
      "Properties": {
        "IsPackable": "true",
        "PackageId": "TimeWarp.Nuru"
      }
    }
    """;

    (bool isPackable, string? packageId) = PackableProjectService.ParseGetPropertyOutput(stdout);

    isPackable.ShouldBeTrue();
    packageId.ShouldBe("TimeWarp.Nuru");

    await Task.CompletedTask;
  }

  public static async Task Missing_properties_key_returns_not_packable_null_id()
  {
    const string stdout = "{}";

    (bool isPackable, string? packageId) = PackableProjectService.ParseGetPropertyOutput(stdout);

    isPackable.ShouldBeFalse();
    packageId.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Missing_package_id_key_returns_null_id()
  {
    const string stdout = """
    {
      "Properties": {
        "IsPackable": "true"
      }
    }
    """;

    (bool isPackable, string? packageId) = PackableProjectService.ParseGetPropertyOutput(stdout);

    isPackable.ShouldBeTrue();
    packageId.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Empty_package_id_value_parses_as_empty_string()
  {
    const string stdout = """
    {
      "Properties": {
        "IsPackable": "true",
        "PackageId": ""
      }
    }
    """;

    (bool isPackable, string? packageId) = PackableProjectService.ParseGetPropertyOutput(stdout);

    isPackable.ShouldBeTrue();
    packageId.ShouldBe("");

    await Task.CompletedTask;
  }

  public static async Task Missing_is_packable_key_defaults_to_false()
  {
    const string stdout = """
    {
      "Properties": {
        "PackageId": "TimeWarp.Nuru"
      }
    }
    """;

    (bool isPackable, string? packageId) = PackableProjectService.ParseGetPropertyOutput(stdout);

    isPackable.ShouldBeFalse();
    packageId.ShouldBe("TimeWarp.Nuru");

    await Task.CompletedTask;
  }

  public static async Task Non_boolean_is_packable_value_defaults_to_false()
  {
    const string stdout = """
    {
      "Properties": {
        "IsPackable": "not-a-bool",
        "PackageId": "TimeWarp.Nuru"
      }
    }
    """;

    (bool isPackable, _) = PackableProjectService.ParseGetPropertyOutput(stdout);
    isPackable.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task No_opening_brace_returns_not_packable_null_id()
  {
    const string stdout = "MSBuild failed to run for an unrelated reason.";

    (bool isPackable, string? packageId) = PackableProjectService.ParseGetPropertyOutput(stdout);

    isPackable.ShouldBeFalse();
    packageId.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Invalid_json_after_opening_brace_returns_not_packable_null_id()
  {
    const string stdout = "{ this is not valid json ";

    (bool isPackable, string? packageId) = PackableProjectService.ParseGetPropertyOutput(stdout);

    isPackable.ShouldBeFalse();
    packageId.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Empty_string_returns_not_packable_null_id()
  {
    (bool isPackable, string? packageId) = PackableProjectService.ParseGetPropertyOutput("");

    isPackable.ShouldBeFalse();
    packageId.ShouldBeNull();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
