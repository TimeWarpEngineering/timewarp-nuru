# AOT CLI Framework Benchmark Results

**Date:** 2026-01-20 23:47:16
**Platform:** Unix 5.15.167.4
**Runtime:** 10.0.2

## AOT Compatibility

| Framework                 | AOT Support | Binary Size | Notes |
| ------------------------- | ----------- | ----------- | ----- |
| bench-consoleappframework | ✓ Yes       | 2.5 MB      |       |
| bench-nuru                | ✓ Yes       | 3.0 MB      |       |
| bench-systemcommandline   | ✓ Yes       | 3.3 MB      |       |
| bench-clifx               | ✓ Yes       | 4.1 MB      |       |
| bench-commandlineparser   | ✓ Yes       | 4.5 MB      |       |
| bench-coconalite          | ✓ Yes       | 4.7 MB      |       |
| bench-powerargs           | ✓ Yes       | 5.0 MB      |       |
| bench-mcmaster            | ✓ Yes       | 5.2 MB      |       |
| bench-cocona              | ✓ Yes       | 6.4 MB      |       |
| bench-spectreconsole      | ✓ Yes       | 9.9 MB      |       |

## Binary Size Ranking

| Rank | Framework                 | Size   |
| ---- | ------------------------- | ------ |
| 1    | bench-consoleappframework | 2.5 MB |
| 2    | bench-nuru                | 3.0 MB |
| 3    | bench-systemcommandline   | 3.3 MB |
| 4    | bench-clifx               | 4.1 MB |
| 5    | bench-commandlineparser   | 4.5 MB |
| 6    | bench-coconalite          | 4.7 MB |
| 7    | bench-powerargs           | 5.0 MB |
| 8    | bench-mcmaster            | 5.2 MB |
| 9    | bench-cocona              | 6.4 MB |
| 10   | bench-spectreconsole      | 9.9 MB |

## Cold Start Performance (hyperfine)

| Command                     |    Mean [ms] | Min [ms] | Max [ms] |      Relative |
| :-------------------------- | -----------: | -------: | -------: | ------------: |
| `bench-consoleappframework` |    2.7 ± 0.3 |      2.1 |      3.5 |          1.00 |
| `bench-nuru`                |    2.7 ± 0.3 |      2.3 |      4.2 |   1.01 ± 0.15 |
| `bench-systemcommandline`   |    3.4 ± 0.4 |      2.7 |      4.7 |   1.25 ± 0.20 |
| `bench-commandlineparser`   |    4.1 ± 0.4 |      3.5 |      5.2 |   1.51 ± 0.20 |
| `bench-coconalite`          |    4.7 ± 0.5 |      4.0 |      6.5 |   1.76 ± 0.26 |
| `bench-spectreconsole`      |    6.6 ± 0.5 |      5.5 |      8.2 |   2.44 ± 0.30 |
| `bench-cocona`              |   14.9 ± 0.7 |     13.6 |     17.6 |   5.52 ± 0.62 |
| `bench-clifx`               |  61.2 ± 12.6 |     50.7 |    163.8 |  22.64 ± 5.21 |
| `bench-powerargs`           |  69.4 ± 16.3 |     55.3 |    162.8 |  25.69 ± 6.57 |
| `bench-mcmaster`            | 104.9 ± 94.4 |     54.0 |    482.0 | 38.82 ± 35.17 |


## Summary

- **Total frameworks tested:** 10
- **AOT compatible:** 10
- **AOT incompatible:** 0

