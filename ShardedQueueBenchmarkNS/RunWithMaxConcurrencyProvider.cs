namespace ShardedQueueBenchmarkNS;

/// <summary>
///     todo move to separate project
/// </summary>
public static class RunWithMaxConcurrencyProvider
{
    public static void RunWithMaxConcurrency(Action action)
    {
        var finishEvent = new CountdownEvent(Environment.ProcessorCount);
        var startEvent = new ManualResetEvent(false);
        var threadCreatedEvent = new CountdownEvent(Environment.ProcessorCount);

        for (var threadIndex = 0; threadIndex < Environment.ProcessorCount; threadIndex++)
            new Thread(() =>
            {
                threadCreatedEvent.Signal();
                startEvent.WaitOne();

                action();

                finishEvent.Signal();
            }).Start();

        threadCreatedEvent.Wait();
        startEvent.Set();
        finishEvent.Wait();
    }
}