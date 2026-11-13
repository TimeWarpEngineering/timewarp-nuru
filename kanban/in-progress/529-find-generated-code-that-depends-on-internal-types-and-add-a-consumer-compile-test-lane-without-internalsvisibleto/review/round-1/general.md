# Round 1 — general
**Date:** 2026-11-14
**Scope reviewed:** task commits `502772ce` and `7c74f2a7` (`36bec28f..HEAD`)

## Summary

generator-75 compiles representative emitter output in memory as `Gen75ConsumerApp`. That name is not an `InternalsVisibleTo` friend of TimeWarp.Nuru, TimeWarp.Nuru.Parsing, or TimeWarp.Nuru.Mcp; the host runfile `generator-75-consumer-surface-no-ivt` stays granted, which matches generator-65 and the restated goal. Five compile cases (fluent routes, endpoints with source-generated DI, REPL/completion/check-updates with Microsoft DI, operation endpoints, daemon and MCP) plus a grant precondition are wired into the standalone CI phase, and `NuruGenerator`'s Design region states the consumer-surface rule. The blanket test grant is kept, with that decision recorded on the generator script. Recorded build, test, and audit results cover the change. The two compile defects the lane avoids (user value-type returns, constraint-alias routes) fail in every assembly and are filed as 583 and 584 on the to-do column. The implementer restatement matches the original task.

## Issues
