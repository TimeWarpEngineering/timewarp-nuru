#!/usr/bin/dotnet --

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
    int? capturedX = null;
    int? capturedY = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("add {x:int} {y:int}").WithHandler((int x, int y) =>
      {
        capturedX = x;
        capturedY = y;
      }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["add", "5", "-3"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedX.ShouldBe(5);
    capturedY.ShouldBe(-3);

    await Task.CompletedTask;
  }

  // TEST 2: Negative double
  public static async Task Should_accept_negative_double_parameter()
  {
    // Arrange
    double? capturedX = null;
    double? capturedY = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("multiply {x:double} {y:double}").WithHandler((double x, double y) =>
      {
        capturedX = x;
        capturedY = y;
      }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["multiply", "2.5", "-3.14"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedX.ShouldBe(2.5);
    capturedY.ShouldBe(-3.14);

    await Task.CompletedTask;
  }

  // TEST 3: Multiple negative numbers
  public static async Task Should_accept_multiple_negative_parameters()
  {
    // Arrange
    int? capturedA = null;
    int? capturedB = null;
    int? capturedC = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("calc {a:int} {b:int} {c:int}").WithHandler((int a, int b, int c) =>
      {
        capturedA = a;
        capturedB = b;
        capturedC = c;
      }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["calc", "-1", "2", "-3"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedA.ShouldBe(-1);
    capturedB.ShouldBe(2);
    capturedC.ShouldBe(-3);

    await Task.CompletedTask;
  }

  // TEST 4: Negative with defined option (ensure options still work)
  public static async Task Should_accept_negative_number_alongside_option()
  {
    // Arrange
    int? capturedValue = null;
    bool? capturedFlag = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("test {value:int} --flag").WithHandler((int value, bool flag) =>
      {
        capturedValue = value;
        capturedFlag = flag;
      }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["test", "-5", "--flag"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedValue.ShouldBe(-5);
    capturedFlag.ShouldBe(true);

    await Task.CompletedTask;
  }

  // TEST 5: Negative decimal
  public static async Task Should_accept_negative_decimal_parameter()
  {
    // Arrange
    decimal? capturedValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("price {amount:decimal}").WithHandler((decimal amount) =>
      {
        capturedValue = amount;
      }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["price", "-42.99"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedValue.ShouldBe(-42.99m);

    await Task.CompletedTask;
  }

  // TEST 6: Dash-prefixed literal (not a number, not an option)
  public static async Task Should_accept_dash_prefixed_literal_when_no_option_defined()
  {
    // Arrange
    string? capturedText = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("echo {text}").WithHandler((string text) =>
      {
        capturedText = text;
      }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["echo", "-sometext"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedText.ShouldBe("-sometext");

    await Task.CompletedTask;
  }

  // TEST 7: Verify defined options still work correctly
  public static async Task Should_still_match_defined_options()
  {
    // Arrange
    bool? capturedVerbose = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("test --verbose").WithHandler((bool verbose) =>
      {
        capturedVerbose = verbose;
      }).AsCommand().Done()
      .Build();

    // Act - with defined option
    int exitCode1 = await app.RunAsync(["test", "--verbose"]);

    // Assert
    exitCode1.ShouldBe(0);
    capturedVerbose.ShouldBe(true);

    // Act - with undefined option (should fail route matching)
    int exitCode2 = await app.RunAsync(["test", "--other"]);

    // Assert
    exitCode2.ShouldBe(1);

    await Task.CompletedTask;
  }

  // TEST 8: Scientific notation negative
  public static async Task Should_accept_scientific_notation_negative()
  {
    // Arrange
    double? capturedValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("calc {value:double}").WithHandler((double value) =>
      {
        capturedValue = value;
      }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["calc", "-1.5e10"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedValue.ShouldBe(-1.5e10);

    await Task.CompletedTask;
  }

  // TEST 9: Option with negative number value
  public static async Task Should_accept_negative_number_as_option_value()
  {
    // Arrange
    int? capturedAmount = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("calc --amount {amount:int}").WithHandler((int amount) =>
      {
        capturedAmount = amount;
      }).AsCommand().Done()
      .Build();

    // Act - negative number as option value: --amount -5
    int exitCode = await app.RunAsync(["calc", "--amount", "-5"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedAmount.ShouldBe(-5);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
