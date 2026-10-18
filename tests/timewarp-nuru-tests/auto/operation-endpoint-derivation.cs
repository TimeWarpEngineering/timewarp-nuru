#!/usr/bin/env -S dotnet --
#pragma warning disable CA1062 // Handlers trust the mediator request instance
#pragma warning disable CA1819 // Repeated and JSON members are arrays on purpose
#pragma warning disable CA1805 // `= false` is the flag initializer this task derives

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Auto
{
using System.ComponentModel;
using System.Text.Json;
using TimeWarp.Contracts;

/// <summary>
/// Derives a Nuru endpoint from [Operation] plus a mediator kind.
/// </summary>
[TestTag("Auto")]
public class OperationEndpointDerivationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OperationEndpointDerivationTests>();

  // Build() and RunAsync must share a field. The generator records intercept sites from the
  // method that contains Build(), or from later calls on the field that Build() assigns.
  private static NuruApp App = null!;

  private static void CreateApp(TestTerminal terminal)
  {
    App = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();
  }

  public static async Task Should_route_from_the_operation_name_and_member_options()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508quote", "--Title", "hi", "--Label", "loud", "--Count", "9"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("title:hi|label:loud|count:9").ShouldBeTrue();
  }

  public static async Task Should_keep_initializer_defaults_when_options_are_omitted()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508quote", "--Title", "hi"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("title:hi|label:plain|count:4").ShouldBeTrue();
  }

  public static async Task Should_reject_a_missing_required_member()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508quote"]);

    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
  }

  public static async Task Should_keep_a_true_bool_initializer_when_the_option_is_omitted()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508bool"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("analyzers:true|verbose:false|maybe:null").ShouldBeTrue();
  }

  public static async Task Should_accept_true_and_false_for_a_bool_initializer()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508bool", "--IncludeAnalyzers", "false", "--Verbose", "--Maybe", "true"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("analyzers:false|verbose:true|maybe:true").ShouldBeTrue();
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

  public static async Task Should_keep_a_non_nullable_enum_initializer_and_require_one_without()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int missing = await App.RunAsync(["t508enum"]);
    missing.ShouldBe(1);

    int exitCode = await App.RunAsync(["t508enum", "--RequiredMode", "Slow"]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:Fast|required:Slow").ShouldBeTrue();
  }

  public static async Task Should_parse_a_non_nullable_enum_value()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508enum", "--Mode", "Slow", "--RequiredMode", "Fast"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:Slow|required:Fast").ShouldBeTrue();
  }

  public static async Task Should_repeat_scalar_arrays()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(
      ["t508repeat", "--Tags", "a", "--Tags", "b", "--Numbers", "2", "--Numbers", "3"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("tags:a,b|numbers:2,3").ShouldBeTrue();
  }

  public static async Task Should_reject_argv_for_a_json_member_and_accept_json_args()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int rejected = await App.RunAsync(["t508json", "--Notes", "nope"]);
    rejected.ShouldBe(1);
    terminal.ErrorContains("--Notes is JSON. Pass it with --json-args.").ShouldBeTrue();

    int exitCode = await App.RunAsync(
      ["t508json", "--json-args", """{"Notes":[{"Path":"a.cs","Version":2}]}"""]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("notes:a.cs:2").ShouldBeTrue();
  }

  public static async Task Should_leave_an_omitted_json_member_empty()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508json"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("notes:0").ShouldBeTrue();
  }

  public static async Task Should_take_the_group_from_a_containing_route_group()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508calc", "add", "--X", "2", "--Y", "3"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("sum:5").ShouldBeTrue();
  }

  public static async Task Should_take_the_group_from_nuru_cli_binding_when_no_route_group_is_present()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508tools", "status"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("tools").ShouldBeTrue();
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

  public static async Task Should_describe_derived_endpoints_with_the_same_capability_shape()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["--capabilities"]);
    exitCode.ShouldBe(0);

    JsonElement quote = FindEndpoint(terminal.Output, "t508quote");
    quote.GetProperty("kind").GetString().ShouldBe("query");
    quote.GetProperty("description").GetString().ShouldBe("Quote a title");
    JsonElement label = FindOption(quote, "Label");
    label.GetProperty("required").GetBoolean().ShouldBeFalse();
    label.GetProperty("defaultValue").GetString().ShouldBe("\"plain\"");
    label.GetProperty("description").GetString().ShouldBe("Label text");

    JsonElement title = FindOption(quote, "Title");
    title.GetProperty("required").GetBoolean().ShouldBeTrue();

    JsonElement analyzers = FindOption(FindEndpoint(terminal.Output, "t508bool"), "IncludeAnalyzers");
    analyzers.GetProperty("type").GetString().ShouldBe("bool");
    analyzers.GetProperty("isFlag").GetBoolean().ShouldBeFalse();
    analyzers.GetProperty("required").GetBoolean().ShouldBeFalse();
    analyzers.GetProperty("defaultValue").GetString().ShouldBe("true");

    JsonElement verbose = FindOption(FindEndpoint(terminal.Output, "t508bool"), "Verbose");
    verbose.GetProperty("isFlag").GetBoolean().ShouldBeTrue();
    verbose.GetProperty("defaultValue").GetString().ShouldBe("false");

    JsonElement mode = FindOption(FindEndpoint(terminal.Output, "t508enum"), "Mode");
    mode.GetProperty("required").GetBoolean().ShouldBeFalse();
    mode.GetProperty("allowedValues").EnumerateArray().Select(value => value.GetString()).ShouldContain("Fast");
    mode.GetProperty("allowedValues").EnumerateArray().Select(value => value.GetString()).ShouldContain("Slow");

    JsonElement requiredMode = FindOption(FindEndpoint(terminal.Output, "t508enum"), "RequiredMode");
    requiredMode.GetProperty("required").GetBoolean().ShouldBeTrue();

    JsonElement numbers = FindOption(FindEndpoint(terminal.Output, "t508repeat"), "Numbers");
    numbers.GetProperty("type").GetString().ShouldBe("int");
    numbers.GetProperty("isRepeated").GetBoolean().ShouldBeTrue();

    JsonElement notes = FindOption(FindEndpoint(terminal.Output, "t508json"), "Notes");
    notes.GetProperty("type").GetString()!.ShouldStartWith("json ");
    notes.GetProperty("type").GetString()!.ShouldContain("T508Note");

    JsonElement grouped = FindEndpoint(terminal.Output, "t508calc add");
    grouped.GetProperty("groupPath").EnumerateArray().Select(value => value.GetString()).ShouldBe(["t508calc"]);

    JsonElement tools = FindEndpoint(terminal.Output, "t508tools status");
    tools.GetProperty("groupPath").EnumerateArray().Select(value => value.GetString()).ShouldBe(["t508tools"]);

    JsonElement ungrouped = FindEndpoint(terminal.Output, "t508quote");
    ungrouped.GetProperty("groupPath").GetArrayLength().ShouldBe(0);

    string[] derivedKeys = [.. label.EnumerateObject().Select(property => property.Name).Order()];
    string[] attributeKeys = [.. FindOption(FindEndpoint(terminal.Output, "t508attr"), "label")
      .EnumerateObject()
      .Select(property => property.Name)
      .Order()];
    derivedKeys.ShouldBe(attributeKeys);
  }

  public static async Task Should_filter_derived_groups()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["--capabilities", "--group-filter", "t508calc"]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("t508calc add").ShouldBeTrue();
    terminal.OutputContains("t508quote").ShouldBeFalse();
    terminal.OutputContains("\"group\": \"t508calc\"").ShouldBeTrue();
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

  private static JsonElement FindOption(JsonElement endpoint, string name)
  {
    foreach (JsonElement option in endpoint.GetProperty("options").EnumerateArray())
    {
      if (option.GetProperty("name").GetString() == name)
        return option.Clone();
    }

    throw new InvalidOperationException($"Option '{name}' was not on '{endpoint.GetProperty("pattern").GetString()}'.");
  }
}

[Operation("t508quote", Description = "Quote a title")]
public sealed class T508QuoteQuery : IQuery<string>
{
  [Description("Title text")]
  public required string Title { get; set; }

  [Description("Label text")]
  public string Label { get; set; } = "plain";

  public int Count { get; set; } = 4;

  public sealed class Handler : IQueryHandler<T508QuoteQuery, string>
  {
    public Task<string> Handle(T508QuoteQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult($"title:{query.Title}|label:{query.Label}|count:{query.Count.ToString(CultureInfo.InvariantCulture)}");
    }
  }
}

[Operation("t508bool", Description = "Bool defaults")]
public sealed class T508BoolQuery : IQuery<string>
{
  [Description("Include analyzers")]
  public bool IncludeAnalyzers { get; set; } = true;

  public bool Verbose { get; set; } = false;

  public bool? Maybe { get; set; }

  public sealed class Handler : IQueryHandler<T508BoolQuery, string>
  {
    public Task<string> Handle(T508BoolQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      string maybe = query.Maybe is null ? "null" : query.Maybe.Value ? "true" : "false";
      return Task.FromResult(
        $"analyzers:{(query.IncludeAnalyzers ? "true" : "false")}|verbose:{(query.Verbose ? "true" : "false")}|maybe:{maybe}");
    }
  }
}

[Operation("t508force")]
public sealed class T508ForceQuery : IQuery<string>
{
  public bool Force { get; set; }

  public sealed class Handler : IQueryHandler<T508ForceQuery, string>
  {
    public Task<string> Handle(T508ForceQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult(query.Force ? "force:true" : "force:false");
    }
  }
}

public enum T508Mode
{
  Fast,
  Slow
}

[Operation("t508enum")]
public sealed class T508EnumQuery : IQuery<string>
{
  public T508Mode Mode { get; set; } = T508Mode.Fast;

  public required T508Mode RequiredMode { get; set; }

  public sealed class Handler : IQueryHandler<T508EnumQuery, string>
  {
    public Task<string> Handle(T508EnumQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult($"mode:{query.Mode}|required:{query.RequiredMode}");
    }
  }
}

[Operation("t508repeat")]
public sealed class T508RepeatQuery : IQuery<string>
{
  public string[] Tags { get; set; } = [];

  public int[] Numbers { get; set; } = [];

  public sealed class Handler : IQueryHandler<T508RepeatQuery, string>
  {
    public Task<string> Handle(T508RepeatQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult($"tags:{string.Join(',', query.Tags)}|numbers:{string.Join(',', query.Numbers)}");
    }
  }
}

public sealed class T508Note
{
  public string Path { get; set; } = "";

  public int Version { get; set; }
}

[Operation("t508json")]
public sealed class T508JsonQuery : IQuery<string>
{
  public T508Note[] Notes { get; set; } = [];

  public sealed class Handler : IQueryHandler<T508JsonQuery, string>
  {
    public Task<string> Handle(T508JsonQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      if (query.Notes is null || query.Notes.Length == 0)
        return Task.FromResult("notes:0");

      T508Note first = query.Notes[0];
      return Task.FromResult($"notes:{first.Path}:{first.Version.ToString(CultureInfo.InvariantCulture)}");
    }
  }
}

[NuruRouteGroup("t508calc")]
public abstract class T508CalcGroup;

[Operation("add", Description = "Add two numbers")]
public sealed class T508GroupedAddQuery : T508CalcGroup, IQuery<string>
{
  public int X { get; set; }

  public int Y { get; set; }

  public sealed class Handler : IQueryHandler<T508GroupedAddQuery, string>
  {
    public Task<string> Handle(T508GroupedAddQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult($"sum:{(query.X + query.Y).ToString(CultureInfo.InvariantCulture)}");
    }
  }
}

[Operation("status")]
[NuruCliBinding(Group = "t508tools")]
public sealed class T508CliGroupQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<T508CliGroupQuery, string>
  {
    public Task<string> Handle(T508CliGroupQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult("tools");
    }
  }
}

[NuruRouteGroup("t508wins")]
public abstract class T508WinsGroup;

[Operation("ping")]
[NuruCliBinding(Group = "t508loses")]
public sealed class T508PrecedenceQuery : T508WinsGroup, IQuery<string>
{
  public sealed class Handler : IQueryHandler<T508PrecedenceQuery, string>
  {
    public Task<string> Handle(T508PrecedenceQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult("wins");
    }
  }
}

[NuruRoute("t508legacy", Description = "Legacy spelling")]
[Operation("t508canonical", Description = "Canonical name")]
public sealed class T508BothQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<T508BothQuery, string>
  {
    public Task<string> Handle(T508BothQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult("legacy");
    }
  }
}

[Operation("t508pos")]
public sealed class T508PosQuery : IQuery<string>
{
  [Parameter(Description = "Left operand")]
  public int Left { get; set; }

  public sealed class Handler : IQueryHandler<T508PosQuery, string>
  {
    public Task<string> Handle(T508PosQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult($"left:{query.Left.ToString(CultureInfo.InvariantCulture)}");
    }
  }
}

[Operation("t508mixed")]
public sealed class T508MixedQuery : IQuery<string>
{
  [Option("title", Description = "Title")]
  public string Title { get; set; } = "n";

  public int Count { get; set; } = 1;

  public sealed class Handler : IQueryHandler<T508MixedQuery, string>
  {
    public Task<string> Handle(T508MixedQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult($"title:{query.Title}|count:{query.Count.ToString(CultureInfo.InvariantCulture)}");
    }
  }
}

[Operation("t508external", Description = "External handler")]
public sealed class T508ExternalCommand : ICommand<string>
{
  public string Name { get; set; } = "world";
}

public sealed class T508ExternalHandler : ICommandHandler<T508ExternalCommand, string>
{
  public Task<string> Handle(T508ExternalCommand command, CancellationToken cancellationToken)
  {
    _ = cancellationToken;
    return Task.FromResult($"ext:{command.Name}");
  }
}

[NuruRoute("t508attr", Description = "Attribute option")]
public sealed class T508AttrQuery : IQuery<string>
{
  [Option("label", Description = "Label text")]
  public string Label { get; set; } = "plain";

  public sealed class Handler : IQueryHandler<T508AttrQuery, string>
  {
    public Task<string> Handle(T508AttrQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult(query.Label);
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Auto
