// AOT Benchmark: System.CommandLine
// Microsoft's official CLI library
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

var strOption = new Option<string?>("--str", ["-s"]);
var intOption = new Option<int>("--int", ["-i"]);
var boolOption = new Option<bool>("--bool", ["-b"]);

RootCommand root = [strOption, intOption, boolOption];

root.SetAction(parseResult =>
{
    BindingHandler handler = CommandHandler.Create((string? s, int i, bool b) => 0);
    return handler.InvokeAsync(parseResult);
});

ParseResult parseResult = root.Parse(args);
return parseResult.Invoke();
