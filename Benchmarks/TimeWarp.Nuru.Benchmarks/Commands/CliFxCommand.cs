namespace TimeWarp.Nuru.Benchmarks.Commands;

using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

[Command]
public class CliFxCommand : ICommand
{
  [CommandOption("str", 's')]
  public string? StrOption { get; set; }

  [CommandOption("int", 'i')]
  public int IntOption { get; set; }

  [CommandOption("bool", 'b')]
  public bool BoolOption { get; set; }

  public ValueTask ExecuteAsync(IConsole console) => ValueTask.CompletedTask;
}
