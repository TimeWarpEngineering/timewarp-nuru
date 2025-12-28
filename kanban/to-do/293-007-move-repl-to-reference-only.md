# #293-007: Move REPL Code to Reference-Only

## Parent

#293 Make DSL Builder Methods No-Ops

## Description

The REPL project (`timewarp-nuru-repl`) is not currently compiling with the V2 generator changes. Rather than fixing it now, move it to a `reference-only/` folder so it doesn't block other work.

This preserves the code for future reference while excluding it from the build.

## Files to Move

```
source/timewarp-nuru-repl/ → reference-only/timewarp-nuru-repl/
tests/timewarp-nuru-repl-tests/ → reference-only/timewarp-nuru-repl-tests/
```

## Checklist

### Create Reference-Only Structure
- [ ] Create `reference-only/` directory at repo root
- [ ] Add `reference-only/readme.md` explaining purpose

### Move REPL Project
- [ ] Move `source/timewarp-nuru-repl/` to `reference-only/`
- [ ] Move `tests/timewarp-nuru-repl-tests/` to `reference-only/`

### Update Solution
- [ ] Remove `timewarp-nuru-repl` from `timewarp-nuru.slnx`
- [ ] Remove `timewarp-nuru-repl-tests` from solution
- [ ] Verify solution still builds

### Update Project References
- [ ] Check if any other projects reference `timewarp-nuru-repl`
- [ ] Remove or comment out those references

### Handle AddReplOptions
- [ ] `AddReplOptions()` in `nuru-core-app-builder.routes.cs` - keep as no-op or remove?
  - Recommendation: Make it a no-op that returns `(TSelf)this`
  - Add `[Obsolete("REPL support temporarily disabled")]` attribute

### Documentation
- [ ] Add note to `reference-only/readme.md` about why REPL was moved
- [ ] Document what needs to happen to restore REPL support

## reference-only/readme.md Content

```markdown
# Reference-Only Code

This directory contains code that is temporarily excluded from the build but preserved for reference.

## Why Code Gets Moved Here

- **Not compatible with current architecture** - Needs updates to work with V2 source generator
- **Deferred features** - Features planned but not prioritized
- **Historical reference** - Useful patterns or implementations to reference later

## Contents

### timewarp-nuru-repl/

The REPL (Read-Eval-Print-Loop) interactive shell support.

**Why moved:** The V2 source generator architecture requires updates to REPL support.
The REPL uses runtime endpoint discovery which conflicts with compile-time route generation.

**To restore:**
1. Update REPL to work with generated route matcher
2. Implement REPL-specific route registration that works alongside source gen
3. Move back to `source/` and re-add to solution

### timewarp-nuru-repl-tests/

Tests for the REPL functionality.
```

## Notes

- REPL may need architectural changes to work with source generator
- Current REPL relies on runtime `EndpointCollection` which is being removed
- Future REPL support might:
  - Use reflection on generated types
  - Have its own route registration parallel to source gen
  - Work differently (TBD)
- This task can be done independently of other #293 sub-tasks
