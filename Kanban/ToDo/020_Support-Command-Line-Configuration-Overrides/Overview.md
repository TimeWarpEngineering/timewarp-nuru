# Task 020: Support Command-Line Configuration Overrides

This folder contains all materials related to implementing ASP.NET Core-style command-line configuration overrides in TimeWarp.Nuru.

## Contents

- **[../020_Support-Command-Line-Configuration-Overrides.md](../020_Support-Command-Line-Configuration-Overrides.md)** - Main task document with requirements, checklist, and implementation plan
- **[issue-75-analysis.md](issue-75-analysis.md)** - Comprehensive technical analysis of the problem, exploring the current architecture and solution approaches

## Quick Summary

**Problem**: Args like `--Section:Key=value` fail route matching, preventing config overrides from working

**Solution**: Filter args containing colons before passing to `EndpointResolver`, while preserving full args for configuration system

**Impact**: Enables users to override any configuration value from command line, matching ASP.NET Core behavior

## Related Sample

[Samples/Configuration/command-line-overrides.cs](../../../Samples/Configuration/command-line-overrides.cs) demonstrates the expected behavior and will work correctly once this task is complete.

## GitHub Issue

[#75 - How to add a route to override an appsetting on the command line?](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/75)
