#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

using System.Net;

// Test built-in type converters including new types from issue #62
return await RunTests<BuiltInTypeConversionTests>(clearCache: true);

[TestTag("TypeConversion")]
[ClearRunfileCache]
public class BuiltInTypeConversionTests
{
  public static async Task Should_convert_uri_absolute()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("https://example.com/path", "uri", out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    result.ShouldBeOfType<Uri>();
    Uri uri = (Uri)result;
    uri.AbsoluteUri.ShouldBe("https://example.com/path");

    await Task.CompletedTask;
  }

  public static async Task Should_convert_uri_relative()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("/relative/path", "uri", out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    result.ShouldBeOfType<Uri>();
    Uri uri = (Uri)result;
    uri.IsAbsoluteUri.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_convert_fileinfo()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("/tmp/test.txt", "fileinfo", out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    result.ShouldBeOfType<FileInfo>();
    FileInfo fileInfo = (FileInfo)result;
    fileInfo.Name.ShouldBe("test.txt");

    await Task.CompletedTask;
  }

  public static async Task Should_convert_fileinfo_pascalcase()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act - Test PascalCase constraint name
    bool success = registry.TryConvert("/tmp/data.json", "FileInfo", out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    result.ShouldBeOfType<FileInfo>();

    await Task.CompletedTask;
  }

  public static async Task Should_convert_directoryinfo()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("/tmp/mydir", "directoryinfo", out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    result.ShouldBeOfType<DirectoryInfo>();
    DirectoryInfo dirInfo = (DirectoryInfo)result;
    dirInfo.Name.ShouldBe("mydir");

    await Task.CompletedTask;
  }

  public static async Task Should_convert_ipaddress_ipv4()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("192.168.1.1", "ipaddress", out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    result.ShouldBeOfType<IPAddress>();
    IPAddress ip = (IPAddress)result;
    ip.ToString().ShouldBe("192.168.1.1");

    await Task.CompletedTask;
  }

  public static async Task Should_convert_ipaddress_ipv6()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("::1", "ipaddress", out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    result.ShouldBeOfType<IPAddress>();
    IPAddress ip = (IPAddress)result;
    ip.ToString().ShouldBe("::1");

    await Task.CompletedTask;
  }

  public static async Task Should_reject_invalid_ipaddress()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("not-an-ip", "ipaddress", out object? result);

    // Assert
    success.ShouldBeFalse();
    result.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_convert_dateonly()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("2024-12-25", "dateonly", out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    result.ShouldBeOfType<DateOnly>();
    DateOnly date = (DateOnly)result;
    date.Year.ShouldBe(2024);
    date.Month.ShouldBe(12);
    date.Day.ShouldBe(25);

    await Task.CompletedTask;
  }

  public static async Task Should_convert_timeonly()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("14:30:00", "timeonly", out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    result.ShouldBeOfType<TimeOnly>();
    TimeOnly time = (TimeOnly)result;
    time.Hour.ShouldBe(14);
    time.Minute.ShouldBe(30);
    time.Second.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_invalid_dateonly()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("not-a-date", "dateonly", out object? result);

    // Assert
    success.ShouldBeFalse();
    result.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_invalid_timeonly()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("not-a-time", "timeonly", out object? result);

    // Assert
    success.ShouldBeFalse();
    result.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_convert_nullable_uri()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("https://example.com", "uri?", out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    result.ShouldBeOfType<Uri>();

    await Task.CompletedTask;
  }

  public static async Task Should_convert_nullable_dateonly()
  {
    // Arrange
    TypeConverterRegistry registry = new();

    // Act
    bool success = registry.TryConvert("2024-01-01", "dateonly?", out object? result);

    // Assert
    success.ShouldBeTrue();
    result.ShouldNotBeNull();
    result.ShouldBeOfType<DateOnly>();

    await Task.CompletedTask;
  }
}
