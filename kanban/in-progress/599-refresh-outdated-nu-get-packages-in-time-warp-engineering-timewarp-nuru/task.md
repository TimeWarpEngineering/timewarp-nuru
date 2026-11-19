# Refresh outdated NuGet packages in TimeWarpEngineering/timewarp-nuru

## Description

Refresh outdated NuGet packages in TimeWarpEngineering/timewarp-nuru.
Filter: (all)
Outdated:
  Serilog.Extensions.Logging 10.0.0 -> 11.0.0 (dev-only: samples/endpoints/09-logging/serilog/structured-logging.cs)
Every outdated pin is dev-only. Bump them. Do not change <Version> in source/Directory.Build.props: the wave merges this task and does not release this repo.
Edit Directory.Packages.props in this repo only.

## Session

- Minted by ganda task wave run 20261119T023451Z-133310
