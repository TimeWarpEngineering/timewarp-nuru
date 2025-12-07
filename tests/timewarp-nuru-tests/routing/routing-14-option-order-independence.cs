#!/usr/bin/dotnet --

return await RunTests<OptionOrderIndependenceTests>(clearCache: true);

[TestTag("Routing")]
public class OptionOrderIndependenceTests
{
  public static async Task Should_match_options_in_different_order_than_pattern()
  {
    // Bug reproduction: backup "something" --output "mydest" --compress
    // should match pattern: backup {source} --compress --output {dest}
    // Arrange
    string? boundSource = null;
    string? boundDest = null;
    bool boundCompress = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("backup {source} --compress --output {dest}",
        (string source, bool compress, string dest) =>
        {
          boundSource = source;
          boundCompress = compress;
          boundDest = dest;
          return 0;
        })
      .Build();

    // Act - options in DIFFERENT order than pattern
    int exitCode = await app.RunAsync(["backup", "something", "--output", "mydest", "--compress"]);

    // Assert
    exitCode.ShouldBe(0);
    boundSource.ShouldBe("something");
    boundDest.ShouldBe("mydest");
    boundCompress.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_options_in_same_order_as_pattern()
  {
    // Verify original order still works
    // Arrange
    string? boundSource = null;
    string? boundDest = null;
    bool boundCompress = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("backup {source} --compress --output {dest}",
        (string source, bool compress, string dest) =>
        {
          boundSource = source;
          boundCompress = compress;
          boundDest = dest;
          return 0;
        })
      .Build();

    // Act - options in SAME order as pattern
    int exitCode = await app.RunAsync(["backup", "something", "--compress", "--output", "mydest"]);

    // Assert
    exitCode.ShouldBe(0);
    boundSource.ShouldBe("something");
    boundDest.ShouldBe("mydest");
    boundCompress.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_three_options_in_any_order_abc()
  {
    // Test with three options: --alpha, --beta {value}, --gamma
    string? boundBeta = null;
    bool boundAlpha = false;
    bool boundGamma = false;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("test --alpha --beta {value} --gamma",
        (bool alpha, string value, bool gamma) =>
        {
          boundAlpha = alpha;
          boundBeta = value;
          boundGamma = gamma;
          return 0;
        })
      .Build();

    // Act - order: alpha, beta, gamma (same as pattern)
    int exitCode = await app.RunAsync(["test", "--alpha", "--beta", "hello", "--gamma"]);

    exitCode.ShouldBe(0);
    boundAlpha.ShouldBeTrue();
    boundBeta.ShouldBe("hello");
    boundGamma.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_three_options_in_any_order_cba()
  {
    // Test with three options in reverse order
    string? boundBeta = null;
    bool boundAlpha = false;
    bool boundGamma = false;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("test --alpha --beta {value} --gamma",
        (bool alpha, string value, bool gamma) =>
        {
          boundAlpha = alpha;
          boundBeta = value;
          boundGamma = gamma;
          return 0;
        })
      .Build();

    // Act - order: gamma, beta, alpha (reverse of pattern)
    int exitCode = await app.RunAsync(["test", "--gamma", "--beta", "world", "--alpha"]);

    exitCode.ShouldBe(0);
    boundAlpha.ShouldBeTrue();
    boundBeta.ShouldBe("world");
    boundGamma.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_three_options_in_any_order_bac()
  {
    // Test with three options in mixed order
    string? boundBeta = null;
    bool boundAlpha = false;
    bool boundGamma = false;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("test --alpha --beta {value} --gamma",
        (bool alpha, string value, bool gamma) =>
        {
          boundAlpha = alpha;
          boundBeta = value;
          boundGamma = gamma;
          return 0;
        })
      .Build();

    // Act - order: beta, alpha, gamma (mixed)
    int exitCode = await app.RunAsync(["test", "--beta", "foo", "--alpha", "--gamma"]);

    exitCode.ShouldBe(0);
    boundAlpha.ShouldBeTrue();
    boundBeta.ShouldBe("foo");
    boundGamma.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_options_in_any_order()
  {
    // Test optional options in different order
    string? boundSource = null;
    string? boundOutput = null;
    bool boundCompress = false;
    bool boundVerbose = false;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("backup {source} --compress? --output? {dest} --verbose?",
        (string source, bool compress, string? dest, bool verbose) =>
        {
          boundSource = source;
          boundCompress = compress;
          boundOutput = dest;
          boundVerbose = verbose;
          return 0;
        })
      .Build();

    // Act - only some options, in different order
    int exitCode = await app.RunAsync(["backup", "myfile", "--verbose", "--output", "out.tar"]);

    exitCode.ShouldBe(0);
    boundSource.ShouldBe("myfile");
    boundCompress.ShouldBeFalse(); // not provided
    boundOutput.ShouldBe("out.tar");
    boundVerbose.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_with_options_interleaved_between_positional_args()
  {
    // Options interleaved between positional arguments is NOT valid CLI behavior
    // Options should come AFTER all positional arguments
#pragma warning disable RCS1163 // Unused parameter
    NuruCoreApp app = new NuruAppBuilder()
      .Map("copy {source} {dest} --verbose?",
        (string source, string dest, bool verbose) => 0)
#pragma warning restore RCS1163 // Unused parameter
      .Build();

    // Act - option BETWEEN positional args (invalid)
    int exitCode = await app.RunAsync(["copy", "file1.txt", "--verbose", "file2.txt"]);

    // Should fail - options cannot be interleaved with positional args
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_match_option_with_short_form_in_different_order()
  {
    // Test with aliases in different order
    string? boundOutput = null;
    bool boundVerbose = false;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --verbose,-v? --output,-o {file}",
        (bool verbose, string file) =>
        {
          boundVerbose = verbose;
          boundOutput = file;
          return 0;
        })
      .Build();

    // Act - short forms in reverse order
    int exitCode = await app.RunAsync(["build", "-o", "result.dll", "-v"]);

    exitCode.ShouldBe(0);
    boundOutput.ShouldBe("result.dll");
    boundVerbose.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_fail_when_required_option_missing_regardless_of_order()
  {
    // Ensure required options are still enforced
#pragma warning disable RCS1163 // Unused parameter
    NuruCoreApp app = new NuruAppBuilder()
      .Map("backup {source} --compress --output {dest}",
        (string source, bool compress, string dest) => 0)
#pragma warning restore RCS1163 // Unused parameter
      .Build();

    // Act - missing required --output
    int exitCode = await app.RunAsync(["backup", "something", "--compress"]);

    // Assert
    exitCode.ShouldBe(1); // Should fail - missing required option

    await Task.CompletedTask;
  }

  public static async Task Should_match_required_flag_with_optional_value()
  {
    // --output {file?} means flag is REQUIRED, but value is OPTIONAL
    // This is different from --output? {file} where the flag itself is optional
    string? boundFile = null;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("build --output {file?}",
        (string? file) =>
        {
          boundFile = file;
          return 0;
        })
      .Build();

    // Act - provide flag with value
    int exitCode = await app.RunAsync(["build", "--output", "out.dll"]);

    // Assert
    exitCode.ShouldBe(0);
    boundFile.ShouldBe("out.dll");

    await Task.CompletedTask;
  }
}
