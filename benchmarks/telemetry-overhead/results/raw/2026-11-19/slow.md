| Command | Mean [ms] | Min [ms] | Max [ms] | Relative |
|:---|---:|---:|---:|---:|
| `3 on, reachable, grpc (AOT)` | 13.1 ± 0.8 | 11.9 | 16.3 | 1.06 ± 0.09 |
| `3 on, reachable, http/protobuf (AOT)` | 12.7 ± 0.5 | 11.7 | 13.6 | 1.04 ± 0.07 |
| `4 on, unreachable, grpc (AOT)` | 12.3 ± 0.7 | 11.5 | 14.4 | 1.00 |
| `3 on, reachable, grpc (JIT)` | 227.9 ± 10.7 | 205.9 | 250.2 | 18.55 ± 1.34 |
