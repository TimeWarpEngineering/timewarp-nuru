# Repo audit checks (`dev audit`)

Part of [TimeWarp.Nuru.DevCli](readme.md).

`ganda repo audit` runs `./bin/dev audit` when the repo's dev CLI has it and fails on a
non-zero exit. DevCli owns the `audit` route, so a repo adds checks and never defines the
command itself.

`dev audit` is for **fast pre-test checks**: config, dependency or layout rules that finish
in seconds. Do not run a build or a test suite from a check; every walk, merge and manual
audit pays for it. `dev audit` never calls `ganda repo audit` (that calls `dev audit`, so
the other direction loops).

Add a check in your own `tools/dev-cli`, then register it by implementing the
`AddRepoChecks` partial method once:

```csharp
namespace DevCli;

internal sealed class DepsCheck : IDevAuditCheck
{
  public string Name => "deps-check";

  public async ValueTask<DevAuditCheckResult> RunAsync(DevAuditContext context, CancellationToken cancellationToken)
  {
    IReadOnlyList<string> problems = await FindOutdatedDependenciesAsync(context.RepoRoot, context.Verbose, cancellationToken);
    return problems.Count == 0
      ? DevAuditCheckResult.Pass()
      : DevAuditCheckResult.Fail(string.Join('\n', problems));
  }
}

internal static partial class DevAuditChecks
{
  static partial void AddRepoChecks(List<IDevAuditCheck> checks)
  {
    checks.Add(new DepsCheck());
  }
}
```

Checks run in the order added. Every check runs even after one fails. Output:

```
PASS deps-check (120ms)
FAIL layout-check (8ms)
<the failing check's message, in full>
dev audit: 1 passed, 1 failed
```

A check that throws counts as failed, with the exception type and message as its message.

## Options

`dev audit --force` and `dev audit --verbose` (`-v`) reach every check through
`DevAuditContext`, with `RepoRoot` (the git root, or the current directory outside a repo).
`Verbose` is for the check to read: print more detail on a failure. `Force` turns off the pass
cache below; a check may read it too.

## Skip a tree that already passed

A slow check (a compile, a focused test) need not rerun on a tree that already passed. Turn on
DevCli's tree stamp in the same partial class:

```csharp
internal static partial class DevAuditChecks
{
  static partial void ConfigurePassCache(ref IDevAuditPassCache? passCache) =>
    passCache = DevAuditTreeStamp.Create();
}
```

The key is SHA-256 of `git ls-tree HEAD` without the `kanban` entry, plus `dotnet --version`.
A kanban-only commit keeps the key; a change outside `kanban/` (untracked files included)
has no key, so that run runs every check. `DevAuditTreeStamp.Create("kanban", "docs")` names
other top-level entries to leave out. The last passing key is stored in
`artifacts/dev-audit/passed.key` (keep `artifacts/` gitignored).

```
dev audit: these inputs already passed (3f9c0a1b2d4e); nothing run. --force runs every check.
```

- `--force` runs every check; a passing forced run records the key again.
- A pass is recorded only when every check passed. A failed run records nothing and keeps
  the earlier pass file as it was, so a fail is never read back as a pass.
- A cache whose `KeyAsync` throws counts as no key: the run goes ahead (`-v` prints why).
- Your own `IDevAuditPassCache` works the same way, for a key the tree stamp does not cover.

## Run a subset in process

Another command can run some of the checks without the route, for example a CI step that
runs only the dependency check because its build step already compiled everything:

```csharp
DevAuditContext context = new(repoRoot, Verbose: verbose);
DevAuditSummary summary = await DevAuditRunner.RunAsync(
  DevAuditChecks.Named("deps-check"), context, terminal, passCache: null, cancellationToken);
```

`Named` keeps registry order and throws `ArgumentException` on a name no check has. With
`passCache: null` the run neither skips nor records, so a later full `dev audit` still runs
every check.

**Why a partial method:** DevCli is source-only, so `DevAuditChecks` compiles into your dev
CLI's own assembly and the repo's `AddRepoChecks` body is bound at compile time. That needs no
reflection and no DI, so it works in the AOT dev CLI. Nuru's source-generated DI does not
resolve `IEnumerable<T>`, which rules out multi-registration. A repo without checks writes
nothing and the call compiles away. `AddRepoChecks` can be implemented only once per dev CLI;
add every check there.
