#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

/// <summary>
/// Tests that generated route binding uses invariant culture for parsing
/// numeric and date types, and that the extended boolean spellings are
/// accepted by the generated fast path.
/// </summary>
[TestTag("Routing")]
[TestTag("TypeConversion")]
public class InvariantCultureBindingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<InvariantCultureBindingTests>();

  // ============================================================================
  // INVARIANT CULTURE TESTS (under de-DE)
  // ============================================================================

  public static async Task Should_parse_double_3_14_under_de_de_culture()
  {
    // Arrange
    CultureInfo originalCulture = CultureInfo.CurrentCulture;
    CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
    CultureInfo.CurrentCulture = new CultureInfo("de-DE");
    CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

    try
    {
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("calc {value:double}")
          .WithHandler((double value) => $"value:{value.ToString(CultureInfo.InvariantCulture)}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["calc", "3.14"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("value:3.14").ShouldBeTrue();
    }
    finally
    {
      CultureInfo.CurrentCulture = originalCulture;
      CultureInfo.CurrentUICulture = originalUiCulture;
    }
  }

  public static async Task Should_parse_comma_as_thousands_separator_under_de_de_culture()
  {
    // Arrange
    CultureInfo originalCulture = CultureInfo.CurrentCulture;
    CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
    CultureInfo.CurrentCulture = new CultureInfo("de-DE");
    CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

    try
    {
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("calc {value:double}")
          .WithHandler((double value) => $"value:{value.ToString(CultureInfo.InvariantCulture)}")
          .AsQuery()
          .Done()
        .Build();

      // Act - InvariantCulture treats comma as a thousands separator, so "3,14" parses as 314.
      int exitCode = await app.RunAsync(["calc", "3,14"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("value:314").ShouldBeTrue();
    }
    finally
    {
      CultureInfo.CurrentCulture = originalCulture;
      CultureInfo.CurrentUICulture = originalUiCulture;
    }
  }

  public static async Task Should_parse_decimal_under_de_de_culture()
  {
    // Arrange
    CultureInfo originalCulture = CultureInfo.CurrentCulture;
    CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
    CultureInfo.CurrentCulture = new CultureInfo("de-DE");
    CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

    try
    {
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("price {value:decimal}")
          .WithHandler((decimal value) => $"price:{value.ToString(CultureInfo.InvariantCulture)}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["price", "19.99"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("price:19.99").ShouldBeTrue();
    }
    finally
    {
      CultureInfo.CurrentCulture = originalCulture;
      CultureInfo.CurrentUICulture = originalUiCulture;
    }
  }

  public static async Task Should_parse_datetime_iso8601_under_de_de_culture()
  {
    // Arrange
    CultureInfo originalCulture = CultureInfo.CurrentCulture;
    CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
    CultureInfo.CurrentCulture = new CultureInfo("de-DE");
    CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

    try
    {
      using TestTerminal terminal = new();
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .Map("date {value:DateTime}")
          .WithHandler((DateTime value) => $"date:{value:yyyy-MM-dd}")
          .AsQuery()
          .Done()
        .Build();

      // Act
      int exitCode = await app.RunAsync(["date", "2024-01-15"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("date:2024-01-15").ShouldBeTrue();
    }
    finally
    {
      CultureInfo.CurrentCulture = originalCulture;
      CultureInfo.CurrentUICulture = originalUiCulture;
    }
  }

  // ============================================================================
  // BOOL SPELLING TESTS (GENERATED PATH)
  // ============================================================================

  public static async Task Should_accept_bool_yes_as_true()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set {flag:bool}")
        .WithHandler((bool flag) => $"flag:{flag}")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set", "yes"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("flag:True").ShouldBeTrue();
  }

  public static async Task Should_accept_bool_no_as_false()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set {flag:bool}")
        .WithHandler((bool flag) => $"flag:{flag}")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set", "no"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("flag:False").ShouldBeTrue();
  }

  public static async Task Should_accept_bool_1_as_true()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set {flag:bool}")
        .WithHandler((bool flag) => $"flag:{flag}")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set", "1"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("flag:True").ShouldBeTrue();
  }

  public static async Task Should_accept_bool_0_as_false()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set {flag:bool}")
        .WithHandler((bool flag) => $"flag:{flag}")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set", "0"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("flag:False").ShouldBeTrue();
  }

  public static async Task Should_accept_bool_on_as_true()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set {flag:bool}")
        .WithHandler((bool flag) => $"flag:{flag}")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set", "on"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("flag:True").ShouldBeTrue();
  }

  public static async Task Should_accept_bool_off_as_false()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set {flag:bool}")
        .WithHandler((bool flag) => $"flag:{flag}")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set", "off"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("flag:False").ShouldBeTrue();
  }

  public static async Task Should_accept_bool_enabled_as_true()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set {flag:bool}")
        .WithHandler((bool flag) => $"flag:{flag}")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set", "enabled"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("flag:True").ShouldBeTrue();
  }

  public static async Task Should_accept_bool_disabled_as_false()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set {flag:bool}")
        .WithHandler((bool flag) => $"flag:{flag}")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set", "disabled"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("flag:False").ShouldBeTrue();
  }

  public static async Task Should_accept_bool_true_case_insensitive()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set {flag:bool}")
        .WithHandler((bool flag) => $"flag:{flag}")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set", "TRUE"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("flag:True").ShouldBeTrue();
  }

  public static async Task Should_accept_bool_false_case_insensitive()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set {flag:bool}")
        .WithHandler((bool flag) => $"flag:{flag}")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set", "FALSE"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("flag:False").ShouldBeTrue();
  }

  public static async Task Should_reject_invalid_bool_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("set {flag:bool}")
        .WithHandler((bool flag) => $"flag:{flag}")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["set", "maybe"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.OutputContains("Error").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
