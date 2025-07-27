using McMaster.Extensions.CommandLineUtils;

namespace TimeWarp.Nuru.Benchmarks.Commands;

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