# Round 1 — general
**Date:** 2026-10-12
**Scope reviewed:** commit `d2abca0f` (`documentation/user/guides/migrating-to-3.0.md`, `documentation/user/features/agent-invocation.md`)

## Summary

The migration section now describes one flat `endpoints` array plus `invocation`, and it says 2.x had no `--capabilities` route. The JSON example parses equal to `app --capabilities` from the 487 launch demo (`TimeWarp.Nuru` 3.0.0-beta.80, copied outside the repo). Endpoint and invocation fields match `CapabilitiesResponse`, `EndpointCapability`, `ParameterCapability`, `OptionCapability`, and `InvocationCapability.Standard`. `agent-invocation.md` states the same flat shape and the same invocation object.

Checked and not filed: changelog history matches the prose (flag added in beta.22, hierarchical `groups`/`commands` in beta.42, replaced by flat `endpoints` in beta.56). Nested `WithGroupPrefix("admin")` / `WithGroupPrefix("config")` produces `groupPath: ["admin", "config"]` and pattern `admin config get {key}` (`capabilities-03-groups.cs`). `--group-filter` / `-g` and `"filter": { "group": "..." }` match `interceptor-emitter.cs` and `capabilities-group-filter.cs`. "under review" and `beta.78` are gone. An in-repo run of the same routes emitted the same endpoint and invocation objects; `name` and `version` differed only because that build used the file name and an informational version with a commit suffix.

## Issues

No issues.
