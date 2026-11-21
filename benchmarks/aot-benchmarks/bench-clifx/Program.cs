// AOT Benchmark: CliFx
// Class-first framework for building CLIs (CliFx 3: source-generated command descriptors)
using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;

return await new CommandLineApplicationBuilder()
    .AddCommandsFromThisAssembly()
    .Build()
    .RunAsync(args);

[Command]
public partial class BenchCommand : ICommand
{
    [CommandOption("str", 's')]
    public string? Str { get; set; }

    [CommandOption("int", 'i')]
    public int Int { get; set; }

    [CommandOption("bool", 'b')]
    public bool Bool { get; set; }

    public ValueTask ExecuteAsync(IConsole console) => default;
}
