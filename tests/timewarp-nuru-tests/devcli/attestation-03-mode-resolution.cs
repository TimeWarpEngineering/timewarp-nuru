#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Pure matrix for AttestationConfigResolver.ResolveMode (kanban task
/// 458-010, round-1 review Fix 3): an unrecognized <c>attestation.mode</c>
/// value (a typo like "requiree") must still resolve to Warn — never
/// silently become Require, and never crash the pipeline — but the caller
/// (workflow-command.cs's RunPrAttestationStepAsync) must be told WHICH
/// value was unrecognized so it can print exactly one warning line instead
/// of leaving the operator believing enforcement is on.
/// </summary>
[TestTag("DevCli")]
public class AttestationModeResolutionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<AttestationModeResolutionTests>();

  // --- Absent/blank -> Warn, no warning ---

  public static async Task Null_mode_resolves_to_warn_with_no_unrecognized_value()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode(null);

    result.Mode.ShouldBe(AttestationMode.Warn);
    result.UnrecognizedValue.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Empty_mode_resolves_to_warn_with_no_unrecognized_value()
  {
    AttestationConfigResolver.ResolveMode("").UnrecognizedValue.ShouldBeNull();
    AttestationConfigResolver.ResolveMode("").Mode.ShouldBe(AttestationMode.Warn);

    await Task.CompletedTask;
  }

  public static async Task Whitespace_only_mode_resolves_to_warn_with_no_unrecognized_value()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("   ");

    result.Mode.ShouldBe(AttestationMode.Warn);
    result.UnrecognizedValue.ShouldBeNull();

    await Task.CompletedTask;
  }

  // --- Recognized values (case-insensitive) -> no warning ---

  public static async Task Warn_resolves_to_warn_with_no_unrecognized_value()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("warn");

    result.Mode.ShouldBe(AttestationMode.Warn);
    result.UnrecognizedValue.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Require_resolves_to_require_with_no_unrecognized_value()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("require");

    result.Mode.ShouldBe(AttestationMode.Require);
    result.UnrecognizedValue.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Recognized_values_are_case_insensitive()
  {
    AttestationConfigResolver.ResolveMode("WARN").Mode.ShouldBe(AttestationMode.Warn);
    AttestationConfigResolver.ResolveMode("Warn").Mode.ShouldBe(AttestationMode.Warn);
    AttestationConfigResolver.ResolveMode("REQUIRE").Mode.ShouldBe(AttestationMode.Require);
    AttestationConfigResolver.ResolveMode("Require").Mode.ShouldBe(AttestationMode.Require);

    AttestationConfigResolver.ResolveMode("WARN").UnrecognizedValue.ShouldBeNull();
    AttestationConfigResolver.ResolveMode("REQUIRE").UnrecognizedValue.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Recognized_values_tolerate_surrounding_whitespace()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("  require  ");

    result.Mode.ShouldBe(AttestationMode.Require);
    result.UnrecognizedValue.ShouldBeNull();

    await Task.CompletedTask;
  }

  // --- Unrecognized non-blank values -> Warn, WITH the value surfaced ---

  public static async Task Typo_value_resolves_to_warn_and_surfaces_the_unrecognized_value()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("requiree");

    result.Mode.ShouldBe(AttestationMode.Warn);
    result.UnrecognizedValue.ShouldBe("requiree");

    await Task.CompletedTask;
  }

  public static async Task Unrecognized_value_is_trimmed_before_being_surfaced()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("  bogus  ");

    result.Mode.ShouldBe(AttestationMode.Warn);
    result.UnrecognizedValue.ShouldBe("bogus");

    await Task.CompletedTask;
  }

  public static async Task Completely_unrelated_value_resolves_to_warn_and_surfaces_the_value()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("enforce");

    result.Mode.ShouldBe(AttestationMode.Warn);
    result.UnrecognizedValue.ShouldBe("enforce");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
