# AOT CLI Framework Benchmark Results

**Date:** 2026-01-21 02:24:14
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
| `bench-consoleappframework` | 3.2 ± 0.6 | 2.5 | 7.0 | 1.00 |
| `bench-systemcommandline` | 3.8 ± 0.7 | 3.0 | 9.6 | 1.20 ± 0.33 |
| `bench-clifx` | 66.8 ± 12.7 | 53.2 | 138.7 | 21.09 ± 5.77 |
| `bench-mcmaster` | 81.9 ± 45.1 | 59.2 | 314.4 | 25.84 ± 15.10 |
| `bench-nuru` | 3.2 ± 0.6 | 2.7 | 8.1 | 1.01 ± 0.27 |
| `bench-powerargs` | 71.3 ± 28.6 | 55.6 | 226.6 | 22.48 ± 10.04 |
| `bench-commandlineparser` | 4.3 ± 0.4 | 3.6 | 6.2 | 1.37 ± 0.30 |
| `bench-cocona` | 15.4 ± 2.2 | 13.6 | 30.9 | 4.87 ± 1.19 |
| `bench-coconalite` | 4.9 ± 0.4 | 4.2 | 6.0 | 1.53 ± 0.33 |
| `bench-spectreconsole` | 6.7 ± 0.5 | 5.5 | 8.7 | 2.11 ± 0.44 |


## Summary

- **Total frameworks tested:** 10
- **AOT compatible:** 10
- **AOT incompatible:** 0

