#!/usr/bin/dotnet --

// Clear cache to ensure parser changes are picked up (parsing is source-compiled)
await TestRunner.RunTests<EndOfOptionsTests>(clearCache: true);

[TestTag("Parser")]
public class EndOfOptionsTests
{
  // Valid patterns with -- separator
  [Input("exec -- {*cmd}")]
  [Input("git log -- {*files}")]
  [Input("docker exec {container} -- {*cmd}")]
  [Input("exec --env {e} -- {*cmd}")]
  [Input("exec --env {e}* -- {*cmd}")]
  [Input("-- {*args}")]
  public static async Task ValidEndOfOptionsPatternsShouldParse(string pattern)
  {
    // Act
    CompiledRoute route = PatternParser.Parse(pattern);

    // Assert
    route.ShouldNotBeNull();
    await Task.CompletedTask;
  }

  // Invalid patterns with -- separator
  [Input("exec -- {param}")]
  [Input("exec -- {param?}")]
  [Input("exec -- {param:int}")]
  [Input("exec --")]
  [Input("exec -- --verbose")]
  [Input("exec -- {*args} --verbose")]
  [Input("exec -- {*args} {other}")]
  [Input("--")]
  [Input("exec {cmd} -- {*args} -- {*more}")]
  public static async Task InvalidEndOfOptionsPatternsShouldFail(string pattern)
  {
    // Act & Assert
    Should.Throw<PatternException>(() =>
    {
      CompiledRoute route = PatternParser.Parse(pattern);
    });

    await Task.CompletedTask;
  }
}