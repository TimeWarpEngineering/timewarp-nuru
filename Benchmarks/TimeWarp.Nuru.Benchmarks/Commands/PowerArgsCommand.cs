namespace TimeWarp.Nuru.Benchmarks.Commands;

using PowerArgs;

public class PowerArgsCommand
{
  public string? str { get; set; }

  [ArgShortcut("-i")]
  public int @int { get; set; }

  [ArgShortcut("-b")]
  public bool @bool { get; set; }

  public void Main()
  {
  }
}
