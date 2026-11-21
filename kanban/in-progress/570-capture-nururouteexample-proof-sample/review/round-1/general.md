# Round 1 — general
**Date:** 2026-11-21
**Scope reviewed:** branch `task/570-capture-nururouteexample-proof-sample` vs `master` (merge-base `2beac66d`): `Directory.Packages.props` and `kanban/in-progress/570-capture-nururouteexample-proof-sample/proof/`, checked against the original task and `## Goal (implementer restatement)`, plus the recorded `## Results` and `### How to validate`.

## Summary

The sample is a one-route runfile, `proof/calc.cs`, that declares `[NuruRouteExample("add 2 3", Description = "Adds 2 and 3, prints 5")]` and takes `TimeWarp.Nuru` from the new central pin `4.0.0-beta.16`. Committed per-route help shows that example, `capabilities.json` carries the same `examples` entry and the documented `invocation` object, and `json-args.txt` records eight commands with exit codes for inline JSON, `@path`, stdin `-` (pipe and heredoc), argv override, an unknown key, and the attached form. The goal restatement matches the original task. The recorded build, audit, and `capture.sh` results cover this diff. No issues.

## Issues

None.
