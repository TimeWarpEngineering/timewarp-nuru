#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class TypedCatchAllTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<TypedCatchAllTests>();

  /// <summary>
  /// Tests that int[] catch-all parameter properly converts string arguments to integers.
  /// </summary>
  public static async Task Should_bind_int_array_catch_all_sum_1_2_3()
  {
    // Arrange
    int[]? boundNumbers = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("sum {*numbers:int}").WithHandler((int[] numbers) => { boundNumbers = numbers; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["sum", "1", "2", "3"]);

    // Assert
    exitCode.ShouldBe(0);
    boundNumbers.ShouldNotBeNull();
    boundNumbers.Length.ShouldBe(3);
    boundNumbers[0].ShouldBe(1);
    boundNumbers[1].ShouldBe(2);
    boundNumbers[2].ShouldBe(3);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests that double[] catch-all parameter properly converts string arguments to doubles.
  /// </summary>
  public static async Task Should_bind_double_array_catch_all_average_1_5_2_5_3_5()
  {
    // Arrange
    double[]? boundValues = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("average {*values:double}").WithHandler((double[] values) => { boundValues = values; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["average", "1.5", "2.5", "3.5"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValues.ShouldNotBeNull();
    boundValues.Length.ShouldBe(3);
    boundValues[0].ShouldBe(1.5);
    boundValues[1].ShouldBe(2.5);
    boundValues[2].ShouldBe(3.5);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests that bool[] catch-all parameter properly converts string arguments to booleans.
  /// </summary>
  public static async Task Should_bind_bool_array_catch_all_flags_true_false_true()
  {
    // Arrange
    bool[]? boundFlags = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("flags {*values:bool}").WithHandler((bool[] values) => { boundFlags = values; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["flags", "true", "false", "true"]);

    // Assert
    exitCode.ShouldBe(0);
    boundFlags.ShouldNotBeNull();
    boundFlags.Length.ShouldBe(3);
    boundFlags[0].ShouldBeTrue();
    boundFlags[1].ShouldBeFalse();
    boundFlags[2].ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests empty typed array when no arguments provided.
  /// </summary>
  public static async Task Should_bind_empty_int_array_when_no_args()
  {
    // Arrange
    int[]? boundNumbers = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("sum {*numbers:int}").WithHandler((int[] numbers) => { boundNumbers = numbers; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["sum"]);

    // Assert
    exitCode.ShouldBe(0);
    boundNumbers.ShouldNotBeNull();
    boundNumbers.Length.ShouldBe(0);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests typed catch-all after regular parameters.
  /// </summary>
  public static async Task Should_bind_mixed_params_and_typed_catch_all()
  {
    // Arrange
    string? boundOperation = null;
    int[]? boundNumbers = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("calc {operation} {*numbers:int}").WithHandler((string operation, int[] numbers) =>
      {
        boundOperation = operation;
        boundNumbers = numbers;
      }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["calc", "sum", "10", "20", "30"]);

    // Assert
    exitCode.ShouldBe(0);
    boundOperation.ShouldBe("sum");
    boundNumbers.ShouldNotBeNull();
    boundNumbers.Length.ShouldBe(3);
    boundNumbers[0].ShouldBe(10);
    boundNumbers[1].ShouldBe(20);
    boundNumbers[2].ShouldBe(30);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests long[] catch-all parameter.
  /// </summary>
  public static async Task Should_bind_long_array_catch_all()
  {
    // Arrange
    long[]? boundValues = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("ids {*values:long}").WithHandler((long[] values) => { boundValues = values; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["ids", "9223372036854775807", "123456789012345"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValues.ShouldNotBeNull();
    boundValues.Length.ShouldBe(2);
    boundValues[0].ShouldBe(9223372036854775807L);
    boundValues[1].ShouldBe(123456789012345L);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests decimal[] catch-all parameter.
  /// </summary>
  public static async Task Should_bind_decimal_array_catch_all()
  {
    // Arrange
    decimal[]? boundValues = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("prices {*values:decimal}").WithHandler((decimal[] values) => { boundValues = values; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["prices", "19.99", "29.99", "39.99"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValues.ShouldNotBeNull();
    boundValues.Length.ShouldBe(3);
    boundValues[0].ShouldBe(19.99m);
    boundValues[1].ShouldBe(29.99m);
    boundValues[2].ShouldBe(39.99m);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests Guid[] catch-all parameter.
  /// </summary>
  public static async Task Should_bind_guid_array_catch_all()
  {
    // Arrange
    Guid[]? boundGuids = null;
    Guid guid1 = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    Guid guid2 = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");
    NuruCoreApp app = new NuruAppBuilder()
      .Map("guids {*values:Guid}").WithHandler((Guid[] values) => { boundGuids = values; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["guids", "550e8400-e29b-41d4-a716-446655440000", "6ba7b810-9dad-11d1-80b4-00c04fd430c8"]);

    // Assert
    exitCode.ShouldBe(0);
    boundGuids.ShouldNotBeNull();
    boundGuids.Length.ShouldBe(2);
    boundGuids[0].ShouldBe(guid1);
    boundGuids[1].ShouldBe(guid2);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests DateTime[] catch-all parameter.
  /// </summary>
  public static async Task Should_bind_datetime_array_catch_all()
  {
    // Arrange
    DateTime[]? boundDates = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("schedule {*dates:DateTime}").WithHandler((DateTime[] dates) => { boundDates = dates; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["schedule", "2024-01-15", "2024-06-30"]);

    // Assert
    exitCode.ShouldBe(0);
    boundDates.ShouldNotBeNull();
    boundDates.Length.ShouldBe(2);
    boundDates[0].Year.ShouldBe(2024);
    boundDates[0].Month.ShouldBe(1);
    boundDates[0].Day.ShouldBe(15);
    boundDates[1].Year.ShouldBe(2024);
    boundDates[1].Month.ShouldBe(6);
    boundDates[1].Day.ShouldBe(30);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests that string[] catch-all with explicit type constraint works.
  /// </summary>
  public static async Task Should_bind_explicit_string_array_catch_all()
  {
    // Arrange
    string[]? boundArgs = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("echo {*args:string}").WithHandler((string[] args) => { boundArgs = args; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["echo", "hello", "world"]);

    // Assert
    exitCode.ShouldBe(0);
    boundArgs.ShouldNotBeNull();
    boundArgs.Length.ShouldBe(2);
    boundArgs[0].ShouldBe("hello");
    boundArgs[1].ShouldBe("world");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests that default string[] catch-all (no type constraint) still works.
  /// </summary>
  public static async Task Should_bind_default_string_array_catch_all_without_type()
  {
    // Arrange
    string[]? boundArgs = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("echo {*args}").WithHandler((string[] args) => { boundArgs = args; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["echo", "hello", "world"]);

    // Assert
    exitCode.ShouldBe(0);
    boundArgs.ShouldNotBeNull();
    boundArgs.Length.ShouldBe(2);
    boundArgs[0].ShouldBe("hello");
    boundArgs[1].ShouldBe("world");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests that invalid conversion produces appropriate error.
  /// </summary>
  public static async Task Should_fail_on_invalid_int_conversion()
  {
    // Arrange
    NuruCoreApp app = new NuruAppBuilder()
      .Map("sum {*numbers:int}").WithHandler((int[] _) => 0).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["sum", "1", "not-a-number", "3"]);

    // Assert - should fail due to conversion error
    exitCode.ShouldBe(1);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests negative numbers in int[] catch-all.
  /// </summary>
  public static async Task Should_bind_negative_numbers_in_int_array()
  {
    // Arrange
    int[]? boundNumbers = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("calc {*numbers:int}").WithHandler((int[] numbers) => { boundNumbers = numbers; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["calc", "-5", "10", "-15"]);

    // Assert
    exitCode.ShouldBe(0);
    boundNumbers.ShouldNotBeNull();
    boundNumbers.Length.ShouldBe(3);
    boundNumbers[0].ShouldBe(-5);
    boundNumbers[1].ShouldBe(10);
    boundNumbers[2].ShouldBe(-15);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
