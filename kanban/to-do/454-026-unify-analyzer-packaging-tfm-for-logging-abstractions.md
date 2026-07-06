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

- [ ] Align both csproj entries on the same TFM (net10.0) or centralize the path
- [ ] Verify packed .nupkg layout (analyzers/dotnet/cs) for both packages
- [ ] Confirm analyzers load in a consumer project (samples build)
