using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShardedQueueNS;

namespace ShardedQueueTestNS;

[TestClass]
public class ShardedQueueTest
{
    private const int Count = (int) 1e5;

    private readonly IScheduler[] _schedulerList =
    {
        Scheduler.Default,
        new EventLoopScheduler(),
        NewThreadScheduler.Default
    };

    [TestMethod]
    public async Task ShouldEnqueueAndDequeue()
    {
        foreach (var scheduler in _schedulerList) await ShouldEnqueueAndDequeueWithScheduler(scheduler);
    }

    private async Task ShouldEnqueueAndDequeueWithScheduler(IScheduler scheduler)
    {
        var queue = new ShardedQueue<int>(Environment.ProcessorCount);
        await Task.WhenAll(Enumerable.Range(1, Count).Select(i =>
        {
            var taskCompletionSource = new TaskCompletionSource<int>();
            scheduler.Schedule(() => { queue.Enqueue(i); });
            scheduler.Schedule(() => { taskCompletionSource.SetResult(queue.DequeueOrSpin()); });
            return taskCompletionSource.Task;
        })).ContinueWith(task =>
        {
            Assert.AreEqual(Count, task.Result.Length);
            Array.Sort(task.Result);
            var count = 0;
            foreach (var i in task.Result) Assert.AreEqual(++count, i);
        });
    }
}