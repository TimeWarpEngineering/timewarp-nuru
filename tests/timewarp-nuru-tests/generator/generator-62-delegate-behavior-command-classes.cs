#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Two apps in one file, both with behaviors on delegate routes (task 508-008).
// The generated command class for a delegate route was named from the route's index across
// all apps, but the behavior code used the index within the app. The second app's behavior
// then built the first app's command class, which does not compile when the routes differ.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.DelegateBehaviorCommandClasses
{
using System.Collections.ObjectModel;

[TestTag("Generator")]
public class DelegateBehaviorCommandClassTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DelegateBehaviorCommandClassTests>();

  public static async Task Should_compile_and_dispatch_two_apps_with_behaviors_on_delegate_routes()
  {
    // Arrange
    DbcTrace.Commands.Clear();

    using TestTerminal greetTerminal = new();
    NuruApp greetApp = NuruApp.CreateBuilder()
      .UseTerminal(greetTerminal)
      .AddBehavior(typeof(DbcTraceBehavior))
      .Map("dbc-greet {name}").WithHandler((string name) => $"hello {name}").AsQuery().Done()
      .Build();

    using TestTerminal deployTerminal = new();
    NuruApp deployApp = NuruApp.CreateBuilder()
      .UseTerminal(deployTerminal)
      .AddBehavior(typeof(DbcTraceBehavior))
      .Map("dbc-deploy {env} --replicas {replicas:int}").WithHandler((string env, int replicas) => $"deploy {env} x{replicas}").AsCommand().Done()
      .Map("dbc-status").WithHandler(() => "ok").AsQuery().Done()
      .Build();

    // Act
    int greetExit = await greetApp.RunAsync(["dbc-greet", "ada"]);
    int deployExit = await deployApp.RunAsync(["dbc-deploy", "prod", "--replicas", "3"]);
    int statusExit = await deployApp.RunAsync(["dbc-status"]);

    // Assert
    greetExit.ShouldBe(0);
    deployExit.ShouldBe(0);
    statusExit.ShouldBe(0);
    greetTerminal.Output.ShouldContain("hello ada");
    deployTerminal.Output.ShouldContain("deploy prod x3");
    deployTerminal.Output.ShouldContain("ok");

    DbcTrace.Commands.Count.ShouldBe(3);
    DbcTrace.Commands[0].ShouldBe("dbc-greet {name}|Name=ada");
    DbcTrace.Commands[1].ShouldBe("dbc-deploy {env} --replicas {replicas:int}|Env=prod,Replicas=3");
    DbcTrace.Commands[2].ShouldBe("dbc-status|");
  }
}

public static class DbcTrace
{
  public static Collection<string> Commands { get; } = [];
}

// Records the route and the generated command's public properties, read by reflection on the
// instance the behavior receives, so each app must hand its behaviors its own command class.
public sealed class DbcTraceBehavior : INuruBehavior
{
  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(proceed);

    object? command = context.Command;
    string properties = command is null
      ? ""
      : string.Join(",", command.GetType().GetProperties()
        .OrderBy(p => p.Name, StringComparer.Ordinal)
        .Select(p => $"{p.Name}={p.GetValue(command)}"));
    DbcTrace.Commands.Add($"{context.CommandName}|{properties}");

    await proceed();
  }
}

}
