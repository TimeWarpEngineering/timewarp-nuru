#!/usr/bin/dotnet --

// Test: RouteMatchEngine - Matches parsed input against routes
// Task: 064 - Implement RouteMatchEngine

using TimeWarp.Nuru;
using Shouldly;

return await RunTests<RouteMatchEngineTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class RouteMatchEngineTests
{
  // Helper to create test endpoints
  private static EndpointCollection CreateEndpoints(params string[] patterns)
  {
    NuruAppBuilder builder = new();
    foreach (string pattern in patterns)
    {
      builder.Map(pattern, () => 0);
    }

    return builder.EndpointCollection;
  }

  // Helper to find match state for a specific pattern
  private static RouteMatchState? FindMatch(IReadOnlyList<RouteMatchState> states, string patternStart)
  {
    return states.FirstOrDefault(s =>
      s.Endpoint.RoutePattern.StartsWith(patternStart, StringComparison.OrdinalIgnoreCase));
  }

  // ==================== Empty Input ====================

  public static async Task Should_match_all_routes_for_empty_input()
  {
    EndpointCollection endpoints = CreateEndpoints("status", "deploy", "build");
    ParsedInput input = ParsedInput.Empty;

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    states.Count.ShouldBe(3);
    states.All(s => s.IsViable).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_suggest_all_commands_for_empty_input()
  {
    EndpointCollection endpoints = CreateEndpoints("status", "deploy", "build");
    ParsedInput input = ParsedInput.Empty;

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    IEnumerable<string> allNextValues = states
      .SelectMany(s => s.NextCandidates)
      .Select(c => c.Value);

    allNextValues.ShouldContain("status");
    allNextValues.ShouldContain("deploy");
    allNextValues.ShouldContain("build");

    await Task.CompletedTask;
  }

  // ==================== Partial Command Matching ====================

  public static async Task Should_filter_by_partial_command()
  {
    EndpointCollection endpoints = CreateEndpoints("status", "deploy", "delete");
    ParsedInput input = new([], "de", false);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    IEnumerable<string> matchingCommands = states
      .SelectMany(s => s.NextCandidates)
      .Where(c => c.Kind == CandidateKind.Literal)
      .Select(c => c.Value);

    matchingCommands.ShouldContain("deploy");
    matchingCommands.ShouldContain("delete");
    matchingCommands.ShouldNotContain("status");

    await Task.CompletedTask;
  }

  public static async Task Should_be_case_insensitive_for_partial()
  {
    EndpointCollection endpoints = CreateEndpoints("Deploy", "Status");
    ParsedInput input = new([], "de", false);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    IEnumerable<string> matchingCommands = states
      .SelectMany(s => s.NextCandidates)
      .Where(c => c.Kind == CandidateKind.Literal)
      .Select(c => c.Value);

    matchingCommands.ShouldContain("Deploy");

    await Task.CompletedTask;
  }

  // ==================== Complete Command Matching ====================

  public static async Task Should_match_complete_literal()
  {
    EndpointCollection endpoints = CreateEndpoints("status", "deploy");
    ParsedInput input = new(["status"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? statusState = FindMatch(states, "status");
    statusState.ShouldNotBeNull();
    statusState.IsViable.ShouldBeTrue();
    statusState.SegmentsMatched.ShouldBe(1);

    RouteMatchState? deployState = FindMatch(states, "deploy");
    deployState.ShouldNotBeNull();
    deployState.IsViable.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_be_case_insensitive_for_literal_match()
  {
    EndpointCollection endpoints = CreateEndpoints("status");
    ParsedInput input = new(["STATUS"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? statusState = FindMatch(states, "status");
    statusState.ShouldNotBeNull();
    statusState.IsViable.ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ==================== Subcommand Matching ====================

  public static async Task Should_match_subcommands()
  {
    EndpointCollection endpoints = CreateEndpoints("git status", "git commit", "git push");
    ParsedInput input = new(["git"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    states.All(s => s.IsViable).ShouldBeTrue();

    IEnumerable<string> subcommands = states
      .SelectMany(s => s.NextCandidates)
      .Where(c => c.Kind == CandidateKind.Literal)
      .Select(c => c.Value)
      .Distinct();

    subcommands.ShouldContain("status");
    subcommands.ShouldContain("commit");
    subcommands.ShouldContain("push");

    await Task.CompletedTask;
  }

  public static async Task Should_filter_subcommands_by_partial()
  {
    EndpointCollection endpoints = CreateEndpoints("git status", "git commit", "git push");
    ParsedInput input = new(["git"], "co", false);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    IEnumerable<string> subcommands = states
      .SelectMany(s => s.NextCandidates)
      .Where(c => c.Kind == CandidateKind.Literal)
      .Select(c => c.Value)
      .Distinct();

    subcommands.ShouldContain("commit");
    subcommands.ShouldNotContain("status");
    subcommands.ShouldNotContain("push");

    await Task.CompletedTask;
  }

  // ==================== Parameter Matching ====================

  public static async Task Should_suggest_parameter_after_command()
  {
    EndpointCollection endpoints = CreateEndpoints("greet {name}");
    ParsedInput input = new(["greet"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? greetState = FindMatch(states, "greet");
    greetState.ShouldNotBeNull();
    greetState.IsViable.ShouldBeTrue();

    NextCandidate? paramCandidate = greetState.NextCandidates
      .FirstOrDefault(c => c.Kind == CandidateKind.Parameter);
    paramCandidate.ShouldNotBeNull();
    paramCandidate.Value.ShouldBe("<name>");

    await Task.CompletedTask;
  }

  public static async Task Should_consume_parameter_value()
  {
    EndpointCollection endpoints = CreateEndpoints("greet {name}");
    ParsedInput input = new(["greet", "World"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? greetState = FindMatch(states, "greet");
    greetState.ShouldNotBeNull();
    greetState.IsViable.ShouldBeTrue();
    greetState.SegmentsMatched.ShouldBe(2);
    greetState.IsExactMatch.ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ==================== Option Matching ====================

  public static async Task Should_suggest_options_after_command()
  {
    EndpointCollection endpoints = CreateEndpoints("build --verbose,-v");
    ParsedInput input = new(["build"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? buildState = FindMatch(states, "build");
    buildState.ShouldNotBeNull();

    IEnumerable<string> options = buildState.NextCandidates
      .Where(c => c.Kind == CandidateKind.Option)
      .Select(c => c.Value);

    options.ShouldContain("--verbose");

    await Task.CompletedTask;
  }

  public static async Task Should_filter_options_by_partial()
  {
    EndpointCollection endpoints = CreateEndpoints("test --verbose,-v --debug,-d");
    ParsedInput input = new(["test"], "--v", false);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? testState = FindMatch(states, "test");
    testState.ShouldNotBeNull();

    IEnumerable<string> options = testState.NextCandidates
      .Where(c => c.Kind == CandidateKind.Option)
      .Select(c => c.Value);

    options.ShouldContain("--verbose");
    options.ShouldNotContain("--debug");

    await Task.CompletedTask;
  }

  public static async Task Should_track_used_options()
  {
    EndpointCollection endpoints = CreateEndpoints("test --verbose,-v --debug,-d");
    ParsedInput input = new(["test", "--verbose"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? testState = FindMatch(states, "test");
    testState.ShouldNotBeNull();
    testState.OptionsUsed.ShouldContain("--verbose");

    IEnumerable<string> options = testState.NextCandidates
      .Where(c => c.Kind == CandidateKind.Option)
      .Select(c => c.Value);

    options.ShouldNotContain("--verbose");
    options.ShouldContain("--debug");

    await Task.CompletedTask;
  }

  public static async Task Should_consume_option_with_value()
  {
    EndpointCollection endpoints = CreateEndpoints("deploy --env {environment}");
    ParsedInput input = new(["deploy", "--env", "prod"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? deployState = FindMatch(states, "deploy");
    deployState.ShouldNotBeNull();
    deployState.IsViable.ShouldBeTrue();
    deployState.OptionsUsed.ShouldContain("--env");
    deployState.ArgsConsumed.ShouldBe(3);

    await Task.CompletedTask;
  }

  // ==================== Mixed Segments ====================

  public static async Task Should_handle_command_with_param_and_options()
  {
    EndpointCollection endpoints = CreateEndpoints("backup {source} --compress,-c");
    ParsedInput input = new(["backup", "data"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? backupState = FindMatch(states, "backup");
    backupState.ShouldNotBeNull();
    backupState.IsViable.ShouldBeTrue();
    backupState.SegmentsMatched.ShouldBe(2);

    IEnumerable<string> options = backupState.NextCandidates
      .Where(c => c.Kind == CandidateKind.Option)
      .Select(c => c.Value);

    options.ShouldContain("--compress");

    await Task.CompletedTask;
  }

  public static async Task Should_filter_option_by_partial_after_param()
  {
    EndpointCollection endpoints = CreateEndpoints("backup {source} --compress,-c --output,-o");
    ParsedInput input = new(["backup", "data"], "--c", false);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? backupState = FindMatch(states, "backup");
    backupState.ShouldNotBeNull();

    IEnumerable<string> options = backupState.NextCandidates
      .Where(c => c.Kind == CandidateKind.Option)
      .Select(c => c.Value);

    options.ShouldContain("--compress");
    options.ShouldNotContain("--output");

    await Task.CompletedTask;
  }

  // ==================== Non-Viable Routes ====================

  public static async Task Should_mark_non_matching_route_as_not_viable()
  {
    EndpointCollection endpoints = CreateEndpoints("status", "deploy");
    ParsedInput input = new(["unknown"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    states.All(s => !s.IsViable).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_too_many_arguments()
  {
    EndpointCollection endpoints = CreateEndpoints("status");
    ParsedInput input = new(["status", "extra", "args"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? statusState = FindMatch(states, "status");
    statusState.ShouldNotBeNull();
    statusState.IsViable.ShouldBeFalse();

    await Task.CompletedTask;
  }

  // ==================== Optional Parameters ====================

  public static async Task Should_handle_optional_parameter()
  {
    EndpointCollection endpoints = CreateEndpoints("deploy {env} {tag?}");
    ParsedInput input = new(["deploy", "prod"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? deployState = FindMatch(states, "deploy");
    deployState.ShouldNotBeNull();
    deployState.IsViable.ShouldBeTrue();
    deployState.IsExactMatch.ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ==================== Catch-All Parameters ====================

  public static async Task Should_handle_catch_all_parameter()
  {
    EndpointCollection endpoints = CreateEndpoints("exec {*args}");
    ParsedInput input = new(["exec", "arg1", "arg2", "arg3"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? execState = FindMatch(states, "exec");
    execState.ShouldNotBeNull();
    execState.IsViable.ShouldBeTrue();

    await Task.CompletedTask;
  }

  // ==================== Short Options ====================

  public static async Task Should_match_short_option()
  {
    EndpointCollection endpoints = CreateEndpoints("build --verbose,-v");
    ParsedInput input = new(["build", "-v"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? buildState = FindMatch(states, "build");
    buildState.ShouldNotBeNull();
    buildState.IsViable.ShouldBeTrue();
    buildState.OptionsUsed.ShouldContain("--verbose");
    buildState.OptionsUsed.ShouldContain("-v");

    await Task.CompletedTask;
  }

  public static async Task Should_filter_by_short_option_partial()
  {
    EndpointCollection endpoints = CreateEndpoints("test --verbose,-v --debug,-d");
    ParsedInput input = new(["test"], "-v", false);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? testState = FindMatch(states, "test");
    testState.ShouldNotBeNull();

    IEnumerable<string> options = testState.NextCandidates
      .Where(c => c.Kind == CandidateKind.Option)
      .Select(c => c.Value);

    options.ShouldContain("-v");
    options.ShouldNotContain("-d");

    await Task.CompletedTask;
  }

  // ==================== IsExactMatch ====================

  public static async Task Should_be_exact_match_when_all_required_consumed()
  {
    EndpointCollection endpoints = CreateEndpoints("greet {name}");
    ParsedInput input = new(["greet", "World"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? greetState = FindMatch(states, "greet");
    greetState.ShouldNotBeNull();
    greetState.IsExactMatch.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_be_exact_match_when_required_remaining()
  {
    EndpointCollection endpoints = CreateEndpoints("greet {name}");
    ParsedInput input = new(["greet"], null, true);

    IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

    RouteMatchState? greetState = FindMatch(states, "greet");
    greetState.ShouldNotBeNull();
    greetState.IsExactMatch.ShouldBeFalse();

    await Task.CompletedTask;
  }
}
