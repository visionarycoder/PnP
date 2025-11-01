# Reader-Writer Locks

**Description**: Comprehensive advanced locking patterns including ReaderWriterLockSlim, upgradeable locks, lock-free readers, hierarchical locking, custom synchronization primitives, and high-performance concurrent access patterns for scenarios with multiple readers and occasional writers.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

// Base interfaces for reader-writer synchronization
public interface IReaderWriterLock : IDisposable
{
    void EnterReadLock();
    bool TryEnterReadLock(TimeSpan timeout);
    void ExitReadLock();
    
    void EnterWriteLock();
    bool TryEnterWriteLock(TimeSpan timeout);
    void ExitWriteLock();
    
    void EnterUpgradeableReadLock();
    bool TryEnterUpgradeableReadLock(TimeSpan timeout);
    void ExitUpgradeableReadLock();
    
    bool IsReadLockHeld { get; }
    bool IsWriteLockHeld { get; }
    bool IsUpgradeableReadLockHeld { get; }
    
    int CurrentReadCount { get; }
    int WaitingReadCount { get; }
    int WaitingWriteCount { get; }
    int WaitingUpgradeCount { get; }
}

public interface IAsyncReaderWriterLock : IDisposable
{
    Task<IDisposable> ReaderLockAsync(CancellationToken cancellationToken = default);
    Task<IDisposable> WriterLockAsync(CancellationToken cancellationToken = default);
    Task<IDisposable> UpgradeableReaderLockAsync(CancellationToken cancellationToken = default);
    
    ValueTask<IDisposable> ReaderLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
    ValueTask<IDisposable> WriterLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
    ValueTask<IDisposable> UpgradeableReaderLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}

// Enhanced ReaderWriterLockSlim wrapper with monitoring
public class EnhancedReaderWriterLock : IReaderWriterLock
{
    private readonly ReaderWriterLockSlim lockSlim;
    private readonly ILogger logger;
    private readonly ReaderWriterLockMetrics metrics;
    private volatile bool isDisposed = false;

    public EnhancedReaderWriterLock(LockRecursionPolicy recursionPolicy = LockRecursionPolicy.NoRecursion, ILogger logger = null)
    {
        lockSlim = new ReaderWriterLockSlim(recursionPolicy);
        this.logger = logger;
        metrics = new ReaderWriterLockMetrics();
    }

    public bool IsReadLockHeld => lockSlim.IsReadLockHeld;
    public bool IsWriteLockHeld => lockSlim.IsWriteLockHeld;
    public bool IsUpgradeableReadLockHeld => lockSlim.IsUpgradeableReadLockHeld;
    
    public int CurrentReadCount => lockSlim.CurrentReadCount;
    public int WaitingReadCount => lockSlim.WaitingReadCount;
    public int WaitingWriteCount => lockSlim.WaitingWriteCount;
    public int WaitingUpgradeCount => lockSlim.WaitingUpgradeCount;

    public ReaderWriterLockMetrics Metrics => metrics;

    public void EnterReadLock()
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(EnhancedReaderWriterLock));

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            lockSlim.EnterReadLock();
            stopwatch.Stop();
            
            metrics.RecordReadLockAcquired(stopwatch.Elapsed);
            logger?.LogTrace("Read lock acquired in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            metrics.RecordReadLockFailed();
            logger?.LogError(ex, "Failed to acquire read lock after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public bool TryEnterReadLock(TimeSpan timeout)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(EnhancedReaderWriterLock));

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var acquired = lockSlim.TryEnterReadLock(timeout);
            stopwatch.Stop();
            
            if (acquired)
            {
                metrics.RecordReadLockAcquired(stopwatch.Elapsed);
                logger?.LogTrace("Read lock acquired in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            else
            {
                metrics.RecordReadLockTimeout();
                logger?.LogWarning("Read lock acquisition timed out after {TimeoutMs}ms", timeout.TotalMilliseconds);
            }
            
            return acquired;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            metrics.RecordReadLockFailed();
            logger?.LogError(ex, "Failed to acquire read lock after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public void ExitReadLock()
    {
        if (isDisposed) return;

        try
        {
            lockSlim.ExitReadLock();
            metrics.RecordReadLockReleased();
            logger?.LogTrace("Read lock released");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error releasing read lock");
            throw;
        }
    }

    public void EnterWriteLock()
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(EnhancedReaderWriterLock));

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            lockSlim.EnterWriteLock();
            stopwatch.Stop();
            
            metrics.RecordWriteLockAcquired(stopwatch.Elapsed);
            logger?.LogTrace("Write lock acquired in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            metrics.RecordWriteLockFailed();
            logger?.LogError(ex, "Failed to acquire write lock after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public bool TryEnterWriteLock(TimeSpan timeout)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(EnhancedReaderWriterLock));

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var acquired = lockSlim.TryEnterWriteLock(timeout);
            stopwatch.Stop();
            
            if (acquired)
            {
                metrics.RecordWriteLockAcquired(stopwatch.Elapsed);
                logger?.LogTrace("Write lock acquired in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            else
            {
                metrics.RecordWriteLockTimeout();
                logger?.LogWarning("Write lock acquisition timed out after {TimeoutMs}ms", timeout.TotalMilliseconds);
            }
            
            return acquired;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            metrics.RecordWriteLockFailed();
            logger?.LogError(ex, "Failed to acquire write lock after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public void ExitWriteLock()
    {
        if (isDisposed) return;

        try
        {
            lockSlim.ExitWriteLock();
            metrics.RecordWriteLockReleased();
            logger?.LogTrace("Write lock released");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error releasing write lock");
            throw;
        }
    }

    public void EnterUpgradeableReadLock()
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(EnhancedReaderWriterLock));

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            lockSlim.EnterUpgradeableReadLock();
            stopwatch.Stop();
            
            metrics.RecordUpgradeableLockAcquired(stopwatch.Elapsed);
            logger?.LogTrace("Upgradeable read lock acquired in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            metrics.RecordUpgradeableLockFailed();
            logger?.LogError(ex, "Failed to acquire upgradeable read lock after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public bool TryEnterUpgradeableReadLock(TimeSpan timeout)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(EnhancedReaderWriterLock));

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var acquired = lockSlim.TryEnterUpgradeableReadLock(timeout);
            stopwatch.Stop();
            
            if (acquired)
            {
                metrics.RecordUpgradeableLockAcquired(stopwatch.Elapsed);
                logger?.LogTrace("Upgradeable read lock acquired in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            }
            else
            {
                metrics.RecordUpgradeableLockTimeout();
                logger?.LogWarning("Upgradeable read lock acquisition timed out after {TimeoutMs}ms", timeout.TotalMilliseconds);
            }
            
            return acquired;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            metrics.RecordUpgradeableLockFailed();
            logger?.LogError(ex, "Failed to acquire upgradeable read lock after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public void ExitUpgradeableReadLock()
    {
        if (isDisposed) return;

        try
        {
            lockSlim.ExitUpgradeableReadLock();
            metrics.RecordUpgradeableLockReleased();
            logger?.LogTrace("Upgradeable read lock released");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error releasing upgradeable read lock");
            throw;
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            lockSlim?.Dispose();
        }
    }
}

// Async reader-writer lock implementation
public class AsyncReaderWriterLock : IAsyncReaderWriterLock
{
    private readonly SemaphoreSlim readerSemaphore;
    private readonly SemaphoreSlim writerSemaphore;
    private readonly SemaphoreSlim upgradeableSemaphore;
    private volatile int readerCount = 0;
    private volatile bool hasWriter = false;
    private volatile bool hasUpgradeableReader = false;
    private readonly object syncLock = new object();
    private readonly ILogger logger;
    private volatile bool isDisposed = false;

    public AsyncReaderWriterLock(int maxConcurrentReaders = int.MaxValue, ILogger logger = null)
    {
        readerSemaphore = new SemaphoreSlim(maxConcurrentReaders, maxConcurrentReaders);
        writerSemaphore = new SemaphoreSlim(1, 1);
        upgradeableSemaphore = new SemaphoreSlim(1, 1);
        this.logger = logger;
    }

    public int CurrentReaderCount => readerCount;
    public bool HasWriter => hasWriter;
    public bool HasUpgradeableReader => hasUpgradeableReader;

    public async Task<IDisposable> ReaderLockAsync(CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(AsyncReaderWriterLock));

        var stopwatch = Stopwatch.StartNew();
        
        await readerSemaphore.WaitAsync(cancellationToken);
        
        lock (syncLock)
        {
            if (hasWriter)
            {
                readerSemaphore.Release();
                throw new InvalidOperationException("Cannot acquire reader lock when writer is active");
            }
            
            Interlocked.Increment(ref readerCount);
        }
        
        stopwatch.Stop();
        logger?.LogTrace("Async reader lock acquired in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        
        return new ReaderLockReleaser(this);
    }

    public async Task<IDisposable> WriterLockAsync(CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(AsyncReaderWriterLock));

        var stopwatch = Stopwatch.StartNew();
        
        await writerSemaphore.WaitAsync(cancellationToken);
        
        // Wait for all readers to complete
        while (readerCount > 0)
        {
            await Task.Delay(1, cancellationToken);
        }
        
        lock (syncLock)
        {
            hasWriter = true;
        }
        
        stopwatch.Stop();
        logger?.LogTrace("Async writer lock acquired in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        
        return new WriterLockReleaser(this);
    }

    public async Task<IDisposable> UpgradeableReaderLockAsync(CancellationToken cancellationToken = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(AsyncReaderWriterLock));

        var stopwatch = Stopwatch.StartNew();
        
        await upgradeableSemaphore.WaitAsync(cancellationToken);
        
        lock (syncLock)
        {
            if (hasWriter)
            {
                upgradeableSemaphore.Release();
                throw new InvalidOperationException("Cannot acquire upgradeable reader lock when writer is active");
            }
            
            hasUpgradeableReader = true;
        }
        
        stopwatch.Stop();
        logger?.LogTrace("Async upgradeable reader lock acquired in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        
        return new UpgradeableReaderLockReleaser(this);
    }

    public async ValueTask<IDisposable> ReaderLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = new CancellationTokenSource(timeout);
        using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
        
        try
        {
            return await ReaderLockAsync(combined.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Failed to acquire reader lock within {timeout.TotalMilliseconds}ms");
        }
    }

    public async ValueTask<IDisposable> WriterLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = new CancellationTokenSource(timeout);
        using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
        
        try
        {
            return await WriterLockAsync(combined.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Failed to acquire writer lock within {timeout.TotalMilliseconds}ms");
        }
    }

    public async ValueTask<IDisposable> UpgradeableReaderLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = new CancellationTokenSource(timeout);
        using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
        
        try
        {
            return await UpgradeableReaderLockAsync(combined.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Failed to acquire upgradeable reader lock within {timeout.TotalMilliseconds}ms");
        }
    }

    private void ReleaseReaderLock()
    {
        if (!isDisposed)
        {
            lock (syncLock)
            {
                Interlocked.Decrement(ref readerCount);
            }
            
            readerSemaphore.Release();
            logger?.LogTrace("Async reader lock released");
        }
    }

    private void ReleaseWriterLock()
    {
        if (!isDisposed)
        {
            lock (syncLock)
            {
                hasWriter = false;
            }
            
            writerSemaphore.Release();
            logger?.LogTrace("Async writer lock released");
        }
    }

    private void ReleaseUpgradeableReaderLock()
    {
        if (!isDisposed)
        {
            lock (syncLock)
            {
                hasUpgradeableReader = false;
            }
            
            upgradeableSemaphore.Release();
            logger?.LogTrace("Async upgradeable reader lock released");
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            
            readerSemaphore?.Dispose();
            writerSemaphore?.Dispose();
            upgradeableSemaphore?.Dispose();
        }
    }

    private class ReaderLockReleaser : IDisposable
    {
        private readonly AsyncReaderWriterLock parent;
        private volatile bool isReleased = false;

        public ReaderLockReleaser(AsyncReaderWriterLock parent)
        {
            this.parent = parent;
        }

        public void Dispose()
        {
            if (!isReleased)
            {
                isReleased = true;
                parent.ReleaseReaderLock();
            }
        }
    }

    private class WriterLockReleaser : IDisposable
    {
        private readonly AsyncReaderWriterLock parent;
        private volatile bool isReleased = false;

        public WriterLockReleaser(AsyncReaderWriterLock parent)
        {
            this.parent = parent;
        }

        public void Dispose()
        {
            if (!isReleased)
            {
                isReleased = true;
                parent.ReleaseWriterLock();
            }
        }
    }

    private class UpgradeableReaderLockReleaser : IDisposable
    {
        private readonly AsyncReaderWriterLock parent;
        private volatile bool isReleased = false;

        public UpgradeableReaderLockReleaser(AsyncReaderWriterLock parent)
        {
            this.parent = parent;
        }

        public void Dispose()
        {
            if (!isReleased)
            {
                isReleased = true;
                parent.ReleaseUpgradeableReaderLock();
            }
        }
    }
}

// Lock-free reader pattern using versioning
public class LockFreeReader<T> where T : class
{
    private volatile VersionedValue<T> current;
    private readonly ILogger logger;

    public LockFreeReader(T initialValue, ILogger logger = null)
    {
        current = new VersionedValue<T>(initialValue, 0);
        this.logger = logger;
    }

    public T Read()
    {
        var snapshot = current;
        logger?.LogTrace("Lock-free read of version {Version}", snapshot.Version);
        return snapshot.Value;
    }

    public bool TryRead(out T value, out long version)
    {
        var snapshot = current;
        value = snapshot.Value;
        version = snapshot.Version;
        
        logger?.LogTrace("Lock-free try-read of version {Version}", version);
        return value != null;
    }

    public void Write(T newValue)
    {
        var oldSnapshot = current;
        var newSnapshot = new VersionedValue<T>(newValue, oldSnapshot.Version + 1);
        
        // Atomic update using compare-and-swap
        var originalSnapshot = Interlocked.CompareExchange(ref current, newSnapshot, oldSnapshot);
        
        if (ReferenceEquals(originalSnapshot, oldSnapshot))
        {
            logger?.LogTrace("Lock-free write successful, new version {Version}", newSnapshot.Version);
        }
        else
        {
            logger?.LogWarning("Lock-free write failed due to concurrent modification");
            throw new InvalidOperationException("Write failed due to concurrent modification");
        }
    }

    public bool TryWrite(T newValue, long expectedVersion)
    {
        var oldSnapshot = current;
        
        if (oldSnapshot.Version != expectedVersion)
        {
            logger?.LogDebug("Lock-free try-write failed: expected version {Expected}, actual {Actual}",
                expectedVersion, oldSnapshot.Version);
            return false;
        }
        
        var newSnapshot = new VersionedValue<T>(newValue, expectedVersion + 1);
        var originalSnapshot = Interlocked.CompareExchange(ref current, newSnapshot, oldSnapshot);
        
        var success = ReferenceEquals(originalSnapshot, oldSnapshot);
        
        logger?.LogTrace("Lock-free try-write {Result}, version {Version}",
            success ? "succeeded" : "failed", success ? newSnapshot.Version : oldSnapshot.Version);
        
        return success;
    }

    public T ReadAndWrite(Func<T, T> updateFunction)
    {
        while (true)
        {
            var oldSnapshot = current;
            var newValue = updateFunction(oldSnapshot.Value);
            var newSnapshot = new VersionedValue<T>(newValue, oldSnapshot.Version + 1);
            
            var originalSnapshot = Interlocked.CompareExchange(ref current, newSnapshot, oldSnapshot);
            
            if (ReferenceEquals(originalSnapshot, oldSnapshot))
            {
                logger?.LogTrace("Lock-free read-and-write successful, version {Version}", newSnapshot.Version);
                return newValue;
            }
            
            // Retry on concurrent modification
            logger?.LogTrace("Lock-free read-and-write retrying due to concurrent modification");
        }
    }

    public long CurrentVersion => current.Version;

    private class VersionedValue<TValue>
    {
        public TValue Value { get; }
        public long Version { get; }

        public VersionedValue(TValue value, long version)
        {
            Value = value;
            Version = version;
        }
    }
}

// Hierarchical locking with deadlock detection
public class HierarchicalLockManager : IDisposable
{
    private readonly ConcurrentDictionary<int, ReaderWriterLockSlim> locks;
    private readonly ThreadLocal<SortedSet<int>> heldLocks;
    private readonly ILogger logger;
    private volatile bool isDisposed = false;

    public HierarchicalLockManager(ILogger logger = null)
    {
        locks = new ConcurrentDictionary<int, ReaderWriterLockSlim>();
        heldLocks = new ThreadLocal<SortedSet<int>>(() => new SortedSet<int>());
        this.logger = logger;
    }

    public IDisposable AcquireReadLock(int lockId)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(HierarchicalLockManager));

        ValidateLockOrder(lockId);
        
        var lockInstance = locks.GetOrAdd(lockId, _ => new ReaderWriterLockSlim());
        
        var stopwatch = Stopwatch.StartNew();
        lockInstance.EnterReadLock();
        stopwatch.Stop();
        
        heldLocks.Value.Add(lockId);
        
        logger?.LogTrace("Acquired hierarchical read lock {LockId} in {ElapsedMs}ms", lockId, stopwatch.ElapsedMilliseconds);
        
        return new HierarchicalLockReleaser(this, lockId, LockType.Read);
    }

    public IDisposable AcquireWriteLock(int lockId)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(HierarchicalLockManager));

        ValidateLockOrder(lockId);
        
        var lockInstance = locks.GetOrAdd(lockId, _ => new ReaderWriterLockSlim());
        
        var stopwatch = Stopwatch.StartNew();
        lockInstance.EnterWriteLock();
        stopwatch.Stop();
        
        heldLocks.Value.Add(lockId);
        
        logger?.LogTrace("Acquired hierarchical write lock {LockId} in {ElapsedMs}ms", lockId, stopwatch.ElapsedMilliseconds);
        
        return new HierarchicalLockReleaser(this, lockId, LockType.Write);
    }

    public IDisposable AcquireUpgradeableLock(int lockId)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(HierarchicalLockManager));

        ValidateLockOrder(lockId);
        
        var lockInstance = locks.GetOrAdd(lockId, _ => new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion));
        
        var stopwatch = Stopwatch.StartNew();
        lockInstance.EnterUpgradeableReadLock();
        stopwatch.Stop();
        
        heldLocks.Value.Add(lockId);
        
        logger?.LogTrace("Acquired hierarchical upgradeable lock {LockId} in {ElapsedMs}ms", lockId, stopwatch.ElapsedMilliseconds);
        
        return new HierarchicalLockReleaser(this, lockId, LockType.Upgradeable);
    }

    private void ValidateLockOrder(int lockId)
    {
        var currentHeldLocks = heldLocks.Value;
        
        if (currentHeldLocks.Count > 0 && lockId <= currentHeldLocks.Max)
        {
            var heldLocksList = string.Join(", ", currentHeldLocks);
            logger?.LogError("Potential deadlock detected: attempting to acquire lock {LockId} while holding {HeldLocks}",
                lockId, heldLocksList);
            
            throw new InvalidOperationException(
                $"Potential deadlock: cannot acquire lock {lockId} while holding locks with higher or equal IDs: {heldLocksList}");
        }
    }

    private void ReleaseLock(int lockId, LockType lockType)
    {
        if (isDisposed) return;

        if (locks.TryGetValue(lockId, out var lockInstance))
        {
            switch (lockType)
            {
                case LockType.Read:
                    lockInstance.ExitReadLock();
                    break;
                case LockType.Write:
                    lockInstance.ExitWriteLock();
                    break;
                case LockType.Upgradeable:
                    lockInstance.ExitUpgradeableReadLock();
                    break;
            }
            
            heldLocks.Value?.Remove(lockId);
            
            logger?.LogTrace("Released hierarchical {LockType} lock {LockId}", lockType, lockId);
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            
            foreach (var lockInstance in locks.Values)
            {
                lockInstance?.Dispose();
            }
            
            heldLocks?.Dispose();
        }
    }

    private enum LockType
    {
        Read,
        Write,
        Upgradeable
    }

    private class HierarchicalLockReleaser : IDisposable
    {
        private readonly HierarchicalLockManager manager;
        private readonly int lockId;
        private readonly LockType lockType;
        private volatile bool isReleased = false;

        public HierarchicalLockReleaser(HierarchicalLockManager manager, int lockId, LockType lockType)
        {
            this.manager = manager;
            this.lockId = lockId;
            this.lockType = lockType;
        }

        public void Dispose()
        {
            if (!isReleased)
            {
                isReleased = true;
                manager.ReleaseLock(lockId, lockType);
            }
        }
    }
}

// Custom spin lock with adaptive spinning
public struct AdaptiveSpinLock
{
    private volatile int lockState;
    private int spinCount;
    private readonly int maxSpinCount;
    private readonly bool useProcessorYield;

    public AdaptiveSpinLock(int maxSpinCount = 1000, bool useProcessorYield = true)
    {
        lockState = 0;
        spinCount = 0;
        this.maxSpinCount = maxSpinCount;
        this.useProcessorYield = useProcessorYield;
    }

    public bool IsHeld => lockState != 0;

    public void Enter()
    {
        var currentSpinCount = 0;
        
        while (true)
        {
            // Try to acquire the lock
            if (Interlocked.CompareExchange(ref lockState, 1, 0) == 0)
            {
                // Lock acquired successfully
                Interlocked.Exchange(ref spinCount, Math.Max(0, spinCount - 1));
                return;
            }
            
            // Lock is contended, spin or yield
            currentSpinCount++;
            
            if (currentSpinCount < maxSpinCount)
            {
                // Adaptive spinning based on previous contention
                var adaptiveSpins = Math.Min(spinCount * 2, maxSpinCount / 4);
                
                for (int i = 0; i < adaptiveSpins; i++)
                {
                    Thread.SpinWait(1);
                }
                
                if (useProcessorYield)
                {
                    Thread.Yield();
                }
            }
            else
            {
                // Fall back to OS scheduling
                Thread.Sleep(0);
                currentSpinCount = 0;
            }
        }
    }

    public bool TryEnter()
    {
        var acquired = Interlocked.CompareExchange(ref lockState, 1, 0) == 0;
        
        if (acquired)
        {
            Interlocked.Exchange(ref spinCount, Math.Max(0, spinCount - 1));
        }
        else
        {
            Interlocked.Increment(ref spinCount);
        }
        
        return acquired;
    }

    public bool TryEnter(TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.Elapsed < timeout)
        {
            if (TryEnter())
            {
                return true;
            }
            
            Thread.Yield();
        }
        
        return false;
    }

    public void Exit()
    {
        if (Interlocked.CompareExchange(ref lockState, 0, 1) != 1)
        {
            throw new SynchronizationLockException("AdaptiveSpinLock was not held by the current thread");
        }
    }
}

// Performance metrics for reader-writer locks
public class ReaderWriterLockMetrics
{
    private volatile long readLocksAcquired = 0;
    private volatile long writeLocksAcquired = 0;
    private volatile long upgradeableLocksAcquired = 0;
    
    private volatile long readLockTimeouts = 0;
    private volatile long writeLockTimeouts = 0;
    private volatile long upgradeableLockTimeouts = 0;
    
    private volatile long readLockFailures = 0;
    private volatile long writeLockFailures = 0;
    private volatile long upgradeableLockFailures = 0;
    
    private volatile long totalReadWaitTime = 0;
    private volatile long totalWriteWaitTime = 0;
    private volatile long totalUpgradeableWaitTime = 0;
    
    private readonly object lockObject = new object();
    private DateTime startTime = DateTime.UtcNow;

    public long ReadLocksAcquired => readLocksAcquired;
    public long WriteLocksAcquired => writeLocksAcquired;
    public long UpgradeableLocksAcquired => upgradeableLocksAcquired;
    
    public long ReadLockTimeouts => readLockTimeouts;
    public long WriteLockTimeouts => writeLockTimeouts;
    public long UpgradeableLockTimeouts => upgradeableLockTimeouts;
    
    public long ReadLockFailures => readLockFailures;
    public long WriteLockFailures => writeLockFailures;
    public long UpgradeableLockFailures => upgradeableLockFailures;

    public TimeSpan AverageReadWaitTime => readLocksAcquired > 0 
        ? TimeSpan.FromTicks(totalReadWaitTime / readLocksAcquired) 
        : TimeSpan.Zero;

    public TimeSpan AverageWriteWaitTime => writeLocksAcquired > 0 
        ? TimeSpan.FromTicks(totalWriteWaitTime / writeLocksAcquired) 
        : TimeSpan.Zero;

    public TimeSpan AverageUpgradeableWaitTime => upgradeableLocksAcquired > 0 
        ? TimeSpan.FromTicks(totalUpgradeableWaitTime / upgradeableLocksAcquired) 
        : TimeSpan.Zero;

    public double ReadLockSuccessRate
    {
        get
        {
            var total = readLocksAcquired + readLockTimeouts + readLockFailures;
            return total > 0 ? (double)readLocksAcquired / total : 0.0;
        }
    }

    public double WriteLockSuccessRate
    {
        get
        {
            var total = writeLocksAcquired + writeLockTimeouts + writeLockFailures;
            return total > 0 ? (double)writeLocksAcquired / total : 0.0;
        }
    }

    public void RecordReadLockAcquired(TimeSpan waitTime)
    {
        Interlocked.Increment(ref readLocksAcquired);
        Interlocked.Add(ref totalReadWaitTime, waitTime.Ticks);
    }

    public void RecordWriteLockAcquired(TimeSpan waitTime)
    {
        Interlocked.Increment(ref writeLocksAcquired);
        Interlocked.Add(ref totalWriteWaitTime, waitTime.Ticks);
    }

    public void RecordUpgradeableLockAcquired(TimeSpan waitTime)
    {
        Interlocked.Increment(ref upgradeableLocksAcquired);
        Interlocked.Add(ref totalUpgradeableWaitTime, waitTime.Ticks);
    }

    public void RecordReadLockTimeout() => Interlocked.Increment(ref readLockTimeouts);
    public void RecordWriteLockTimeout() => Interlocked.Increment(ref writeLockTimeouts);
    public void RecordUpgradeableLockTimeout() => Interlocked.Increment(ref upgradeableLockTimeouts);
    
    public void RecordReadLockFailed() => Interlocked.Increment(ref readLockFailures);
    public void RecordWriteLockFailed() => Interlocked.Increment(ref writeLockFailures);
    public void RecordUpgradeableLockFailed() => Interlocked.Increment(ref upgradeableLockFailures);
    
    public void RecordReadLockReleased() { /* Track if needed */ }
    public void RecordWriteLockReleased() { /* Track if needed */ }
    public void RecordUpgradeableLockReleased() { /* Track if needed */ }

    public void Reset()
    {
        lock (lockObject)
        {
            readLocksAcquired = 0;
            writeLocksAcquired = 0;
            upgradeableLocksAcquired = 0;
            readLockTimeouts = 0;
            writeLockTimeouts = 0;
            upgradeableLockTimeouts = 0;
            readLockFailures = 0;
            writeLockFailures = 0;
            upgradeableLockFailures = 0;
            totalReadWaitTime = 0;
            totalWriteWaitTime = 0;
            totalUpgradeableWaitTime = 0;
            startTime = DateTime.UtcNow;
        }
    }

    public ReaderWriterLockStats GetStats()
    {
        return new ReaderWriterLockStats
        {
            ReadLocksAcquired = ReadLocksAcquired,
            WriteLocksAcquired = WriteLocksAcquired,
            UpgradeableLocksAcquired = UpgradeableLocksAcquired,
            ReadLockTimeouts = ReadLockTimeouts,
            WriteLockTimeouts = WriteLockTimeouts,
            UpgradeableLockTimeouts = UpgradeableLockTimeouts,
            ReadLockFailures = ReadLockFailures,
            WriteLockFailures = WriteLockFailures,
            UpgradeableLockFailures = UpgradeableLockFailures,
            AverageReadWaitTime = AverageReadWaitTime,
            AverageWriteWaitTime = AverageWriteWaitTime,
            AverageUpgradeableWaitTime = AverageUpgradeableWaitTime,
            ReadLockSuccessRate = ReadLockSuccessRate,
            WriteLockSuccessRate = WriteLockSuccessRate
        };
    }
}

public class ReaderWriterLockStats
{
    public long ReadLocksAcquired { get; set; }
    public long WriteLocksAcquired { get; set; }
    public long UpgradeableLocksAcquired { get; set; }
    public long ReadLockTimeouts { get; set; }
    public long WriteLockTimeouts { get; set; }
    public long UpgradeableLockTimeouts { get; set; }
    public long ReadLockFailures { get; set; }
    public long WriteLockFailures { get; set; }
    public long UpgradeableLockFailures { get; set; }
    public TimeSpan AverageReadWaitTime { get; set; }
    public TimeSpan AverageWriteWaitTime { get; set; }
    public TimeSpan AverageUpgradeableWaitTime { get; set; }
    public double ReadLockSuccessRate { get; set; }
    public double WriteLockSuccessRate { get; set; }
}

// Thread-safe cache with reader-writer synchronization
public class ReaderWriterCache<TKey, TValue> : IDisposable where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> cache;
    private readonly EnhancedReaderWriterLock rwLock;
    private readonly Func<TKey, TValue> valueFactory;
    private readonly ILogger logger;
    private volatile bool isDisposed = false;

    public ReaderWriterCache(Func<TKey, TValue> valueFactory, ILogger logger = null)
    {
        cache = new Dictionary<TKey, TValue>();
        rwLock = new EnhancedReaderWriterLock(LockRecursionPolicy.NoRecursion, logger);
        this.valueFactory = valueFactory ?? throw new ArgumentNullException(nameof(valueFactory));
        this.logger = logger;
    }

    public int Count
    {
        get
        {
            if (isDisposed) return 0;
            
            rwLock.EnterReadLock();
            try
            {
                return cache.Count;
            }
            finally
            {
                rwLock.ExitReadLock();
            }
        }
    }

    public ReaderWriterLockMetrics Metrics => rwLock.Metrics;

    public TValue GetOrAdd(TKey key)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(ReaderWriterCache<TKey, TValue>));

        // First try to read with read lock
        rwLock.EnterReadLock();
        try
        {
            if (cache.TryGetValue(key, out var existingValue))
            {
                logger?.LogTrace("Cache hit for key: {Key}", key);
                return existingValue;
            }
        }
        finally
        {
            rwLock.ExitReadLock();
        }

        // Not found, need to add with write lock
        rwLock.EnterWriteLock();
        try
        {
            // Double-check pattern
            if (cache.TryGetValue(key, out var existingValue))
            {
                logger?.LogTrace("Cache hit after write lock acquisition for key: {Key}", key);
                return existingValue;
            }

            // Create new value
            var newValue = valueFactory(key);
            cache[key] = newValue;
            
            logger?.LogDebug("Cache miss, added new value for key: {Key}", key);
            return newValue;
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (isDisposed)
        {
            value = default(TValue);
            return false;
        }

        rwLock.EnterReadLock();
        try
        {
            var found = cache.TryGetValue(key, out value);
            logger?.LogTrace("Cache lookup for key {Key}: {Result}", key, found ? "hit" : "miss");
            return found;
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    public void Add(TKey key, TValue value)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(ReaderWriterCache<TKey, TValue>));

        rwLock.EnterWriteLock();
        try
        {
            cache[key] = value;
            logger?.LogDebug("Added/updated cache entry for key: {Key}", key);
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public bool Remove(TKey key)
    {
        if (isDisposed) return false;

        rwLock.EnterWriteLock();
        try
        {
            var removed = cache.Remove(key);
            logger?.LogDebug("Remove cache entry for key {Key}: {Result}", key, removed ? "success" : "not found");
            return removed;
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public void Clear()
    {
        if (isDisposed) return;

        rwLock.EnterWriteLock();
        try
        {
            var count = cache.Count;
            cache.Clear();
            logger?.LogInformation("Cleared cache, removed {Count} entries", count);
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public IReadOnlyDictionary<TKey, TValue> GetSnapshot()
    {
        if (isDisposed) return ImmutableDictionary<TKey, TValue>.Empty;

        rwLock.EnterReadLock();
        try
        {
            var snapshot = new Dictionary<TKey, TValue>(cache);
            logger?.LogTrace("Created cache snapshot with {Count} entries", snapshot.Count);
            return snapshot;
        }
        finally
        {
            rwLock.ExitReadLock();
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            rwLock?.Dispose();
        }
    }
}
```

**Usage**:

```csharp
// Example 1: Enhanced ReaderWriterLock with Monitoring
Console.WriteLine("Enhanced ReaderWriter Lock Examples:");

var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("LockExample");
var enhancedLock = new EnhancedReaderWriterLock(LockRecursionPolicy.NoRecursion, logger);

var sharedData = new List<string>();

// Multiple reader tasks
var readerTasks = Enumerable.Range(1, 5).Select(readerId =>
    Task.Run(async () =>
    {
        for (int i = 0; i < 100; i++)
        {
            enhancedLock.EnterReadLock();
            try
            {
                var count = sharedData.Count;
                Console.WriteLine($"Reader {readerId}: Found {count} items");
                
                // Simulate read work
                await Task.Delay(Random.Shared.Next(1, 5));
            }
            finally
            {
                enhancedLock.ExitReadLock();
            }
            
            await Task.Delay(10);
        }
    })
).ToArray();

// Single writer task
var writerTask = Task.Run(async () =>
{
    for (int i = 1; i <= 50; i++)
    {
        enhancedLock.EnterWriteLock();
        try
        {
            sharedData.Add($"Item-{i}");
            Console.WriteLine($"Writer: Added Item-{i}, total: {sharedData.Count}");
            
            // Simulate write work
            await Task.Delay(Random.Shared.Next(10, 20));
        }
        finally
        {
            enhancedLock.ExitWriteLock();
        }
        
        await Task.Delay(50);
    }
});

await Task.WhenAll(readerTasks.Concat(new[] { writerTask }));

var stats = enhancedLock.Metrics.GetStats();
Console.WriteLine($"Lock Statistics:");
Console.WriteLine($"  Read locks acquired: {stats.ReadLocksAcquired}");
Console.WriteLine($"  Write locks acquired: {stats.WriteLocksAcquired}");
Console.WriteLine($"  Average read wait time: {stats.AverageReadWaitTime.TotalMilliseconds:F2}ms");
Console.WriteLine($"  Average write wait time: {stats.AverageWriteWaitTime.TotalMilliseconds:F2}ms");
Console.WriteLine($"  Read success rate: {stats.ReadLockSuccessRate:P2}");

// Example 2: Async ReaderWriter Lock
Console.WriteLine("\nAsync ReaderWriter Lock Examples:");

var asyncLock = new AsyncReaderWriterLock(maxConcurrentReaders: 10, logger);
var asyncData = new Dictionary<string, int>();

// Async reader tasks
var asyncReaderTasks = Enumerable.Range(1, 8).Select(readerId =>
    Task.Run(async () =>
    {
        for (int i = 0; i < 50; i++)
        {
            using (await asyncLock.ReaderLockAsync())
            {
                var totalValue = asyncData.Values.Sum();
                Console.WriteLine($"Async Reader {readerId}: Total value = {totalValue}");
                
                await Task.Delay(Random.Shared.Next(5, 15));
            }
            
            await Task.Delay(20);
        }
    })
).ToArray();

// Async writer tasks
var asyncWriterTasks = Enumerable.Range(1, 2).Select(writerId =>
    Task.Run(async () =>
    {
        for (int i = 1; i <= 25; i++)
        {
            using (await asyncLock.WriterLockAsync())
            {
                var key = $"Writer{writerId}-{i}";
                asyncData[key] = Random.Shared.Next(1, 100);
                
                Console.WriteLine($"Async Writer {writerId}: Added {key} = {asyncData[key]}");
                
                await Task.Delay(Random.Shared.Next(10, 30));
            }
            
            await Task.Delay(100);
        }
    })
).ToArray();

await Task.WhenAll(asyncReaderTasks.Concat(asyncWriterTasks));

Console.WriteLine($"Async lock final state: {asyncData.Count} items, total value: {asyncData.Values.Sum()}");

// Example 3: Lock-Free Reader Pattern
Console.WriteLine("\nLock-Free Reader Examples:");

var lockFreeData = new LockFreeReader<ImmutableList<string>>(ImmutableList<string>.Empty, logger);

// Lock-free writer task
var lockFreeWriterTask = Task.Run(async () =>
{
    for (int i = 1; i <= 100; i++)
    {
        var success = false;
        var attempts = 0;
        
        while (!success && attempts < 10)
        {
            var (currentList, version) = (lockFreeData.Read(), lockFreeData.CurrentVersion);
            var newList = currentList.Add($"LockFree-Item-{i}");
            
            success = lockFreeData.TryWrite(newList, version);
            attempts++;
            
            if (!success)
            {
                await Task.Delay(1); // Brief delay before retry
            }
        }
        
        if (success)
        {
            Console.WriteLine($"Lock-free writer: Added item {i}");
        }
        else
        {
            Console.WriteLine($"Lock-free writer: Failed to add item {i} after {attempts} attempts");
        }
        
        await Task.Delay(Random.Shared.Next(5, 15));
    }
});

// Lock-free reader tasks
var lockFreeReaderTasks = Enumerable.Range(1, 6).Select(readerId =>
    Task.Run(async () =>
    {
        for (int i = 0; i < 80; i++)
        {
            var data = lockFreeData.Read();
            Console.WriteLine($"Lock-free Reader {readerId}: Found {data.Count} items");
            
            await Task.Delay(Random.Shared.Next(10, 25));
        }
    })
).ToArray();

await Task.WhenAll(lockFreeReaderTasks.Concat(new[] { lockFreeWriterTask }));

var finalData = lockFreeData.Read();
Console.WriteLine($"Lock-free final state: {finalData.Count} items");

// Example 4: Hierarchical Lock Manager
Console.WriteLine("\nHierarchical Lock Examples:");

var hierarchicalManager = new HierarchicalLockManager(logger);

// Task that acquires locks in correct order
var correctOrderTask = Task.Run(async () =>
{
    for (int i = 0; i < 20; i++)
    {
        using (var lock1 = hierarchicalManager.AcquireReadLock(1))
        using (var lock2 = hierarchicalManager.AcquireReadLock(2))
        using (var lock3 = hierarchicalManager.AcquireWriteLock(3))
        {
            Console.WriteLine($"Correct order task {i}: Acquired locks 1, 2, 3");
            await Task.Delay(Random.Shared.Next(10, 30));
        }
        
        await Task.Delay(50);
    }
});

// Task that demonstrates lock upgrade
var upgradeTask = Task.Run(async () =>
{
    for (int i = 0; i < 15; i++)
    {
        using (var readLock = hierarchicalManager.AcquireUpgradeableLock(10))
        {
            // Read operation
            await Task.Delay(Random.Shared.Next(5, 15));
            
            // Need to write, upgrade to write lock
            using (var writeLock = hierarchicalManager.AcquireWriteLock(11))
            {
                Console.WriteLine($"Upgrade task {i}: Upgraded from read to write");
                await Task.Delay(Random.Shared.Next(10, 20));
            }
        }
        
        await Task.Delay(75);
    }
});

await Task.WhenAll(correctOrderTask, upgradeTask);

// Example 5: Adaptive Spin Lock
Console.WriteLine("\nAdaptive Spin Lock Examples:");

var spinLockData = new List<int>();
var adaptiveSpinLock = new AdaptiveSpinLock(maxSpinCount: 500, useProcessorYield: true);

var spinLockTasks = Enumerable.Range(1, 8).Select(taskId =>
    Task.Run(() =>
    {
        for (int i = 0; i < 200; i++)
        {
            adaptiveSpinLock.Enter();
            try
            {
                spinLockData.Add(taskId * 1000 + i);
                
                if (spinLockData.Count % 100 == 0)
                {
                    Console.WriteLine($"Spin lock task {taskId}: Total items = {spinLockData.Count}");
                }
                
                // Very brief critical section
                Thread.SpinWait(Random.Shared.Next(1, 10));
            }
            finally
            {
                adaptiveSpinLock.Exit();
            }
        }
    })
).ToArray();

await Task.WhenAll(spinLockTasks);

Console.WriteLine($"Adaptive spin lock final count: {spinLockData.Count}");

// Example 6: Thread-Safe Cache with Reader-Writer Lock
Console.WriteLine("\nReader-Writer Cache Examples:");

var cache = new ReaderWriterCache<string, ExpensiveData>(
    key => new ExpensiveData(key, $"Value for {key}", DateTime.UtcNow),
    logger);

// Cache reader tasks
var cacheReaderTasks = Enumerable.Range(1, 10).Select(readerId =>
    Task.Run(async () =>
    {
        var keys = new[] { "Key1", "Key2", "Key3", "Key4", "Key5" };
        
        for (int i = 0; i < 40; i++)
        {
            var key = keys[Random.Shared.Next(keys.Length)];
            var data = cache.GetOrAdd(key);
            
            Console.WriteLine($"Cache Reader {readerId}: Got {data.Id} (created: {data.CreatedAt:HH:mm:ss.fff})");
            
            await Task.Delay(Random.Shared.Next(10, 50));
        }
    })
).ToArray();

// Cache writer task
var cacheWriterTask = Task.Run(async () =>
{
    for (int i = 1; i <= 20; i++)
    {
        var key = $"Dynamic-{i}";
        var data = new ExpensiveData(key, $"Dynamic value {i}", DateTime.UtcNow);
        
        cache.Add(key, data);
        Console.WriteLine($"Cache Writer: Added {key}");
        
        if (i % 5 == 0)
        {
            var snapshot = cache.GetSnapshot();
            Console.WriteLine($"Cache snapshot: {snapshot.Count} total entries");
        }
        
        await Task.Delay(Random.Shared.Next(100, 200));
    }
});

await Task.WhenAll(cacheReaderTasks.Concat(new[] { cacheWriterTask }));

var cacheStats = cache.Metrics.GetStats();
Console.WriteLine($"Cache Performance:");
Console.WriteLine($"  Total entries: {cache.Count}");
Console.WriteLine($"  Read locks: {cacheStats.ReadLocksAcquired}");
Console.WriteLine($"  Write locks: {cacheStats.WriteLocksAcquired}");
Console.WriteLine($"  Average read wait: {cacheStats.AverageReadWaitTime.TotalMilliseconds:F2}ms");

// Example 7: Performance Comparison
Console.WriteLine("\nPerformance Comparison Examples:");

const int iterationCount = 10000;
var testData = new List<int>();

// Test 1: ReaderWriterLockSlim
var rwLockSlim = new ReaderWriterLockSlim();
var rwStopwatch = Stopwatch.StartNew();

var rwTasks = Enumerable.Range(0, 4).Select(taskId =>
    Task.Run(() =>
    {
        for (int i = 0; i < iterationCount / 4; i++)
        {
            if (taskId == 0) // Writer
            {
                rwLockSlim.EnterWriteLock();
                try
                {
                    testData.Add(i);
                }
                finally
                {
                    rwLockSlim.ExitWriteLock();
                }
            }
            else // Readers
            {
                rwLockSlim.EnterReadLock();
                try
                {
                    var count = testData.Count;
                }
                finally
                {
                    rwLockSlim.ExitReadLock();
                }
            }
        }
    })
).ToArray();

await Task.WhenAll(rwTasks);
rwStopwatch.Stop();

Console.WriteLine($"ReaderWriterLockSlim: {rwStopwatch.ElapsedMilliseconds}ms for {iterationCount} operations");

// Test 2: Simple lock
testData.Clear();
var lockObject = new object();
var lockStopwatch = Stopwatch.StartNew();

var lockTasks = Enumerable.Range(0, 4).Select(taskId =>
    Task.Run(() =>
    {
        for (int i = 0; i < iterationCount / 4; i++)
        {
            lock (lockObject)
            {
                if (taskId == 0)
                {
                    testData.Add(i);
                }
                else
                {
                    var count = testData.Count;
                }
            }
        }
    })
).ToArray();

await Task.WhenAll(lockTasks);
lockStopwatch.Stop();

Console.WriteLine($"Simple lock: {lockStopwatch.ElapsedMilliseconds}ms for {iterationCount} operations");

// Cleanup
enhancedLock?.Dispose();
asyncLock?.Dispose();
hierarchicalManager?.Dispose();
cache?.Dispose();
rwLockSlim?.Dispose();

Console.WriteLine("\nReader-Writer Lock examples completed!");

// Data class for cache examples
public record ExpensiveData(string Id, string Value, DateTime CreatedAt);
```

**Notes**:

- Implement comprehensive reader-writer locking patterns for scenarios with multiple concurrent readers and occasional writers
- Support both synchronous and asynchronous locking patterns with proper timeout handling and cancellation
- Use ReaderWriterLockSlim enhancements with comprehensive performance monitoring and diagnostics
- Implement lock-free reader patterns using versioning and atomic operations for high-performance scenarios
- Support hierarchical locking with deadlock detection to prevent circular dependency issues
- Provide custom adaptive spin locks for very short critical sections with minimal overhead
- Use comprehensive performance metrics collection for lock contention analysis and optimization
- Support upgradeable locks that can be promoted from read to write access when needed
- Implement thread-safe caching patterns using reader-writer locks for optimal read performance
- Support proper disposal patterns and resource cleanup for long-running applications
- Use extensive error handling and logging for debugging lock-related issues
- Provide performance comparison examples between different locking strategies and implementations

**Prerequisites**:

- Understanding of reader-writer synchronization patterns and lock semantics
- Knowledge of ReaderWriterLockSlim and threading synchronization primitives
- Familiarity with lock-free programming and atomic operations
- Experience with hierarchical locking and deadlock prevention techniques
- Understanding of spin locks and adaptive spinning strategies for performance optimization

**Related Snippets**:

- [Concurrent Collections](concurrent-collections.md) - Thread-safe collections and lock-free data structures
- [Producer Consumer](producer-consumer.md) - Producer-consumer patterns with channels and backpressure handling
- [Actor Model](actor-model.md) - Actor-based concurrency patterns with message-passing isolation
