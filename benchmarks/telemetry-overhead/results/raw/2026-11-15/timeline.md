| Case | Main to handler [ms] | Handler to exit [ms] |
|:---|---:|---:|
| 1 off (AOT) | 0.03 | 0.09 |
| 2 on, no endpoint (AOT) | 0.09 | 0.08 |
| 3 on, reachable, grpc (AOT) | 4.84 | 1003.28 |
| 3 on, reachable, http/protobuf (AOT) | 4.81 | 1002.40 |
| 4 on, unreachable, grpc (AOT) | 4.89 | 1002.61 |
| 1 off (JIT) | 9.64 | 3.38 |
| 2 on, no endpoint (JIT) | 15.44 | 3.77 |
| 3 on, reachable, grpc (JIT) | 144.11 | 1055.80 |
