namespace CSharp.ConcurrentCollections;

/// <summary>
/// Atomic counter that provides thread-safe atomic operations on a 64-bit integer.
/// Supports increment, decrement, add, compare-and-swap, and other atomic operations.
/// </summary>
public class AtomicCounter
{
    private long value = 0;

    /// <summary>
    /// Initializes a new AtomicCounter with the specified initial value.
    /// </summary>
    /// <param name="initialValue">The initial value of the counter.</param>
    public AtomicCounter(long initialValue = 0)
    {
        value = initialValue;
    }

    /// <summary>
    /// Gets the current value of the counter.
    /// </summary>
    public long Value => Interlocked.Read(ref value);

    /// <summary>
    /// Atomically increments the counter by 1.
    /// </summary>
    /// <returns>The new value after incrementing.</returns>
    public long Increment() => Interlocked.Increment(ref value);

    /// <summary>
    /// Atomically decrements the counter by 1.
    /// </summary>
    /// <returns>The new value after decrementing.</returns>
    public long Decrement() => Interlocked.Decrement(ref value);

    /// <summary>
    /// Atomically adds the specified value to the counter.
    /// </summary>
    /// <param name="delta">The value to add.</param>
    /// <returns>The new value after adding.</returns>
    public long Add(long delta) => Interlocked.Add(ref value, delta);

    /// <summary>
    /// Atomically sets the counter to the specified value.
    /// </summary>
    /// <param name="newValue">The new value to set.</param>
    /// <returns>The previous value.</returns>
    public long Exchange(long newValue) => Interlocked.Exchange(ref value, newValue);

    /// <summary>
    /// Atomically compares the counter with the expected value and sets it to the new value if they match.
    /// </summary>
    /// <param name="expected">The expected current value.</param>
    /// <param name="newValue">The new value to set if the comparison succeeds.</param>
    /// <returns>True if the comparison succeeded and the value was updated, false otherwise.</returns>
    public bool CompareAndSwap(long expected, long newValue)
    {
        return Interlocked.CompareExchange(ref value, newValue, expected) == expected;
    }

    /// <summary>
    /// Atomically gets the current value and then increments it.
    /// </summary>
    /// <returns>The value before incrementing.</returns>
    public long GetAndIncrement()
    {
        while (true)
        {
            var current = Interlocked.Read(ref value);
            if (Interlocked.CompareExchange(ref value, current + 1, current) == current)
            {
                return current;
            }
        }
    }

    /// <summary>
    /// Atomically gets the current value and then adds the specified delta.
    /// </summary>
    /// <param name="delta">The value to add.</param>
    /// <returns>The value before adding.</returns>
    public long GetAndAdd(long delta)
    {
        while (true)
        {
            var current = Interlocked.Read(ref value);
            if (Interlocked.CompareExchange(ref value, current + delta, current) == current)
            {
                return current;
            }
        }
    }

    /// <summary>
    /// Atomically gets the current value and then decrements it.
    /// </summary>
    /// <returns>The value before decrementing.</returns>
    public long GetAndDecrement()
    {
        while (true)
        {
            var current = Interlocked.Read(ref value);
            if (Interlocked.CompareExchange(ref value, current - 1, current) == current)
            {
                return current;
            }
        }
    }

    /// <summary>
    /// Atomically gets the current value and then sets it to the new value.
    /// </summary>
    /// <param name="newValue">The new value to set.</param>
    /// <returns>The value before setting.</returns>
    public long GetAndSet(long newValue)
    {
        return Interlocked.Exchange(ref value, newValue);
    }

    /// <summary>
    /// Resets the counter to zero.
    /// </summary>
    public void Reset() => Interlocked.Exchange(ref value, 0);

    /// <summary>
    /// Returns a string representation of the current value.
    /// </summary>
    public override string ToString() => Value.ToString();

    /// <summary>
    /// Implicitly converts an AtomicCounter to its current value.
    /// </summary>
    public static implicit operator long(AtomicCounter counter) => counter.Value;

    /// <summary>
    /// Determines whether the specified object is equal to the current counter value.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj switch
        {
            AtomicCounter other => Value == other.Value,
            long longValue => Value == longValue,
            int intValue => Value == intValue,
            _ => false
        };
    }

    /// <summary>
    /// Returns the hash code for the current counter value.
    /// </summary>
    public override int GetHashCode() => Value.GetHashCode();
}