# Round 1 — general
**Date:** 2026-10-31
**Scope reviewed:** branch `task/560-devcli-content-must-not-raise-ca1034-or-ca2000-in` vs `master` (product commit `3914c2a6`)

## Summary

Every type under `source/timewarp-nuru-devcli/content/` is `internal`: 53 top-level types and the four nested `Handler`s, which stay nested. `NuGetVersionService` keeps its `HttpMessageHandler` in a field, builds `HttpClient` with `disposeHandler: false`, and disposes the handler after the client. `ServiceExtractor.IsInternalType` now uses `compilation.IsSymbolAccessibleWithin`, the same rule `ExtensionMethodLowerer` already used, so a same-assembly internal service is constructed and a private nested type or an other-assembly internal type still reports NURU054. The diff adds no `#pragma`, `[SuppressMessage]`, content globalconfig, `auto-generated` header, or package `.editorconfig`. `tools/dev-cli` dropped `CA1034;CA2000` from `NoWarn`; its handlers were already `internal`.

A `latest-all` probe of this ownership shape reports no CA2000. The previous shape, a local `HttpClientHandler` passed to `new HttpClient(handler)`, still reports CA2000. CA1034 cannot fire: no content type is externally visible. Sampled master checkouts match the consumer fix list (amuru ctor lines 64–66, mediator, ganda `Directory.Build.props:17`, architecture `:13`, taratibu `:6`, terminal `:24`, quickbooks `:6`, Roslynk `:26`, and the public nested `Handler`s named for amuru, mediator, quickbooks, Roslynk, and terminal). `timewarp-flow` references `TimeWarp.Nuru` and not `TimeWarp.Nuru.DevCli`.

No issues.

## Issues
