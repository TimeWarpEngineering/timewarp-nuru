// AOT Benchmark: Cocona.Lite
// Lightweight version without hosting dependencies
// Note: Cocona was archived in December 2025
using Cocona;

CoconaLiteApp.Run<BenchCommand>(args);

public class BenchCommand
{
    public void Run(
        [Option('s')] string? str,
        [Option('i')] int @int,
        [Option('b')] bool @bool)
    { }
}
