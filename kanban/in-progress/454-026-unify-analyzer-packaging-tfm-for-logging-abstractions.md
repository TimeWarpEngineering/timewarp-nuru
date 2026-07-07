# Unify Analyzer Packaging TFM For Logging Abstractions

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M26).

## Description

The two packages pack DIFFERENT TFM builds of the same analyzer dependency into
`analyzers/dotnet/cs`:

- `source/timewarp-nuru/timewarp-nuru.csproj:101` packs
  `lib/net9.0/Microsoft.Extensions.Logging.Abstractions.dll`
- `source/timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj:58` packs `lib/net10.0/`

Both TFMs exist in the resolved 10.0.9 package so it builds today, but the net9.0 path is
a leftover and a latent break if that TFM is dropped from the NuGet package — and the two
packages shipping different builds of the same DLL into the analyzer host is asking for
load conflicts.

## Checklist

- [x] Align both csproj entries on the same TFM (net10.0) or centralize the path
- [x] Verify packed .nupkg layout (analyzers/dotnet/cs) for both packages
- [x] Confirm analyzers load in a consumer project (samples build)

## Results

### What was implemented

Aligned both NuGet packages on `net10.0` for `Microsoft.Extensions.Logging.Abstractions.dll` in the `analyzers/dotnet/cs` path. Previously `timewarp-nuru.csproj` packed `lib/net9.0/` while `timewarp-nuru-analyzers.csproj` packed `lib/net10.0/` — a latent break if the `net9.0` TFM is dropped from a future package version, and a load-conflict risk from shipping different builds of the same DLL into the analyzer host.

### Files changed

- `source/timewarp-nuru/timewarp-nuru.csproj:101` — `net9.0` → `net10.0` (one line, the only change)

### Verification

- **Build**: `dotnet build source/timewarp-nuru/timewarp-nuru.csproj` — succeeded, 0 warnings, 0 errors
- **Pack**: Both `dotnet pack` commands succeeded
- **Nupkg layout**: Both packages now contain `analyzers/dotnet/cs/Microsoft.Extensions.Logging.Abstractions.dll`. SHA-256 verification confirmed both nupkg files contain the identical `net10.0` DLL (hash `5dcb4934cb0dcc5547aeaebebc5bb687cc2522390b7032d68713b29af64f7fd5`) — no more load conflict risk
- **Consumer project**: `samples/endpoints/01-hello-world/endpoint-hello-world.cs` compiled and executed successfully. `tests/test-apps/timewarp-nuru-testapp-delegates` build produced intentional NURU_R003 analyzer errors, confirming the analyzer and its logging dependency load correctly in a consumer project
- **CI**: 1360 passed, 7 skipped, 0 failed — matches previous baseline (no test count change expected, packaging fix only)

### Key decisions made

- **One-line fix**: Changed only `timewarp-nuru.csproj:101` from `net9.0` to `net10.0`. The analyzer csproj already used `net10.0` — no change needed there.
- **SHA-256 verification**: Confirmed both nupkg files contain the byte-identical DLL from the `net10.0` TFM, eliminating the load-conflict risk.
- **Package version**: 10.0.9 (from `Directory.Packages.props:18`) has both `net9.0` and `net10.0` TFMs available, so the build works before and after. The fix is forward-looking — it prevents a latent break if a future version drops `net9.0`.

### Test outcomes

- **Full CI** (`dotnet run tests/ci-tests/run-ci-tests.cs`): 1360 passed, 7 skipped, 0 failed. No regressions.
