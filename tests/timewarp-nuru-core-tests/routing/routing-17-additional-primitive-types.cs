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
    byte? boundValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("setbyte {value:byte}").WithHandler((byte value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setbyte", "255"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldBe((byte)255);

    await Task.CompletedTask;
  }

  public static async Task Should_bind_byte_zero()
  {
    // Arrange
    byte? boundValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("setbyte {value:byte}").WithHandler((byte value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setbyte", "0"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldBe((byte)0);

    await Task.CompletedTask;
  }

  // ============================================================================
  // SBYTE TESTS
  // ============================================================================

  public static async Task Should_bind_sbyte_positive()
  {
    // Arrange
    sbyte? boundValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("setsbyte {value:sbyte}").WithHandler((sbyte value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setsbyte", "127"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldBe((sbyte)127);

    await Task.CompletedTask;
  }

  public static async Task Should_bind_sbyte_negative()
  {
    // Arrange
    sbyte? boundValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("setsbyte {value:sbyte}").WithHandler((sbyte value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setsbyte", "-128"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldBe((sbyte)-128);

    await Task.CompletedTask;
  }

  // ============================================================================
  // SHORT TESTS
  // ============================================================================

  public static async Task Should_bind_short_parameter()
  {
    // Arrange
    short? boundValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("setshort {value:short}").WithHandler((short value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setshort", "32767"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldBe((short)32767);

    await Task.CompletedTask;
  }

  public static async Task Should_bind_short_negative()
  {
    // Arrange
    short? boundValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("setshort {value:short}").WithHandler((short value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setshort", "-32768"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldBe((short)-32768);

    await Task.CompletedTask;
  }

  // ============================================================================
  // USHORT TESTS
  // ============================================================================

  public static async Task Should_bind_ushort_parameter()
  {
    // Arrange
    ushort? boundValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("setushort {value:ushort}").WithHandler((ushort value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setushort", "65535"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldBe((ushort)65535);

    await Task.CompletedTask;
  }

  // ============================================================================
  // UINT TESTS
  // ============================================================================

  public static async Task Should_bind_uint_parameter()
  {
    // Arrange
    uint? boundValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("setuint {value:uint}").WithHandler((uint value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setuint", "4294967295"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldBe(4294967295U);

    await Task.CompletedTask;
  }

  // ============================================================================
  // ULONG TESTS
  // ============================================================================

  public static async Task Should_bind_ulong_parameter()
  {
    // Arrange
    ulong? boundValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("setulong {value:ulong}").WithHandler((ulong value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setulong", "18446744073709551615"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldBe(18446744073709551615UL);

    await Task.CompletedTask;
  }

  // ============================================================================
  // FLOAT TESTS
  // ============================================================================

  public static async Task Should_bind_float_parameter()
  {
    // Arrange
    float? boundValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("setfloat {value:float}").WithHandler((float value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setfloat", "3.14"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldNotBeNull();
    Math.Abs(boundValue.Value - 3.14f).ShouldBeLessThan(0.001f);

    await Task.CompletedTask;
  }

  public static async Task Should_bind_float_negative()
  {
    // Arrange
    float? boundValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("setfloat {value:float}").WithHandler((float value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setfloat", "-99.5"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldNotBeNull();
    Math.Abs(boundValue.Value - (-99.5f)).ShouldBeLessThan(0.001f);

    await Task.CompletedTask;
  }

  // ============================================================================
  // CHAR TESTS
  // ============================================================================

  public static async Task Should_bind_char_parameter()
  {
    // Arrange
    char? boundValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("setchar {value:char}").WithHandler((char value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setchar", "X"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldBe('X');

    await Task.CompletedTask;
  }

  public static async Task Should_bind_char_digit()
  {
    // Arrange
    char? boundValue = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("setchar {value:char}").WithHandler((char value) => { boundValue = value; return 0; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["setchar", "7"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValue.ShouldBe('7');

    await Task.CompletedTask;
  }

  // ============================================================================
  // ARRAY TESTS (catch-all with new types)
  // ============================================================================

  public static async Task Should_bind_byte_array_catch_all()
  {
    // Arrange
    byte[]? boundValues = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("bytes {*values:byte}").WithHandler((byte[] values) => { boundValues = values; return 0; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["bytes", "10", "20", "255"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValues.ShouldNotBeNull();
    boundValues.Length.ShouldBe(3);
    boundValues[0].ShouldBe((byte)10);
    boundValues[1].ShouldBe((byte)20);
    boundValues[2].ShouldBe((byte)255);

    await Task.CompletedTask;
  }

  public static async Task Should_bind_short_array_catch_all()
  {
    // Arrange
    short[]? boundValues = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("shorts {*values:short}").WithHandler((short[] values) => { boundValues = values; return 0; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["shorts", "-100", "0", "100"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValues.ShouldNotBeNull();
    boundValues.Length.ShouldBe(3);
    boundValues[0].ShouldBe((short)-100);
    boundValues[1].ShouldBe((short)0);
    boundValues[2].ShouldBe((short)100);

    await Task.CompletedTask;
  }

  public static async Task Should_bind_float_array_catch_all()
  {
    // Arrange
    float[]? boundValues = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("floats {*values:float}").WithHandler((float[] values) => { boundValues = values; return 0; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["floats", "1.5", "2.5", "3.5"]);

    // Assert
    exitCode.ShouldBe(0);
    boundValues.ShouldNotBeNull();
    boundValues.Length.ShouldBe(3);
    Math.Abs(boundValues[0] - 1.5f).ShouldBeLessThan(0.001f);
    Math.Abs(boundValues[1] - 2.5f).ShouldBeLessThan(0.001f);
    Math.Abs(boundValues[2] - 3.5f).ShouldBeLessThan(0.001f);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
