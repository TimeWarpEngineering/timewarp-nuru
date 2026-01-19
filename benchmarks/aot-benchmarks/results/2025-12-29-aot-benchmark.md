# AOT CLI Framework Benchmark Results

**Date:** 2025-12-29 11:14:27
**Platform:** Unix 5.15.167.4
**Runtime:** 10.0.1

## AOT Compatibility

| Framework | AOT Support | Binary Size | Notes |
|-----------|-------------|-------------|-------|
| bench-consoleappframework | ✓ Yes | 2.5 MB |  |
| bench-systemcommandline | ✓ Yes | 3.3 MB |  |
| bench-clifx | ✓ Yes | 4.1 MB |  |
| bench-mcmaster | ✓ Yes | 5.2 MB |  |
| bench-nuru-direct | ✗ No | N/A | AOT publish failed (non-zero exit code) |
| bench-nuru-full | ✓ Yes | 9.9 MB |  |
| bench-powerargs | ✓ Yes | 5.0 MB |  |
| bench-commandlineparser | ✓ Yes | 4.5 MB |  |
| bench-cocona | ✓ Yes | 6.4 MB |  |
| bench-coconalite | ✓ Yes | 4.7 MB |  |
| bench-spectreconsole | ✓ Yes | 9.9 MB |  |

## Binary Size Ranking

| Rank | Framework | Size |
|------|-----------|------|
| 1 | bench-consoleappframework | 2.5 MB |
| 2 | bench-systemcommandline | 3.3 MB |
| 3 | bench-clifx | 4.1 MB |
| 4 | bench-commandlineparser | 4.5 MB |
| 5 | bench-coconalite | 4.7 MB |
| 6 | bench-powerargs | 5.0 MB |
| 7 | bench-mcmaster | 5.2 MB |
| 8 | bench-cocona | 6.4 MB |
| 9 | bench-nuru-full | 9.9 MB |
| 10 | bench-spectreconsole | 9.9 MB |

## Cold Start Performance (hyperfine)

| Command | Mean [ms] | Min [ms] | Max [ms] | Relative |
|:---|---:|---:|---:|---:|
| `bench-consoleappframework` | 4.2 ± 1.6 | 2.6 | 12.1 | 1.06 ± 0.41 |
| `bench-systemcommandline` | 4.0 ± 0.3 | 3.2 | 5.0 | 1.00 |
| `bench-clifx` | 77.2 ± 32.1 | 52.3 | 209.3 | 19.51 ± 8.26 |
| `bench-mcmaster` | 82.3 ± 28.8 | 59.1 | 284.8 | 20.80 ± 7.48 |
| `bench-nuru-full` | 6.6 ± 1.7 | 4.6 | 15.8 | 1.67 ± 0.46 |
| `bench-powerargs` | 94.9 ± 40.3 | 62.5 | 254.5 | 23.98 ± 10.37 |
| `bench-commandlineparser` | 5.3 ± 0.4 | 4.5 | 6.6 | 1.34 ± 0.14 |
| `bench-cocona` | 20.0 ± 5.0 | 16.5 | 44.3 | 5.06 ± 1.34 |
| `bench-coconalite` | 7.1 ± 2.4 | 5.4 | 19.6 | 1.80 ± 0.63 |
| `bench-spectreconsole` | 7.8 ± 0.8 | 6.1 | 9.9 | 1.97 ± 0.25 |


## Failed Builds (AOT Not Supported)

### bench-nuru-direct

**Error:** AOT publish failed (non-zero exit code)

## Summary

- **Total frameworks tested:** 11
- **AOT compatible:** 10
- **AOT incompatible:** 1

