#!/usr/bin/env -S dotnet --

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
/// non-JSON log noise before the JSON payload — INCLUDING noise that itself
/// contains a brace (round-1 review finding #1a: anchoring on the first '{'
/// in stdout, rather than on the '{' immediately preceding "Properties",
/// silently mis-locates the payload when a log line has a stray brace before
/// the real JSON) — case-insensitive boolean parsing, and malformed/missing
/// shape all resolving to a sensible (false, null) rather than throwing.
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

  public static async Task Leading_noise_containing_its_own_brace_before_payload_still_parses()
  {
    // Round-1 review finding #1a: a naive "first '{' in stdout" anchor would
    // latch onto the '{123}' brace below and fail to parse the real payload
    // that follows. The anchor must instead find the '{' immediately (modulo
    // whitespace) preceding "Properties".
    const string stdout = "warning XY{123}: something\n{\"Properties\":{\"IsPackable\":\"true\",\"PackageId\":\"A\"}}";

    (bool isPackable, string? packageId) = PackableProjectService.ParseGetPropertyOutput(stdout);

    isPackable.ShouldBeTrue();
    packageId.ShouldBe("A");

    await Task.CompletedTask;
  }

  public static async Task Compact_json_shape_parses()
  {
    // msbuild's real output is pretty-printed (see other tests), but the
    // parser must not assume that shape.
    const string stdout = "{\"Properties\":{\"IsPackable\":\"true\",\"PackageId\":\"TimeWarp.Nuru\"}}";

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

  public static async Task No_properties_marker_returns_not_packable_null_id()
  {
    const string stdout = "MSBuild failed to run for an unrelated reason.";

    (bool isPackable, string? packageId) = PackableProjectService.ParseGetPropertyOutput(stdout);

    isPackable.ShouldBeFalse();
    packageId.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Properties_marker_with_no_preceding_brace_returns_not_packable_null_id()
  {
    // "Properties" text present, but nothing but non-whitespace precedes it —
    // the backward scan must not find a '{' and must fail closed.
    const string stdout = "not json at all \"Properties\": \"whatever\"";

    (bool isPackable, string? packageId) = PackableProjectService.ParseGetPropertyOutput(stdout);

    isPackable.ShouldBeFalse();
    packageId.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Invalid_json_after_opening_brace_returns_not_packable_null_id()
  {
    const string stdout = "{\"Properties\": this is not valid json ";

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

  // --- ValidateDerivedSet (round-1 review finding #1c: two projects
  // deriving the same PackageId would otherwise collapse in a HashSet or
  // silently overwrite one another's .nupkg on push) ---

  public static async Task Unique_package_ids_pass_through_sorted()
  {
    IReadOnlyList<PackableProject> result = PackableProjectService.ValidateDerivedSet
    ([
      new PackableProject("source/b/b.csproj", "B"),
      new PackableProject("source/a/a.csproj", "A")
    ]);

    result.Select(p => p.PackageId).ShouldBe(["A", "B"]);

    await Task.CompletedTask;
  }

  public static async Task Duplicate_package_id_throws_naming_both_paths()
  {
    InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
      PackableProjectService.ValidateDerivedSet
      ([
        new PackableProject("source/a/a.csproj", "Same.Id"),
        new PackableProject("source/b/b.csproj", "Same.Id")
      ]));

    exception.Message.ShouldContain("source/a/a.csproj");
    exception.Message.ShouldContain("source/b/b.csproj");
    exception.Message.ShouldContain("Same.Id");

    await Task.CompletedTask;
  }

  public static async Task Empty_set_returns_empty()
  {
    IReadOnlyList<PackableProject> result = PackableProjectService.ValidateDerivedSet([]);
    result.ShouldBeEmpty();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
