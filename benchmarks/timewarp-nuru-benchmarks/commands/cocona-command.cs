namespace TimeWarp.Nuru.Benchmarks.Commands;

using Cocona;

public class CoconaCommand
{
  public void Execute(
      [Option('s')] string? str,
      [Option('i')] int @int,
      [Option('b')] bool @bool)
  {
  }
}
