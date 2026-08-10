#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Pure matrix for AttestationConfigResolver (kanban task 458-011): ResolveMode
/// returns nullable Mode + optional UnrecognizedValue; EffectiveMode applies
/// context-sensitive defaults (PR → Warn, release → Require). Blank is not
/// explicit warn; typos never become Off.
/// </summary>
[TestTag("DevCli")]
public class AttestationModeResolutionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<AttestationModeResolutionTests>();

  // --- Absent/blank -> Mode null, no unrecognized (caller applies default) ---

  public static async Task Null_mode_resolves_to_null_mode_with_no_unrecognized_value()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode(null);

    result.Mode.ShouldBeNull();
    result.UnrecognizedValue.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Empty_mode_resolves_to_null_mode_with_no_unrecognized_value()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("");

    result.Mode.ShouldBeNull();
    result.UnrecognizedValue.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Whitespace_only_mode_resolves_to_null_mode_with_no_unrecognized_value()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("   ");

    result.Mode.ShouldBeNull();
    result.UnrecognizedValue.ShouldBeNull();

    await Task.CompletedTask;
  }

  // --- Recognized values (case-insensitive) -> no unrecognized ---

  public static async Task Off_resolves_to_off_with_no_unrecognized_value()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("off");

    result.Mode.ShouldBe(AttestationMode.Off);
    result.UnrecognizedValue.ShouldBeNull();

    await Task.CompletedTask;
  }

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
    AttestationConfigResolver.ResolveMode("OFF").Mode.ShouldBe(AttestationMode.Off);
    AttestationConfigResolver.ResolveMode("Off").Mode.ShouldBe(AttestationMode.Off);
    AttestationConfigResolver.ResolveMode("WARN").Mode.ShouldBe(AttestationMode.Warn);
    AttestationConfigResolver.ResolveMode("Warn").Mode.ShouldBe(AttestationMode.Warn);
    AttestationConfigResolver.ResolveMode("REQUIRE").Mode.ShouldBe(AttestationMode.Require);
    AttestationConfigResolver.ResolveMode("Require").Mode.ShouldBe(AttestationMode.Require);

    AttestationConfigResolver.ResolveMode("OFF").UnrecognizedValue.ShouldBeNull();
    AttestationConfigResolver.ResolveMode("WARN").UnrecognizedValue.ShouldBeNull();
    AttestationConfigResolver.ResolveMode("REQUIRE").UnrecognizedValue.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Recognized_values_tolerate_surrounding_whitespace()
  {
    AttestationModeResolution off = AttestationConfigResolver.ResolveMode("  off  ");
    AttestationModeResolution require = AttestationConfigResolver.ResolveMode("  require  ");

    off.Mode.ShouldBe(AttestationMode.Off);
    off.UnrecognizedValue.ShouldBeNull();
    require.Mode.ShouldBe(AttestationMode.Require);
    require.UnrecognizedValue.ShouldBeNull();

    await Task.CompletedTask;
  }

  // --- Unrecognized non-blank -> Mode null + surface value (never Off) ---

  public static async Task Typo_value_resolves_to_null_mode_and_surfaces_the_unrecognized_value()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("requiree");

    result.Mode.ShouldBeNull();
    result.UnrecognizedValue.ShouldBe("requiree");

    await Task.CompletedTask;
  }

  public static async Task Unrecognized_value_is_trimmed_before_being_surfaced()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("  bogus  ");

    result.Mode.ShouldBeNull();
    result.UnrecognizedValue.ShouldBe("bogus");

    await Task.CompletedTask;
  }

  public static async Task Completely_unrelated_value_resolves_to_null_mode_and_surfaces_the_value()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("enforce");

    result.Mode.ShouldBeNull();
    result.UnrecognizedValue.ShouldBe("enforce");

    await Task.CompletedTask;
  }

  public static async Task Typo_never_silently_becomes_off()
  {
    AttestationModeResolution result = AttestationConfigResolver.ResolveMode("of");

    result.Mode.ShouldBeNull();
    result.UnrecognizedValue.ShouldBe("of");
    AttestationConfigResolver.EffectiveMode(result, AttestationConfigResolver.DefaultPrMode)
      .ShouldBe(AttestationMode.Warn);
    AttestationConfigResolver.EffectiveMode(result, AttestationConfigResolver.DefaultReleaseMode)
      .ShouldBe(AttestationMode.Require);

    await Task.CompletedTask;
  }

  // --- EffectiveMode + context defaults ---

  public static async Task Blank_effective_mode_is_warn_for_pr_and_require_for_release()
  {
    AttestationModeResolution blank = AttestationConfigResolver.ResolveMode(null);

    AttestationConfigResolver.EffectiveMode(blank, AttestationConfigResolver.DefaultPrMode)
      .ShouldBe(AttestationMode.Warn);
    AttestationConfigResolver.EffectiveMode(blank, AttestationConfigResolver.DefaultReleaseMode)
      .ShouldBe(AttestationMode.Require);

    await Task.CompletedTask;
  }

  public static async Task Explicit_warn_is_warn_even_for_release_default_context()
  {
    AttestationModeResolution warn = AttestationConfigResolver.ResolveMode("warn");

    AttestationConfigResolver.EffectiveMode(warn, AttestationConfigResolver.DefaultReleaseMode)
      .ShouldBe(AttestationMode.Warn);

    await Task.CompletedTask;
  }

  public static async Task Explicit_off_is_off_for_both_context_defaults()
  {
    AttestationModeResolution off = AttestationConfigResolver.ResolveMode("off");

    AttestationConfigResolver.EffectiveMode(off, AttestationConfigResolver.DefaultPrMode)
      .ShouldBe(AttestationMode.Off);
    AttestationConfigResolver.EffectiveMode(off, AttestationConfigResolver.DefaultReleaseMode)
      .ShouldBe(AttestationMode.Off);

    await Task.CompletedTask;
  }

  public static async Task Explicit_require_is_require_for_both_context_defaults()
  {
    AttestationModeResolution require = AttestationConfigResolver.ResolveMode("require");

    AttestationConfigResolver.EffectiveMode(require, AttestationConfigResolver.DefaultPrMode)
      .ShouldBe(AttestationMode.Require);
    AttestationConfigResolver.EffectiveMode(require, AttestationConfigResolver.DefaultReleaseMode)
      .ShouldBe(AttestationMode.Require);

    await Task.CompletedTask;
  }

  public static async Task Typo_effective_mode_fail_open_on_pr_fail_closed_on_release()
  {
    AttestationModeResolution typo = AttestationConfigResolver.ResolveMode("requiree");

    AttestationConfigResolver.EffectiveMode(typo, AttestationConfigResolver.DefaultPrMode)
      .ShouldBe(AttestationMode.Warn);
    AttestationConfigResolver.EffectiveMode(typo, AttestationConfigResolver.DefaultReleaseMode)
      .ShouldBe(AttestationMode.Require);

    await Task.CompletedTask;
  }

  public static async Task Default_constants_match_locked_product_semantics()
  {
    AttestationConfigResolver.DefaultPrMode.ShouldBe(AttestationMode.Warn);
    AttestationConfigResolver.DefaultReleaseMode.ShouldBe(AttestationMode.Require);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
