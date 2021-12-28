using SpinLockUtilNS;

namespace ShardedQueueNS;

internal class IsLockedAndQueue<T>
{
    internal readonly Queue<T> Queue = new();
    internal int IsLocked = SpinLockUtil.False;
}