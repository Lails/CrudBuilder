```

BenchmarkDotNet v0.13.12, Windows 10 (10.0.19045.6216/22H2/2022Update)
AMD Ryzen 7 2700X, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.307
  [Host]     : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2
  Job-KAFVBB : .NET 8.0.22 (8.0.2225.52707), X64 RyuJIT AVX2

InvocationCount=100  IterationCount=10  UnrollFactor=1  
WarmupCount=5  

```
| Method                                                | Mean        | Error       | StdDev       | Median      | Ratio | RatioSD | Gen0       | Gen1     | Gen2     | Allocated   | Alloc Ratio |
|------------------------------------------------------ |------------:|------------:|-------------:|------------:|------:|--------:|-----------:|---------:|---------:|------------:|------------:|
| CreateCustomer_ReadWrite_WithoutTransaction           |  1,292.5 μs |    881.7 μs |    524.68 μs |  1,114.6 μs |  1.00 |    0.00 |   190.0000 |  10.0000 |        - |   801.69 KB |        1.00 |
| CreateCustomer_ReadWrite_WithTransaction              |  1,461.3 μs |  1,082.5 μs |    644.15 μs |  1,089.8 μs |  1.29 |    0.71 |   190.0000 |  10.0000 |        - |   804.29 KB |        1.00 |
| CreateCustomer_ReadWrite_WithTransaction_RetryCount2  |  2,232.1 μs |  2,029.5 μs |  1,342.40 μs |  1,560.2 μs |  1.99 |    1.43 |   190.0000 |        - |        - |   804.18 KB |        1.00 |
| CreateCustomer_ReadWrite_WithTransaction_RetryCount3  |  2,104.4 μs |  2,142.7 μs |  1,417.26 μs |  1,283.1 μs |  1.87 |    1.53 |   190.0000 |        - |        - |   804.18 KB |        1.00 |
| CreateCustomer_ReadWrite_WithTransaction_Serializable |  2,211.1 μs |  2,210.1 μs |  1,461.83 μs |  1,376.3 μs |  1.98 |    1.60 |   190.0000 |        - |        - |    804.3 KB |        1.00 |
| CreateCustomer_ReadWrite_WithTransaction_WithDelay    |  2,173.7 μs |  1,786.3 μs |  1,181.51 μs |  1,817.5 μs |  1.83 |    1.05 |   190.0000 |        - |        - |   804.03 KB |        1.00 |
| CreateAndUpdate_ReadWrite_WithTransaction             |  3,763.8 μs |    189.3 μs |     99.00 μs |  3,727.7 μs |  3.20 |    0.93 |   390.0000 |  10.0000 |        - |  1604.37 KB |        2.00 |
| CreateMultipleCustomers_ReadWrite_WithTransaction     | 52,908.6 μs | 25,698.6 μs | 16,998.02 μs | 51,517.1 μs | 43.58 |   17.84 | 19230.0000 | 630.0000 | 210.0000 | 78902.67 KB |       98.42 |
| ReadCustomers_ReadWrite_Query_ReadContext             |  3,367.5 μs |  1,747.4 μs |  1,155.77 μs |  3,054.0 μs |  3.06 |    1.51 |   200.0000 |  10.0000 |        - |   813.24 KB |        1.01 |
| Write_ReadWrite_Command_WriteContext                  |    952.2 μs |    162.0 μs |     84.74 μs |    963.7 μs |  0.82 |    0.26 |   190.0000 |        - |        - |   801.72 KB |        1.00 |
| WriteThenRead_ReadWrite_SeparateContexts              |  2,354.5 μs |  1,741.6 μs |  1,151.97 μs |  1,855.2 μs |  1.75 |    0.64 |   200.0000 |  10.0000 |        - |   813.26 KB |        1.01 |
| MultipleReads_ReadWrite_ReadContextOnly               | 14,824.6 μs |  5,588.1 μs |  3,696.21 μs | 14,725.6 μs | 12.35 |    4.52 |  4910.0000 | 190.0000 |        - | 20065.24 KB |       25.03 |
| MultipleWrites_ReadWrite_WriteContextOnly             | 19,928.7 μs |  2,075.6 μs |  1,372.85 μs | 19,438.7 μs | 16.89 |    4.80 |  4840.0000 | 280.0000 |        - | 19758.42 KB |       24.65 |
| ReadWriteMixed_ReadWrite_SeparateContexts             |  7,071.5 μs |  1,225.3 μs |    810.49 μs |  7,383.1 μs |  5.95 |    1.67 |   780.0000 |        - |        - |  3201.24 KB |        3.99 |
