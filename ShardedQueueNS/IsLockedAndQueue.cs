using SpinLockUtilNS;

namespace ShardedQueueNS;

/// <summary>
///     Notice that, if you consider reusing it somewhere, most likely you need to use either
///     <see cref="ShardedQueue{T}" />, or <see cref="RunAtomicallyProvider"/>
/// </summary>
/// <typeparam name="T"></typeparam>
internal class IsLockedAndQueue<T>
{
    public readonly Queue<T> Queue = new();
    public int IsLocked = SpinLockUtil.False;
}