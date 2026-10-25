# Delegate handler ILogger<T> parameter is silently null when AddLogging is configured

## Description

A delegate handler that takes `ILogger<T>` gets null. With `ConfigureServices(s => s.AddLogging(...))` configured, `.WithHandler((ILogger<X> logger) => ...)` generates `ILogger<X> logger = default!; // Service not registered`, with no diagnostic, so the handler throws a NullReferenceException the first time it logs.

`ServiceResolverEmitter.EmitServiceResolution` has no `ILogger<T>` case. `HandlerInvokerEmitter.ResolveServiceForCommand` does, which is why endpoint handler constructors and behaviors receive a logger. Found by task 527 (PR #356), which removed the legacy logging builder APIs; 527's logging docs currently say delegate parameters are not resolved yet. That sentence documents this bug and should go once it is fixed.

## Checklist

- [ ] Read the Purpose/Design regions of `ServiceResolverEmitter` and `HandlerInvokerEmitter`; share one `ILogger<T>` resolution path rather than duplicating it
- [ ] Delegate handlers resolve `ILogger<T>` from the app's logger factory when logging is configured
- [ ] When logging is not configured, behave like other injection sites (NullLogger or NURU_H007), never a silent `default!`
- [ ] Look for other service types that fall through to `default!` silently; report a diagnostic instead of emitting null
- [ ] Regression test: delegate handler with `ILogger<T>` logs (use `Map<T>()`/fluent, not `DiscoverEndpoints()`)
- [ ] Remove the "delegate parameters are not resolved yet" note from both logging.md copies
- [ ] Extend the tw-nuru-verify `add-logging` probe (or add a case) to cover a delegate handler
- [ ] `ganda runfile cache --clear`, full CI suite green

## Notes

- Source is the authority: `ILogger<T>` injection is supported for endpoints and behaviors, so a delegate handler getting null is a code defect.
- New analyzer diagnostics go in `AnalyzerReleases.Unshipped.md`, never Shipped. No trailing whitespace.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
