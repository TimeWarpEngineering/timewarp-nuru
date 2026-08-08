#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.TypeConversion
{

/// <summary>
/// Tests that the runtime <see cref="TypeConverterRegistry"/> fallback behaves
/// identically to the source-generated fast path for enum validation, invariant
/// culture parsing, and extended boolean spellings.
/// </summary>
[TestTag("TypeConversion")]
public class RuntimeParityTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RuntimeParityTests>();

  public enum Priority
  {
    Low = 0,
    Normal = 10,
    High = 20,
    Critical = 30
  }

  [Flags]
  public enum FilePermissions
  {
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4,
    ReadWrite = 3
  }

  // ============================================================================
  // ENUM PARITY TESTS
  // ============================================================================

  public static async Task Should_reject_undefined_numeric_enum_via_registry()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("999", typeof(Priority), out object? result);

    // Assert
    success.ShouldBeFalse();
    result.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_accept_defined_numeric_enum_via_registry()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("10", typeof(Priority), out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    Priority priority = (Priority)result!;
    priority.ShouldBe(Priority.Normal);

    await Task.CompletedTask;
  }

  public static async Task Should_accept_enum_name_via_registry()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("High", typeof(Priority), out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    Priority priority = (Priority)result!;
    priority.ShouldBe(Priority.High);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_flags_raw_numeric_via_registry()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("12", typeof(FilePermissions), out object? result);

    // Assert
    success.ShouldBeFalse();
    result.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_accept_flags_comma_separated_names_via_registry()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("Read,Write", typeof(FilePermissions), out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    FilePermissions permissions = (FilePermissions)result!;
    permissions.ShouldBe(FilePermissions.ReadWrite);

    await Task.CompletedTask;
  }

  // ============================================================================
  // CULTURE PARITY TESTS
  // ============================================================================

  public static async Task Should_parse_double_invariant_under_de_de_via_registry()
  {
    // Arrange
    CultureInfo originalCulture = CultureInfo.CurrentCulture;
    CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
    CultureInfo.CurrentCulture = new CultureInfo("de-DE");
    CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

    try
    {
      TypeConverterRegistry registry = new();

      // Act
      bool success = registry.TryConvert("3.14", typeof(double), out object? result);

      // Assert
      success.ShouldBeTrue();
      result.ShouldNotBeNull();
      double value = (double)result!;
      value.ShouldBe(3.14);
    }
    finally
    {
      CultureInfo.CurrentCulture = originalCulture;
      CultureInfo.CurrentUICulture = originalUiCulture;
    }

    await Task.CompletedTask;
  }

  public static async Task Should_parse_comma_double_as_thousands_under_de_de_via_registry()
  {
    // Arrange
    CultureInfo originalCulture = CultureInfo.CurrentCulture;
    CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
    CultureInfo.CurrentCulture = new CultureInfo("de-DE");
    CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

    try
    {
      TypeConverterRegistry registry = new();

      // Act - InvariantCulture with AllowThousands treats comma as a thousands separator, so "3,14" parses as 314.
      bool success = registry.TryConvert("3,14", typeof(double), out object? result);

      // Assert
      success.ShouldBeTrue();
      result.ShouldNotBeNull();
      double value = (double)result!;
      value.ShouldBe(314);
    }
    finally
    {
      CultureInfo.CurrentCulture = originalCulture;
      CultureInfo.CurrentUICulture = originalUiCulture;
    }

    await Task.CompletedTask;
  }

  public static async Task Should_parse_datetime_invariant_under_de_de_via_registry()
  {
    // Arrange
    CultureInfo originalCulture = CultureInfo.CurrentCulture;
    CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
    CultureInfo.CurrentCulture = new CultureInfo("de-DE");
    CultureInfo.CurrentUICulture = new CultureInfo("de-DE");

    try
    {
      TypeConverterRegistry registry = new();

      // Act
      bool success = registry.TryConvert("2024-01-15", typeof(DateTime), out object? result);

      // Assert
      success.ShouldBeTrue();
      result.ShouldNotBeNull();
      DateTime dateTime = (DateTime)result!;
      dateTime.Year.ShouldBe(2024);
      dateTime.Month.ShouldBe(1);
      dateTime.Day.ShouldBe(15);
    }
    finally
    {
      CultureInfo.CurrentCulture = originalCulture;
      CultureInfo.CurrentUICulture = originalUiCulture;
    }

    await Task.CompletedTask;
  }

  // ============================================================================
  // BOOL SPELLING TESTS
  // ============================================================================

  public static async Task Should_accept_yes_via_registry()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("yes", typeof(bool), out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    bool value = (bool)result!;
    value.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_accept_no_via_registry()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("no", typeof(bool), out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    bool value = (bool)result!;
    value.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_invalid_bool_via_registry()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("maybe", typeof(bool), out object? result);

    // Assert
    success.ShouldBeFalse();
    result.ShouldBeNull();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.TypeConversion
