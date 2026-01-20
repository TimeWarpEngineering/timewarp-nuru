# AOT CLI Framework Benchmark Results

**Date:** 2026-01-20 15:59:34
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
| `bench-consoleappframework` | 2.7 ± 0.3 | 2.2 | 4.1 | 1.00 |
| `bench-systemcommandline` | 3.4 ± 0.2 | 2.8 | 4.0 | 1.26 ± 0.15 |
| `bench-clifx` | 64.9 ± 20.1 | 50.2 | 169.7 | 23.93 ± 7.76 |
| `bench-mcmaster` | 75.6 ± 21.8 | 53.8 | 171.3 | 27.88 ± 8.47 |
| `bench-nuru` | 3.0 ± 0.2 | 2.6 | 3.9 | 1.09 ± 0.13 |
| `bench-powerargs` | 78.9 ± 118.3 | 56.1 | 1246.7 | 29.11 ± 43.73 |
| `bench-commandlineparser` | 4.4 ± 0.3 | 3.9 | 5.5 | 1.62 ± 0.18 |
| `bench-cocona` | 15.6 ± 0.8 | 14.6 | 19.5 | 5.74 ± 0.62 |
| `bench-coconalite` | 5.1 ± 0.3 | 4.5 | 5.8 | 1.88 ± 0.21 |
| `bench-spectreconsole` | 6.9 ± 0.4 | 5.8 | 8.7 | 2.54 ± 0.29 |


## Summary

- **Total frameworks tested:** 10
- **AOT compatible:** 10
- **AOT incompatible:** 0

