#!/usr/bin/env -S dotnet --

#region Purpose
// Pins operation derivation for enum, record, inherited, hidden, and handlerless member shapes.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.AutoShapes
{
using System.Text.Json;
using TimeWarp.Contracts;

[TestTag("Auto")]
public class OperationEndpointDerivationShapeTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OperationEndpointDerivationShapeTests>();

  // Build() and RunAsync share a field: intercept sites come from Build()'s method or later calls on it.
  private static NuruApp App = null!;

  private static void CreateApp(TestTerminal terminal)
  {
    App = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<T508RecordQuery>()
      .Map<T508InheritedQuery>()
      .Map<T508ParamsQuery>()
      .Map<T508HideQuery>()
      .Map<T508SpecialQuery>()
      .Map<T508HandlerlessQuery>()
      .Map<T508NullableEnumQuery>()
      .Map<T508PositionalDefaultQuery>()
      .Map<T508PositionalInitQuery>()
      .Build();
  }

  public static async Task Should_keep_nullable_enum_and_non_const_field_initializers()
  {
    const string Expected = "unset:null|preset:Slow|combined:Read, Write|shared:Slow";
    using TestTerminal terminal = new();
    CreateApp(terminal);
    (await App.RunAsync(["t508nullenum"])).ShouldBe(0);
    terminal.OutputContains(Expected).ShouldBeTrue();

    using TestTerminal json = new();
    CreateApp(json);
    (await App.RunAsync(["t508nullenum", "--json-args", "{}"])).ShouldBe(0);
    json.OutputContains(Expected).ShouldBeTrue();

    using TestTerminal supplied = new();
    CreateApp(supplied);
    (await App.RunAsync(["t508nullenum", "--Preset", "Fast"])).ShouldBe(0);
    supplied.OutputContains("preset:Fast").ShouldBeTrue();

    using TestTerminal described = new();
    CreateApp(described);
    (await App.RunAsync(["--capabilities"])).ShouldBe(0);
    JsonElement endpoint = FindEndpoint(described.Output, "t508nullenum");
    FindOption(endpoint, "Unset").GetProperty("defaultValue").GetString().ShouldBe("null");
    FindOption(endpoint, "Preset").GetProperty("defaultValue").GetString().ShouldBe("Slow");
    FindOption(endpoint, "Combined").GetProperty("defaultValue").GetString().ShouldBe("Read, Write");
    FindOption(endpoint, "Shared").TryGetProperty("defaultValue", out _).ShouldBeFalse();
  }

  public static async Task Should_take_defaults_from_primary_constructor_parameters()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);
    (await App.RunAsync(["t508posdefault", "--Name", "ada"])).ShouldBe(0);
    terminal.OutputContains("name:ada|count:5|mode:Slow").ShouldBeTrue();

    using TestTerminal json = new();
    CreateApp(json);
    (await App.RunAsync(["t508posdefault", "--json-args", """{"Name":"ada"}"""])).ShouldBe(0);
    json.OutputContains("name:ada|count:5|mode:Slow").ShouldBeTrue();

    using TestTerminal supplied = new();
    CreateApp(supplied);
    (await App.RunAsync(["t508posdefault", "--Name", "ada", "--Count", "2", "--Mode", "Fast"])).ShouldBe(0);
    supplied.OutputContains("name:ada|count:2|mode:Fast").ShouldBeTrue();

    using TestTerminal described = new();
    CreateApp(described);
    (await App.RunAsync(["--capabilities"])).ShouldBe(0);
    JsonElement endpoint = FindEndpoint(described.Output, "t508posdefault");
    JsonElement count = FindOption(endpoint, "Count");
    count.GetProperty("required").GetBoolean().ShouldBeFalse();
    count.GetProperty("defaultValue").GetString().ShouldBe("5");
    FindOption(endpoint, "Mode").GetProperty("defaultValue").GetString().ShouldBe("Slow");
    FindOption(endpoint, "Name").GetProperty("required").GetBoolean().ShouldBeTrue();
  }

  public static async Task Should_not_copy_a_record_property_initializer_that_reads_a_parameter()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);
    (await App.RunAsync(["t508posinit", "--Name", " ada "])).ShouldBe(0);
    terminal.OutputContains("name:ada|note:n").ShouldBeTrue();

    using TestTerminal omitted = new();
    CreateApp(omitted);
    (await App.RunAsync(["t508posinit"])).ShouldBe(1);
  }

  public static async Task Should_keep_non_finite_enum_const_and_flags_defaults()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508special"]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("quiet:nan|up:inf|down:-inf|accent:fast|access:rw|cast:3|option:nan|soft:nan|rise:inf").ShouldBeTrue();

    using TestTerminal describedTerminal = new();
    CreateApp(describedTerminal);
    int described = await App.RunAsync(["--capabilities"]);
    described.ShouldBe(0);
    JsonElement endpoint = FindEndpoint(describedTerminal.Output, "t508special");
    FindOption(endpoint, "Quiet").GetProperty("defaultValue").GetString().ShouldBe("NaN");
    FindOption(endpoint, "Up").GetProperty("defaultValue").GetString().ShouldBe("Infinity");
    FindOption(endpoint, "Down").GetProperty("defaultValue").GetString().ShouldBe("-Infinity");
    FindOption(endpoint, "Accent").GetProperty("defaultValue").GetString().ShouldBe("Fast");
    FindOption(endpoint, "Access").GetProperty("defaultValue").GetString().ShouldBe("Read, Write");
    FindOption(endpoint, "Casted").GetProperty("defaultValue").GetString().ShouldBe("Read, Write");
    FindOption(endpoint, "quiet").GetProperty("defaultValue").GetString().ShouldBe("NaN");
    FindOption(endpoint, "Soft").GetProperty("defaultValue").GetString().ShouldBe("NaN");
    FindOption(endpoint, "Rise").GetProperty("defaultValue").GetString().ShouldBe("Infinity");
  }

  public static async Task Should_reject_a_keyword_json_member_on_argv()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int exitCode = await App.RunAsync(["t508params", "--Params", "nope"]);
    exitCode.ShouldBe(1);
    terminal.ErrorContains("--Params is JSON. Pass it with --json-args.").ShouldBeTrue();
  }

  public static async Task Should_not_derive_a_hidden_base_property()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int named = await App.RunAsync(["t508hide", "--title", "ada"]);
    named.ShouldBe(0);
    terminal.OutputContains("name:ada").ShouldBeTrue();

    using TestTerminal omitted = new();
    CreateApp(omitted);
    int exitCode = await App.RunAsync(["t508hide"]);
    exitCode.ShouldBe(0);
    omitted.OutputContains("name:child").ShouldBeTrue();
  }

  public static async Task Should_derive_a_record_class_and_an_inherited_property()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int recordExit = await App.RunAsync(["t508record", "--Title", "ada"]);
    recordExit.ShouldBe(0);
    terminal.OutputContains("record:ada").ShouldBeTrue();

    int inherited = await App.RunAsync(["t508inherited", "--Owner", "grace"]);
    inherited.ShouldBe(0);
    terminal.OutputContains("owner:grace").ShouldBeTrue();
  }

  public static async Task Should_keep_help_available_when_one_handler_is_missing()
  {
    using TestTerminal terminal = new();
    CreateApp(terminal);

    int help = await App.RunAsync(["--help"]);
    help.ShouldBe(0);
    terminal.ErrorContains("No handler is registered").ShouldBeFalse();
    terminal.OutputContains("No handler is registered").ShouldBeFalse();

    int capabilities = await App.RunAsync(["--capabilities"]);
    capabilities.ShouldBe(0);

    int missing = await App.RunAsync(["t508handlerless"]);
    missing.ShouldBe(1);
    terminal.ErrorContains("No handler is registered for").ShouldBeTrue();
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

public enum T508Mode
{
  Fast,
  Slow
}

internal sealed class T508Note
{
  public string Path { get; set; } = "";

  public int Version { get; set; }
}

#pragma warning restore TWM001
#pragma warning restore NURU_H008

internal static class T508Numbers
{
  public const double Up = double.PositiveInfinity;

  public const double Down = double.NegativeInfinity;

  public const T508Mode Accent = T508Mode.Fast;
}

[Flags]
internal enum T508Access
{
  None = 0,
  Read = 1,
  Write = 2
}

public static class T508Shared
{
  public static readonly T508Mode Color = T508Mode.Slow;
}

[Operation("t508special")]
internal sealed class T508SpecialQuery : IQuery<string>
{
  public double Quiet { get; set; } = double.NaN;

  public double Up { get; set; } = T508Numbers.Up;

  public double Down { get; set; } = T508Numbers.Down;

  public T508Mode Accent { get; set; } = T508Numbers.Accent;

  public T508Access Access { get; set; } = T508Access.Read | T508Access.Write;

  public T508Access Casted { get; set; } = (T508Access)3;

  [Option("quiet")]
  public double QuietOption { get; set; } = double.NaN;

  public float Soft { get; set; } = float.NaN;

  public float Rise { get; set; } = float.PositiveInfinity;

  public sealed class Handler : IRequestHandler<T508SpecialQuery, string>
  {
    public ValueTask<string> Handle(T508SpecialQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      string quiet = double.IsNaN(query.Quiet) ? "nan" : "no";
      string up = double.IsPositiveInfinity(query.Up) ? "inf" : "no";
      string down = double.IsNegativeInfinity(query.Down) ? "-inf" : "no";
      string accent = query.Accent == T508Mode.Fast ? "fast" : "no";
      string access = query.Access == (T508Access.Read | T508Access.Write) ? "rw" : "no";
      string casted = ((int)query.Casted).ToString(CultureInfo.InvariantCulture);
      string option = double.IsNaN(query.QuietOption) ? "nan" : "no";
      string soft = float.IsNaN(query.Soft) ? "nan" : "no";
      string rise = float.IsPositiveInfinity(query.Rise) ? "inf" : "no";
      return ValueTask.FromResult(
        $"quiet:{quiet}|up:{up}|down:{down}|accent:{accent}|access:{access}|cast:{casted}|option:{option}|soft:{soft}|rise:{rise}");
    }
  }
}

[Operation("t508nullenum")]
internal sealed class T508NullableEnumQuery : IQuery<string>
{
#pragma warning disable CA1805 // `= null` is the explicit nullable-enum initializer the capability defaultValue pins
  public T508Mode? Unset { get; set; } = null;
#pragma warning restore CA1805

  public T508Mode? Preset { get; set; } = T508Mode.Slow;

  public T508Access? Combined { get; set; } = (T508Access)3;

  public T508Mode Shared { get; set; } = T508Shared.Color;

  public sealed class Handler : IRequestHandler<T508NullableEnumQuery, string>
  {
    public ValueTask<string> Handle(T508NullableEnumQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      string unset = query.Unset?.ToString() ?? "null";
      string preset = query.Preset?.ToString() ?? "null";
      string combined = query.Combined?.ToString() ?? "null";
      return ValueTask.FromResult($"unset:{unset}|preset:{preset}|combined:{combined}|shared:{query.Shared}");
    }
  }
}

[Operation("t508posdefault")]
internal sealed record T508PositionalDefaultQuery(string Name, int Count = 5, T508Mode Mode = T508Mode.Slow) : IQuery<string>
{
  public sealed class Handler : IRequestHandler<T508PositionalDefaultQuery, string>
  {
    public ValueTask<string> Handle(T508PositionalDefaultQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult($"name:{query.Name}|count:{query.Count.ToString(CultureInfo.InvariantCulture)}|mode:{query.Mode}");
    }
  }
}

[Operation("t508posinit")]
internal sealed record T508PositionalInitQuery(string Name, string Note = "n") : IQuery<string>
{
  public string Name { get; init; } = Name.Trim();

  public string Note { get; init; } = Note;

  public sealed class Handler : IRequestHandler<T508PositionalInitQuery, string>
  {
    public ValueTask<string> Handle(T508PositionalInitQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult($"name:{query.Name}|note:{query.Note}");
    }
  }
}

[Operation("t508params")]
internal sealed class T508ParamsQuery : IQuery<string>
{
  public T508Note[] Params { get; set; } = [];

  public sealed class Handler : IRequestHandler<T508ParamsQuery, string>
  {
    public ValueTask<string> Handle(T508ParamsQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return ValueTask.FromResult("params");
    }
  }
}

internal abstract class T508HideBase
{
  public string Name { get; set; } = "base";
}

[Operation("t508hide")]
internal sealed class T508HideQuery : T508HideBase, IQuery<string>
{
  [Option("title")]
  public new string Name { get; set; } = "child";

  public sealed class Handler : IRequestHandler<T508HideQuery, string>
  {
    public ValueTask<string> Handle(T508HideQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult("name:" + query.Name);
    }
  }
}

internal abstract class T508InheritedBase
{
  public string Owner { get; set; } = "ada";
}

[Operation("t508inherited")]
internal sealed class T508InheritedQuery : T508InheritedBase, IQuery<string>
{
  public sealed class Handler : IRequestHandler<T508InheritedQuery, string>
  {
    public ValueTask<string> Handle(T508InheritedQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult("owner:" + query.Owner);
    }
  }
}

[Operation("t508record")]
internal sealed record class T508RecordQuery : IQuery<string>
{
  public string Title { get; set; } = "rec";

  public sealed class Handler : IRequestHandler<T508RecordQuery, string>
  {
    public ValueTask<string> Handle(T508RecordQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return ValueTask.FromResult("record:" + query.Title);
    }
  }
}

#pragma warning disable NURU_H008 // The handler is registered outside this compilation.
#pragma warning disable TWM001
[Operation("t508handlerless")]
internal sealed class T508HandlerlessQuery : IQuery<string>
{
}
} // namespace TimeWarp.Nuru.Tests.AutoShapes
