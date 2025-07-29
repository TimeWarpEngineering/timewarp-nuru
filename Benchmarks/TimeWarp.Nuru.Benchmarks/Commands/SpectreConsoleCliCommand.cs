namespace TimeWarp.Nuru.Benchmarks.Commands;

using Spectre.Console.Cli;
using System.ComponentModel;

public class SpectreConsoleCliCommand : Command<SpectreConsoleCliCommand.Settings>
{
  public class Settings : CommandSettings
  {
    [CommandOption("--str|-s")]
    [Description("String option")]
    public string? Str { get; set; }

    [CommandOption("--int|-i")]
    [Description("Integer option")]
    public int Int { get; set; }

    [CommandOption("--bool|-b")]
    [Description("Boolean option")]
    public bool Bool { get; set; }
  }

  public override int Execute(CommandContext context, Settings settings)
  {
    return 0;
  }
}
