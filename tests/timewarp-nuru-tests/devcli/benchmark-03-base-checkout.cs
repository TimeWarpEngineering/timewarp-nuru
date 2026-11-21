#!/usr/bin/env -S dotnet --

#region Purpose
// Pins BenchmarkBaseCheckout, the git half of `dev benchmark run --base` (kanban task 578),
// against a throwaway repository: the base ref checks out into a temporary detached
// worktree with the base content, the worktree is gone after dispose, a ref that names no
// commit is refused, and the head revision reports uncommitted changes.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>BenchmarkBaseCheckout against a fixture repository built with git.</summary>
[TestTag("DevCli")]
public class BenchmarkBaseCheckoutTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<BenchmarkBaseCheckoutTests>();

  public static async Task Base_ref_checks_out_base_content_and_dispose_removes_it()
  {
    string repo = await CreateFixtureAsync();
    try
    {
      string baseCommit = await GitAsync(repo, "rev-parse", "HEAD~1");
      string checkoutRoot;

      await using (BenchmarkBaseCheckout checkout = await BenchmarkBaseCheckout.CreateAsync(repo, "HEAD~1", CancellationToken.None))
      {
        checkoutRoot = checkout.Root;
        checkout.Commit.ShouldBe(baseCommit);
        (await File.ReadAllTextAsync(Path.Combine(checkout.Root, "value.txt"))).ShouldBe("base");
        // The head tree still has its own content.
        (await File.ReadAllTextAsync(Path.Combine(repo, "value.txt"))).ShouldBe("head");
      }

      Directory.Exists(checkoutRoot).ShouldBeFalse();
      (await GitAsync(repo, "worktree", "list")).ShouldNotContain(checkoutRoot);
    }
    finally
    {
      DeleteFixture(repo);
    }
  }

  public static async Task A_ref_that_names_no_commit_is_refused()
  {
    string repo = await CreateFixtureAsync();
    try
    {
      InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>
      (
        async () => await BenchmarkBaseCheckout.CreateAsync(repo, "no-such-ref", CancellationToken.None)
      );
      exception.Message.ShouldContain("--base 'no-such-ref' does not name a commit");

      // A ref that starts with a dash is a ref, not an option.
      (await BenchmarkBaseCheckout.ResolveCommitAsync(repo, "--all", CancellationToken.None)).ShouldBeNull();
    }
    finally
    {
      DeleteFixture(repo);
    }
  }

  public static async Task Head_revision_reports_commit_and_dirty_state()
  {
    string repo = await CreateFixtureAsync();
    try
    {
      string headCommit = await GitAsync(repo, "rev-parse", "HEAD");

      DevBenchmarkRevision clean = await BenchmarkBaseCheckout.ReadHeadAsync(repo, CancellationToken.None);
      clean.Ref.ShouldBe("HEAD");
      clean.Commit.ShouldBe(headCommit);
      clean.Dirty.ShouldBeFalse();

      await File.WriteAllTextAsync(Path.Combine(repo, "value.txt"), "edited");
      DevBenchmarkRevision dirty = await BenchmarkBaseCheckout.ReadHeadAsync(repo, CancellationToken.None);
      dirty.Dirty.ShouldBeTrue();
    }
    finally
    {
      DeleteFixture(repo);
    }
  }

  private static async Task<string> CreateFixtureAsync()
  {
    string repo = Directory.CreateTempSubdirectory("benchmark-checkout-fixture-").FullName;
    await GitAsync(repo, "init", "--quiet");
    await File.WriteAllTextAsync(Path.Combine(repo, "value.txt"), "base");
    await GitAsync(repo, "add", "value.txt");
    await GitAsync(repo, "commit", "--quiet", "--no-verify", "-m", "base");
    await File.WriteAllTextAsync(Path.Combine(repo, "value.txt"), "head");
    await GitAsync(repo, "commit", "--quiet", "--no-verify", "-am", "head");
    return repo;
  }

  private static async Task<string> GitAsync(string repo, params string[] arguments)
  {
    CommandOutput result = await Shell.Builder("git")
      .WithArguments(["-c", "user.name=fixture", "-c", "user.email=fixture@example.invalid", "-c", "commit.gpgsign=false", .. arguments])
      .WithWorkingDirectory(repo)
      .WithNoValidation()
      .CaptureAsync(CancellationToken.None);
    result.ExitCode.ShouldBe(0, $"git {string.Join(' ', arguments)}: {result.Stderr}");
    return result.Stdout.Trim();
  }

  private static void DeleteFixture(string repo)
  {
    if (!Directory.Exists(repo))
    {
      return;
    }

    // git writes read-only object files; Windows refuses to delete those.
    foreach (string file in Directory.EnumerateFiles(repo, "*", SearchOption.AllDirectories))
    {
      File.SetAttributes(file, FileAttributes.Normal);
    }

    Directory.Delete(repo, recursive: true);
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
