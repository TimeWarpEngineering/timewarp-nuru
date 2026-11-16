#!/usr/bin/env -S dotnet --

#region Purpose
// Pins PullRequestBuildClassifier (task 595): every rule row ported from ganda's
// WorkflowChangeFilter.RequiresBuild, the repo additions and their protected
// roots, and every never-skip case of the one-call decision.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// The pull request build rule and decision shipped in TimeWarp.Nuru.DevCli content.
/// </summary>
[TestTag("DevCli")]
public class PullRequestBuildClassifierTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PullRequestBuildClassifierTests>();

  private const string BaseSha = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
  private const string HeadSha = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

  // --- Default rule rows (ganda WorkflowChangeFilter.RequiresBuild cases) ---

  public static async Task Empty_diff_does_not_require_a_build()
  {
    PullRequestBuildClassifier.RequiresBuild([]).ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Declared_non_build_trees_skip()
  {
    PullRequestBuildClassifier.RequiresBuild(["kanban/task.md"]).ShouldBeFalse();
    PullRequestBuildClassifier.RequiresBuild(["documentation/readme.md"]).ShouldBeFalse();
    PullRequestBuildClassifier.RequiresBuild(["assets/logo.png"]).ShouldBeFalse();
    PullRequestBuildClassifier.RequiresBuild(["notes/a.txt"]).ShouldBeFalse();
    PullRequestBuildClassifier.RequiresBuild([".github/CODEOWNERS"]).ShouldBeFalse();
    PullRequestBuildClassifier.RequiresBuild(["documentation/readme.md", ".gitignore"]).ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Github_workflows_build()
  {
    PullRequestBuildClassifier.RequiresBuild([".github/workflows/workflow.yml"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild([".github/workflows"]).ShouldBeTrue();
    await Task.CompletedTask;
  }

  public static async Task Gitignore_anywhere_skips()
  {
    PullRequestBuildClassifier.RequiresBuild([".gitignore"]).ShouldBeFalse();
    PullRequestBuildClassifier.RequiresBuild(["samples/.gitignore"]).ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Markdown_outside_build_roots_skips()
  {
    PullRequestBuildClassifier.RequiresBuild(["readme.md"]).ShouldBeFalse();
    PullRequestBuildClassifier.RequiresBuild(["samples/overview.md"]).ShouldBeFalse();
    PullRequestBuildClassifier.RequiresBuild(["CHANGELOG.MD"]).ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Build_roots_build_including_markdown()
  {
    PullRequestBuildClassifier.RequiresBuild(["source/readme.md"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["./source/readme.md"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["source\\readme.md"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["tests/readme.md"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["tools/readme.md"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["msbuild/readme.md"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["source/app/role-reviewer.md"]).ShouldBeTrue();
    await Task.CompletedTask;
  }

  public static async Task Editorconfig_builds()
  {
    PullRequestBuildClassifier.RequiresBuild([".editorconfig"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["nested/.editorconfig"]).ShouldBeTrue();
    await Task.CompletedTask;
  }

  public static async Task Anything_else_builds()
  {
    PullRequestBuildClassifier.RequiresBuild(["source-notes/a.cs"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["install-ganda.cs"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["app.slnx"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["src/app.csproj"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["Directory.Build.targets"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["global.json"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["kanban"]).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild([""]).ShouldBeTrue();
    await Task.CompletedTask;
  }

  public static async Task One_build_path_among_non_build_paths_builds()
  {
    PullRequestBuildClassifier.RequiresBuild(["kanban/task.md", "source/a.cs"]).ShouldBeTrue();
    await Task.CompletedTask;
  }

  // --- Repo additions (dev.jsonc pullRequestBuild) ---

  public static async Task Build_paths_override_default_non_build_paths()
  {
    PullRequestBuildConfig config = new() { BuildPaths = ["readme.md", "assets/logo.png", "documentation/api/"] };
    PullRequestBuildClassifier.RequiresBuild(["readme.md"], config).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["assets/logo.png"], config).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["documentation/api/x.md"], config).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["assets/mascot/a.png"], config).ShouldBeFalse();
    PullRequestBuildClassifier.RequiresBuild(["samples/readme.md"], config).ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Non_build_paths_add_trees_and_files()
  {
    PullRequestBuildConfig config = new() { NonBuildPaths = ["skills/**", ".agent/", ".claude", "./scratch.txt"] };
    PullRequestBuildClassifier.RequiresBuild(["skills/tw-nuru-verify/probes/a.cs"], config).ShouldBeFalse();
    PullRequestBuildClassifier.RequiresBuild([".agent/AGENTS.md", ".claude/settings.json"], config).ShouldBeFalse();
    PullRequestBuildClassifier.RequiresBuild(["scratch.txt"], config).ShouldBeFalse();
    PullRequestBuildClassifier.RequiresBuild(["skills-extra/a.cs"], config).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["nested/scratch.txt"], config).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["skills/a.cs"]).ShouldBeTrue();
    await Task.CompletedTask;
  }

  public static async Task Non_build_paths_cannot_cover_protected_paths()
  {
    PullRequestBuildConfig config = new()
    {
      NonBuildPaths = ["source/**", "tests", "tools/", "msbuild/**", ".github/workflows/**", ".editorconfig", "nested/"]
    };
    PullRequestBuildClassifier.RequiresBuild(["source/a.cs"], config).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["tests/a.cs"], config).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["tools/dev-cli/dev.cs"], config).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["msbuild/a.targets"], config).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild([".github/workflows/workflow.yml"], config).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild([".editorconfig"], config).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["nested/.editorconfig"], config).ShouldBeTrue();
    PullRequestBuildClassifier.RequiresBuild(["nested/a.cs"], config).ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Repo_config_reads_pull_request_build()
  {
    RepoConfig config = RepoConfigService.Parse("""
      {
        // comment
        "pullRequestBuild": {
          "buildPaths": ["readme.md"],
          "nonBuildPaths": ["skills/**"],
        }
      }
      """);
    config.PullRequestBuild.ShouldNotBeNull();
    config.PullRequestBuild.BuildPaths.ShouldBe(["readme.md"]);
    config.PullRequestBuild.NonBuildPaths.ShouldBe(["skills/**"]);
    await Task.CompletedTask;
  }

  // --- Event JSON ---

  public static async Task Event_json_reads_base_and_head()
  {
    PullRequestBuildClassifier.TryReadPullRequestShas(EventJson(BaseSha, HeadSha), out string baseSha, out string headSha)
      .ShouldBeTrue();
    baseSha.ShouldBe(BaseSha);
    headSha.ShouldBe(HeadSha);
    PullRequestBuildClassifier.TryReadPullRequestShas("{}", out _, out _).ShouldBeFalse();
    PullRequestBuildClassifier.TryReadPullRequestShas("[]", out _, out _).ShouldBeFalse();
    PullRequestBuildClassifier.TryReadPullRequestShas("not json", out _, out _).ShouldBeFalse();
    PullRequestBuildClassifier.TryReadPullRequestShas(null, out _, out _).ShouldBeFalse();
    PullRequestBuildClassifier.TryReadPullRequestShas(EventJson("not-a-sha", HeadSha), out _, out _).ShouldBeFalse();
    await Task.CompletedTask;
  }

  // --- Never-skip cases of the one-call decision ---

  public static async Task Push_release_dispatch_and_local_runs_build()
  {
    string root = Directory.GetCurrentDirectory();
    foreach (string? eventName in new[] { "push", "release", "workflow_dispatch", "pull_request_target", null, "" })
    {
      PullRequestBuildDecision decision =
        await PullRequestBuildClassifier.DecideAsync(root, eventName, "/nonexistent/event.json", null);
      decision.RequiresBuild.ShouldBeTrue(eventName ?? "(null)");
      decision.Message.ShouldContain("Full build");
    }
  }

  public static async Task Missing_or_unreadable_event_builds()
  {
    string root = Directory.GetCurrentDirectory();
    (await PullRequestBuildClassifier.DecideAsync(root, "pull_request", null, null)).RequiresBuild.ShouldBeTrue();
    (await PullRequestBuildClassifier.DecideAsync(root, "pull_request", "", null)).RequiresBuild.ShouldBeTrue();

    DirectoryInfo work = Directory.CreateTempSubdirectory("pr-build-event-");
    try
    {
      string missing = Path.Combine(work.FullName, "missing.json");
      PullRequestBuildDecision noFile = await PullRequestBuildClassifier.DecideAsync(root, "pull_request", missing, null);
      noFile.RequiresBuild.ShouldBeTrue();
      noFile.Message.ShouldContain("could not be read");

      string malformed = Path.Combine(work.FullName, "malformed.json");
      await File.WriteAllTextAsync(malformed, "{ not json");
      (await PullRequestBuildClassifier.DecideAsync(root, "pull_request", malformed, null)).RequiresBuild.ShouldBeTrue();

      string noShas = Path.Combine(work.FullName, "no-shas.json");
      await File.WriteAllTextAsync(noShas, """{"pull_request":{}}""");
      PullRequestBuildDecision noShaDecision = await PullRequestBuildClassifier.DecideAsync(root, "pull_request", noShas, null);
      noShaDecision.RequiresBuild.ShouldBeTrue();
      noShaDecision.Message.ShouldContain("base.sha");
    }
    finally
    {
      work.Delete(recursive: true);
    }
  }

  public static async Task Failed_diff_builds()
  {
    DirectoryInfo repo = Directory.CreateTempSubdirectory("pr-build-diff-");
    try
    {
      await CommitFileAsync(repo.FullName, "kanban/task.md", "a", "base");
      string eventPath = await WriteEventAsync(repo.FullName, BaseSha, HeadSha);
      PullRequestBuildDecision decision =
        await PullRequestBuildClassifier.DecideAsync(repo.FullName, "pull_request", eventPath, null);
      decision.RequiresBuild.ShouldBeTrue();
      decision.Message.ShouldContain("git diff");
    }
    finally
    {
      repo.Delete(recursive: true);
    }
  }

  public static async Task Invalid_config_entry_builds()
  {
    DirectoryInfo repo = Directory.CreateTempSubdirectory("pr-build-config-");
    try
    {
      (string baseSha, string headSha) = await KanbanOnlyHistoryAsync(repo.FullName);
      string eventPath = await WriteEventAsync(repo.FullName, baseSha, headSha);
      foreach (string invalid in new[] { "**", "*.md", "", "docs/*/x" })
      {
        PullRequestBuildConfig config = new() { NonBuildPaths = [invalid] };
        PullRequestBuildDecision decision =
          await PullRequestBuildClassifier.DecideAsync(repo.FullName, "pull_request", eventPath, config);
        decision.RequiresBuild.ShouldBeTrue(invalid);
        decision.Message.ShouldContain("pullRequestBuild");
      }
    }
    finally
    {
      repo.Delete(recursive: true);
    }
  }

  // --- End to end through git ---

  public static async Task Kanban_only_pull_request_skips()
  {
    DirectoryInfo repo = Directory.CreateTempSubdirectory("pr-build-skip-");
    try
    {
      (string baseSha, string headSha) = await KanbanOnlyHistoryAsync(repo.FullName);
      string eventPath = await WriteEventAsync(repo.FullName, baseSha, headSha);
      PullRequestBuildDecision decision =
        await PullRequestBuildClassifier.DecideAsync(repo.FullName, "pull_request", eventPath, null);
      decision.RequiresBuild.ShouldBeFalse();
      decision.Message.ShouldStartWith(PullRequestBuildClassifier.SkipMessage);
      decision.ChangedPaths.ShouldBe(["kanban/task.md"]);
    }
    finally
    {
      repo.Delete(recursive: true);
    }
  }

  public static async Task Empty_pull_request_diff_skips()
  {
    DirectoryInfo repo = Directory.CreateTempSubdirectory("pr-build-empty-");
    try
    {
      await CommitFileAsync(repo.FullName, "source/a.cs", "class A {}", "base");
      string head = await HeadAsync(repo.FullName);
      string eventPath = await WriteEventAsync(repo.FullName, head, head);
      PullRequestBuildDecision decision =
        await PullRequestBuildClassifier.DecideAsync(repo.FullName, "pull_request", eventPath, null);
      decision.RequiresBuild.ShouldBeFalse();
      decision.ChangedPaths.ShouldBeEmpty();
    }
    finally
    {
      repo.Delete(recursive: true);
    }
  }

  public static async Task Source_pull_request_builds()
  {
    DirectoryInfo repo = Directory.CreateTempSubdirectory("pr-build-source-");
    try
    {
      await CommitFileAsync(repo.FullName, "kanban/task.md", "a", "base");
      string baseSha = await HeadAsync(repo.FullName);
      await CommitFileAsync(repo.FullName, "source/a.cs", "class A {}", "source");
      string headSha = await HeadAsync(repo.FullName);
      string eventPath = await WriteEventAsync(repo.FullName, baseSha, headSha);
      PullRequestBuildDecision decision =
        await PullRequestBuildClassifier.DecideAsync(repo.FullName, "pull_request", eventPath, null);
      decision.RequiresBuild.ShouldBeTrue();
      decision.Message.ShouldContain("source/a.cs");
    }
    finally
    {
      repo.Delete(recursive: true);
    }
  }

  public static async Task Build_path_addition_builds_end_to_end()
  {
    DirectoryInfo repo = Directory.CreateTempSubdirectory("pr-build-readme-");
    try
    {
      await CommitFileAsync(repo.FullName, "readme.md", "a", "base");
      string baseSha = await HeadAsync(repo.FullName);
      await CommitFileAsync(repo.FullName, "readme.md", "b", "readme");
      string headSha = await HeadAsync(repo.FullName);
      string eventPath = await WriteEventAsync(repo.FullName, baseSha, headSha);

      (await PullRequestBuildClassifier.DecideAsync(repo.FullName, "pull_request", eventPath, null))
        .RequiresBuild.ShouldBeFalse();
      PullRequestBuildConfig config = new() { BuildPaths = ["readme.md"] };
      (await PullRequestBuildClassifier.DecideAsync(repo.FullName, "pull_request", eventPath, config))
        .RequiresBuild.ShouldBeTrue();
    }
    finally
    {
      repo.Delete(recursive: true);
    }
  }

  // --- Helpers ---

  private static string EventJson(string baseSha, string headSha) =>
    "{\"pull_request\":{\"base\":{\"sha\":\"" + baseSha + "\"},\"head\":{\"sha\":\"" + headSha + "\"}}}";

  private static async Task<string> WriteEventAsync(string root, string baseSha, string headSha)
  {
    string path = Path.Combine(root, ".git", "test-event.json");
    await File.WriteAllTextAsync(path, EventJson(baseSha, headSha));
    return path;
  }

  private static async Task<(string BaseSha, string HeadSha)> KanbanOnlyHistoryAsync(string root)
  {
    await CommitFileAsync(root, "kanban/task.md", "a", "base");
    string baseSha = await HeadAsync(root);
    await CommitFileAsync(root, "kanban/task.md", "b", "kanban");
    return (baseSha, await HeadAsync(root));
  }

  private static async Task CommitFileAsync(string root, string relativePath, string content, string message)
  {
    string fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    await File.WriteAllTextAsync(fullPath, content);
    if (!Directory.Exists(Path.Combine(root, ".git")))
    {
      (await GitAsync(root, "init", "-b", "master")).ExitCode.ShouldBe(0);
    }

    (await GitAsync(root, "add", relativePath)).ExitCode.ShouldBe(0);
    CommandOutput commit = await GitAsync(
      root,
      "-c", "user.email=dev@example.com",
      "-c", "user.name=dev",
      "-c", "commit.gpgsign=false",
      "-c", "core.hooksPath=/dev/null",
      "commit", "-m", message);
    commit.ExitCode.ShouldBe(0, commit.Stderr);
  }

  private static async Task<string> HeadAsync(string root)
  {
    CommandOutput head = await GitAsync(root, "rev-parse", "HEAD");
    head.ExitCode.ShouldBe(0, head.Stderr);
    return head.Stdout.Trim();
  }

  private static Task<CommandOutput> GitAsync(string root, params string[] args) =>
    Shell.Builder("git")
      .WithArguments(args)
      .WithWorkingDirectory(root)
      .WithNoValidation()
      .CaptureAsync(CancellationToken.None);
}

} // namespace TimeWarp.Nuru.Tests.DevCli
