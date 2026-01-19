#!/usr/bin/dotnet --
#:project ../../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test CommandLineParser quote handling

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.CommandLineParserTests.QuotedStrings
{

[TestTag("CommandLineParser")]
public class CommandLineParserQuoteTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CommandLineParserQuoteTests>();

  public static async Task Should_parse_double_quoted_string()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("greet \"John Doe\"");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("greet");
    result[1].ShouldBe("John Doe");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_single_quoted_string()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("greet 'Jane Smith'");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("greet");
    result[1].ShouldBe("Jane Smith");

    await Task.CompletedTask;
  }

  public static async Task Should_preserve_spaces_inside_quotes()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("message \"Hello   World   !\"");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("message");
    result[1].ShouldBe("Hello   World   !");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_quoted_string()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("send \"\"");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("send");
    result[1].ShouldBe("");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_multiple_quoted_arguments()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("copy \"source file.txt\" \"dest folder/\"");

    // Assert
    result.Length.ShouldBe(3);
    result[0].ShouldBe("copy");
    result[1].ShouldBe("source file.txt");
    result[2].ShouldBe("dest folder/");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_mixed_quoted_and_unquoted()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("deploy \"my app\" to production");

    // Assert
    result.Length.ShouldBe(4);
    result[0].ShouldBe("deploy");
    result[1].ShouldBe("my app");
    result[2].ShouldBe("to");
    result[3].ShouldBe("production");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_escaped_quotes_inside_double_quotes()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("echo \"Hello \\\"World\\\"\"");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("echo");
    result[1].ShouldBe("Hello \"World\"");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_escaped_quotes_inside_single_quotes()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("echo 'Hello \\'World\\''");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("echo");
    result[1].ShouldBe("Hello 'World'");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_escaped_backslash()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("path \"C:\\\\Users\\\\test\"");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("path");
    result[1].ShouldBe("C:\\Users\\test");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_single_quote_inside_double_quotes()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("message \"It's working\"");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("message");
    result[1].ShouldBe("It's working");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_double_quote_inside_single_quotes()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("message 'Say \"hello\"'");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("message");
    result[1].ShouldBe("Say \"hello\"");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.CommandLineParserTests.QuotedStrings
