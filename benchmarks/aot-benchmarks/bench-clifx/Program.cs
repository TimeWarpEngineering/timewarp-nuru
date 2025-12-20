// AOT Benchmark: CliFx
// Class-first framework for building CLIs
using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;

return await new CliApplicationBuilder()
    .AddCommand<BenchCommand>()
    .Build()
    .RunAsync(args);

[Command]
public class BenchCommand : ICommand
{
    [CommandOption("str", 's')]
    public string? Str { get; init; }

    [CommandOption("int", 'i')]
    public int Int { get; init; }

    [CommandOption("bool", 'b')]
    public bool Bool { get; init; }

    public ValueTask ExecuteAsync(IConsole console) => default;
}
