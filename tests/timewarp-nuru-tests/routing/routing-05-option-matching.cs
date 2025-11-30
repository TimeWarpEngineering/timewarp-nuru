#!/usr/bin/dotnet --

return await RunTests<OptionMatchingTests>(clearCache: true);

[TestTag("Routing")]
public class OptionMatchingTests
{
  public static async Task Should_match_required_option_build_config_debug()
  {
    // Arrange
    string? boundMode = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config {mode}", (string mode) => { boundMode = mode; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config", "debug"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBe("debug");

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_missing_required_option_build()
  {
    // Arrange
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config {mode}", (string _) => 0)
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(1); // Missing required option

    await Task.CompletedTask;
  }

  public static async Task Should_match_required_flag_optional_value_build_config_release()
  {
    // Behavior #2: Required flag + Optional value (--config {mode?})
    // Arrange
    string? boundMode = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config {mode?}", (string? mode) => { boundMode = mode; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config", "release"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBe("release");

    await Task.CompletedTask;
  }

  public static async Task Should_match_required_flag_optional_value_build_config_no_value()
  {
    // Behavior #2: Flag present without value should bind null
    // Arrange
    string? boundMode = "unexpected";
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config {mode?}", (string? mode) => { boundMode = mode; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_required_flag_optional_value_build_missing_flag()
  {
    // Behavior #2: Missing required flag should not match
    // Arrange
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config {mode?}", (string? _) => 0)
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(1); // Missing required flag

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_optional_value_build_config_release()
  {
    // Behavior #3: Optional flag + Optional value (--config? {mode?})
    // Arrange
    string? boundMode = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config? {mode?}", (string? mode) => { boundMode = mode; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config", "release"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBe("release");

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_optional_value_build_config_no_value()
  {
    // Behavior #3: Optional flag present without value
    // Arrange
    string? boundMode = "unexpected";
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config? {mode?}", (string? mode) => { boundMode = mode; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_optional_value_build_missing_flag()
  {
    // Behavior #3: Optional flag omitted entirely
    // Arrange
    string? boundMode = "unexpected";
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config? {mode?}", (string? mode) => { boundMode = mode; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_required_value_build_config_debug()
  {
    // Behavior #4: Optional flag + Required value (--config? {mode})
    // Arrange
    string? boundMode = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config? {mode}", (string? mode) => { boundMode = mode; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config", "debug"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBe("debug");

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_required_value_build_missing_flag()
  {
    // Behavior #4: Optional flag omitted entirely
    // Arrange
    string? boundMode = "unexpected";
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config? {mode}", (string? mode) => { boundMode = mode; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_optional_flag_required_value_build_config_no_value()
  {
    // Behavior #4: Flag present without required value should not match
    // Arrange
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config? {mode}", (string? _) => 0)
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config"]);

    // Assert
    exitCode.ShouldBe(1); // Flag present but missing required value

    await Task.CompletedTask;
  }

  public static async Task Should_match_boolean_flag_build_verbose_true()
  {
    // Arrange
    bool boundVerbose = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --verbose", (bool verbose) => { boundVerbose = verbose; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    boundVerbose.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_boolean_flag_build_false()
  {
    // Arrange
    bool boundVerbose = true;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --verbose", (bool verbose) => { boundVerbose = verbose; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    boundVerbose.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_required_optional_deploy_env_prod_tag_v1_0_verbose()
  {
    // Arrange
    string? boundE = null;
    string? boundT = null;
    bool boundVerbose = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy --env {e} --tag {t?} --verbose", (string e, string? t, bool verbose) => { boundE = e; boundT = t; boundVerbose = verbose; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod", "--tag", "v1.0", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    boundE.ShouldBe("prod");
    boundT.ShouldBe("v1.0");
    boundVerbose.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_required_optional_deploy_env_prod_defaults()
  {
    // Arrange
    string? boundE = null;
    string? boundT = "unexpected";
    bool boundVerbose = true;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy --env {e} --tag? {t?} --verbose", (string e, string? t, bool verbose) => { boundE = e; boundT = t; boundVerbose = verbose; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod"]);

    // Assert
    exitCode.ShouldBe(0);
    boundE.ShouldBe("prod");
    boundT.ShouldBeNull();
    boundVerbose.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_mixed_missing_required_deploy_tag_v1_0()
  {
    // Arrange
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy --env {e} --tag? {t?} --verbose", (string _, string? _, bool _) => 0)
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--tag", "v1.0"]);

    // Assert
    exitCode.ShouldBe(1); // Missing required --env

    await Task.CompletedTask;
  }

  public static async Task Should_match_typed_option_server_port_8080()
  {
    // Arrange
    int boundNum = 0;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("server --port {num:int}", (int num) => { boundNum = num; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["server", "--port", "8080"]);

    // Assert
    exitCode.ShouldBe(0);
    boundNum.ShouldBe(8080);

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_typed_option_server_port_abc()
  {
    // Arrange
    NuruCoreApp app = new NuruAppBuilder()
      .Map("server --port {num:int}", (int _) => 0)
      .Build();

    // Act
    int exitCode = await app.RunAsync(["server", "--port", "abc"]);

    // Assert
    exitCode.ShouldBe(1); // Type conversion failure

    await Task.CompletedTask;
  }

  public static async Task Should_match_option_alias_build_verbose()
  {
    // Arrange
    bool boundVerbose = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --verbose,-v", (bool verbose) => { boundVerbose = verbose; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    boundVerbose.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_option_alias_build_v()
  {
    // Arrange
    bool boundVerbose = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --verbose,-v", (bool verbose) => { boundVerbose = verbose; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "-v"]);

    // Assert
    exitCode.ShouldBe(0);
    boundVerbose.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_boolean_long_form()
  {
    // Arrange - Test optional boolean flag with alias using long form
    // Pattern: --verbose,-v? (per optional-flag-alias-syntax.md)
    bool boundVerbose = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --verbose,-v?", (bool verbose) => { boundVerbose = verbose; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    boundVerbose.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_boolean_short_form()
  {
    // Arrange - Test optional boolean flag with alias using short form
    // Pattern: --verbose,-v? (per optional-flag-alias-syntax.md)
    bool boundVerbose = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --verbose,-v?", (bool verbose) => { boundVerbose = verbose; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "-v"]);

    // Assert
    exitCode.ShouldBe(0);
    boundVerbose.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_boolean_omitted()
  {
    // Arrange - Test optional boolean flag with alias omitted
    // Pattern: --verbose,-v? (per optional-flag-alias-syntax.md)
    bool boundVerbose = true;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --verbose,-v?", (bool verbose) => { boundVerbose = verbose; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    boundVerbose.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_value_long_form()
  {
    // Arrange - Test optional flag with alias and value using long form
    // Pattern: --output,-o? {file} (per optional-flag-alias-syntax.md)
    string? boundFile = null;
#pragma warning disable RCS1163 // Unused parameter
    NuruCoreApp app = new NuruAppBuilder()
      .Map("backup {source} --output,-o? {file}",
        (string source, string? file) => { boundFile = file; return 0; })
#pragma warning restore RCS1163 // Unused parameter
      .Build();

    // Act
    int exitCode = await app.RunAsync(["backup", "/data", "--output", "result.tar"]);

    // Assert
    exitCode.ShouldBe(0);
    boundFile.ShouldBe("result.tar");

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_value_short_form()
  {
    // Arrange - Test optional flag with alias and value using short form
    // Pattern: --output,-o? {file} (per optional-flag-alias-syntax.md)
    string? boundFile = null;
#pragma warning disable RCS1163 // Unused parameter
    NuruCoreApp app = new NuruAppBuilder()
      .Map("backup {source} --output,-o? {file}",
        (string source, string? file) => { boundFile = file; return 0; })
#pragma warning restore RCS1163 // Unused parameter
      .Build();

    // Act
    int exitCode = await app.RunAsync(["backup", "/data", "-o", "result.tar"]);

    // Assert
    exitCode.ShouldBe(0);
    boundFile.ShouldBe("result.tar");

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_value_omitted()
  {
    // Arrange - Test optional flag with alias omitted entirely
    // Pattern: --output,-o? {file} (per optional-flag-alias-syntax.md)
    string? boundFile = "unexpected";
#pragma warning disable RCS1163 // Unused parameter
    NuruCoreApp app = new NuruAppBuilder()
      .Map("backup {source} --output,-o? {file}",
        (string source, string? file) => { boundFile = file; return 0; })
#pragma warning restore RCS1163 // Unused parameter
      .Build();

    // Act
    int exitCode = await app.RunAsync(["backup", "/data"]);

    // Assert
    exitCode.ShouldBe(0);
    boundFile.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_optional_value_long_form()
  {
    // Arrange - Test optional flag with alias and optional value using long form
    // Pattern: --config,-c? {mode?} (per optional-flag-alias-syntax.md)
    string? boundMode = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config,-c? {mode?}",
        (string? mode) => { boundMode = mode; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config", "debug"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBe("debug");

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_optional_value_short_form()
  {
    // Arrange - Test optional flag with alias and optional value using short form
    // Pattern: --config,-c? {mode?} (per optional-flag-alias-syntax.md)
    string? boundMode = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config,-c? {mode?}",
        (string? mode) => { boundMode = mode; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "-c", "release"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBe("release");

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_optional_value_flag_omitted()
  {
    // Arrange - Test optional flag with alias and optional value with flag omitted
    // Pattern: --config,-c? {mode?} (per optional-flag-alias-syntax.md)
    string? boundMode = "unexpected";
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config,-c? {mode?}",
        (string? mode) => { boundMode = mode; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_optional_value_value_omitted_long()
  {
    // Arrange - Test optional flag present without value (long form)
    // Pattern: --config,-c? {mode?} (per optional-flag-alias-syntax.md)
    string? boundMode = "unexpected";
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config,-c? {mode?}",
        (string? mode) => { boundMode = mode; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_optional_value_value_omitted_short()
  {
    // Arrange - Test optional flag present without value (short form)
    // Pattern: --config,-c? {mode?} (per optional-flag-alias-syntax.md)
    string? boundMode = "unexpected";
    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --config,-c? {mode?}",
        (string? mode) => { boundMode = mode; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "-c"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBeNull();

    await Task.CompletedTask;
  }
}