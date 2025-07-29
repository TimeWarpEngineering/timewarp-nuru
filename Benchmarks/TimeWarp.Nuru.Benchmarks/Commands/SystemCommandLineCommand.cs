namespace TimeWarp.Nuru.Benchmarks.Commands;

using System.CommandLine;
using System.CommandLine.NamingConventionBinder;

public class SystemCommandLineCommand
{
  public static int ExecuteHandler(string s, int i, bool b) => 0;

  public static int Execute(string[] args)
  {
    var command = new RootCommand
        {
            new Option<string?>("--str", ["-s"]),
            new Option<int>("--int", ["-i"]),
            new Option<bool>("--bool", ["-b"]),
        };

    command.SetAction(parseResult =>
    {
      BindingHandler handler = CommandHandler.Create(ExecuteHandler);
      return handler.InvokeAsync(parseResult);
    });

    ParseResult parseResult = command.Parse(args);
    return parseResult.Invoke();
  }
}
