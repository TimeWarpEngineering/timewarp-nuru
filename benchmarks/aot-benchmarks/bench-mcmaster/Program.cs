// AOT Benchmark: McMaster.Extensions.CommandLineUtils
// Attribute-based CLI framework
using McMaster.Extensions.CommandLineUtils;

return CommandLineApplication.Execute<BenchCommand>(args);

[Command]
public class BenchCommand
{
    [Option("-s|--str")]
    public string? Str { get; set; }

    [Option("-i|--int")]
    public int Int { get; set; }

    [Option("-b|--bool")]
    public bool Bool { get; set; }

    public void OnExecute() { }
}
