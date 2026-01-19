# #296: Remove bench-nuru-direct benchmark

## Description

The `bench-nuru-direct` benchmark is no longer needed. It was a variant that used direct delegate invocation without the source generator, which is now obsolete since all Nuru apps use the source generator.

The benchmark consistently fails AOT compilation and should be removed.

## Checklist

- [x] Delete `benchmarks/aot-benchmarks/bench-nuru-direct/` directory
- [x] Edit `benchmarks/aot-benchmarks/run-benchmark.sh` - remove Nuru-Direct entry
- [x] Leave existing result files as historical records (they show it failed anyway)

## Notes

- `bench-nuru-full` remains as the sole Nuru benchmark (uses source generator, AOT compatible)
- Historical benchmark results in `results/` folder reference `bench-nuru-direct` but can stay as-is
