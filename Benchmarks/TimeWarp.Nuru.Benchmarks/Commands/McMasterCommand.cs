namespace TimeWarp.Nuru.Benchmarks.Commands;

using McMaster.Extensions.CommandLineUtils;

public class McMasterCommand
{
  [Option(ShortName = "s", LongName = "str")]
  public string? Str { get; set; }

  [Option(ShortName = "i", LongName = "int")]
  public int Int { get; set; }

  [Option(ShortName = "b", LongName = "bool")]
  public bool Bool { get; set; }

  private void OnExecute()
  {
  }
}
