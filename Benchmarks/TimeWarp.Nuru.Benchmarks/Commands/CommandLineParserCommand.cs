using CommandLine;

namespace TimeWarp.Nuru.Benchmarks.Commands;

public class CommandLineParserCommand
{
    [Option('s', "str", Required = false)]
    public string? Str { get; set; }

    [Option('i', "int", Required = false)]
    public int Int { get; set; }

    [Option('b', "bool", Required = false)]
    public bool Bool { get; set; }

    public void Execute()
    {
    }
}