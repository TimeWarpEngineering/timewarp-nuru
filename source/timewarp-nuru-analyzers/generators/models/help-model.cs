namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Configuration options for help output generation.
/// </summary>
/// <param name="ShowHeader">Whether to show application header</param>
/// <param name="ShowUsage">Whether to show usage line</param>
/// <param name="ShowCommands">Whether to show command list</param>
/// <param name="ShowOptions">Whether to show global options</param>
/// <param name="GroupByCategory">Whether to group commands by category</param>
/// <param name="MaxWidth">Maximum width for help output (0 = auto)</param>
/// <param name="IndentSize">Number of spaces for indentation</param>
internal sealed record HelpModel(
  bool ShowHeader,
  bool ShowUsage,
  bool ShowCommands,
  bool ShowOptions,
  bool GroupByCategory,
  int MaxWidth,
  int IndentSize)
{
  /// <summary>
  /// Default help configuration.
  /// </summary>
  public static readonly HelpModel Default = new(
    ShowHeader: true,
    ShowUsage: true,
    ShowCommands: true,
    ShowOptions: true,
    GroupByCategory: false,
    MaxWidth: 0,
    IndentSize: 2);
}
