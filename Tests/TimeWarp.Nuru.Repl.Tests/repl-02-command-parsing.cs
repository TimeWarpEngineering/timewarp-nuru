#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

// Test command line parsing with quotes and escapes (Section 2 of REPL Test Plan)
return await RunTests<CommandParsingTests>();

[TestTag("REPL")]
public class CommandParsingTests
{
  public static async Task Should_parse_simple_command()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("status");

    // Assert
    result.Length.ShouldBe(1);
    result[0].ShouldBe("status");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_multi_word_command()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("git status");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("git");
    result[1].ShouldBe("status");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_double_quoted_string()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("greet \"John Doe\"");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("greet");
    result[1].ShouldBe("John Doe");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_single_quoted_string()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("deploy 'production'");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("deploy");
    result[1].ShouldBe("production");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_mixed_quotes()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("echo \"Hello\" 'World'");

    // Assert
    result.Length.ShouldBe(3);
    result[0].ShouldBe("echo");
    result[1].ShouldBe("Hello");
    result[2].ShouldBe("World");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_escaped_quotes()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("echo \"Hello\\\"World\"");

    // Assert
    result.Length.ShouldBe(2);
    result[0].ShouldBe("echo");
    result[1].ShouldBe("Hello\"World");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_quoted_strings()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("cmd \"\" ''");

    // Assert
    result.Length.ShouldBe(3);
    result[0].ShouldBe("cmd");
    result[1].ShouldBe("");
    result[2].ShouldBe("");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_unclosed_quotes_gracefully()
  {
    // Arrange & Act - unclosed quotes should include rest of string
    string[] result = CommandLineParser.Parse("echo \"unclosed");

    // Assert - parser should handle gracefully (no crash, return something sensible)
    result.Length.ShouldBeGreaterThanOrEqualTo(1);
    result[0].ShouldBe("echo");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_complex_mixed_pattern()
  {
    // Arrange & Act
    string[] result = CommandLineParser.Parse("docker run -d --name \"my container\" nginx");

    // Assert
    result.Length.ShouldBe(6);
    result[0].ShouldBe("docker");
    result[1].ShouldBe("run");
    result[2].ShouldBe("-d");
    result[3].ShouldBe("--name");
    result[4].ShouldBe("my container");
    result[5].ShouldBe("nginx");

    await Task.CompletedTask;
  }
}
