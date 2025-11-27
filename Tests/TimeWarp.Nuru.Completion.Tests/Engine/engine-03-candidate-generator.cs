#!/usr/bin/dotnet --

// Test: CandidateGenerator - Generates completion candidates from route match states
// Task: 065 - Implement CandidateGenerator

using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;
using Shouldly;

return await RunTests<CandidateGeneratorTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class CandidateGeneratorTests
{
  private static readonly CandidateGenerator Generator = CandidateGenerator.Instance;

  // ============================================================================
  // Helper Methods
  // ============================================================================

  private static Endpoint CreateEndpoint(string pattern)
  {
    NuruAppBuilder builder = new();
    builder.Map(pattern, () => 0);
    return builder.EndpointCollection.Endpoints[0];
  }

  private static RouteMatchState CreateViableState(
    string pattern,
    params NextCandidate[] nextCandidates)
  {
    return new RouteMatchState(
      Endpoint: CreateEndpoint(pattern),
      IsViable: true,
      IsExactMatch: false,
      SegmentsMatched: 1,
      ArgsConsumed: 1,
      OptionsUsed: new HashSet<string>(),
      NextCandidates: nextCandidates.ToList());
  }

  private static RouteMatchState CreateNotViableState(string pattern)
  {
    return RouteMatchState.NotViable(CreateEndpoint(pattern));
  }

  private static NextCandidate Literal(string value, string? description = null)
  {
    return new NextCandidate(
      CandidateKind.Literal, value, null, description, null, IsRequired: false);
  }

  private static NextCandidate Parameter(string value, string? type = null, string? description = null)
  {
    return new NextCandidate(
      CandidateKind.Parameter, value, null, description, type, IsRequired: true);
  }

  private static NextCandidate Option(string longForm, string? shortForm = null, string? description = null)
  {
    return new NextCandidate(
      CandidateKind.Option, longForm, shortForm, description, null, IsRequired: false);
  }

  // ============================================================================
  // Basic Generation Tests
  // ============================================================================

  public static async Task Should_return_empty_for_empty_states()
  {
    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate([], null);

    result.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_for_only_non_viable_states()
  {
    List<RouteMatchState> states =
    [
      CreateNotViableState("deploy"),
      CreateNotViableState("backup")
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_generate_candidates_from_viable_state()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("status", Literal("check"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.Count.ShouldBe(1);
    result.First().Value.ShouldBe("check");
    result.First().Type.ShouldBe(CompletionType.Command);

    await Task.CompletedTask;
  }

  public static async Task Should_aggregate_candidates_from_multiple_viable_states()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("deploy", Literal("staging")),
      CreateViableState("backup", Literal("daily"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.Count.ShouldBe(2);
    result.Select(c => c.Value).ShouldContain("staging");
    result.Select(c => c.Value).ShouldContain("daily");

    await Task.CompletedTask;
  }

  public static async Task Should_include_multiple_candidates_from_same_state()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("git", Literal("commit"), Literal("push"), Literal("pull"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.Count.ShouldBe(3);

    await Task.CompletedTask;
  }

  // ============================================================================
  // Partial Word Filtering Tests
  // ============================================================================

  public static async Task Should_filter_by_partial_word()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("git", Literal("status"), Literal("stash"), Literal("push"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, "st");

    result.Count.ShouldBe(2);
    result.All(c => c.Value.StartsWith("st", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_filter_case_insensitively()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("git", Literal("Status"), Literal("Stash"), Literal("Push"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, "st");

    result.Count.ShouldBe(2);

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_when_no_matches_for_partial()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("git", Literal("commit"), Literal("push"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, "xyz");

    result.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_return_all_when_partial_is_null()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("git", Literal("commit"), Literal("push"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.Count.ShouldBe(2);

    await Task.CompletedTask;
  }

  public static async Task Should_return_all_when_partial_is_empty()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("git", Literal("commit"), Literal("push"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, "");

    result.Count.ShouldBe(2);

    await Task.CompletedTask;
  }

  // ============================================================================
  // Deduplication Tests
  // ============================================================================

  public static async Task Should_deduplicate_same_value_from_multiple_states()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("route1", Literal("status")),
      CreateViableState("route2", Literal("status"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.Count.ShouldBe(1);
    result.First().Value.ShouldBe("status");

    await Task.CompletedTask;
  }

  public static async Task Should_deduplicate_case_insensitively()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("route1", Literal("Status")),
      CreateViableState("route2", Literal("STATUS"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.Count.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_keep_first_occurrence_when_deduplicating()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("route1", Literal("status", "First description")),
      CreateViableState("route2", Literal("status", "Second description"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.First().Description.ShouldBe("First description");

    await Task.CompletedTask;
  }

  // ============================================================================
  // Sorting Tests
  // ============================================================================

  public static async Task Should_sort_commands_before_options()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Option("--verbose"), Literal("status"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    List<CompletionCandidate> list = [.. result];
    list[0].Type.ShouldBe(CompletionType.Command);
    list[1].Type.ShouldBe(CompletionType.Option);

    await Task.CompletedTask;
  }

  public static async Task Should_sort_alphabetically_within_type_group()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("git", Literal("zebra"), Literal("alpha"), Literal("middle"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    List<CompletionCandidate> list = [.. result];
    list[0].Value.ShouldBe("alpha");
    list[1].Value.ShouldBe("middle");
    list[2].Value.ShouldBe("zebra");

    await Task.CompletedTask;
  }

  public static async Task Should_sort_alphabetically_case_insensitively()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("git", Literal("Zebra"), Literal("alpha"), Literal("MIDDLE"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    List<CompletionCandidate> list = [.. result];
    list[0].Value.ShouldBe("alpha");
    list[1].Value.ShouldBe("MIDDLE");
    list[2].Value.ShouldBe("Zebra");

    await Task.CompletedTask;
  }

  public static async Task Should_sort_options_alphabetically()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Option("--zebra"), Option("--alpha"), Option("--middle"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    List<CompletionCandidate> list = [.. result];
    list[0].Value.ShouldBe("--alpha");
    list[1].Value.ShouldBe("--middle");
    list[2].Value.ShouldBe("--zebra");

    await Task.CompletedTask;
  }

  // ============================================================================
  // Type Mapping Tests
  // ============================================================================

  public static async Task Should_map_literal_to_command_type()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Literal("deploy"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.First().Type.ShouldBe(CompletionType.Command);

    await Task.CompletedTask;
  }

  public static async Task Should_map_option_to_option_type()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Option("--force"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.First().Type.ShouldBe(CompletionType.Option);

    await Task.CompletedTask;
  }

  public static async Task Should_map_parameter_to_parameter_type()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Parameter("<name>"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.First().Type.ShouldBe(CompletionType.Parameter);

    await Task.CompletedTask;
  }

  public static async Task Should_map_file_parameter_type()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Parameter("<path>", type: "file"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.First().Type.ShouldBe(CompletionType.File);

    await Task.CompletedTask;
  }

  public static async Task Should_map_directory_parameter_type()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Parameter("<path>", type: "directory"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.First().Type.ShouldBe(CompletionType.Directory);

    await Task.CompletedTask;
  }

  public static async Task Should_map_dir_parameter_type_to_directory()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Parameter("<path>", type: "dir"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.First().Type.ShouldBe(CompletionType.Directory);

    await Task.CompletedTask;
  }

  // ============================================================================
  // Option Alternate Value Tests
  // ============================================================================

  public static async Task Should_include_alternate_option_form()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Option("--verbose", shortForm: "-v"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.Count.ShouldBe(2);
    result.Select(c => c.Value).ShouldContain("--verbose");
    result.Select(c => c.Value).ShouldContain("-v");

    await Task.CompletedTask;
  }

  public static async Task Should_filter_alternate_form_by_partial()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Option("--verbose", shortForm: "-v"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, "-v");

    result.Count.ShouldBe(1);
    result.First().Value.ShouldBe("-v");

    await Task.CompletedTask;
  }

  public static async Task Should_filter_primary_form_by_partial()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Option("--verbose", shortForm: "-v"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, "--v");

    result.Count.ShouldBe(1);
    result.First().Value.ShouldBe("--verbose");

    await Task.CompletedTask;
  }

  public static async Task Should_not_duplicate_same_alternate_form()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("route1", Option("--verbose", shortForm: "-v")),
      CreateViableState("route2", Option("--verbose", shortForm: "-v"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.Count.ShouldBe(2);
    result.Count(c => c.Value == "--verbose").ShouldBe(1);
    result.Count(c => c.Value == "-v").ShouldBe(1);

    await Task.CompletedTask;
  }

  // ============================================================================
  // Description Preservation Tests
  // ============================================================================

  public static async Task Should_preserve_description()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Literal("deploy", "Deploy the application"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.First().Description.ShouldBe("Deploy the application");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_null_description()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Literal("deploy", description: null))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.First().Description.ShouldBeNull();

    await Task.CompletedTask;
  }

  // ============================================================================
  // Complex Scenario Tests
  // ============================================================================

  public static async Task Should_handle_mixed_candidate_types()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("git",
        Literal("commit"),
        Literal("push"),
        Parameter("<branch>"),
        Option("--force", "-f"),
        Option("--verbose", "-v"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    // Should have: commit, push, <branch>, --force, -f, --verbose, -v = 7
    result.Count.ShouldBe(7);

    // Commands first, then parameters, then options
    List<CompletionCandidate> list = [.. result];
    int commandIndex = list.FindIndex(c => c.Value == "commit");
    int paramIndex = list.FindIndex(c => c.Value == "<branch>");
    int optionIndex = list.FindIndex(c => c.Value == "--force");

    commandIndex.ShouldBeLessThan(paramIndex);
    paramIndex.ShouldBeLessThan(optionIndex);

    await Task.CompletedTask;
  }

  public static async Task Should_filter_mixed_types_by_partial()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("git",
        Literal("commit"),
        Literal("checkout"),
        Option("--config"),
        Option("--core"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, "com");

    // Only "commit" starts with "com"
    result.Count.ShouldBe(1);
    result.First().Value.ShouldBe("commit");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_many_viable_states()
  {
    List<RouteMatchState> states = [];
    for (int i = 0; i < 100; i++)
    {
      states.Add(CreateViableState($"route{i}", Literal($"cmd{i}")));
    }

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.Count.ShouldBe(100);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_many_candidates_per_state()
  {
    NextCandidate[] candidates = [.. Enumerable.Range(0, 50)
      .Select(i => Literal($"cmd{i:D2}"))];

    List<RouteMatchState> states =
    [
      CreateViableState("app", candidates)
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.Count.ShouldBe(50);
    // Should be sorted alphabetically
    result.First().Value.ShouldBe("cmd00");
    result.Last().Value.ShouldBe("cmd49");

    await Task.CompletedTask;
  }

  // ============================================================================
  // Edge Case Tests
  // ============================================================================

  public static async Task Should_handle_empty_value()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Literal(""))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.Count.ShouldBe(1);
    result.First().Value.ShouldBe("");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_special_characters()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Literal("cmd:with:colons"), Literal("cmd/with/slashes"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, null);

    result.Count.ShouldBe(2);

    await Task.CompletedTask;
  }

  public static async Task Should_filter_partial_with_special_characters()
  {
    List<RouteMatchState> states =
    [
      CreateViableState("app", Literal("--config"), Literal("--context"))
    ];

    IReadOnlyCollection<CompletionCandidate> result = Generator.Generate(states, "--con");

    result.Count.ShouldBe(2);

    await Task.CompletedTask;
  }

  public static async Task Should_use_singleton_instance()
  {
    CandidateGenerator instance1 = CandidateGenerator.Instance;
    CandidateGenerator instance2 = CandidateGenerator.Instance;

    ReferenceEquals(instance1, instance2).ShouldBeTrue();

    await Task.CompletedTask;
  }
}
