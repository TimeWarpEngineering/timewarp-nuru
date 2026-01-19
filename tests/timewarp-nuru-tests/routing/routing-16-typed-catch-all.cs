#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

#pragma warning disable RCS1163 // Unused parameter - expected in negative test cases

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
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("sum {*numbers:int}").WithHandler((int[] numbers) => $"numbers:[{string.Join(",", numbers)}]|len:{numbers.Length}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["sum", "1", "2", "3"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("numbers:[1,2,3]").ShouldBeTrue();
    terminal.OutputContains("len:3").ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests that double[] catch-all parameter properly converts string arguments to doubles.
  /// </summary>
  public static async Task Should_bind_double_array_catch_all_average_1_5_2_5_3_5()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("average {*values:double}").WithHandler((double[] values) => $"values:[{string.Join(",", values)}]|len:{values.Length}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["average", "1.5", "2.5", "3.5"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("values:[1.5,2.5,3.5]").ShouldBeTrue();
    terminal.OutputContains("len:3").ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests that bool[] catch-all parameter properly converts string arguments to booleans.
  /// </summary>
  public static async Task Should_bind_bool_array_catch_all_flags_true_false_true()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("flags {*values:bool}").WithHandler((bool[] values) => $"values:[{string.Join(",", values)}]|len:{values.Length}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["flags", "true", "false", "true"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("values:[True,False,True]").ShouldBeTrue();
    terminal.OutputContains("len:3").ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests empty typed array when no arguments provided.
  /// </summary>
  public static async Task Should_bind_empty_int_array_when_no_args()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("sum {*numbers:int}").WithHandler((int[] numbers) => $"numbers:[{string.Join(",", numbers)}]|len:{numbers.Length}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["sum"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("numbers:[]").ShouldBeTrue();
    terminal.OutputContains("len:0").ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests typed catch-all after regular parameters.
  /// </summary>
  public static async Task Should_bind_mixed_params_and_typed_catch_all()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("calc {operation} {*numbers:int}").WithHandler((string operation, int[] numbers) => $"operation:{operation}|numbers:[{string.Join(",", numbers)}]").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["calc", "sum", "10", "20", "30"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("operation:sum").ShouldBeTrue();
    terminal.OutputContains("numbers:[10,20,30]").ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests long[] catch-all parameter.
  /// </summary>
  public static async Task Should_bind_long_array_catch_all()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("ids {*values:long}").WithHandler((long[] values) => $"values:[{string.Join(",", values)}]|len:{values.Length}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["ids", "9223372036854775807", "123456789012345"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("values:[9223372036854775807,123456789012345]").ShouldBeTrue();
    terminal.OutputContains("len:2").ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests decimal[] catch-all parameter.
  /// </summary>
  public static async Task Should_bind_decimal_array_catch_all()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("prices {*values:decimal}").WithHandler((decimal[] values) => $"values:[{string.Join(",", values)}]|len:{values.Length}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["prices", "19.99", "29.99", "39.99"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("values:[19.99,29.99,39.99]").ShouldBeTrue();
    terminal.OutputContains("len:3").ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests Guid[] catch-all parameter.
  /// </summary>
  public static async Task Should_bind_guid_array_catch_all()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("guids {*values:Guid}").WithHandler((Guid[] values) => $"values:[{string.Join(",", values)}]|len:{values.Length}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["guids", "550e8400-e29b-41d4-a716-446655440000", "6ba7b810-9dad-11d1-80b4-00c04fd430c8"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("550e8400-e29b-41d4-a716-446655440000").ShouldBeTrue();
    terminal.OutputContains("6ba7b810-9dad-11d1-80b4-00c04fd430c8").ShouldBeTrue();
    terminal.OutputContains("len:2").ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests DateTime[] catch-all parameter.
  /// </summary>
  public static async Task Should_bind_datetime_array_catch_all()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("schedule {*dates:DateTime}").WithHandler((DateTime[] dates) => $"count:{dates.Length}|first:{dates[0]:yyyy-MM-dd}|second:{dates[1]:yyyy-MM-dd}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["schedule", "2024-01-15", "2024-06-30"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("count:2").ShouldBeTrue();
    terminal.OutputContains("first:2024-01-15").ShouldBeTrue();
    terminal.OutputContains("second:2024-06-30").ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests that string[] catch-all with explicit type constraint works.
  /// </summary>
  public static async Task Should_bind_explicit_string_array_catch_all()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("echo {*args:string}").WithHandler((string[] args) => $"args:[{string.Join(",", args)}]|len:{args.Length}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["echo", "hello", "world"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("args:[hello,world]").ShouldBeTrue();
    terminal.OutputContains("len:2").ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests that default string[] catch-all (no type constraint) still works.
  /// </summary>
  public static async Task Should_bind_default_string_array_catch_all_without_type()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("echo {*args}").WithHandler((string[] args) => $"args:[{string.Join(",", args)}]|len:{args.Length}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["echo", "hello", "world"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("args:[hello,world]").ShouldBeTrue();
    terminal.OutputContains("len:2").ShouldBeTrue();

    await Task.CompletedTask;
  }

  /// <summary>
  /// Tests that invalid conversion produces appropriate error.
  /// </summary>
  public static async Task Should_fail_on_invalid_int_conversion()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("sum {*numbers:int}").WithHandler((int[] numbers) => 0).AsCommand().Done()
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
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("calc {*numbers:int}").WithHandler((int[] numbers) => $"numbers:[{string.Join(",", numbers)}]|len:{numbers.Length}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["calc", "-5", "10", "-15"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("numbers:[-5,10,-15]").ShouldBeTrue();
    terminal.OutputContains("len:3").ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
