# Dual-Build Parity and Benchmark Testing

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Configure dual-build apps (appA/appB) with the UseNewGen toggle and create scripts for parity testing and benchmarking.

## Checklist

- [ ] appA: `<UseNewGen>false</UseNewGen>` (V1 path)
- [ ] appB: `<UseNewGen>true</UseNewGen>` (V2 path)
- [ ] Create parity test script (run same commands against both, compare output)
- [ ] Create benchmark script (compare binary size, startup time, memory)
- [ ] Document results and any discrepancies
- [ ] Both apps should produce identical output for same inputs

## Notes

### Archive Reason (2024-12-25)

**Status: Superseded by redesign**

Parent epic #239 is being redesigned. This task will be recreated according to the new design approach.
