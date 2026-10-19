#!/usr/bin/env -S dotnet --
#pragma warning disable CA1062 // Handlers trust the mediator request instance
#pragma warning disable CA1819 // Repeated and JSON members are arrays on purpose
#pragma warning disable CA1002 // Bag is List<T> so the capability type has a type argument
#pragma warning disable CA2227 // Derived members need a public setter
#pragma warning disable CA1805 // `= false` is the flag initializer this task derives

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Auto
{
using System.ComponentModel;
using System.Numerics;
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
      .Map<T508QuoteQuery>()
      .Map<T508BoolQuery>()
      .Map<T508ForceQuery>()
      .Map<T508EnumQuery>()
      .Map<T508RepeatQuery>()
      .Map<T508JsonQuery>()
      .Map<T508JsonRequiredQuery>()
      .Map<T508GroupedAddQuery>()
      .Map<T508CliGroupQuery>()
      .Map<T508PrecedenceQuery>()
      .Map<T508BothQuery>()
      .Map<T508PosQuery>()
      .Map<T508MixedQuery>()
      .Map<T508ExternalCommand>()
      .Map<T508AttrQuery>()
      .Map<T508ScalarQuery>()
      .Map<T508DefaultQuery>()
      .Map<T508RecordQuery>()
      .Map<T508InheritedQuery>()
      .Map<T508UserQuery>()
      .Map<T508InitQuery>()
      .Map<T508ParamsQuery>()
      .Map<T508HideQuery>()
      .Map<T508SpecialQuery>()
      .Map<T508KeepDescriptionQuery>()
      .Map<T508HandlerlessQuery>()
      .Map<T508InitManyQuery>()
      .Map<T508RequiredDefaultQuery>()
      .Map<T508CaseQuery>()
      .Map<T508NullableEnumQuery>()
      .Map<T508PositionalDefaultQuery>()
      .Map<T508PositionalInitQuery>()
      .Map<T514DottedGroupQuery>()
      .Map<T514SpacedGroupQuery>()
      .Map<T514ContractWinsQuery>()
      .Map<T514BlankGroupQuery>()
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
    notes.GetProperty("type").GetString()!.ShouldNotContain("global::");

    JsonElement bag = FindOption(FindEndpoint(terminal.Output, "t508json"), "Bag");
    string? bagType = bag.GetProperty("type").GetString();
    bagType.ShouldNotBeNull();
    bagType.ShouldContain("List<");
    bagType.ShouldContain("T508Note");
    bagType.ShouldNotContain("global::");

    JsonElement grouped = FindEndpoint(terminal.Output, "t508calc add");
    grouped.GetProperty("groupPath").EnumerateArray().Select(value => value.GetString()).ShouldBe(["t508calc"]);

    JsonElement tools = FindEndpoint(terminal.Output, "t508tools status");
    tools.GetProperty("groupPath").EnumerateArray().Select(value => value.GetString()).ShouldBe(["t508tools"]);

    JsonElement ungrouped = FindEndpoint(terminal.Output, "t508quote");
    ungrouped.GetProperty("groupPath").GetArrayLength().ShouldBe(0);

    JsonElement attributeLabel = FindOption(FindEndpoint(terminal.Output, "t508attr"), "label");
    ShouldMatchExceptName(label, attributeLabel);

    JsonElement quoteEndpoint = FindEndpoint(terminal.Output, "t508quote");
    JsonElement attributeEndpoint = FindEndpoint(terminal.Output, "t508attr");
    string[] derivedEndpointKeys = [.. quoteEndpoint.EnumerateObject().Select(property => property.Name).Order()];
    string[] attributeEndpointKeys = [.. attributeEndpoint.EnumerateObject().Select(property => property.Name).Order()];
    derivedEndpointKeys.ShouldBe(attributeEndpointKeys);

    JsonElement kept = FindEndpoint(terminal.Output, "t508keepdesc");
    kept.GetProperty("description").GetString().ShouldBe("Kept from operation");
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

  // 508-014: [Operation(Group)] is the groupPath and never a route or command prefix.
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

  private static void ShouldMatchExceptName(JsonElement derived, JsonElement attribute)
  {
    Dictionary<string, string> derivedValues = PropertiesExceptName(derived);
    Dictionary<string, string> attributeValues = PropertiesExceptName(attribute);
    derivedValues.Keys.Order().ShouldBe(attributeValues.Keys.Order());
    foreach (string key in derivedValues.Keys.Order())
      derivedValues[key].ShouldBe(attributeValues[key]);
  }

  private static Dictionary<string, string> PropertiesExceptName(JsonElement element)
  {
    Dictionary<string, string> values = [];
    foreach (JsonProperty property in element.EnumerateObject())
    {
      if (property.NameEquals("name"))
        continue;

      values[property.Name] = property.Value.GetRawText();
    }

    return values;
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

  public List<T508Note> Bag { get; set; } = [];

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

[Operation("t514dotted", Group = "t514ops.symbols")]
public sealed class T514DottedGroupQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<T514DottedGroupQuery, string>
  {
    public Task<string> Handle(T514DottedGroupQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult("dotted");
    }
  }
}

[Operation("t514spaced", Group = " t514ops  symbols ")]
public sealed class T514SpacedGroupQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<T514SpacedGroupQuery, string>
  {
    public Task<string> Handle(T514SpacedGroupQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult("spaced");
    }
  }
}

[NuruRouteGroup("t514routegroup")]
public abstract class T514RouteGroup;

[Operation("t514wins", Group = "t514meta")]
[NuruCliBinding(Group = "t514cligroup")]
public sealed class T514ContractWinsQuery : T514RouteGroup, IQuery<string>
{
  public sealed class Handler : IQueryHandler<T514ContractWinsQuery, string>
  {
    public Task<string> Handle(T514ContractWinsQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult("contract");
    }
  }
}

[Operation("t514blank", Group = "  ")]
[NuruCliBinding(Group = "t514blankcli")]
public sealed class T514BlankGroupQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<T514BlankGroupQuery, string>
  {
    public Task<string> Handle(T514BlankGroupQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult("blank");
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

[Operation("t508scalars")]
public sealed class T508ScalarQuery : IQuery<string>
{
  public sbyte Narrow { get; set; }

  public ushort Words { get; set; }

  public uint Ticks { get; set; }

  public ulong Huge { get; set; }

  public DateOnly Day { get; set; }

  public TimeOnly Clock { get; set; }

  public BigInteger Big { get; set; }

  public ushort[] More { get; set; } = [];

  public sealed class Handler : IQueryHandler<T508ScalarQuery, string>
  {
    public Task<string> Handle(T508ScalarQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult(
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

public sealed class T508Fresh
{
}

public static class T508Const
{
  public const bool On = true;
}

[Operation("t508defaults")]
public sealed class T508DefaultQuery : IQuery<string>
{
  public decimal Price { get; set; } = 1.5m;

  public float Ratio { get; set; } = 2f;

  public bool ConstFlag { get; set; } = T508Const.On;

  // `= new()` is not a constant. The catalog omits it. Omission keeps the initializer.
  public T508Fresh? Items { get; set; } = new();

  public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(30);

  public Guid Token { get; set; } = Guid.NewGuid();

  public sealed class Handler : IQueryHandler<T508DefaultQuery, string>
  {
    public Task<string> Handle(T508DefaultQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      string items = query.Items is null ? "null" : "set";
      string token = query.Token == Guid.Empty ? "empty" : "set";
      return Task.FromResult(
        $"price:{query.Price.ToString(CultureInfo.InvariantCulture)}"
        + $"|ratio:{query.Ratio.ToString(CultureInfo.InvariantCulture)}"
        + $"|const:{(query.ConstFlag ? "true" : "false")}"
        + $"|items:{items}"
        + $"|delay:{query.Delay.TotalSeconds.ToString(CultureInfo.InvariantCulture)}"
        + $"|token:{token}");
    }
  }
}

[Operation("t508record")]
public sealed record class T508RecordQuery : IQuery<string>
{
  public string Title { get; set; } = "rec";

  public sealed class Handler : IQueryHandler<T508RecordQuery, string>
  {
    public Task<string> Handle(T508RecordQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult("record:" + query.Title);
    }
  }
}

public abstract class T508InheritedBase
{
  public string Owner { get; set; } = "ada";
}

[Operation("t508inherited")]
public sealed class T508InheritedQuery : T508InheritedBase, IQuery<string>
{
  public sealed class Handler : IQueryHandler<T508InheritedQuery, string>
  {
    public Task<string> Handle(T508InheritedQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult("owner:" + query.Owner);
    }
  }
}

[NuruRoute("t508keepdesc")]
[Operation("t508wiped", Description = "Kept from operation")]
public sealed class T508KeepDescriptionQuery : IQuery<string>
{
  public sealed class Handler : IQueryHandler<T508KeepDescriptionQuery, string>
  {
    public Task<string> Handle(T508KeepDescriptionQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult("kept");
    }
  }
}

public sealed class T508Body
{
  public string Name { get; set; } = "";
}

[Operation("t508jsonreq")]
public sealed class T508JsonRequiredQuery : IQuery<string>
{
  public required T508Body Body { get; set; }

  public sealed class Handler : IQueryHandler<T508JsonRequiredQuery, string>
  {
    public Task<string> Handle(T508JsonRequiredQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult("body:" + query.Body.Name);
    }
  }
}

#pragma warning disable NURU_H008 // The handler is registered outside this compilation.
#pragma warning disable TWM001
[Operation("t508handlerless")]
public sealed class T508HandlerlessQuery : IQuery<string>
{
}
#pragma warning restore TWM001
#pragma warning restore NURU_H008

public static class T508Numbers
{
  public const double Up = double.PositiveInfinity;

  public const double Down = double.NegativeInfinity;

  public const T508Mode Accent = T508Mode.Fast;
}

[Flags]
public enum T508Access
{
  None = 0,
  Read = 1,
  Write = 2
}

[Operation("t508special")]
public sealed class T508SpecialQuery : IQuery<string>
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

  public sealed class Handler : IQueryHandler<T508SpecialQuery, string>
  {
    public Task<string> Handle(T508SpecialQuery query, CancellationToken cancellationToken)
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
      return Task.FromResult(
        $"quiet:{quiet}|up:{up}|down:{down}|accent:{accent}|access:{access}|cast:{casted}|option:{option}|soft:{soft}|rise:{rise}");
    }
  }
}

[Operation("t508user")]
public sealed record T508UserQuery(int Id) : IQuery<string>
{
  public sealed class Handler : IQueryHandler<T508UserQuery, string>
  {
    public Task<string> Handle(T508UserQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult("id:" + query.Id.ToString(CultureInfo.InvariantCulture));
    }
  }
}

[Operation("t508init")]
public sealed class T508InitQuery : IQuery<string>
{
  public required string Name { get; init; }

  public int Extra { get; init; } = 3;

  public sealed class Handler : IQueryHandler<T508InitQuery, string>
  {
    public Task<string> Handle(T508InitQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult($"name:{query.Name}|extra:{query.Extra.ToString(CultureInfo.InvariantCulture)}");
    }
  }
}

[Operation("t508params")]
public sealed class T508ParamsQuery : IQuery<string>
{
  public T508Note[] Params { get; set; } = [];

  public sealed class Handler : IQueryHandler<T508ParamsQuery, string>
  {
    public Task<string> Handle(T508ParamsQuery query, CancellationToken cancellationToken)
    {
      _ = query;
      _ = cancellationToken;
      return Task.FromResult("params");
    }
  }
}

public abstract class T508HideBase
{
  public string Name { get; set; } = "base";
}

[Operation("t508hide")]
public sealed class T508HideQuery : T508HideBase, IQuery<string>
{
  [Option("title")]
  public new string Name { get; set; } = "child";

  public sealed class Handler : IQueryHandler<T508HideQuery, string>
  {
    public Task<string> Handle(T508HideQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult("name:" + query.Name);
    }
  }
}

public static class T508Seed
{
  public static int Next() => 41;

  public static string Text() => "seed";
}

public static class T508Shared
{
  public static readonly T508Mode Color = T508Mode.Slow;
}

// Init-only members whose initializers the generator cannot copy, plus `= null!`.
[Operation("t508initmany")]
public sealed class T508InitManyQuery : IQuery<string>
{
  public TimeSpan Wait { get; init; } = TimeSpan.FromSeconds(4);

  public int Seed { get; init; } = T508Seed.Next();

  public Guid Token { get; init; } = Guid.NewGuid();

  public string Label { get; init; } = null!;

  public string Note { get; init; } = string.Concat("n", string.Empty);

  public sealed class Handler : IQueryHandler<T508InitManyQuery, string>
  {
    public Task<string> Handle(T508InitManyQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      string token = query.Token == Guid.Empty ? "empty" : "set";
      return Task.FromResult(
        $"wait:{query.Wait.TotalSeconds.ToString(CultureInfo.InvariantCulture)}"
        + $"|seed:{query.Seed.ToString(CultureInfo.InvariantCulture)}"
        + $"|token:{token}"
        + $"|label:{query.Label ?? "null"}"
        + $"|note:{query.Note}");
    }
  }
}

[Operation("t508reqdefault")]
public sealed class T508RequiredDefaultQuery : IQuery<string>
{
  public required TimeSpan Wait { get; set; } = TimeSpan.FromSeconds(1);

  public required string Tag { get; init; } = null!;

  public required T508Body Body { get; set; } = new();

  public sealed class Handler : IQueryHandler<T508RequiredDefaultQuery, string>
  {
    public Task<string> Handle(T508RequiredDefaultQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult(
        $"wait:{query.Wait.TotalSeconds.ToString(CultureInfo.InvariantCulture)}|tag:{query.Tag}|body:{query.Body.Name}");
    }
  }
}

[Operation("t508case")]
public sealed class T508CaseQuery : IQuery<string>
{
  [Option("mute")]
  public string Level { get; set; } = T508Seed.Text();

  public bool Mute { get; set; } = false;

  public sealed class Handler : IQueryHandler<T508CaseQuery, string>
  {
    public Task<string> Handle(T508CaseQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult($"level:{query.Level}|mute:{query.Mute}");
    }
  }
}

[Operation("t508nullenum")]
public sealed class T508NullableEnumQuery : IQuery<string>
{
  public T508Mode? Unset { get; set; } = null;

  public T508Mode? Preset { get; set; } = T508Mode.Slow;

  public T508Access? Combined { get; set; } = (T508Access)3;

  public T508Mode Shared { get; set; } = T508Shared.Color;

  public sealed class Handler : IQueryHandler<T508NullableEnumQuery, string>
  {
    public Task<string> Handle(T508NullableEnumQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      string unset = query.Unset?.ToString() ?? "null";
      string preset = query.Preset?.ToString() ?? "null";
      string combined = query.Combined?.ToString() ?? "null";
      return Task.FromResult($"unset:{unset}|preset:{preset}|combined:{combined}|shared:{query.Shared}");
    }
  }
}

[Operation("t508posdefault")]
public sealed record T508PositionalDefaultQuery(string Name, int Count = 5, T508Mode Mode = T508Mode.Slow) : IQuery<string>
{
  public sealed class Handler : IQueryHandler<T508PositionalDefaultQuery, string>
  {
    public Task<string> Handle(T508PositionalDefaultQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult($"name:{query.Name}|count:{query.Count.ToString(CultureInfo.InvariantCulture)}|mode:{query.Mode}");
    }
  }
}

[Operation("t508posinit")]
public sealed record T508PositionalInitQuery(string Name, string Note = "n") : IQuery<string>
{
  public string Name { get; init; } = Name.Trim();

  public string Note { get; init; } = Note;

  public sealed class Handler : IQueryHandler<T508PositionalInitQuery, string>
  {
    public Task<string> Handle(T508PositionalInitQuery query, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult($"name:{query.Name}|note:{query.Note}");
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.Auto
