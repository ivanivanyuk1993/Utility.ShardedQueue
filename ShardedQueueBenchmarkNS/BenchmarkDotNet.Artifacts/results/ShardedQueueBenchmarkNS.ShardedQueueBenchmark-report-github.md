``` ini

BenchmarkDotNet=v0.13.1, OS=ubuntu 21.10
AMD Ryzen 9 3900XT, 1 CPU, 24 logical and 12 physical cores
.NET SDK=6.0.200
  [Host]     : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT
  DefaultJob : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT


```
|                             Method |     N |      Mean |     Error |     StdDev |
|----------------------------------- |------ |----------:|----------:|-----------:|
|  **ConcurrentQueue_EnqueueAndDequeue** |  **1000** |  **3.998 ms** | **0.0780 ms** |  **0.2249 ms** |
| ConcurrentQueue_EnqueueThenDequeue |  1000 |  2.785 ms | 0.0546 ms |  0.0671 ms |
|     ShardedQueue_EnqueueAndDequeue |  1000 |  3.506 ms | 0.0692 ms |  0.1057 ms |
|    ShardedQueue_EnqueueThenDequeue |  1000 |  4.705 ms | 0.2940 ms |  0.8624 ms |
|  **ConcurrentQueue_EnqueueAndDequeue** | **10000** | **57.734 ms** | **1.1541 ms** |  **2.5812 ms** |
| ConcurrentQueue_EnqueueThenDequeue | 10000 | 56.894 ms | 1.4069 ms |  4.1263 ms |
|     ShardedQueue_EnqueueAndDequeue | 10000 | 34.521 ms | 0.8879 ms |  2.6181 ms |
|    ShardedQueue_EnqueueThenDequeue | 10000 | 56.981 ms | 3.9807 ms | 11.6747 ms |
