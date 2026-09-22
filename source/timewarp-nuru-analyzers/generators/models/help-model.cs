namespace TimeWarp.Nuru.Generators;

#region Purpose
// Compile-time snapshot of HelpOptions extracted from ConfigureHelp(Action<>).
#endregion

#region Design
// Properties mirror TimeWarp.Nuru.HelpOptions (filtering), not the unused ShowHeader /
// ShowUsage layout flags that never reached HelpEmitter. Defaults match HelpOptions:
// per-command help routes, REPL commands, and completion routes are hidden unless opted in.
#endregion

/// <summary>
/// Configuration options for help output filtering.
/// </summary>
/// <param name="ShowPerCommandHelpRoutes">Whether to list per-command <c>--help</c> routes.</param>
/// <param name="ShowReplCommandsInCli">Whether to list REPL commands in CLI <c>--help</c>.</param>
/// <param name="ShowCompletionRoutes">Whether to list shell completion infrastructure routes.</param>
/// <param name="ExcludePatterns">Wildcard patterns that hide matching command rows.</param>
public sealed record HelpModel(
  bool ShowPerCommandHelpRoutes,
  bool ShowReplCommandsInCli,
  bool ShowCompletionRoutes,
  EquatableArray<string> ExcludePatterns)
{
  /// <summary>
  /// Default help configuration matching <c>HelpOptions</c> defaults.
  /// </summary>
  public static readonly HelpModel Default = new(
    ShowPerCommandHelpRoutes: false,
    ShowReplCommandsInCli: false,
    ShowCompletionRoutes: false,
    ExcludePatterns: []);
}
