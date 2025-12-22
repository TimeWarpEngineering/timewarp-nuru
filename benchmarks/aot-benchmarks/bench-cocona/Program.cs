// AOT Benchmark: Cocona
// Note: Cocona was archived in December 2025
using Cocona;

CoconaApp.Run<BenchCommand>(args);

public class BenchCommand
{
    public void Run(
        [Option('s')] string? str,
        [Option('i')] int @int,
        [Option('b')] bool @bool)
    { }
}
