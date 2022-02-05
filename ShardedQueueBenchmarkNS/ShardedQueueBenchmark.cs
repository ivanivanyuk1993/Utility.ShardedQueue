using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using RunWithMaxConcurrencyProviderNS;
using ShardedQueueNS;

namespace ShardedQueueBenchmarkNS;

public class ShardedQueueBenchmark
{
    public static readonly Action EmptyAction = () => { };

    public static int[] NSource => new[]
    {
        (int) 1e3,
        (int) 1e4
    };

    [ParamsSource(nameof(NSource))] public int N { get; set; }

    [Benchmark]
    public void ConcurrentQueue_EnqueueAndDequeue()
    {
        var queue = new ConcurrentQueue<Action>();

        RunWithMaxConcurrencyProvider.RunWithMaxConcurrency(() =>
        {
            for (var i = 0; i < N; i++)
            {
                queue.Enqueue(EmptyAction);
                while (true)
                    if (queue.TryDequeue(out var action))
                    {
                        DeadCodeEliminationHelper.KeepAliveWithoutBoxing(action);
                        break;
                    }
            }
        });
    }

    [Benchmark]
    public void ConcurrentQueue_EnqueueThenDequeue()
    {
        var queue = new ConcurrentQueue<Action>();

        RunWithMaxConcurrencyProvider.RunWithMaxConcurrency(() =>
        {
            for (var i = 0; i < N; i++) queue.Enqueue(EmptyAction);

            for (var i = 0; i < N; i++)
                while (true)
                    if (queue.TryDequeue(out var action))
                    {
                        DeadCodeEliminationHelper.KeepAliveWithoutBoxing(action);
                        break;
                    }
        });
    }

    [Benchmark]
    public void LockedQueue_EnqueueAndDequeue()
    {
        var queue = new Queue<Action>();

        RunWithMaxConcurrencyProvider.RunWithMaxConcurrency(() =>
        {
            for (var i = 0; i < N; i++)
            {
                lock (queue)
                {
                    queue.Enqueue(EmptyAction);
                }

                lock (queue)
                {
                    DeadCodeEliminationHelper.KeepAliveWithoutBoxing(queue.Dequeue());
                }
            }
        });
    }

    [Benchmark]
    public void LockedQueue_EnqueueThenDequeue()
    {
        var queue = new Queue<Action>();

        RunWithMaxConcurrencyProvider.RunWithMaxConcurrency(() =>
        {
            for (var i = 0; i < N; i++)
                lock (queue)
                {
                    queue.Enqueue(EmptyAction);
                }

            for (var i = 0; i < N; i++)
                lock (queue)
                {
                    DeadCodeEliminationHelper.KeepAliveWithoutBoxing(queue.Dequeue());
                }
        });
    }

    [Benchmark]
    public void ShardedQueue_EnqueueAndDequeue()
    {
        var queue = new ShardedQueue<Action>(Environment.ProcessorCount);

        RunWithMaxConcurrencyProvider.RunWithMaxConcurrency(() =>
        {
            for (var i = 0; i < N; i++)
            {
                queue.Enqueue(EmptyAction);
                DeadCodeEliminationHelper.KeepAliveWithoutBoxing(queue.DequeueOrSpin());
            }
        });
    }

    [Benchmark]
    public void ShardedQueue_EnqueueThenDequeue()
    {
        var queue = new ShardedQueue<Action>(Environment.ProcessorCount);

        RunWithMaxConcurrencyProvider.RunWithMaxConcurrency(() =>
        {
            for (var i = 0; i < N; i++) queue.Enqueue(EmptyAction);

            for (var i = 0; i < N; i++) DeadCodeEliminationHelper.KeepAliveWithoutBoxing(queue.DequeueOrSpin());
        });
    }
}