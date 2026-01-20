#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class OptionOrderIndependenceTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OptionOrderIndependenceTests>();

  public static async Task Should_match_options_in_different_order_than_pattern()
  {
    // Bug reproduction: backup "something" --output "mydest" --compress
    // should match pattern: backup {source} --compress --output {dest}
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("backup {source} --compress --output {dest}").WithHandler(
        (string source, bool compress, string dest) =>
          $"source={source}|compress={compress}|dest={dest}").AsQuery().Done()
      .Build();

    // Act - options in DIFFERENT order than pattern
    int exitCode = await app.RunAsync(["backup", "something", "--output", "mydest", "--compress"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("source=something").ShouldBeTrue();
    terminal.OutputContains("dest=mydest").ShouldBeTrue();
    terminal.OutputContains("compress=True").ShouldBeTrue();
  }

  public static async Task Should_match_options_in_same_order_as_pattern()
  {
    // Verify original order still works
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("backup {source} --compress --output {dest}").WithHandler(
        (string source, bool compress, string dest) =>
          $"source={source}|compress={compress}|dest={dest}").AsQuery().Done()
      .Build();

    // Act - options in SAME order as pattern
    int exitCode = await app.RunAsync(["backup", "something", "--compress", "--output", "mydest"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("source=something").ShouldBeTrue();
    terminal.OutputContains("dest=mydest").ShouldBeTrue();
    terminal.OutputContains("compress=True").ShouldBeTrue();
  }

  public static async Task Should_match_three_options_in_any_order_abc()
  {
    // Test with three options: --alpha, --beta {value}, --gamma
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("test --alpha --beta {value} --gamma").WithHandler(
        (bool alpha, string value, bool gamma) =>
          $"alpha={alpha}|beta={value}|gamma={gamma}").AsQuery().Done()
      .Build();

    // Act - order: alpha, beta, gamma (same as pattern)
    int exitCode = await app.RunAsync(["test", "--alpha", "--beta", "hello", "--gamma"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("alpha=True").ShouldBeTrue();
    terminal.OutputContains("beta=hello").ShouldBeTrue();
    terminal.OutputContains("gamma=True").ShouldBeTrue();
  }

  public static async Task Should_match_three_options_in_any_order_cba()
  {
    // Test with three options in reverse order
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("test --alpha --beta {value} --gamma").WithHandler(
        (bool alpha, string value, bool gamma) =>
          $"alpha={alpha}|beta={value}|gamma={gamma}").AsQuery().Done()
      .Build();

    // Act - order: gamma, beta, alpha (reverse of pattern)
    int exitCode = await app.RunAsync(["test", "--gamma", "--beta", "world", "--alpha"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("alpha=True").ShouldBeTrue();
    terminal.OutputContains("beta=world").ShouldBeTrue();
    terminal.OutputContains("gamma=True").ShouldBeTrue();
  }

  public static async Task Should_match_three_options_in_any_order_bac()
  {
    // Test with three options in mixed order
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("test --alpha --beta {value} --gamma").WithHandler(
        (bool alpha, string value, bool gamma) =>
          $"alpha={alpha}|beta={value}|gamma={gamma}").AsQuery().Done()
      .Build();

    // Act - order: beta, alpha, gamma (mixed)
    int exitCode = await app.RunAsync(["test", "--beta", "foo", "--alpha", "--gamma"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("alpha=True").ShouldBeTrue();
    terminal.OutputContains("beta=foo").ShouldBeTrue();
    terminal.OutputContains("gamma=True").ShouldBeTrue();
  }

  public static async Task Should_match_optional_options_in_any_order()
  {
    // Test optional options in different order
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("backup {source} --compress? --output? {dest} --verbose?").WithHandler(
        (string source, bool compress, string? dest, bool verbose) =>
          $"source={source}|compress={compress}|dest={dest ?? "NULL"}|verbose={verbose}").AsQuery().Done()
      .Build();

    // Act - only some options, in different order
    int exitCode = await app.RunAsync(["backup", "myfile", "--verbose", "--output", "out.tar"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("source=myfile").ShouldBeTrue();
    terminal.OutputContains("compress=False").ShouldBeTrue(); // not provided
    terminal.OutputContains("dest=out.tar").ShouldBeTrue();
    terminal.OutputContains("verbose=True").ShouldBeTrue();
  }

  public static async Task Should_match_with_options_interleaved_between_positional_args()
  {
    // Options can appear anywhere - before, after, or interleaved with positional args
    // The router uses a two-pass approach: extract options first, then process positionals
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("copy {source} {dest} --verbose?").WithHandler(
        (string source, string dest, bool verbose) =>
          $"source={source}|dest={dest}|verbose={verbose}").AsQuery().Done()
      .Build();

    // Act - option BETWEEN positional args (now valid with two-pass approach)
    int exitCode = await app.RunAsync(["copy", "file1.txt", "--verbose", "file2.txt"]);

    // Should succeed - options are extracted first, leaving positionals in order
    exitCode.ShouldBe(0);
    terminal.OutputContains("source=file1.txt").ShouldBeTrue();
    terminal.OutputContains("dest=file2.txt").ShouldBeTrue();
    terminal.OutputContains("verbose=True").ShouldBeTrue();
  }

  public static async Task Should_match_option_with_short_form_in_different_order()
  {
    // Test with aliases in different order
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("build --verbose,-v? --output,-o {file}").WithHandler(
        (bool verbose, string file) =>
          $"verbose={verbose}|file={file}").AsQuery().Done()
      .Build();

    // Act - short forms in reverse order
    int exitCode = await app.RunAsync(["build", "-o", "result.dll", "-v"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("file=result.dll").ShouldBeTrue();
    terminal.OutputContains("verbose=True").ShouldBeTrue();
  }

  public static async Task Should_fail_when_required_option_missing_regardless_of_order()
  {
    // Ensure required options are still enforced
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("backup {source} --compress --output {dest}").WithHandler(
        (string source, bool compress, string dest) =>
          $"source={source}|compress={compress}|dest={dest}").AsQuery().Done()
      .Build();

    // Act - missing required --output
    int exitCode = await app.RunAsync(["backup", "something", "--compress"]);

    // Assert
    exitCode.ShouldBe(1); // Should fail - missing required option
  }

  public static async Task Should_match_required_flag_with_optional_value()
  {
    // --output {file?} means flag is REQUIRED, but value is OPTIONAL
    // This is different from --output? {file} where the flag itself is optional
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("build --output {file?}").WithHandler(
        (string? file) => $"file={file ?? "NULL"}").AsQuery().Done()
      .Build();

    // Act - provide flag with value
    int exitCode = await app.RunAsync(["build", "--output", "out.dll"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("file=out.dll").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
