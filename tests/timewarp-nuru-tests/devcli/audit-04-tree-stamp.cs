#!/usr/bin/env -S dotnet --

#region Purpose
// Pins DevAuditTreeStamp, DevCli's ready-made `dev audit` pass cache (kanban task 601): the
// key ignores kanban/ and tracks the SDK, a change outside kanban/ has no key, and the pass
// file round-trips under artifacts/dev-audit/.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// DevAuditTreeStamp's pure key and status rules, the pass file, and one real git repo.
/// </summary>
[TestTag("DevCli")]
public class DevAuditTreeStampTests
{
  private const string Listing =
    "100644 blob 1111111111111111111111111111111111111111\tDirectory.Build.props\n" +
    "040000 tree 2222222222222222222222222222222222222222\tkanban\n" +
    "040000 tree 3333333333333333333333333333333333333333\tsource\n";

  [ModuleInitializer]
  internal static void Register() => RegisterTests<DevAuditTreeStampTests>();

  public static async Task Key_ignores_the_kanban_entry()
  {
    DevAuditTreeStamp stamp = DevAuditTreeStamp.Create();
    string otherKanban = Listing.Replace("2222222222222222222222222222222222222222", "9999999999999999999999999999999999999999", StringComparison.Ordinal);

    stamp.Compute(otherKanban, "11.0.100").ShouldBe(stamp.Compute(Listing, "11.0.100"));
    await Task.CompletedTask;
  }

  public static async Task Key_changes_with_source_and_with_the_sdk()
  {
    DevAuditTreeStamp stamp = DevAuditTreeStamp.Create();
    string key = stamp.Compute(Listing, "11.0.100");
    string otherSource = Listing.Replace("3333333333333333333333333333333333333333", "8888888888888888888888888888888888888888", StringComparison.Ordinal);

    stamp.Compute(otherSource, "11.0.100").ShouldNotBe(key);
    stamp.Compute(Listing, "11.0.101").ShouldNotBe(key);
    key.Length.ShouldBe(64);
    await Task.CompletedTask;
  }

  public static async Task Custom_ignored_entries_replace_the_default()
  {
    DevAuditTreeStamp stamp = DevAuditTreeStamp.Create("docs");
    string otherKanban = Listing.Replace("2222222222222222222222222222222222222222", "9999999999999999999999999999999999999999", StringComparison.Ordinal);

    stamp.Compute(otherKanban, "11.0.100").ShouldNotBe(stamp.Compute(Listing, "11.0.100"));
    stamp.OnlyIgnoredChanges("?? docs/readme.md\n").ShouldBeTrue();
    stamp.OnlyIgnoredChanges("?? kanban/x.md\n").ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Only_kanban_changes_keep_a_key()
  {
    DevAuditTreeStamp stamp = DevAuditTreeStamp.Create();

    stamp.OnlyIgnoredChanges("").ShouldBeTrue();
    stamp.OnlyIgnoredChanges(" M kanban/to-do/1/task.md\n?? kanban/new.md\n").ShouldBeTrue();
    stamp.OnlyIgnoredChanges("R  kanban/a.md -> kanban/b.md\n").ShouldBeTrue();
    stamp.OnlyIgnoredChanges(" M kanban/a.md\n M source/x.cs\n").ShouldBeFalse();
    stamp.OnlyIgnoredChanges("R  kanban/a.md -> source/b.md\n").ShouldBeFalse();
    stamp.OnlyIgnoredChanges("?? kanbanx/a.md\n").ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Pass_file_round_trips_under_artifacts()
  {
    string root = Directory.CreateTempSubdirectory("dev-audit-stamp-").FullName;
    try
    {
      DevAuditTreeStamp stamp = DevAuditTreeStamp.Create();
      DevAuditContext context = new(root);

      stamp.HasPass(context, "k1").ShouldBeFalse();
      stamp.RecordPass(context, "k1");
      stamp.HasPass(context, "k1").ShouldBeTrue();
      stamp.HasPass(context, "k2").ShouldBeFalse();
      File.Exists(Path.Combine(root, DevAuditTreeStamp.RelativePath)).ShouldBeTrue();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }

    await Task.CompletedTask;
  }

  public static async Task Key_from_a_real_repo_follows_the_worktree()
  {
    string root = Directory.CreateTempSubdirectory("dev-audit-stamp-git-").FullName;
    try
    {
      (await GitAsync(root, "init", "-q")).ShouldBe(0);
      await File.WriteAllTextAsync(Path.Combine(root, "a.txt"), "a");
      (await GitAsync(root, "add", "a.txt")).ShouldBe(0);
      (await GitAsync(root, "-c", "user.name=test", "-c", "user.email=test@example.invalid", "-c", "commit.gpgsign=false", "commit", "-q", "-m", "init")).ShouldBe(0);

      DevAuditTreeStamp stamp = DevAuditTreeStamp.Create();
      DevAuditContext context = new(root);
      string? clean = await stamp.KeyAsync(context, CancellationToken.None);
      clean.ShouldNotBeNull();

      Directory.CreateDirectory(Path.Combine(root, "kanban"));
      await File.WriteAllTextAsync(Path.Combine(root, "kanban", "task.md"), "note");
      (await stamp.KeyAsync(context, CancellationToken.None)).ShouldBe(clean);

      await File.WriteAllTextAsync(Path.Combine(root, "b.txt"), "b");
      (await stamp.KeyAsync(context, CancellationToken.None)).ShouldBeNull();
    }
    finally
    {
      Directory.Delete(root, recursive: true);
    }
  }

  private static Task<int> GitAsync(string root, params string[] arguments) =>
    Shell.Builder("git")
      .WithArguments(arguments)
      .WithWorkingDirectory(root)
      .WithNoValidation()
      .RunAsync();
}

} // namespace TimeWarp.Nuru.Tests.DevCli
