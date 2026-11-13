#!/usr/bin/env -S dotnet --

#region Purpose
// Pins how operation-derived endpoints bind scalar members and keep or replace initializers the generator cannot copy.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.AutoInitializers
{
using System.Numerics;
using System.Text.Json;
using TimeWarp.Contracts;

[TestTag("Auto")]
public class OperationEndpointDerivationInitializerTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OperationEndpointDerivationInitializerTests>();

  // Build() and RunAsync share a field: intercept sites come from Build()'s method or later calls on it.
  private static NuruApp App = null!;

  private static void CreateApp(TestTerminal terminal)
  {
    App = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<T508JsonRequiredQuery>()
      .Map<T508ScalarQuery>()
      .Map<T508DefaultQuery>()
      .Map<T508UserQuery>()
      .Map<T508InitQuery>()
      .Map<T508InitManyQuery>()
      .Map<T508RequiredDefaultQuery>()
      .Map<T508CaseQuery>()
      .Build();
  }

  public static async Task Should_bind_the_wider_scalar_set()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(
    [
      "t508scalars",
      "--Narrow", "-2",
      "--Words", "4",
      "--Ticks", "8",
      "--Huge", "16",
      "--Day", "2020-01-02",
      "--Clock", "13:45",
      "--Big", "99",
      "--More", "1",
      "--More", "2"
    ]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("signed:-2|words:4|ticks:8|huge:16|day:2020-01-02|clock:13:45|big:99|more:1,2").ShouldBeTrue();
  }

  public static async Task Should_keep_decimal_float_const_bool_and_skip_unrepresentable_defaults()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508defaults"]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("price:1.5|ratio:2|const:true|items:set|delay:30|token:set").ShouldBeTrue();

    using TestTerminal jsonTerminal = new();
    CreateApp(jsonTerminal);
    int jsonCode = await App.RunAsync(["t508defaults", "--json-args", "{}"]);
    jsonCode.ShouldBe(0);
    jsonTerminal.OutputContains("price:1.5|ratio:2|const:true|items:set|delay:30|token:set").ShouldBeTrue();

    using TestTerminal describedTerminal = new();
    CreateApp(describedTerminal);
    int described = await App.RunAsync(["--capabilities"]);
    described.ShouldBe(0);
    JsonElement price = FindOption(FindEndpoint(describedTerminal.Output, "t508defaults"), "Price");
    price.GetProperty("defaultValue").GetString().ShouldBe("1.5");
    JsonElement ratio = FindOption(FindEndpoint(describedTerminal.Output, "t508defaults"), "Ratio");
    ratio.GetProperty("defaultValue").GetString().ShouldBe("2");
    JsonElement flag = FindOption(FindEndpoint(describedTerminal.Output, "t508defaults"), "ConstFlag");
    flag.GetProperty("defaultValue").GetString().ShouldBe("true");
    flag.GetProperty("isFlag").GetBoolean().ShouldBeFalse();
    FindOption(FindEndpoint(describedTerminal.Output, "t508defaults"), "Items").TryGetProperty("defaultValue", out _).ShouldBeFalse();
    FindOption(FindEndpoint(describedTerminal.Output, "t508defaults"), "Delay").TryGetProperty("defaultValue", out _).ShouldBeFalse();
    FindOption(FindEndpoint(describedTerminal.Output, "t508defaults"), "Token").TryGetProperty("defaultValue", out _).ShouldBeFalse();
  }

  public static async Task Should_use_a_supplied_value_over_an_initializer_the_generator_cannot_copy()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);
    int argv = await App.RunAsync(["t508defaults", "--Delay", "00:00:05"]);
    argv.ShouldBe(0);
    terminal.OutputContains("delay:5|token:set").ShouldBeTrue();

    using TestTerminal jsonTerminal = new();
    CreateApp(jsonTerminal);
    int json = await App.RunAsync(["t508defaults", "--json-args", """{"Delay":"00:00:05"}"""]);
    json.ShouldBe(0);
    jsonTerminal.OutputContains("delay:5|token:set").ShouldBeTrue();

    using TestTerminal initTerminal = new();
    CreateApp(initTerminal);
    int init = await App.RunAsync(["t508init", "--Name", "ada", "--Extra", "5"]);
    init.ShouldBe(0);
    initTerminal.OutputContains("name:ada|extra:5").ShouldBeTrue();
  }

  public static async Task Should_keep_or_replace_many_init_only_initializers()
  {
    using TestTerminal omitted = new();
    CreateApp(omitted);
    (await App.RunAsync(["t508initmany"])).ShouldBe(0);
    omitted.OutputContains("wait:4|seed:41|token:set|label:null|note:n").ShouldBeTrue();

    using TestTerminal supplied = new();
    CreateApp(supplied);
    (await App.RunAsync(["t508initmany", "--Wait", "00:00:09", "--Seed", "5", "--Label", "x", "--Note", "m"])).ShouldBe(0);
    supplied.OutputContains("wait:9|seed:5|token:set|label:x|note:m").ShouldBeTrue();

    using TestTerminal jsonOmitted = new();
    CreateApp(jsonOmitted);
    (await App.RunAsync(["t508initmany", "--json-args", "{}"])).ShouldBe(0);
    jsonOmitted.OutputContains("wait:4|seed:41|token:set|label:null|note:n").ShouldBeTrue();

    using TestTerminal jsonSupplied = new();
    CreateApp(jsonSupplied);
    (await App.RunAsync(["t508initmany", "--json-args", """{"Wait":"00:00:09","Seed":5,"Label":"x"}"""])).ShouldBe(0);
    jsonSupplied.OutputContains("wait:9|seed:5|token:set|label:x|note:n").ShouldBeTrue();

    using TestTerminal described = new();
    CreateApp(described);
    (await App.RunAsync(["--capabilities"])).ShouldBe(0);
    JsonElement endpoint = FindEndpoint(described.Output, "t508initmany");
    FindOption(endpoint, "Label").TryGetProperty("defaultValue", out _).ShouldBeFalse();
    FindOption(endpoint, "Seed").TryGetProperty("defaultValue", out _).ShouldBeFalse();
  }

  public static async Task Should_always_set_a_required_member_with_an_initializer_the_generator_cannot_copy()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);
    int exitCode = await App.RunAsync(
      ["t508reqdefault", "--json-args", """{"Wait":"00:00:02","Tag":"t","Body":{"Name":"ada"}}"""]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("wait:2|tag:t|body:ada").ShouldBeTrue();
  }

  public static async Task Should_bind_a_mixed_case_option_pair_with_a_non_copyable_initializer()
  {
    using TestTerminal flag = new();
    CreateApp(flag);
    (await App.RunAsync(["t508case", "--Mute"])).ShouldBe(0);
    flag.OutputContains("level:seed|mute:True").ShouldBeTrue();

    using TestTerminal level = new();
    CreateApp(level);
    (await App.RunAsync(["t508case", "--mute", "loud"])).ShouldBe(0);
    level.OutputContains("level:loud|mute:False").ShouldBeTrue();

    using TestTerminal json = new();
    CreateApp(json);
    (await App.RunAsync(["t508case", "--json-args", """{"Mute":true}"""])).ShouldBe(0);
    json.OutputContains("level:seed|mute:True").ShouldBeTrue();

    using TestTerminal jsonLevel = new();
    CreateApp(jsonLevel);
    (await App.RunAsync(["t508case", "--json-args", """{"mute":"loud"}"""])).ShouldBe(0);
    jsonLevel.OutputContains("level:loud|mute:False").ShouldBeTrue();
  }

  public static async Task Should_construct_a_positional_record_and_a_required_init_member()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int recordExit = await App.RunAsync(["t508user", "--Id", "7"]);
    recordExit.ShouldBe(0);
    terminal.OutputContains("id:7").ShouldBeTrue();

    int initExit = await App.RunAsync(["t508init", "--Name", "ada"]);
    initExit.ShouldBe(0);
    terminal.OutputContains("name:ada|extra:3").ShouldBeTrue();
  }

  public static async Task Should_reject_argv_for_a_required_json_member()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int argv = await App.RunAsync(["t508jsonreq", "--Body", "nope"]);
    argv.ShouldBe(1);
    terminal.ErrorContains("--Body is JSON. Pass it with --json-args.").ShouldBeTrue();

    int missing = await App.RunAsync(["t508jsonreq"]);
    missing.ShouldBe(1);
    terminal.ErrorContains("Error: pass --Body via --json-args.").ShouldBeTrue();

    int exitCode = await App.RunAsync(["t508jsonreq", "--json-args", """{"Body":{"Name":"ada"}}"""]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("body:ada").ShouldBeTrue();
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

[Operation("t508scalars")]
internal sealed class T508ScalarQuery : IQuery<string>
{
  public sbyte Narrow { get; set; }

  public ushort Words { get; set; }

  public uint Ticks { get; set; }

  public ulong Huge { get; set; }

  public DateOnly Day { get; set; }

  public TimeOnly Clock { get; set; }

  public BigInteger Big { get; set; }

  public ushort[] More { get; set; } = [];

  public sealed class Handler : IRequestHandler<T508ScalarQuery, string>
  {
    public ValueTask<string> Handle(T508ScalarQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult(
        $"signed:{query.Narrow.ToString(CultureInfo.InvariantCulture)}"
        + $"|words:{query.Words.ToString(CultureInfo.InvariantCulture)}"
        + $"|ticks:{query.Ticks.ToString(CultureInfo.InvariantCulture)}"
        + $"|huge:{query.Huge.ToString(CultureInfo.InvariantCulture)}"
        + $"|day:{query.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}"
        + $"|clock:{query.Clock.ToString("HH:mm", CultureInfo.InvariantCulture)}"
        + $"|big:{query.Big.ToString(CultureInfo.InvariantCulture)}"
        + $"|more:{string.Join(',', query.More)}");
    }
  }
}

internal sealed class T508Fresh
{
}

internal static class T508Const
{
  public const bool On = true;
}

[Operation("t508defaults")]
internal sealed class T508DefaultQuery : IQuery<string>
{
  public decimal Price { get; set; } = 1.5m;

  public float Ratio { get; set; } = 2f;

  public bool ConstFlag { get; set; } = T508Const.On;

  // `= new()` is not a constant. The catalog omits it. Omission keeps the initializer.
  public T508Fresh? Items { get; set; } = new();

  public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(30);

  public Guid Token { get; set; } = Guid.NewGuid();

  public sealed class Handler : IRequestHandler<T508DefaultQuery, string>
  {
    public ValueTask<string> Handle(T508DefaultQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      string items = query.Items is null ? "null" : "set";
      string token = query.Token == Guid.Empty ? "empty" : "set";
      return ValueTask.FromResult(
        $"price:{query.Price.ToString(CultureInfo.InvariantCulture)}"
        + $"|ratio:{query.Ratio.ToString(CultureInfo.InvariantCulture)}"
        + $"|const:{(query.ConstFlag ? "true" : "false")}"
        + $"|items:{items}"
        + $"|delay:{query.Delay.TotalSeconds.ToString(CultureInfo.InvariantCulture)}"
        + $"|token:{token}");
    }
  }
}

internal sealed class T508Body
{
  public string Name { get; set; } = "";
}

[Operation("t508jsonreq")]
internal sealed class T508JsonRequiredQuery : IQuery<string>
{
  public required T508Body Body { get; set; }

  public sealed class Handler : IRequestHandler<T508JsonRequiredQuery, string>
  {
    public ValueTask<string> Handle(T508JsonRequiredQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult("body:" + query.Body.Name);
    }
  }
}

[Operation("t508user")]
internal sealed record T508UserQuery(int Id) : IQuery<string>
{
  public sealed class Handler : IRequestHandler<T508UserQuery, string>
  {
    public ValueTask<string> Handle(T508UserQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult("id:" + query.Id.ToString(CultureInfo.InvariantCulture));
    }
  }
}

[Operation("t508init")]
internal sealed class T508InitQuery : IQuery<string>
{
  public required string Name { get; init; }

  public int Extra { get; init; } = 3;

  public sealed class Handler : IRequestHandler<T508InitQuery, string>
  {
    public ValueTask<string> Handle(T508InitQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult($"name:{query.Name}|extra:{query.Extra.ToString(CultureInfo.InvariantCulture)}");
    }
  }
}

internal static class T508Seed
{
  public static int Next() => 41;

  public static string Text() => "seed";
}

// Init-only members whose initializers the generator cannot copy, plus `= null!`.
[Operation("t508initmany")]
internal sealed class T508InitManyQuery : IQuery<string>
{
  public TimeSpan Wait { get; init; } = TimeSpan.FromSeconds(4);

  public int Seed { get; init; } = T508Seed.Next();

  public Guid Token { get; init; } = Guid.NewGuid();

  public string Label { get; init; } = null!;

  public string Note { get; init; } = string.Concat("n", string.Empty);

  public sealed class Handler : IRequestHandler<T508InitManyQuery, string>
  {
    public ValueTask<string> Handle(T508InitManyQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      string token = query.Token == Guid.Empty ? "empty" : "set";
      return ValueTask.FromResult(
        $"wait:{query.Wait.TotalSeconds.ToString(CultureInfo.InvariantCulture)}"
        + $"|seed:{query.Seed.ToString(CultureInfo.InvariantCulture)}"
        + $"|token:{token}"
        + $"|label:{query.Label ?? "null"}"
        + $"|note:{query.Note}");
    }
  }
}

[Operation("t508reqdefault")]
internal sealed class T508RequiredDefaultQuery : IQuery<string>
{
  public required TimeSpan Wait { get; set; } = TimeSpan.FromSeconds(1);

  public required string Tag { get; init; } = null!;

  public required T508Body Body { get; set; } = new();

  public sealed class Handler : IRequestHandler<T508RequiredDefaultQuery, string>
  {
    public ValueTask<string> Handle(T508RequiredDefaultQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult(
        $"wait:{query.Wait.TotalSeconds.ToString(CultureInfo.InvariantCulture)}|tag:{query.Tag}|body:{query.Body.Name}");
    }
  }
}

[Operation("t508case")]
internal sealed class T508CaseQuery : IQuery<string>
{
  [Option("mute")]
  public string Level { get; set; } = T508Seed.Text();

#pragma warning disable CA1805 // `= false` makes Mute the flag this case-option test binds
  public bool Mute { get; set; } = false;
#pragma warning restore CA1805

  public sealed class Handler : IRequestHandler<T508CaseQuery, string>
  {
    public ValueTask<string> Handle(T508CaseQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult($"level:{query.Level}|mute:{query.Mute}");
    }
  }
}
} // namespace TimeWarp.Nuru.Tests.AutoInitializers
