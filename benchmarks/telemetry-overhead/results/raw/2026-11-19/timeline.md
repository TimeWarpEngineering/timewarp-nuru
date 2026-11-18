| Case | Main to handler [ms] | Handler to exit [ms] |
|:---|---:|---:|
| 1 off (AOT) | 0.04 | 0.11 |
| 2 on, no endpoint (AOT) | 0.10 | 0.10 |
| 3 on, reachable, grpc (AOT) | 4.99 | 1.68 |
| 3 on, reachable, http/protobuf (AOT) | 5.03 | 1.41 |
| 4 on, unreachable, grpc (AOT) | 4.94 | 1.33 |
| 1 off (JIT) | 10.13 | 3.57 |
| 2 on, no endpoint (JIT) | 16.01 | 3.91 |
| 3 on, reachable, grpc (JIT) | 148.81 | 56.31 |
