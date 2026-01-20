#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class AdditionalPrimitiveTypeTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<AdditionalPrimitiveTypeTests>();

  // ============================================================================
  // BYTE TESTS
  // ============================================================================

  public static async Task Should_bind_byte_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("setbyte {value:byte}").WithHandler((byte value) => $"value:{value}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setbyte", "255"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:255").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_byte_zero()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("setbyte {value:byte}").WithHandler((byte value) => $"value:{value}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setbyte", "0"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:0").ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ============================================================================
  // SBYTE TESTS
  // ============================================================================

  public static async Task Should_bind_sbyte_positive()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("setsbyte {value:sbyte}").WithHandler((sbyte value) => $"value:{value}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setsbyte", "127"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:127").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_sbyte_negative()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("setsbyte {value:sbyte}").WithHandler((sbyte value) => $"value:{value}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setsbyte", "-128"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:-128").ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ============================================================================
  // SHORT TESTS
  // ============================================================================

  public static async Task Should_bind_short_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("setshort {value:short}").WithHandler((short value) => $"value:{value}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setshort", "32767"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:32767").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_short_negative()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("setshort {value:short}").WithHandler((short value) => $"value:{value}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setshort", "-32768"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:-32768").ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ============================================================================
  // USHORT TESTS
  // ============================================================================

  public static async Task Should_bind_ushort_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("setushort {value:ushort}").WithHandler((ushort value) => $"value:{value}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setushort", "65535"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:65535").ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ============================================================================
  // UINT TESTS
  // ============================================================================

  public static async Task Should_bind_uint_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("setuint {value:uint}").WithHandler((uint value) => $"value:{value}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setuint", "4294967295"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:4294967295").ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ============================================================================
  // ULONG TESTS
  // ============================================================================

  public static async Task Should_bind_ulong_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("setulong {value:ulong}").WithHandler((ulong value) => $"value:{value}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setulong", "18446744073709551615"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:18446744073709551615").ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ============================================================================
  // FLOAT TESTS
  // ============================================================================

  public static async Task Should_bind_float_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("setfloat {value:float}").WithHandler((float value) => $"value:{value}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setfloat", "3.14"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:3.14").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_float_negative()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("setfloat {value:float}").WithHandler((float value) => $"value:{value}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setfloat", "-99.5"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:-99.5").ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ============================================================================
  // CHAR TESTS
  // ============================================================================

  public static async Task Should_bind_char_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("setchar {value:char}").WithHandler((char value) => $"value:{value}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setchar", "X"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:X").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_char_digit()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("setchar {value:char}").WithHandler((char value) => $"value:{value}").AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setchar", "7"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("value:7").ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ============================================================================
  // ARRAY TESTS (catch-all with new types)
  // ============================================================================

  public static async Task Should_bind_byte_array_catch_all()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("bytes {*values:byte}").WithHandler((byte[] values) => $"values:[{string.Join(",", values)}]|len:{values.Length}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["bytes", "10", "20", "255"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("values:[10,20,255]").ShouldBeTrue();
    terminal.OutputContains("len:3").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_short_array_catch_all()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("shorts {*values:short}").WithHandler((short[] values) => $"values:[{string.Join(",", values)}]|len:{values.Length}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["shorts", "-100", "0", "100"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("values:[-100,0,100]").ShouldBeTrue();
    terminal.OutputContains("len:3").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_bind_float_array_catch_all()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("floats {*values:float}").WithHandler((float[] values) => $"values:[{string.Join(",", values)}]|len:{values.Length}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["floats", "1.5", "2.5", "3.5"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("values:[1.5,2.5,3.5]").ShouldBeTrue();
    terminal.OutputContains("len:3").ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
