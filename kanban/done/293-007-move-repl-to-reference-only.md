# #293-007: Move REPL Code to Reference-Only

## Parent

#293 Make DSL Builder Methods No-Ops

## Description

The REPL project (`timewarp-nuru-repl`) is not currently compiling with the V2 generator changes. Rather than fixing it now, move it to a `reference-only/` folder so it doesn't block other work.

This preserves the code for future reference while excluding it from the build.

## Checklist

- [x] Rename `source/timewarp-nuru-repl/` to `source/timewarp-nuru-repl-reference-only/`
- [x] Rename `tests/timewarp-nuru-repl-tests/` to `tests/timewarp-nuru-repl-tests-reference-only/`
- [x] Projects already excluded from solution (commented out in `timewarp-nuru.slnx`)

## Results

**Approach changed:** Instead of moving to a separate `reference-only/` folder at repo root, renamed the folders in-place with `-reference-only` suffix. This keeps the code more visible and easier to reference while building the new source-gen REPL implementation.

**Files renamed:**
- `source/timewarp-nuru-repl` → `source/timewarp-nuru-repl-reference-only`
- `tests/timewarp-nuru-repl-tests` → `tests/timewarp-nuru-repl-tests-reference-only`

**Next steps:** Create new task to implement REPL functionality directly in `timewarp-nuru` with source generator support.
