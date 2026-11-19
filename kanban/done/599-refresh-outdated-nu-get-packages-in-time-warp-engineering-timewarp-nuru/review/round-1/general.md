# Round 1 — general
**Date:** 2026-11-19
**Scope reviewed:** branch `task/599-refresh-outdated-nu-get-packages-in-time-warp` vs `master`. Product file `Directory.Packages.props`. Call sites `samples/endpoints/09-logging/serilog/structured-logging.cs` and `samples/endpoints/09-logging/serilog/endpoints/order-command.cs`. Nuspec for Serilog.Extensions.Logging 11.0.0. `source/Directory.Build.props` `<Version>` left at 4.0.0-beta.16.

## Summary

The change bumps the central pin `Serilog.Extensions.Logging` from 10.0.0 to 11.0.0. That is the only outdated package the task lists, and it is the only product line in the diff. The sample route `order` logs the two messages named in `### How to validate`. The 11.0.0 nuspec depends on `Microsoft.Extensions.Logging` 11.0.0 and `Serilog` 4.4.0, which are already pinned. The implementer restatement matches the original task. Recorded gates (build, sample run resolving 11.0.0, audit) cover this change.

## Issues
