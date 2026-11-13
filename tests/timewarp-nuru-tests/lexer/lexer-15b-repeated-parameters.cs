#!/usr/bin/env -S dotnet --

#region Purpose
// Pins lexer tokenization of repeated parameters and optional/repeated flag combinations; split from lexer-15-advanced-features.cs.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Lexer")]
public class RepeatedParametersTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RepeatedParametersTests>();

    /// <summary>
    /// Tests repeated parameter modifier: {var}*
    /// Asterisk after closing brace indicates repeatable param
    /// </summary>
    public static async Task Should_tokenize_repeated_parameter()
    {
        // Arrange
        string pattern = "--env {var}*";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(7);
        tokens[0].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[0].Value.ShouldBe("--");
        tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[1].Value.ShouldBe("env");
        tokens[2].Type.ShouldBe(RouteTokenType.LeftBrace);
        tokens[2].Value.ShouldBe("{");
        tokens[3].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[3].Value.ShouldBe("var");
        tokens[4].Type.ShouldBe(RouteTokenType.RightBrace);
        tokens[4].Value.ShouldBe("}");
        tokens[5].Type.ShouldBe(RouteTokenType.Asterisk);
        tokens[5].Value.ShouldBe("*");
        tokens[6].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests repeated typed parameter: {p:int}*
    /// </summary>
    public static async Task Should_tokenize_repeated_typed_parameter()
    {
        // Arrange
        string pattern = "--port {p:int}*";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(9);
        tokens[0].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[0].Value.ShouldBe("--");
        tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[1].Value.ShouldBe("port");
        tokens[2].Type.ShouldBe(RouteTokenType.LeftBrace);
        tokens[2].Value.ShouldBe("{");
        tokens[3].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[3].Value.ShouldBe("p");
        tokens[4].Type.ShouldBe(RouteTokenType.Colon);
        tokens[4].Value.ShouldBe(":");
        tokens[5].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[5].Value.ShouldBe("int");
        tokens[6].Type.ShouldBe(RouteTokenType.RightBrace);
        tokens[6].Value.ShouldBe("}");
        tokens[7].Type.ShouldBe(RouteTokenType.Asterisk);
        tokens[7].Value.ShouldBe("*");
        tokens[8].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests multiple repeated parameters
    /// </summary>
    public static async Task Should_tokenize_multiple_repeated_parameters()
    {
        // Arrange
        string pattern = "--label {l}* --tag {t}*";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(13);
        tokens[0].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[0].Value.ShouldBe("--");
        tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[1].Value.ShouldBe("label");
        tokens[2].Type.ShouldBe(RouteTokenType.LeftBrace);
        tokens[2].Value.ShouldBe("{");
        tokens[3].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[3].Value.ShouldBe("l");
        tokens[4].Type.ShouldBe(RouteTokenType.RightBrace);
        tokens[4].Value.ShouldBe("}");
        tokens[5].Type.ShouldBe(RouteTokenType.Asterisk);
        tokens[5].Value.ShouldBe("*");
        tokens[6].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[6].Value.ShouldBe("--");
        tokens[7].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[7].Value.ShouldBe("tag");
        tokens[8].Type.ShouldBe(RouteTokenType.LeftBrace);
        tokens[8].Value.ShouldBe("{");
        tokens[9].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[9].Value.ShouldBe("t");
        tokens[10].Type.ShouldBe(RouteTokenType.RightBrace);
        tokens[10].Value.ShouldBe("}");
        tokens[11].Type.ShouldBe(RouteTokenType.Asterisk);
        tokens[11].Value.ShouldBe("*");
        tokens[12].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests combined optional flag and repeated parameter
    /// </summary>
    public static async Task Should_tokenize_optional_flag_with_repeated_parameter()
    {
        // Arrange
        string pattern = "--env? {var}*";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(8);
        tokens[0].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[0].Value.ShouldBe("--");
        tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[1].Value.ShouldBe("env");
        tokens[2].Type.ShouldBe(RouteTokenType.Question);
        tokens[2].Value.ShouldBe("?");
        tokens[3].Type.ShouldBe(RouteTokenType.LeftBrace);
        tokens[3].Value.ShouldBe("{");
        tokens[4].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[4].Value.ShouldBe("var");
        tokens[5].Type.ShouldBe(RouteTokenType.RightBrace);
        tokens[5].Value.ShouldBe("}");
        tokens[6].Type.ShouldBe(RouteTokenType.Asterisk);
        tokens[6].Value.ShouldBe("*");
        tokens[7].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests complex optional repeated combination: --opt? {val?}*
    /// </summary>
    public static async Task Should_tokenize_complex_optional_repeated_combination()
    {
        // Arrange
        string pattern = "--opt? {val?}*";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(9);
        tokens[0].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[0].Value.ShouldBe("--");
        tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[1].Value.ShouldBe("opt");
        tokens[2].Type.ShouldBe(RouteTokenType.Question);
        tokens[2].Value.ShouldBe("?");
        tokens[3].Type.ShouldBe(RouteTokenType.LeftBrace);
        tokens[3].Value.ShouldBe("{");
        tokens[4].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[4].Value.ShouldBe("val");
        tokens[5].Type.ShouldBe(RouteTokenType.Question);
        tokens[5].Value.ShouldBe("?");
        tokens[6].Type.ShouldBe(RouteTokenType.RightBrace);
        tokens[6].Value.ShouldBe("}");
        tokens[7].Type.ShouldBe(RouteTokenType.Asterisk);
        tokens[7].Value.ShouldBe("*");
        tokens[8].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests optional repeated flag: --flag?*
    /// </summary>
    public static async Task Should_tokenize_optional_repeated_flag()
    {
        // Arrange
        string pattern = "--flag?*";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(5);
        tokens[0].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[0].Value.ShouldBe("--");
        tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[1].Value.ShouldBe("flag");
        tokens[2].Type.ShouldBe(RouteTokenType.Question);
        tokens[2].Value.ShouldBe("?");
        tokens[3].Type.ShouldBe(RouteTokenType.Asterisk);
        tokens[3].Value.ShouldBe("*");
        tokens[4].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests deploy with multiple optional flags
    /// </summary>
    public static async Task Should_tokenize_deploy_with_multiple_optional_flags()
    {
        // Arrange
        string pattern = "deploy {env} --force? --dry-run?";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(11);
        tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[0].Value.ShouldBe("deploy");
        tokens[1].Type.ShouldBe(RouteTokenType.LeftBrace);
        tokens[1].Value.ShouldBe("{");
        tokens[2].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[2].Value.ShouldBe("env");
        tokens[3].Type.ShouldBe(RouteTokenType.RightBrace);
        tokens[3].Value.ShouldBe("}");
        tokens[4].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[4].Value.ShouldBe("--");
        tokens[5].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[5].Value.ShouldBe("force");
        tokens[6].Type.ShouldBe(RouteTokenType.Question);
        tokens[6].Value.ShouldBe("?");
        tokens[7].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[7].Value.ShouldBe("--");
        tokens[8].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[8].Value.ShouldBe("dry-run");
        tokens[9].Type.ShouldBe(RouteTokenType.Question);
        tokens[9].Value.ShouldBe("?");
        tokens[10].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests docker with optional repeated and catch-all
    /// </summary>
    public static async Task Should_tokenize_docker_with_optional_repeated_and_catchall()
    {
        // Arrange
        string pattern = "docker --env? {e}* {*cmd}";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(13);
        tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[0].Value.ShouldBe("docker");
        tokens[1].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[1].Value.ShouldBe("--");
        tokens[2].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[2].Value.ShouldBe("env");
        tokens[3].Type.ShouldBe(RouteTokenType.Question);
        tokens[3].Value.ShouldBe("?");
        tokens[4].Type.ShouldBe(RouteTokenType.LeftBrace);
        tokens[4].Value.ShouldBe("{");
        tokens[5].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[5].Value.ShouldBe("e");
        tokens[6].Type.ShouldBe(RouteTokenType.RightBrace);
        tokens[6].Value.ShouldBe("}");
        tokens[7].Type.ShouldBe(RouteTokenType.Asterisk);
        tokens[7].Value.ShouldBe("*");
        tokens[8].Type.ShouldBe(RouteTokenType.LeftBrace);
        tokens[8].Value.ShouldBe("{");
        tokens[9].Type.ShouldBe(RouteTokenType.Asterisk);
        tokens[9].Value.ShouldBe("*");
        tokens[10].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[10].Value.ShouldBe("cmd");
        tokens[11].Type.ShouldBe(RouteTokenType.RightBrace);
        tokens[11].Value.ShouldBe("}");
        tokens[12].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }
}
