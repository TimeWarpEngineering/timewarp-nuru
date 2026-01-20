# AOT CLI Framework Benchmark Results

**Date:** 2026-01-20 22:11:19
**Platform:** Unix 5.15.167.4
**Runtime:** 10.0.2

## AOT Compatibility

| Framework | AOT Support | Binary Size | Notes |
|-----------|-------------|-------------|-------|
| bench-consoleappframework | ✓ Yes | 2.5 MB |  |
| bench-systemcommandline | ✓ Yes | 3.3 MB |  |
| bench-clifx | ✓ Yes | 4.1 MB |  |
| bench-mcmaster | ✓ Yes | 5.2 MB |  |
| bench-nuru | ✓ Yes | 3.0 MB |  |
| bench-powerargs | ✓ Yes | 5.0 MB |  |
| bench-commandlineparser | ✓ Yes | 4.5 MB |  |
| bench-cocona | ✓ Yes | 6.4 MB |  |
| bench-coconalite | ✓ Yes | 4.7 MB |  |
| bench-spectreconsole | ✓ Yes | 9.9 MB |  |

## Binary Size Ranking

| Rank | Framework | Size |
|------|-----------|------|
| 1 | bench-consoleappframework | 2.5 MB |
| 2 | bench-nuru | 3.0 MB |
| 3 | bench-systemcommandline | 3.3 MB |
| 4 | bench-clifx | 4.1 MB |
| 5 | bench-commandlineparser | 4.5 MB |
| 6 | bench-coconalite | 4.7 MB |
| 7 | bench-powerargs | 5.0 MB |
| 8 | bench-mcmaster | 5.2 MB |
| 9 | bench-cocona | 6.4 MB |
| 10 | bench-spectreconsole | 9.9 MB |

## Cold Start Performance (hyperfine)

| Command | Mean [ms] | Min [ms] | Max [ms] | Relative |
|:---|---:|---:|---:|---:|
| `bench-consoleappframework` | 2.8 ± 0.4 | 2.2 | 4.4 | 1.00 |
| `bench-systemcommandline` | 3.5 ± 0.4 | 2.7 | 5.3 | 1.27 ± 0.22 |
| `bench-clifx` | 66.0 ± 26.6 | 44.8 | 248.2 | 23.53 ± 9.95 |
| `bench-mcmaster` | 81.6 ± 75.6 | 54.3 | 725.6 | 29.08 ± 27.22 |
| `bench-nuru` | 3.0 ± 0.4 | 2.3 | 3.8 | 1.07 ± 0.18 |
| `bench-powerargs` | 69.8 ± 14.1 | 55.1 | 150.6 | 24.90 ± 5.93 |
| `bench-commandlineparser` | 4.4 ± 0.4 | 3.6 | 5.5 | 1.57 ± 0.25 |
| `bench-cocona` | 15.4 ± 0.7 | 14.1 | 16.9 | 5.48 ± 0.73 |
| `bench-coconalite` | 5.1 ± 0.4 | 4.2 | 6.5 | 1.80 ± 0.28 |
| `bench-spectreconsole` | 6.9 ± 0.6 | 5.7 | 8.3 | 2.47 ± 0.38 |


## Summary

- **Total frameworks tested:** 10
- **AOT compatible:** 10
- **AOT incompatible:** 0

