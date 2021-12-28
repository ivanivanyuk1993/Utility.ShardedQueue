using SpinLockUtilNS;

namespace ShardedQueueNS;

/// <summary>
///     <para>
///         <see cref="ShardedQueue{T}"/> is needed for <see cref="EventLoopGroupSchedulerNS.EventLoopGroupScheduler"/> and
///         <see cref="RunAtomicallyProviderNS.RunAtomicallyProvider"/> as a highly specialized for their use case
///         alternative to slow performing <see cref="System.Collections.Concurrent.ConcurrentQueue{T}" />
///     </para>
///     <para>
///         <see cref="ShardedQueue{T}"/> is a specialized concurrent queue, which uses spin locking and fights lock
///         contention with sharding
///     </para>
///     <para>
///         Note that while it may seem that FIFO order is guaranteed, it is not, because Array-based Queue was chosen
///         for shards instead of LinkedList-based
///         => There is a possible situation, when multiple Producers triggered long resize of very large shards,
///         all but last, then passed enough time for resize to finish, then 1 Producer triggers long resize of
///         last shard, and all other threads start to consume or produce, and eventually start spinning on
///         last shard, without guarantee which will acquire spin lock first, so we can't even guarantee that there
///         will always be item to consume in shard
///     </para>
///     <para>
///         Notice that this queue doesn't track length, since it's increment/decrement position may change
///         depending on use case, as well as logic when it goes from 1 to 0 or reverse
///         (in some cases, like <see cref="RunAtomicallyProvider"/>, we don't even add action to queue when count
///         reaches 1, but run it immediately in same thread), or even negative
///         (to optimize some hot paths, like in <see cref="EventLoopGroupSchedulerNS.EventLoopGroupScheduler"/>,
///         since it is cheaper to restore count to correct state than to enforce that it can not go negative)
///     </para>
/// </summary>
public class ShardedQueue<T>
{
    private readonly IsLockedAndQueue<T>[] _isLockedAndQueueList;
    private readonly int _moduloNumber;

    private int _headIndex;
    private int _tailIndex;

    public ShardedQueue(int maxConcurrentThreadCount)
    {
        // queueCount should be greater than maxConcurrentThreadCount and a power of 2 (to make modulo more performant)
        var queueCount = (int) Math.Pow(2, Math.Ceiling(Math.Log2(maxConcurrentThreadCount)));
        // Computing modulo number, knowing that x % 2^n == x & (2^n - 1)
        _moduloNumber = queueCount - 1;

        _isLockedAndQueueList = new IsLockedAndQueue<T>[queueCount];
        for (var i = 0; i < queueCount; i++)
        {
            _isLockedAndQueueList[i] = new IsLockedAndQueue<T>();
        }
    }

    /// <remarks>
    ///     Note that it will spin until item is added => it can deadlock if DequeueOrSpin is called without guarantee that
    ///     Enqueue will be called
    /// </remarks>
    /// <returns></returns>
    public T DequeueOrSpin()
    {
        T removedItem;
        var queueIndex = Interlocked.Increment(ref _headIndex) & _moduloNumber;

        var isLockedAndQueue = _isLockedAndQueueList[queueIndex];
        // Note that since we already incremented _headIndex, it is important to complete operation, if we tried
        // to implement TryRemoveFirst, we would need to somehow balance our _headIndex, complexifying logic and
        // introducing overhead, and even if we need it, we can just check length before calling this method
        while (true)
            if (SpinLockUtil.TryLock(ref isLockedAndQueue.IsLocked))
            {
                // If we acquired lock after Enqueue, we can break spinning, otherwise we should Unlock and restart
                // spinning, giving Enqueue chance to lock
                if (isLockedAndQueue.Queue.TryDequeue(out removedItem))
                {
                    SpinLockUtil.UnlockOne(ref isLockedAndQueue.IsLocked);
                    break;
                }

                SpinLockUtil.UnlockOne(ref isLockedAndQueue.IsLocked);
            }

        return removedItem;
    }

    public void Enqueue(T item)
    {
        var queueIndex = Interlocked.Increment(ref _tailIndex) & _moduloNumber;

        var isLockedAndQueue = _isLockedAndQueueList[queueIndex];
        SpinLockUtil.AcquireLock(ref isLockedAndQueue.IsLocked);
        isLockedAndQueue.Queue.Enqueue(item);
        SpinLockUtil.UnlockOne(ref isLockedAndQueue.IsLocked);
    }
}