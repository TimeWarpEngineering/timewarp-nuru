#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Publish-state classification matrix for the check-version release gate.
/// Task 458-005: none/some/all published packages now map to None/Partial/All
/// so a partially-published version resumes the push instead of aborting.
/// </summary>
[TestTag("DevCli")]
public class PublishStateClassifierTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PublishStateClassifierTests>();

  public static async Task Zero_published_is_none()
  {
    PublishStateClassifier.Classify(5, 0).ShouldBe(PublishState.None);
    await Task.CompletedTask;
  }

  public static async Task All_published_is_all()
  {
    PublishStateClassifier.Classify(5, 5).ShouldBe(PublishState.All);
    await Task.CompletedTask;
  }

  public static async Task Majority_published_is_partial()
  {
    PublishStateClassifier.Classify(5, 3).ShouldBe(PublishState.Partial);
    await Task.CompletedTask;
  }

  public static async Task Minority_published_is_partial()
  {
    PublishStateClassifier.Classify(5, 1).ShouldBe(PublishState.Partial);
    await Task.CompletedTask;
  }

  public static async Task Single_package_none_published_is_none()
  {
    PublishStateClassifier.Classify(1, 0).ShouldBe(PublishState.None);
    await Task.CompletedTask;
  }

  public static async Task Single_package_published_is_all()
  {
    PublishStateClassifier.Classify(1, 1).ShouldBe(PublishState.All);
    await Task.CompletedTask;
  }

  public static async Task Zero_total_packages_throws()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => PublishStateClassifier.Classify(0, 0));
    await Task.CompletedTask;
  }

  public static async Task Negative_published_count_throws()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => PublishStateClassifier.Classify(5, -1));
    await Task.CompletedTask;
  }

  public static async Task Published_count_exceeding_total_throws()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => PublishStateClassifier.Classify(5, 6));
    await Task.CompletedTask;
  }

  // --- ParsePackageList (round-1 review finding #1: delimiter-only input
  // must yield an empty list, not a one-element list of whitespace, so the
  // caller's Count == 0 guard catches it before Classify ever sees zero
  // total packages) ---

  public static async Task Null_input_parses_to_empty()
  {
    PublishStateClassifier.ParsePackageList(null).ShouldBeEmpty();
    await Task.CompletedTask;
  }

  public static async Task Empty_input_parses_to_empty()
  {
    PublishStateClassifier.ParsePackageList("").ShouldBeEmpty();
    await Task.CompletedTask;
  }

  public static async Task Delimiter_only_input_parses_to_empty()
  {
    PublishStateClassifier.ParsePackageList(",").ShouldBeEmpty();
    await Task.CompletedTask;
  }

  public static async Task Whitespace_and_delimiter_input_parses_to_empty()
  {
    PublishStateClassifier.ParsePackageList(" , ").ShouldBeEmpty();
    await Task.CompletedTask;
  }

  public static async Task Adjacent_delimiters_drop_empty_entries()
  {
    PublishStateClassifier.ParsePackageList("A,,B").ShouldBe(["A", "B"]);
    await Task.CompletedTask;
  }

  public static async Task Entries_are_trimmed()
  {
    PublishStateClassifier.ParsePackageList(" A , B ").ShouldBe(["A", "B"]);
    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
