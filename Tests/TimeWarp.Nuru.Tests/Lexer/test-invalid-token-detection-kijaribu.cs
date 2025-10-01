#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../../Source/TimeWarp.Kijaribu/TimeWarp.Kijaribu.csproj

using TimeWarp.Nuru.Parsing;
using Shouldly;
using TimeWarp.Kijaribu;

return await TestRunner.RunTests<InvalidTokenDetectionTests>();

[TestTag("Lexer")]
public class InvalidTokenDetectionTests
{
  // Valid patterns that should NOT produce Invalid tokens
  [Input("dry-run")]
  [Input("no-edit")]
  [Input("save-dev")]
  [Input("my-long-command")]
  [Input("--dry-run")]
  [Input("--no-edit")]
  [Input("--save-dev")]
  [Input("-h")]
  [Input("-v")]
  [Input("git commit --amend")]
  [Input("deploy --dry-run")]
  [Input("exec --")]
  [Input("git log -- {*files}")]
  public static async Task ValidPatternsShouldNotProduceInvalidTokens(string pattern)
  {
    // Arrange
    var lexer = new RoutePatternLexer(pattern);

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    bool hasInvalidToken = tokens.Any(t => t.Type == TokenType.Invalid);
    hasInvalidToken.ShouldBeFalse($"Pattern '{pattern}' should not produce Invalid tokens");

    await Task.CompletedTask;
  }

  // Invalid patterns that SHOULD produce Invalid tokens
  [Input("test--case")]
  [Input("foo--bar--baz")]
  [Input("my--option")]
  [Input("test-")]
  [Input("test--")]
  [Input("foo---")]
  [Input("test<param>")]
  [Input("<input>")]
  [Input("-test")]
  [Input("-foo-bar")]
  public static async Task InvalidPatternsShouldProduceInvalidTokens(string pattern)
  {
    // Arrange
    var lexer = new RoutePatternLexer(pattern);

    // Act
    IReadOnlyList<Token> tokens = lexer.Tokenize();

    // Assert
    bool hasInvalidToken = tokens.Any(t => t.Type == TokenType.Invalid);
    hasInvalidToken.ShouldBeTrue($"Pattern '{pattern}' should produce Invalid tokens");

    await Task.CompletedTask;
  }
}
