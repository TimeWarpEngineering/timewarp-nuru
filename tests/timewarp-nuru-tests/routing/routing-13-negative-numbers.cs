#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class NegativeNumberTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<NegativeNumberTests>();

  // TEST 1: Basic negative integer
  public static async Task Should_accept_negative_integer_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("add {x:int} {y:int}").WithHandler((int x, int y) => $"x:{x}|y:{y}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["add", "5", "-3"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("x:5|y:-3").ShouldBeTrue();
  }

  // TEST 2: Negative double
  public static async Task Should_accept_negative_double_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("multiply {x:double} {y:double}").WithHandler((double x, double y) => $"x:{x}|y:{y}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["multiply", "2.5", "-3.14"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("x:2.5|y:-3.14").ShouldBeTrue();
  }

  // TEST 3: Multiple negative numbers
  public static async Task Should_accept_multiple_negative_parameters()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("calc {a:int} {b:int} {c:int}").WithHandler((int a, int b, int c) => $"a:{a}|b:{b}|c:{c}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["calc", "-1", "2", "-3"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("a:-1|b:2|c:-3").ShouldBeTrue();
  }

  // TEST 4: Negative with defined option (ensure options still work)
  public static async Task Should_accept_negative_number_alongside_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test {value:int} --flag").WithHandler((int value, bool flag) => $"value:{value}|flag:{flag}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["test", "-5", "--flag"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:-5|flag:True").ShouldBeTrue();
  }

  // TEST 5: Negative decimal
  public static async Task Should_accept_negative_decimal_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("price {amount:decimal}").WithHandler((decimal amount) => $"amount:{amount}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["price", "-42.99"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("amount:-42.99").ShouldBeTrue();
  }

  // TEST 6: Dash-prefixed text is an unknown option; after -- it binds as a value (task 515)
  public static async Task Should_reject_dash_prefixed_text_as_unknown_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("echo {text}").WithHandler((string text) => $"text:{text}")
      .AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["echo", "-sometext"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '-sometext'").ShouldBeTrue();
    terminal.OutputContains("text:").ShouldBeFalse();
  }

  // TEST 6b: Dash-prefixed text after the end-of-options separator binds as a value
  public static async Task Should_accept_dash_prefixed_text_after_separator()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("echo {text}").WithHandler((string text) => $"text:{text}")
      .AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["echo", "--", "-sometext"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("text:-sometext").ShouldBeTrue();
  }

  // TEST 7: Verify defined options still work correctly
  public static async Task Should_still_match_defined_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test --verbose").WithHandler((bool verbose) => $"verbose:{verbose}")
      .AsCommand().Done()
      .Build();

    // Act - with defined option
    int exitCode = await app.RunAsync(["test", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("verbose:True").ShouldBeTrue();
  }

  // TEST 7b: Undefined options fail the call and name the option (task 515)
  public static async Task Should_reject_undefined_options()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("test --verbose").WithHandler((bool verbose) => $"verbose:{verbose}")
      .AsCommand().Done()
      .Build();

    // Act - with undefined option
    int exitCode = await app.RunAsync(["test", "--other"]);

    // Assert - the handler does not run
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--other'").ShouldBeTrue();
    terminal.OutputContains("verbose:").ShouldBeFalse();
  }

  // TEST 8: Scientific notation negative
  public static async Task Should_accept_scientific_notation_negative()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("calc {value:double}").WithHandler((double value) => $"value:{value}")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["calc", "-1.5e10"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:-15000000000").ShouldBeTrue();
  }

  // TEST 9: Option with negative number value
  public static async Task Should_accept_negative_number_as_option_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("calc --amount {amount:int}").WithHandler((int amount) => $"amount:{amount}")
      .AsCommand().Done()
      .Build();

    // Act - negative number as option value: --amount -5
    int exitCode = await app.RunAsync(["calc", "--amount", "-5"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("amount:-5").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
