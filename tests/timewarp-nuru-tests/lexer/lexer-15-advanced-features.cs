#!/usr/bin/dotnet --

return await RunTests<AdvancedFeaturesTests>();

[TestTag("Lexer")]
public class AdvancedFeaturesTests
{
    /// <summary>
    /// Tests parameter descriptions using internal pipe: {env|Environment}
    /// Validates lexer tokenizes pipe inside braces as Identifier | Identifier
    /// Parser handles semantic extraction of description
    /// </summary>
    public static async Task Should_tokenize_parameter_with_description()
    {
        // Arrange
        string pattern = "deploy {env|Environment}";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(7);
        tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[0].Value.ShouldBe("deploy");
        tokens[1].Type.ShouldBe(RouteTokenType.LeftBrace);
        tokens[2].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[2].Value.ShouldBe("env");
        tokens[3].Type.ShouldBe(RouteTokenType.Pipe);
        tokens[3].Value.ShouldBe("|");
        tokens[4].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[4].Value.ShouldBe("Environment");
        tokens[5].Type.ShouldBe(RouteTokenType.RightBrace);
        tokens[6].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests option descriptions: --dry-run,-d|Preview
    /// Includes comma-separated short options and pipe for description
    /// </summary>
    public static async Task Should_tokenize_option_with_description()
    {
        // Arrange
        string pattern = "--dry-run,-d|Preview";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(8);
        tokens[0].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[0].Value.ShouldBe("--");
        tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[1].Value.ShouldBe("dry-run");
        tokens[2].Type.ShouldBe(RouteTokenType.Comma);
        tokens[2].Value.ShouldBe(",");
        tokens[3].Type.ShouldBe(RouteTokenType.SingleDash);
        tokens[3].Value.ShouldBe("-");
        tokens[4].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[4].Value.ShouldBe("d");
        tokens[5].Type.ShouldBe(RouteTokenType.Pipe);
        tokens[5].Value.ShouldBe("|");
        tokens[6].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[6].Value.ShouldBe("Preview");
        tokens[7].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Complex pattern mixing parameter and option descriptions
    /// Ensures lexer handles combined internal pipes correctly
    /// </summary>
    public static async Task Should_tokenize_complex_pattern_with_descriptions()
    {
        // Arrange
        string pattern = "deploy {env|Environment} --dry-run,-d|Preview";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(14);
        tokens[0].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[0].Value.ShouldBe("deploy");
        tokens[1].Type.ShouldBe(RouteTokenType.LeftBrace);
        tokens[2].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[2].Value.ShouldBe("env");
        tokens[3].Type.ShouldBe(RouteTokenType.Pipe);
        tokens[3].Value.ShouldBe("|");
        tokens[4].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[4].Value.ShouldBe("Environment");
        tokens[5].Type.ShouldBe(RouteTokenType.RightBrace);
        tokens[6].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[6].Value.ShouldBe("--");
        tokens[7].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[7].Value.ShouldBe("dry-run");
        tokens[8].Type.ShouldBe(RouteTokenType.Comma);
        tokens[8].Value.ShouldBe(",");
        tokens[9].Type.ShouldBe(RouteTokenType.SingleDash);
        tokens[9].Value.ShouldBe("-");
        tokens[10].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[10].Value.ShouldBe("d");
        tokens[11].Type.ShouldBe(RouteTokenType.Pipe);
        tokens[11].Value.ShouldBe("|");
        tokens[12].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[12].Value.ShouldBe("Preview");
        tokens[13].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests optional flag modifiers: --verbose?
    /// Question mark after option indicates optional boolean flag
    /// </summary>
    public static async Task Should_tokenize_optional_verbose_flag()
    {
        // Arrange
        string pattern = "--verbose?";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(4);
        tokens[0].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[0].Value.ShouldBe("--");
        tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[1].Value.ShouldBe("verbose");
        tokens[2].Type.ShouldBe(RouteTokenType.Question);
        tokens[2].Value.ShouldBe("?");
        tokens[3].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests optional dry-run flag with compound identifier
    /// </summary>
    public static async Task Should_tokenize_optional_dry_run_flag()
    {
        // Arrange
        string pattern = "--dry-run?";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(4);
        tokens[0].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[0].Value.ShouldBe("--");
        tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[1].Value.ShouldBe("dry-run");
        tokens[2].Type.ShouldBe(RouteTokenType.Question);
        tokens[2].Value.ShouldBe("?");
        tokens[3].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests optional short flag: -v?
    /// </summary>
    public static async Task Should_tokenize_optional_short_flag()
    {
        // Arrange
        string pattern = "-v?";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(4);
        tokens[0].Type.ShouldBe(RouteTokenType.SingleDash);
        tokens[0].Value.ShouldBe("-");
        tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[1].Value.ShouldBe("v");
        tokens[2].Type.ShouldBe(RouteTokenType.Question);
        tokens[2].Value.ShouldBe("?");
        tokens[3].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests optional flag with parameter: --config? {mode}
    /// </summary>
    public static async Task Should_tokenize_optional_flag_with_parameter()
    {
        // Arrange
        string pattern = "--config? {mode}";
        Lexer lexer = CreateLexer(pattern);
        IReadOnlyList<Token> tokens = lexer.Tokenize();

        // Assert
        tokens.Count.ShouldBe(7);
        tokens[0].Type.ShouldBe(RouteTokenType.DoubleDash);
        tokens[0].Value.ShouldBe("--");
        tokens[1].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[1].Value.ShouldBe("config");
        tokens[2].Type.ShouldBe(RouteTokenType.Question);
        tokens[2].Value.ShouldBe("?");
        tokens[3].Type.ShouldBe(RouteTokenType.LeftBrace);
        tokens[3].Value.ShouldBe("{");
        tokens[4].Type.ShouldBe(RouteTokenType.Identifier);
        tokens[4].Value.ShouldBe("mode");
        tokens[5].Type.ShouldBe(RouteTokenType.RightBrace);
        tokens[5].Value.ShouldBe("}");
        tokens[6].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tests optional flag with optional parameter: --env? {name?}
    /// </summary>
    public static async Task Should_tokenize_optional_flag_with_optional_parameter()
    {
        // Arrange
        string pattern = "--env? {name?}";
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
        tokens[4].Value.ShouldBe("name");
        tokens[5].Type.ShouldBe(RouteTokenType.Question);
        tokens[5].Value.ShouldBe("?");
        tokens[6].Type.ShouldBe(RouteTokenType.RightBrace);
        tokens[6].Value.ShouldBe("}");
        tokens[7].Type.ShouldBe(RouteTokenType.EndOfInput);

        await Task.CompletedTask;
    }

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