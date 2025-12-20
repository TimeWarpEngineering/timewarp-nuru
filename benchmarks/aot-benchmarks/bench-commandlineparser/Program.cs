// AOT Benchmark: CommandLineParser
// Attribute-based argument parsing
using CommandLine;

Parser.Default.ParseArguments<BenchOptions>(args)
    .WithParsed(opts => { });

public class BenchOptions
{
    [Option('s', "str")]
    public string? Str { get; set; }

    [Option('i', "int")]
    public int Int { get; set; }

    [Option('b', "bool")]
    public bool Bool { get; set; }
}
