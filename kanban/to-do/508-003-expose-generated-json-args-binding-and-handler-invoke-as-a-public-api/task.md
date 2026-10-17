# Expose generated --json-args binding and handler invoke as a public API

## Description

`json-args-emitter.cs` already generates reflection-free binding from a JSON object to a route's request (exact `name` keys, ordinal comparison, unknown keys rejected, argv-overrides-JSON merge) and the invoker that runs the handler and renders the result. All of it is inline in the interceptor and generated app class. A host that receives JSON from somewhere other than argv (an MCP `tools/call`, a batch, a daemon request) has to write its own binder and cannot guarantee the same semantics.

Expose the binding and invocation as a generated public entry point, roughly:

```csharp
bool TryBind(string route, JsonElement args, out object request, out BindError error);
Task<object?> InvokeAsync(object request, CancellationToken ct);
```

Exact shape is open: a generated static on the app class, or an `INuruInvoker` registered in DI.

## Requirements

- Same semantics as `--json-args`: exact names, unknown keys error, type errors reported with the key, JSON `null` rules, arrays for repeated options and catch-alls.
- Binding does not go through argv parsing, the route matcher's literal handling aside (the route is named explicitly).
- Invoke runs the Nuru behavior chain and the handler, and returns the typed result rather than printing it, so a host can render it in its own protocol. A separate helper renders with the existing `OutputStrategy` when a host wants the CLI's text.
- Works in the REPL and under AOT. No reflection.
- Tests: parity between `--json-args` and `TryBind` for every binding case the emitter already covers.

## Checklist

- [ ] Public surface decided and documented
- [ ] Emitter refactor so `--json-args` and the public path share one binder
- [ ] Invoke returns typed result; render helper
- [ ] Parity tests
- [ ] Docs in `agent-invocation.md`

## Notes

- Parent: 508. 508-004 depends on this.
- Roslynk's `multi_query` batch binder (string/bool/int coercions, unknown keys fail) is the kind of per-app binder this replaces.

## Session

- Created: 1479989 (2026-10-17)
