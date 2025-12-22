// AOT Benchmark: Spectre.Console.Cli
// Beautiful console UI library with CLI support
using Spectre.Console.Cli;
using System.ComponentModel;

var app = new CommandApp<BenchCommand>();
return app.Run(args);

public sealed class BenchCommand : Command<BenchCommand.Settings>
{
    public sealed class Settings : CommandSettings
    {
        [CommandOption("-s|--str")]
        public string? Str { get; init; }

        [CommandOption("-i|--int")]
        [DefaultValue(0)]
        public int Int { get; init; }

        [CommandOption("-b|--bool")]
        public bool Bool { get; init; }
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken) => 0;
}
