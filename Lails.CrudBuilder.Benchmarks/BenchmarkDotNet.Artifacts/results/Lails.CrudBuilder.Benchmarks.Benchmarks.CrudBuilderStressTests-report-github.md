```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19045.6216/22H2/2022Update)
AMD Ryzen 7 2700X, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.307
  [Host]     : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  Job-LZLXIC : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2

InvocationCount=20  IterationCount=5  UnrollFactor=1  
WarmupCount=3  

```
| Method                                             | Mean          | Error        | StdDev       | Gen0      | Gen1    | Allocated   |
|--------------------------------------------------- |--------------:|-------------:|-------------:|----------:|--------:|------------:|
| StressTest_RetryCount1_ThrowsAfterFirstAttempt     |      87.78 μs |     17.53 μs |     4.553 μs |         - |       - |     9.09 KB |
| StressTest_RetryCount2_ThrowsAfterSecondAttempt    | 203,513.73 μs |  5,204.53 μs | 1,351.600 μs |         - |       - |    14.38 KB |
| StressTest_RetryCount3_ThrowsAfterThirdAttempt     | 407,902.91 μs | 11,886.15 μs | 1,839.394 μs |         - |       - |    19.68 KB |
| StressTest_RetryCount2_SecondAttemptSucceeds       | 203,050.77 μs |  4,407.23 μs |   682.024 μs |         - |       - |     7.46 KB |
| StressTest_RetryCount3_ThirdAttemptSucceeds        | 408,027.77 μs |  5,565.43 μs | 1,445.324 μs |         - |       - |    12.84 KB |
| StressTest_RetryWithDelay_50ms                     |  62,104.81 μs |    895.51 μs |   232.560 μs |         - |       - |    11.95 KB |
| StressTest_RealConcurrencyConflict_ParallelUpdates |   6,761.58 μs |    535.47 μs |   139.059 μs |  100.0000 |       - |   529.18 KB |
| StressTest_MultipleOperations_WithRetry            | 863,379.20 μs | 18,698.49 μs | 4,855.939 μs | 4200.0000 | 50.0000 | 17258.07 KB |
| StressTest_SerializableIsolation_WithConflicts     | 407,289.85 μs |  6,637.40 μs | 1,027.145 μs |         - |       - |    20.11 KB |
