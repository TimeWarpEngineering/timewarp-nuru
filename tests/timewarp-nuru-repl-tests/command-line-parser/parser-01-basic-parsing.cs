#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Test CommandLineParser basic parsing functionality

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.CommandLineParserTests.BasicParsing
{

[TestTag("CommandLineParser")]
public class CommandLineParserBasicTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CommandLineParserBasicTests>();

  public static async Task Should_parse_simple_command()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("status");

    // Assert
    result.Length.ShouldBe(1);
    result[0].ShouldBe("status");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_command_with_argument()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("greet John");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("greet");
    result[1].ShouldBe("John");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_multiple_arguments()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("deploy staging v1.0 fast");

    // Assert
    result.Length.ShouldBe(4);
    result[0].ShouldBe("deploy");
    result[1].ShouldBe("staging");
    result[2].ShouldBe("v1.0");
    result[3].ShouldBe("fast");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_input()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("");

    // Assert
    result.Length.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_whitespace_only_input()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("   \t  ");

    // Assert
    result.Length.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_multiple_spaces_between_arguments()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("greet    John    Doe");

    // Assert
    result.Length.ShouldBe(3);
    result[0].ShouldBe("greet");
    result[1].ShouldBe("John");
    result[2].ShouldBe("Doe");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_leading_and_trailing_whitespace()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("  status  ");

    // Assert
    result.Length.ShouldBe(1);
    result[0].ShouldBe("status");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_options_with_dashes()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("build --config Release --verbose");

    // Assert
    result.Length.ShouldBe(4);
    result[0].ShouldBe("build");
    result[1].ShouldBe("--config");
    result[2].ShouldBe("Release");
    result[3].ShouldBe("--verbose");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.CommandLineParserTests.BasicParsing
