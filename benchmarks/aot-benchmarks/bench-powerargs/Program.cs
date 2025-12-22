// AOT Benchmark: PowerArgs
// Uses Windows-style /name:value format by default
using PowerArgs;

// PowerArgs uses /name:value format
string[] powerArgsArgs = args.Length > 0 ? args : ["/str:hello", "/int:13", "/bool"];
Args.InvokeMain<BenchCommand>(powerArgsArgs);

public class BenchCommand
{
    [ArgShortcut("str")]
    public string? Str { get; set; }

    [ArgShortcut("int")]
    public int Int { get; set; }

    [ArgShortcut("bool")]
    public bool Bool { get; set; }

    public void Main() { }
}
