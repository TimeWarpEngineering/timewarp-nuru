#!/usr/bin/env -S dotnet --

#region Purpose
// Pins operation group resolution (route group, CLI binding, operation Group), attribute overrides, and external handlers.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.AutoGroups
{
using System.Text.Json;
using TimeWarp.Contracts;

[TestTag("Auto")]
public class OperationEndpointDerivationGroupTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OperationEndpointDerivationGroupTests>();

  // Build() and RunAsync share a field: intercept sites come from Build()'s method or later calls on it.
  private static NuruApp App = null!;

  private static void CreateApp(TestTerminal terminal)
  {
    App = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<T508ForceQuery>()
      .Map<T508PrecedenceQuery>()
      .Map<T508BothQuery>()
      .Map<T508PosQuery>()
      .Map<T508MixedQuery>()
      .Map<T508ExternalCommand>()
      .Map<T514DottedGroupQuery>()
      .Map<T514SpacedGroupQuery>()
      .Map<T514ContractWinsQuery>()
      .Map<T514BlankGroupQuery>()
      .Build();
  }

  public static async Task Should_require_a_value_for_a_non_nullable_bool_without_an_initializer()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int missing = await App.RunAsync(["t508force"]);
    missing.ShouldBe(1);

    int exitCode = await App.RunAsync(["t508force", "--Force", "true"]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("force:true").ShouldBeTrue();
  }

  public static async Task Should_prefer_a_route_group_over_nuru_cli_binding()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int winner = await App.RunAsync(["t508wins", "ping"]);
    winner.ShouldBe(0);
    terminal.OutputContains("wins").ShouldBeTrue();

    int loser = await App.RunAsync(["t508loses", "ping"]);
    loser.ShouldBe(1);
  }

  public static async Task Should_let_nuru_route_override_the_operation_name()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508legacy"]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("legacy").ShouldBeTrue();

    int canonical = await App.RunAsync(["t508canonical"]);
    canonical.ShouldBe(1);
  }

  public static async Task Should_keep_parameter_and_option_attribute_overrides()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int positional = await App.RunAsync(["t508pos", "4"]);
    positional.ShouldBe(0);
    terminal.OutputContains("left:4").ShouldBeTrue();

    int named = await App.RunAsync(["t508mixed", "--title", "hi"]);
    named.ShouldBe(0);
    terminal.OutputContains("title:hi|count:1").ShouldBeTrue();
  }

  public static async Task Should_dispatch_an_external_handler()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508external", "--Name", "ada"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("ext:ada").ShouldBeTrue();
  }

  public static async Task Should_route_an_operation_group_by_the_operation_name_only()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    (await App.RunAsync(["t514dotted"])).ShouldBe(0);
    terminal.OutputContains("dotted").ShouldBeTrue();
    (await App.RunAsync(["t514spaced"])).ShouldBe(0);
    terminal.OutputContains("spaced").ShouldBeTrue();

    (await App.RunAsync(["t514ops", "t514dotted"])).ShouldBe(1);
    (await App.RunAsync(["t514ops", "symbols", "t514dotted"])).ShouldBe(1);
  }

  public static async Task Should_let_the_operation_group_win_over_route_group_and_cli_binding()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    (await App.RunAsync(["t514wins"])).ShouldBe(0);
    terminal.OutputContains("contract").ShouldBeTrue();
    (await App.RunAsync(["t514routegroup", "t514wins"])).ShouldBe(1);
    (await App.RunAsync(["t514cligroup", "t514wins"])).ShouldBe(1);
  }

  public static async Task Should_keep_cli_binding_group_when_the_operation_group_is_blank()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    (await App.RunAsync(["t514blankcli", "t514blank"])).ShouldBe(0);
    terminal.OutputContains("blank").ShouldBeTrue();
  }

  public static async Task Should_list_the_operation_group_as_group_path()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    (await App.RunAsync(["--capabilities"])).ShouldBe(0);

    GroupPath(FindEndpoint(terminal.Output, "t514dotted")).ShouldBe(["t514ops", "symbols"]);
    GroupPath(FindEndpoint(terminal.Output, "t514spaced")).ShouldBe(["t514ops", "symbols"]);
    GroupPath(FindEndpoint(terminal.Output, "t514wins")).ShouldBe(["t514meta"]);
    GroupPath(FindEndpoint(terminal.Output, "t514blankcli t514blank")).ShouldBe(["t514blankcli"]);
  }

  public static async Task Should_filter_operation_groups_by_dotted_or_spaced_segments()
  {
    foreach (string filter in new[] { "t514ops", "t514ops.symbols", "t514ops symbols", "T514OPS.Symbols" })
    {
      using TestTerminal terminal = new();
      CreateApp(terminal);

      (await App.RunAsync(["--capabilities", "--group-filter", filter])).ShouldBe(0);

      using JsonDocument document = JsonDocument.Parse(terminal.Output);
      string?[] patterns = [.. document.RootElement.GetProperty("endpoints").EnumerateArray()
        .Select(endpoint => endpoint.GetProperty("pattern").GetString())];
      patterns.ShouldBe(["t514dotted", "t514spaced"], ignoreOrder: true);
    }

    using TestTerminal other = new();
    CreateApp(other);
    (await App.RunAsync(["--capabilities", "--group-filter", "t514ops.other"])).ShouldBe(0);
    using JsonDocument empty = JsonDocument.Parse(other.Output);
    empty.RootElement.GetProperty("endpoints").GetArrayLength().ShouldBe(0);
  }

  private static JsonElement FindEndpoint(string json, string pattern)
  {
    using JsonDocument document = JsonDocument.Parse(json);
    foreach (JsonElement endpoint in document.RootElement.GetProperty("endpoints").EnumerateArray())
    {
      if (endpoint.GetProperty("pattern").GetString() == pattern)
        return endpoint.Clone();
    }

    throw new InvalidOperationException($"Endpoint '{pattern}' was not in --capabilities.");
  }

  private static string?[] GroupPath(JsonElement endpoint) =>
    [.. endpoint.GetProperty("groupPath").EnumerateArray().Select(value => value.GetString())];
}

[Operation("t508force")]
internal sealed class T508ForceQuery : IQuery<string>
{
  public bool Force { get; set; }

  public sealed class Handler : IRequestHandler<T508ForceQuery, string>
  {
    public ValueTask<string> Handle(T508ForceQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult(query.Force ? "force:true" : "force:false");
    }
  }
}

[NuruRouteGroup("t508wins")]
internal abstract class T508WinsGroup;

[Operation("ping")]
[NuruCliBinding(Group = "t508loses")]
internal sealed class T508PrecedenceQuery : T508WinsGroup, IQuery<string>
{
  public sealed class Handler : IRequestHandler<T508PrecedenceQuery, string>
  {
    public ValueTask<string> Handle(T508PrecedenceQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return ValueTask.FromResult("wins");
    }
  }
}

[Operation("t514dotted", Group = "t514ops.symbols")]
internal sealed class T514DottedGroupQuery : IQuery<string>
{
  public sealed class Handler : IRequestHandler<T514DottedGroupQuery, string>
  {
    public ValueTask<string> Handle(T514DottedGroupQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return ValueTask.FromResult("dotted");
    }
  }
}

[Operation("t514spaced", Group = " t514ops  symbols ")]
internal sealed class T514SpacedGroupQuery : IQuery<string>
{
  public sealed class Handler : IRequestHandler<T514SpacedGroupQuery, string>
  {
    public ValueTask<string> Handle(T514SpacedGroupQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return ValueTask.FromResult("spaced");
    }
  }
}

[NuruRouteGroup("t514routegroup")]
internal abstract class T514RouteGroup;

[Operation("t514wins", Group = "t514meta")]
[NuruCliBinding(Group = "t514cligroup")]
internal sealed class T514ContractWinsQuery : T514RouteGroup, IQuery<string>
{
  public sealed class Handler : IRequestHandler<T514ContractWinsQuery, string>
  {
    public ValueTask<string> Handle(T514ContractWinsQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return ValueTask.FromResult("contract");
    }
  }
}

[Operation("t514blank", Group = "  ")]
[NuruCliBinding(Group = "t514blankcli")]
internal sealed class T514BlankGroupQuery : IQuery<string>
{
  public sealed class Handler : IRequestHandler<T514BlankGroupQuery, string>
  {
    public ValueTask<string> Handle(T514BlankGroupQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return ValueTask.FromResult("blank");
    }
  }
}

[NuruRoute("t508legacy", Description = "Legacy spelling")]
[Operation("t508canonical", Description = "Canonical name")]
internal sealed class T508BothQuery : IQuery<string>
{
  public sealed class Handler : IRequestHandler<T508BothQuery, string>
  {
    public ValueTask<string> Handle(T508BothQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return ValueTask.FromResult("legacy");
    }
  }
}

[Operation("t508pos")]
internal sealed class T508PosQuery : IQuery<string>
{
  [Parameter(Description = "Left operand")]
  public int Left { get; set; }

  public sealed class Handler : IRequestHandler<T508PosQuery, string>
  {
    public ValueTask<string> Handle(T508PosQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult($"left:{query.Left.ToString(CultureInfo.InvariantCulture)}");
    }
  }
}

[Operation("t508mixed")]
internal sealed class T508MixedQuery : IQuery<string>
{
  [Option("title", Description = "Title")]
  public string Title { get; set; } = "n";

  public int Count { get; set; } = 1;

  public sealed class Handler : IRequestHandler<T508MixedQuery, string>
  {
    public ValueTask<string> Handle(T508MixedQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult($"title:{query.Title}|count:{query.Count.ToString(CultureInfo.InvariantCulture)}");
    }
  }
}

[Operation("t508external", Description = "External handler")]
internal sealed class T508ExternalCommand : ICommand<string>
{
  public string Name { get; set; } = "world";
}

internal sealed class T508ExternalHandler : IRequestHandler<T508ExternalCommand, string>
{
  public ValueTask<string> Handle(T508ExternalCommand command, CancellationToken cancellationToken)
  {
    _ = cancellationToken;
    return ValueTask.FromResult($"ext:{command.Name}");
  }
}
} // namespace TimeWarp.Nuru.Tests.AutoGroups
