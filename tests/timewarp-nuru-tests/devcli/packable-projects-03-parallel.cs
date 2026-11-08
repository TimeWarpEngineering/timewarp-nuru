#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Parallel evaluation of packable projects (kanban task 569). The evaluator
/// is a stub, so finish order, a named failure, and the progress line
/// are asserted without starting MSBuild. Real <c>dotnet msbuild</c> coverage
/// stays in <c>packable-projects-02-derivation-fixture.cs</c>.
/// </summary>
[TestTag("DevCli")]
public class PackableProjectParallelTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PackableProjectParallelTests>();

  public static async Task Parallel_completions_still_sort_by_package_id()
  {
    StringWriter progress = new();

    // Z is released only after A and M have returned, so Z finishes last.
    TaskCompletionSource aAndMReturned = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int finishedEarly = 0;

    IReadOnlyList<PackableProject> result = await PackableProjectService.EvaluateProjectsAsync
    (
      ["source/z/z.csproj", "source/m/m.csproj", "source/a/a.csproj"],
      async (path, cancellationToken) =>
      {
        string packageId;
        if (path.Contains("/z/", StringComparison.Ordinal))
        {
          packageId = "Z";
          await aAndMReturned.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (path.Contains("/m/", StringComparison.Ordinal))
        {
          packageId = "M";
        }
        else
        {
          packageId = "A";
        }

        CommandOutput output = Packable(packageId);
        if (packageId != "Z" && Interlocked.Increment(ref finishedEarly) == 2)
        {
          aAndMReturned.SetResult();
        }

        return output;
      },
      progress.WriteLine,
      CancellationToken.None,
      maxDegreeOfParallelism: 3
    ).ConfigureAwait(false);

    result.Select(project => project.PackageId).ShouldBe(["A", "M", "Z"]);
    progress.ToString().ShouldContain("Evaluating 3 projects for IsPackable…");
  }

  public static async Task Duplicate_package_id_names_paths_in_input_order_when_the_first_finishes_last()
  {
    // b returns before a does. Input order, not finish order, must name a first.
    TaskCompletionSource aStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    TaskCompletionSource bFinished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>
    (
      async () => await PackableProjectService.EvaluateProjectsAsync
      (
        ["source/a/a.csproj", "source/b/b.csproj"],
        async (path, cancellationToken) =>
        {
          if (path.EndsWith("a.csproj", StringComparison.Ordinal))
          {
            aStarted.TrySetResult();
            await bFinished.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
          }
          else
          {
            await aStarted.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            bFinished.TrySetResult();
          }

          return Packable("Same.Id");
        },
        _ => { },
        CancellationToken.None,
        maxDegreeOfParallelism: 2
      ).ConfigureAwait(false)
    ).ConfigureAwait(false);

    exception.Message.ShouldContain("both 'source/a/a.csproj' and 'source/b/b.csproj'");
  }

  public static async Task Failing_project_throws_invalid_operation_naming_the_project()
  {
    StringWriter progress = new();

    InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>
    (
      async () => await PackableProjectService.EvaluateProjectsAsync
      (
        ["source/ok/ok.csproj", "source/bad/bad.csproj"],
        (path, _) =>
        {
          if (path.EndsWith("bad.csproj", StringComparison.Ordinal))
          {
            return Task.FromResult(new CommandOutput("", "MSB4019 missing", 1));
          }

          return Task.FromResult(NotPackable());
        },
        progress.WriteLine,
        CancellationToken.None,
        maxDegreeOfParallelism: 2
      ).ConfigureAwait(false)
    ).ConfigureAwait(false);

    exception.Message.ShouldContain("source/bad/bad.csproj");
    exception.Message.ShouldContain("exit code 1");
    progress.ToString().ShouldContain("Evaluating 2 projects for IsPackable…");
  }

  public static async Task Two_failing_projects_throw_naming_both()
  {
    InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>
    (
      async () => await PackableProjectService.EvaluateProjectsAsync
      (
        ["source/a/a.csproj", "source/b/b.csproj"],
        (path, _) => Task.FromResult(new CommandOutput("", "failed " + path, 1)),
        _ => { },
        CancellationToken.None,
        maxDegreeOfParallelism: 2
      ).ConfigureAwait(false)
    ).ConfigureAwait(false);

    exception.Message.ShouldContain("source/a/a.csproj");
    exception.Message.ShouldContain("source/b/b.csproj");
    AggregateException aggregate = exception.InnerException.ShouldBeOfType<AggregateException>();
    aggregate.InnerExceptions.Count.ShouldBe(2);
    aggregate.InnerExceptions.ShouldContain(failure => failure.Message.Contains("source/a/a.csproj", StringComparison.Ordinal));
    aggregate.InnerExceptions.ShouldContain(failure => failure.Message.Contains("source/b/b.csproj", StringComparison.Ordinal));
  }

  public static async Task Unparseable_output_throws_naming_the_project()
  {
    InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>
    (
      async () => await PackableProjectService.EvaluateProjectsAsync
      (
        ["source/odd/odd.csproj"],
        (_, _) => Task.FromResult(new CommandOutput("not json", "", 0)),
        _ => { },
        CancellationToken.None,
        maxDegreeOfParallelism: 1
      ).ConfigureAwait(false)
    ).ConfigureAwait(false);

    exception.Message.ShouldContain("source/odd/odd.csproj");
    exception.Message.ShouldContain("unparseable");
  }

  public static async Task Blank_package_id_throws_naming_the_project()
  {
    InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>
    (
      async () => await PackableProjectService.EvaluateProjectsAsync
      (
        ["source/blank/blank.csproj"],
        (_, _) => Task.FromResult(new CommandOutput("""{"Properties":{"IsPackable":"true","PackageId":"  "}}""", "", 0)),
        _ => { },
        CancellationToken.None,
        maxDegreeOfParallelism: 1
      ).ConfigureAwait(false)
    ).ConfigureAwait(false);

    exception.Message.ShouldContain("source/blank/blank.csproj");
    exception.Message.ShouldContain("empty PackageId");
  }

  public static async Task Not_packable_projects_are_excluded()
  {
    IReadOnlyList<PackableProject> result = await PackableProjectService.EvaluateProjectsAsync
    (
      ["source/no/no.csproj", "source/yes/yes.csproj"],
      (path, _) => Task.FromResult(path.Contains("/yes/", StringComparison.Ordinal) ? Packable("Yes") : NotPackable()),
      _ => { },
      CancellationToken.None,
      maxDegreeOfParallelism: 2
    ).ConfigureAwait(false);

    result.Select(project => project.PackageId).ShouldBe(["Yes"]);
  }

  public static async Task No_projects_writes_no_progress_and_returns_empty()
  {
    StringWriter progress = new();

    IReadOnlyList<PackableProject> result = await PackableProjectService.EvaluateProjectsAsync
    (
      [],
      (_, _) => Task.FromResult(NotPackable()),
      progress.WriteLine,
      CancellationToken.None
    ).ConfigureAwait(false);

    result.ShouldBeEmpty();
    progress.ToString().ShouldBeEmpty();
  }

  private static CommandOutput Packable(string packageId) =>
    new("{\"Properties\":{\"IsPackable\":\"true\",\"PackageId\":\"" + packageId + "\"}}", "", 0);

  private static CommandOutput NotPackable() =>
    new("""{"Properties":{"IsPackable":"false","PackageId":"Skipped"}}""", "", 0);
}

} // namespace TimeWarp.Nuru.Tests.DevCli
