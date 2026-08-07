#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Release-gate tag assertion matrix for the dev CLI workflow command.
/// Task 458-003: on a `release` event the triggering tag must equal
/// "v" + &lt;Version&gt; from source/Directory.Build.props, or the release aborts.
/// </summary>
[TestTag("DevCli")]
public class ReleaseTagAssertionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ReleaseTagAssertionTests>();

  public static async Task Exact_match_passes()
  {
    TagAssertionResult result = TagAssertion.Validate("v1.2.3", "1.2.3");
    result.IsValid.ShouldBeTrue();
    result.ExpectedTag.ShouldBe("v1.2.3");
    result.Error.ShouldBeNull();
    await Task.CompletedTask;
  }

  public static async Task Mismatch_fails_with_both_values_in_error()
  {
    TagAssertionResult result = TagAssertion.Validate("v1.2.3", "1.2.4");
    result.IsValid.ShouldBeFalse();
    result.ExpectedTag.ShouldBe("v1.2.4");
    result.Error.ShouldNotBeNull();
    result.Error.ShouldContain("v1.2.3");
    result.Error.ShouldContain("v1.2.4");
    await Task.CompletedTask;
  }

  public static async Task Missing_leading_v_fails()
  {
    TagAssertionResult result = TagAssertion.Validate("1.2.3", "1.2.3");
    result.IsValid.ShouldBeFalse();
    result.ExpectedTag.ShouldBe("v1.2.3");
    await Task.CompletedTask;
  }

  public static async Task Case_sensitive_uppercase_v_fails()
  {
    TagAssertion.Validate("V1.2.3", "1.2.3").IsValid.ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Null_ref_name_fails_with_env_message()
  {
    TagAssertionResult result = TagAssertion.Validate(null, "1.2.3");
    result.IsValid.ShouldBeFalse();
    result.ExpectedTag.ShouldBe("v1.2.3");
    result.Error.ShouldNotBeNull();
    result.Error.ShouldContain("GITHUB_REF_NAME");
    await Task.CompletedTask;
  }

  public static async Task Empty_ref_name_fails_with_env_message()
  {
    TagAssertionResult result = TagAssertion.Validate("", "1.2.3");
    result.IsValid.ShouldBeFalse();
    result.ExpectedTag.ShouldBe("v1.2.3");
    result.Error.ShouldNotBeNull();
    result.Error.ShouldContain("GITHUB_REF_NAME");
    await Task.CompletedTask;
  }

  public static async Task Whitespace_ref_name_fails_with_env_message()
  {
    TagAssertionResult result = TagAssertion.Validate("   ", "1.2.3");
    result.IsValid.ShouldBeFalse();
    result.ExpectedTag.ShouldBe("v1.2.3");
    result.Error.ShouldNotBeNull();
    result.Error.ShouldContain("GITHUB_REF_NAME");
    await Task.CompletedTask;
  }

  public static async Task Null_props_version_fails_with_props_message()
  {
    TagAssertionResult result = TagAssertion.Validate("v1.2.3", null);
    result.IsValid.ShouldBeFalse();
    result.ExpectedTag.ShouldBeNull();
    result.Error.ShouldNotBeNull();
    result.Error.ShouldContain("Directory.Build.props");
    await Task.CompletedTask;
  }

  public static async Task Empty_props_version_fails_with_props_message()
  {
    TagAssertionResult result = TagAssertion.Validate("v1.2.3", "");
    result.IsValid.ShouldBeFalse();
    result.ExpectedTag.ShouldBeNull();
    result.Error.ShouldNotBeNull();
    result.Error.ShouldContain("Directory.Build.props");
    await Task.CompletedTask;
  }

  public static async Task Whitespace_props_version_fails_with_props_message()
  {
    TagAssertionResult result = TagAssertion.Validate("v1.2.3", "   ");
    result.IsValid.ShouldBeFalse();
    result.ExpectedTag.ShouldBeNull();
    result.Error.ShouldNotBeNull();
    result.Error.ShouldContain("Directory.Build.props");
    await Task.CompletedTask;
  }

  public static async Task Branch_name_master_fails()
  {
    TagAssertionResult result = TagAssertion.Validate("master", "1.2.3");
    result.IsValid.ShouldBeFalse();
    result.ExpectedTag.ShouldBe("v1.2.3");
    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
