# Micro-Optimizations and Low-Level Performance

**Description**: Advanced low-level performance optimization techniques including JIT compilation hints, CPU-specific optimizations, branch prediction improvements, cache-friendly algorithms, and memory layout optimizations for maximum execution speed.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading;

// Aggressive inlining and method optimization attributes
public static class OptimizationHints
{
    // Force method inlining for hot paths
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FastAdd(int a, int b) => a + b;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPositive(int value) => value > 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int UnsafeArrayAccess(int[] array, int index)
    {
        // Skip bounds checking for performance-critical code
        // Use only when bounds are guaranteed to be valid
        fixed (int* ptr = array)
        {
            return ptr[index];
        }
    }

    // Prevent inlining for large methods to avoid code bloat
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void LargeComplexMethod()
    {
        // Complex logic that shouldn't be inlined
        for (int i = 0; i < 1000; i++)
        {
            // Simulate complex operations
            Thread.Sleep(0);
        }
    }

    // Optimize for size rather than speed
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static double ComplexCalculation(double x, double y, double z)
    {
        return Math.Sqrt(x * x + y * y + z * z) * Math.Sin(x) + Math.Cos(y) - Math.Tan(z);
    }

    // Mark hot paths for aggressive optimization
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static int HotPathCalculation(int[] data)
    {
        int sum = 0;
        for (int i = 0; i < data.Length; i++)
        {
            sum += data[i] * 2;
        }
        return sum;
    }
}

// Branch prediction optimizations
public static class BranchOptimization
{
    // Use likely/unlikely hints when available
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ProcessWithLikelyBranch(int value)
    {
        // Most values are expected to be positive
        if (value > 0) // Likely branch
        {
            return value * 2;
        }
        else // Unlikely branch
        {
            return HandleNegative(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // Keep unlikely code out of hot path
    private static int HandleNegative(int value)
    {
        return Math.Abs(value);
    }

    // Minimize branch misprediction with branchless programming
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BranchlessMax(int a, int b)
    {
        // Branchless maximum using bit manipulation
        int diff = a - b;
        int sign = diff >> 31; // Get sign bit (0 for positive, -1 for negative)
        return a - (diff & sign);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BranchlessMin(int a, int b)
    {
        int diff = a - b;
        int sign = diff >> 31;
        return b + (diff & sign);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BranchlessAbs(int value)
    {
        int mask = value >> 31; // Sign mask
        return (value ^ mask) - mask;
    }

    // Use lookup tables for complex branch-heavy logic
    private static readonly int[] PowersOf2 = {
        1, 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384, 32768
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetPowerOf2(int exponent)
    {
        return exponent < PowersOf2.Length ? PowersOf2[exponent] : 1 << exponent;
    }

    // Switch expressions for better optimization
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ProcessByType(int type) => type switch
    {
        0 => 100,
        1 => 200,
        2 => 300,
        3 => 400,
        _ => 0
    };

    // Jump table optimization for dense switch cases
    private static readonly Func<int, int>[] JumpTable = {
        x => x * 1,
        x => x * 2,
        x => x * 3,
        x => x * 4,
        x => x * 5
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ProcessWithJumpTable(int value, int operation)
    {
        return operation < JumpTable.Length ? JumpTable[operation](value) : value;
    }
}

// Cache-friendly data structures and algorithms
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CacheFriendlyData
{
    public int Id;
    public float Value;
    public byte Flag;
    // Total: 9 bytes, fits nicely in cache lines
}

public static class CacheOptimization
{
    // Structure of Arrays (SoA) vs Array of Structures (AoS)
    public class ArrayOfStructures
    {
        public struct Point3D
        {
            public float X, Y, Z;
        }

        private Point3D[] points;

        public ArrayOfStructures(int count)
        {
            points = new Point3D[count];
        }

        // Less cache-friendly when processing only X coordinates
        public void ProcessX()
        {
            for (int i = 0; i < points.Length; i++)
            {
                points[i].X *= 2.0f;
            }
        }
    }

    public class StructureOfArrays
    {
        private float[] x;
        private float[] y;
        private float[] z;

        public StructureOfArrays(int count)
        {
            x = new float[count];
            y = new float[count];
            z = new float[count];
        }

        // More cache-friendly when processing only X coordinates
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ProcessX()
        {
            for (int i = 0; i < x.Length; i++)
            {
                x[i] *= 2.0f;
            }
        }

        // Vectorization-friendly layout
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ProcessXVectorized()
        {
            var span = x.AsSpan();
            for (int i = 0; i < span.Length; i++)
            {
                span[i] *= 2.0f;
            }
        }
    }

    // Cache-conscious loop tiling for large datasets
    public static void MatrixMultiplyTiled(float[,] a, float[,] b, float[,] result, int size)
    {
        const int TileSize = 64; // Tune based on L1 cache size

        for (int ii = 0; ii < size; ii += TileSize)
        {
            for (int jj = 0; jj < size; jj += TileSize)
            {
                for (int kk = 0; kk < size; kk += TileSize)
                {
                    // Process tiles
                    int iMax = Math.Min(ii + TileSize, size);
                    int jMax = Math.Min(jj + TileSize, size);
                    int kMax = Math.Min(kk + TileSize, size);

                    for (int i = ii; i < iMax; i++)
                    {
                        for (int j = jj; j < jMax; j++)
                        {
                            float sum = result[i, j];
                            for (int k = kk; k < kMax; k++)
                            {
                                sum += a[i, k] * b[k, j];
                            }
                            result[i, j] = sum;
                        }
                    }
                }
            }
        }
    }

    // Memory prefetching hints
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ProcessWithPrefetch(int* data, int length)
    {
        const int PrefetchDistance = 64; // Prefetch 64 elements ahead

        for (int i = 0; i < length; i++)
        {
            // Prefetch future data
            if (i + PrefetchDistance < length)
            {
                // Note: .NET doesn't have built-in prefetch intrinsics
                // This is conceptual - actual implementation would use platform-specific code
                // Prefetch.PrefetchToL1(&data[i + PrefetchDistance]);
            }

            // Process current data
            data[i] *= 2;
        }
    }

    // Loop unrolling for small, known iterations
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ProcessArray4Elements(Span<int> data)
    {
        for (int i = 0; i < data.Length; i += 4)
        {
            // Unroll 4 iterations to reduce loop overhead
            if (i + 3 < data.Length)
            {
                data[i] *= 2;
                data[i + 1] *= 2;
                data[i + 2] *= 2;
                data[i + 3] *= 2;
            }
            else
            {
                // Handle remainder
                for (int j = i; j < data.Length; j++)
                {
                    data[j] *= 2;
                }
                break;
            }
        }
    }

    // Data locality optimization with spatial locality
    public static void ProcessMatrix2D(int[,] matrix, int rows, int cols)
    {
        // Row-major order processing (better for C# 2D arrays)
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                matrix[i, j] += 1;
            }
        }
    }

    // Use jagged arrays for better cache performance in some cases
    public static void ProcessJaggedMatrix(int[][] matrix)
    {
        for (int i = 0; i < matrix.Length; i++)
        {
            var row = matrix[i];
            for (int j = 0; j < row.Length; j++)
            {
                row[j] += 1;
            }
        }
    }
}

// Memory alignment and padding optimizations
[StructLayout(LayoutKind.Explicit, Size = 64)] // Cache line size
public struct CacheLinePadded
{
    [FieldOffset(0)]
    public int Value;
    // Remaining 60 bytes are padding to prevent false sharing
}

[StructLayout(LayoutKind.Sequential)]
public struct OptimalPacking
{
    // Order fields by size (largest first) to minimize padding
    public double LargeValue;      // 8 bytes
    public int MediumValue1;       // 4 bytes
    public int MediumValue2;       // 4 bytes
    public short SmallValue1;      // 2 bytes
    public short SmallValue2;      // 2 bytes
    public byte TinyValue1;        // 1 byte
    public byte TinyValue2;        // 1 byte
    public byte TinyValue3;        // 1 byte
    public byte TinyValue4;        // 1 byte
    // Total: 24 bytes with optimal packing
}

// False sharing prevention
public class FalseSharingPrevention
{
    [StructLayout(LayoutKind.Explicit, Size = 128)]
    private struct PaddedInt
    {
        [FieldOffset(64)]
        public int Value;
    }

    private PaddedInt counter1;
    private PaddedInt counter2;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementCounter1() => counter1.Value++;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementCounter2() => counter2.Value++;
}

// String optimization techniques
public static class StringOptimizations
{
    // String interning for frequently used strings
    private static readonly Dictionary<string, string> InternCache = new();

    public static string GetInternedString(string value)
    {
        if (InternCache.TryGetValue(value, out string? cached))
        {
            return cached;
        }

        var interned = string.Intern(value);
        InternCache[value] = interned;
        return interned;
    }

    // StringBuilder capacity optimization
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string BuildStringOptimal(IEnumerable<string> parts)
    {
        // Pre-calculate capacity to avoid reallocations
        int totalLength = 0;
        var partsList = parts.ToList();
        
        foreach (var part in partsList)
        {
            totalLength += part.Length;
        }

        var sb = new System.Text.StringBuilder(totalLength);
        foreach (var part in partsList)
        {
            sb.Append(part);
        }

        return sb.ToString();
    }

    // Span-based string operations (no allocation)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool StartsWithFast(ReadOnlySpan<char> text, ReadOnlySpan<char> prefix)
    {
        return text.Length >= prefix.Length && text.Slice(0, prefix.Length).SequenceEqual(prefix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfFast(ReadOnlySpan<char> text, char character)
    {
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == character)
                return i;
        }
        return -1;
    }

    // String comparison optimization
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsOrdinal(string? a, string? b)
    {
        return string.Equals(a, b, StringComparison.Ordinal);
    }

    // Avoid string allocations in hot paths
    private static readonly char[] HexChars = "0123456789ABCDEF".ToCharArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe string ToHexString(byte[] bytes)
    {
        fixed (byte* bytesPtr = bytes)
        fixed (char* hexCharsPtr = HexChars)
        {
            var result = new string('\0', bytes.Length * 2);
            fixed (char* resultPtr = result)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    byte b = bytesPtr[i];
                    resultPtr[i * 2] = hexCharsPtr[b >> 4];
                    resultPtr[i * 2 + 1] = hexCharsPtr[b & 0xF];
                }
            }
            return result;
        }
    }
}

// Bit manipulation optimizations
public static class BitOptimizations
{
    // Fast bit counting
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PopCount(uint value)
    {
        // Use hardware instruction when available
        return System.Numerics.BitOperations.PopCount(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LeadingZeroCount(uint value)
    {
        return System.Numerics.BitOperations.LeadingZeroCount(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TrailingZeroCount(uint value)
    {
        return System.Numerics.BitOperations.TrailingZeroCount(value);
    }

    // Fast power of 2 operations
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPowerOf2(int value)
    {
        return value > 0 && (value & (value - 1)) == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NextPowerOf2(int value)
    {
        return System.Numerics.BitOperations.RoundUpToPowerOf2((uint)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Log2(uint value)
    {
        return System.Numerics.BitOperations.Log2(value);
    }

    // Bit field operations
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetBit(uint value, int position)
    {
        return (value & (1u << position)) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint SetBit(uint value, int position)
    {
        return value | (1u << position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ClearBit(uint value, int position)
    {
        return value & ~(1u << position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ToggleBit(uint value, int position)
    {
        return value ^ (1u << position);
    }

    // Fast division by power of 2
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DivideByPowerOf2(int value, int powerOf2Exponent)
    {
        return value >> powerOf2Exponent;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ModuloByPowerOf2(int value, int powerOf2)
    {
        return value & (powerOf2 - 1);
    }

    // Bit reversal
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReverseBits(uint value)
    {
        // Swap odd and even bits
        value = ((value & 0xAAAAAAAA) >> 1) | ((value & 0x55555555) << 1);
        // Swap consecutive pairs
        value = ((value & 0xCCCCCCCC) >> 2) | ((value & 0x33333333) << 2);
        // Swap nibbles
        value = ((value & 0xF0F0F0F0) >> 4) | ((value & 0x0F0F0F0F) << 4);
        // Swap bytes
        value = ((value & 0xFF00FF00) >> 8) | ((value & 0x00FF00FF) << 8);
        // Swap 2-byte long pairs
        return (value >> 16) | (value << 16);
    }
}

// Arithmetic optimizations
public static class ArithmeticOptimizations
{
    // Fast integer square root
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint IntegerSquareRoot(uint value)
    {
        if (value == 0) return 0;

        uint result = 0;
        uint bit = 1u << 30; // Second-to-top bit set

        while (bit > value)
            bit >>= 2;

        while (bit != 0)
        {
            if (value >= result + bit)
            {
                value -= result + bit;
                result = (result >> 1) + bit;
            }
            else
            {
                result >>= 1;
            }
            bit >>= 2;
        }

        return result;
    }

    // Fast modulo for power of 2
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FastModPowerOf2(int value, int powerOf2)
    {
        return value & (powerOf2 - 1);
    }

    // Multiply by constant optimizations (compiler usually does this)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MultiplyBy3(int value)
    {
        return (value << 1) + value; // x * 3 = x * 2 + x
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MultiplyBy5(int value)
    {
        return (value << 2) + value; // x * 5 = x * 4 + x
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int MultiplyBy7(int value)
    {
        return (value << 3) - value; // x * 7 = x * 8 - x
    }

    // Fast average without overflow
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FastAverage(int a, int b)
    {
        return (a & b) + ((a ^ b) >> 1);
    }

    // Fast absolute value
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int FastAbs(int value)
    {
        int mask = value >> 31;
        return (value ^ mask) - mask;
    }

    // Sign function
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Sign(int value)
    {
        return (value > 0 ? 1 : 0) - (value < 0 ? 1 : 0);
    }

    // Fast integer comparison
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Compare(int a, int b)
    {
        return (a > b ? 1 : 0) - (a < b ? 1 : 0);
    }
}

// Collection optimizations
public static class CollectionOptimizations
{
    // Pre-sized collections to avoid reallocations
    public static List<T> CreateOptimalList<T>(int expectedSize)
    {
        return new List<T>(expectedSize);
    }

    public static Dictionary<TKey, TValue> CreateOptimalDictionary<TKey, TValue>(int expectedSize) 
        where TKey : notnull
    {
        return new Dictionary<TKey, TValue>(expectedSize);
    }

    // Struct enumerators to avoid allocations
    public struct ArrayEnumerator<T>
    {
        private readonly T[] array;
        private int index;

        public ArrayEnumerator(T[] array)
        {
            this.array = array;
            this.index = -1;
        }

        public T Current => array[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return ++index < array.Length;
        }
    }

    // Use ArrayPool for temporary collections
    public static void ProcessWithPooledArray<T>(int size, Action<T[]> processor)
    {
        var pool = ArrayPool<T>.Shared;
        var array = pool.Rent(size);
        
        try
        {
            processor(array);
        }
        finally
        {
            pool.Return(array);
        }
    }

    // Efficient copying with Buffer.BlockCopy for primitive types
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FastCopy(int[] source, int[] destination, int length)
    {
        Buffer.BlockCopy(source, 0, destination, 0, length * sizeof(int));
    }

    // Batch processing to improve cache locality
    public static void ProcessInBatches<T>(IList<T> items, int batchSize, Action<T> processor)
    {
        for (int i = 0; i < items.Count; i += batchSize)
        {
            int end = Math.Min(i + batchSize, items.Count);
            
            // Process batch
            for (int j = i; j < end; j++)
            {
                processor(items[j]);
            }
            
            // Optional: yield periodically for long-running operations
            if ((i / batchSize) % 100 == 0)
            {
                Thread.Yield();
            }
        }
    }
}

// Performance measurement and profiling
public static class PerformanceMeasurement
{
    // High-resolution timing
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetHighResolutionTimestamp()
    {
        return Stopwatch.GetTimestamp();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ElapsedMilliseconds(long startTimestamp)
    {
        return (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
    }

    // Micro-benchmark helper
    public static TimeSpan BenchmarkAction(Action action, int iterations = 1000)
    {
        // Warmup
        for (int i = 0; i < Math.Min(iterations / 10, 100); i++)
        {
            action();
        }

        // Force GC before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var stopwatch = Stopwatch.StartNew();
        
        for (int i = 0; i < iterations; i++)
        {
            action();
        }
        
        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    // Memory allocation tracking
    public static (TimeSpan time, long allocations) BenchmarkWithAllocations(Action action, int iterations = 1000)
    {
        // Warmup
        for (int i = 0; i < Math.Min(iterations / 10, 100); i++)
        {
            action();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var startMemory = GC.GetTotalMemory(false);
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            action();
        }

        stopwatch.Stop();
        var endMemory = GC.GetTotalMemory(false);
        var allocations = Math.Max(0, endMemory - startMemory);

        return (stopwatch.Elapsed, allocations);
    }

    // CPU cache performance profiling
    public static void ProfileCachePerformance()
    {
        const int arraySize = 64 * 1024 * 1024; // 64MB
        const int iterations = 5;

        var data = new int[arraySize];
        
        // Sequential access (cache-friendly)
        var sequentialTime = BenchmarkAction(() =>
        {
            for (int i = 0; i < arraySize; i++)
            {
                data[i]++;
            }
        }, iterations);

        // Random access (cache-unfriendly)
        var random = new Random(42);
        var indices = Enumerable.Range(0, arraySize).OrderBy(_ => random.Next()).ToArray();
        
        var randomTime = BenchmarkAction(() =>
        {
            for (int i = 0; i < arraySize; i++)
            {
                data[indices[i]]++;
            }
        }, iterations);

        Console.WriteLine($"Sequential access: {sequentialTime.TotalMilliseconds:F2}ms");
        Console.WriteLine($"Random access: {randomTime.TotalMilliseconds:F2}ms");
        Console.WriteLine($"Random/Sequential ratio: {randomTime.TotalMilliseconds / sequentialTime.TotalMilliseconds:F2}x");
    }
}

// JIT and runtime optimizations
public static class JitOptimizations
{
    // Force JIT compilation to avoid first-call overhead
    public static void WarmupMethod<T>(Func<T> method)
    {
        // Call method once to trigger JIT compilation
        _ = method();
        
        // Force compilation with RuntimeHelpers if needed
        RuntimeHelpers.PrepareMethod(method.Method.MethodHandle);
    }

    // Generic specialization hint
    public static void ProcessGeneric<T>(T[] items) where T : struct
    {
        // The JIT will create specialized versions for each T
        for (int i = 0; i < items.Length; i++)
        {
            // Generic operations are optimized for each type
            items[i] = default(T);
        }
    }

    // Encourage inlining with small method size
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEven(int value) => (value & 1) == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsOdd(int value) => (value & 1) == 1;

    // Profile-guided optimization hints
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static int OptimizedCalculation(int x)
    {
        // Mark hot paths for aggressive optimization
        return x * x + x - 1;
    }

    // Reduce virtual call overhead
    public sealed class SealedClass
    {
        // Virtual calls become direct calls
        public virtual void Process() { }
    }
}

// Memory optimization patterns
public static class MemoryOptimizations
{
    // Object pooling for expensive objects
    public class ExpensiveObjectPool
    {
        private readonly ConcurrentQueue<ExpensiveObject> pool = new();
        private readonly Func<ExpensiveObject> factory;

        public ExpensiveObjectPool(Func<ExpensiveObject> factory)
        {
            this.factory = factory;
        }

        public ExpensiveObject Rent()
        {
            return pool.TryDequeue(out var item) ? item : factory();
        }

        public void Return(ExpensiveObject item)
        {
            item.Reset();
            pool.Enqueue(item);
        }
    }

    public class ExpensiveObject
    {
        public void Reset() { /* Reset state for reuse */ }
    }

    // Stack allocation with Span<T>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ProcessOnStack()
    {
        // Allocate small arrays on stack
        Span<int> stackArray = stackalloc int[16];
        
        for (int i = 0; i < stackArray.Length; i++)
        {
            stackArray[i] = i * i;
        }
    }

    // Reduce boxing with generic constraints
    public static void ProcessValue<T>(T value) where T : struct
    {
        // No boxing for value types
        Console.WriteLine(value.ToString());
    }

    // Efficient string operations with Span<char>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<char> GetFileNameWithoutExtension(ReadOnlySpan<char> path)
    {
        var fileName = System.IO.Path.GetFileName(path);
        var lastDot = fileName.LastIndexOf('.');
        return lastDot >= 0 ? fileName.Slice(0, lastDot) : fileName;
    }
}
```

**Usage**:

```csharp
// Example 1: Method optimization attributes
Console.WriteLine("Method Optimization Examples:");

var numbers = new int[] { 1, 2, 3, 4, 5 };

// Fast inlined operations
var sum = 0;
for (int i = 0; i < numbers.Length; i++)
{
    sum = OptimizationHints.FastAdd(sum, numbers[i]);
}
Console.WriteLine($"Fast sum: {sum}");

// Hot path calculation with aggressive optimization
var result = OptimizationHints.HotPathCalculation(numbers);
Console.WriteLine($"Hot path result: {result}");

// Example 2: Branch prediction optimizations
Console.WriteLine("\nBranch Optimization Examples:");

var testValues = new int[] { 5, -3, 10, -1, 7, -8, 2 };

foreach (var value in testValues)
{
    var processed = BranchOptimization.ProcessWithLikelyBranch(value);
    Console.WriteLine($"Value {value} -> {processed}");
}

// Branchless operations
var a = 15;
var b = 8;
Console.WriteLine($"Branchless max({a}, {b}) = {BranchOptimization.BranchlessMax(a, b)}");
Console.WriteLine($"Branchless min({a}, {b}) = {BranchOptimization.BranchlessMin(a, b)}");
Console.WriteLine($"Branchless abs({-a}) = {BranchOptimization.BranchlessAbs(-a)}");

// Jump table optimization
Console.WriteLine($"Jump table process(10, 2) = {BranchOptimization.ProcessWithJumpTable(10, 2)}");

// Example 3: Cache optimization demonstrations
Console.WriteLine("\nCache Optimization Examples:");

const int DataSize = 1000;

// Structure of Arrays (more cache-friendly for selective processing)
var soa = new CacheOptimization.StructureOfArrays(DataSize);
soa.ProcessXVectorized();

// Array of Structures (better for processing all fields together)
var aos = new CacheOptimization.ArrayOfStructures(DataSize);
aos.ProcessX();

Console.WriteLine($"Created and processed {DataSize} elements with SoA and AoS patterns");

// Loop unrolling demonstration
var unrollData = new int[16] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
Console.WriteLine($"Before unrolled processing: [{string.Join(", ", unrollData)}]");

CacheOptimization.ProcessArray4Elements(unrollData.AsSpan());
Console.WriteLine($"After unrolled processing: [{string.Join(", ", unrollData)}]");

// Example 4: String optimizations
Console.WriteLine("\nString Optimization Examples:");

var parts = new[] { "Hello", " ", "World", "!" };
var optimizedString = StringOptimizations.BuildStringOptimal(parts);
Console.WriteLine($"Optimized string building: '{optimizedString}'");

// Span-based operations (no allocations)
var text = "Hello, World!".AsSpan();
var prefix = "Hello".AsSpan();
var hasPrefix = StringOptimizations.StartsWithFast(text, prefix);
Console.WriteLine($"Text starts with prefix: {hasPrefix}");

var charIndex = StringOptimizations.IndexOfFast(text, 'W');
Console.WriteLine($"Index of 'W': {charIndex}");

// Fast hex conversion
var bytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
var hexString = StringOptimizations.ToHexString(bytes);
Console.WriteLine($"Bytes to hex: {hexString}");

// Example 5: Bit manipulation optimizations
Console.WriteLine("\nBit Manipulation Examples:");

uint bitValue = 0b11010110; // 214 in binary
Console.WriteLine($"Original value: {bitValue} (binary: {Convert.ToString(bitValue, 2).PadLeft(8, '0')})");

Console.WriteLine($"Population count (1-bits): {BitOptimizations.PopCount(bitValue)}");
Console.WriteLine($"Leading zero count: {BitOptimizations.LeadingZeroCount(bitValue)}");
Console.WriteLine($"Trailing zero count: {BitOptimizations.TrailingZeroCount(bitValue)}");

var powerTest = 16;
Console.WriteLine($"{powerTest} is power of 2: {BitOptimizations.IsPowerOf2(powerTest)}");
Console.WriteLine($"Next power of 2 after 100: {BitOptimizations.NextPowerOf2(100)}");

// Bit operations
Console.WriteLine($"Get bit 3: {BitOptimizations.GetBit(bitValue, 3)}");
var setBit = BitOptimizations.SetBit(bitValue, 0);
Console.WriteLine($"Set bit 0: {setBit} (binary: {Convert.ToString(setBit, 2).PadLeft(8, '0')})");

var reversedBits = BitOptimizations.ReverseBits(bitValue);
Console.WriteLine($"Reversed bits: {reversedBits} (binary: {Convert.ToString(reversedBits, 2).PadLeft(32, '0')})");

// Example 6: Arithmetic optimizations
Console.WriteLine("\nArithmetic Optimization Examples:");

var sqrtTest = 144u;
var intSqrt = ArithmeticOptimizations.IntegerSquareRoot(sqrtTest);
Console.WriteLine($"Integer square root of {sqrtTest}: {intSqrt}");

var multTest = 7;
Console.WriteLine($"{multTest} * 3 = {ArithmeticOptimizations.MultiplyBy3(multTest)}");
Console.WriteLine($"{multTest} * 5 = {ArithmeticOptimizations.MultiplyBy5(multTest)}");
Console.WriteLine($"{multTest} * 7 = {ArithmeticOptimizations.MultiplyBy7(multTest)}");

var avg = ArithmeticOptimizations.FastAverage(10, 20);
Console.WriteLine($"Fast average of 10 and 20: {avg}");

// Example 7: Collection optimizations
Console.WriteLine("\nCollection Optimization Examples:");

// Pre-sized collections
var optimalList = CollectionOptimizations.CreateOptimalList<int>(1000);
var optimalDict = CollectionOptimizations.CreateOptimalDictionary<string, int>(100);
Console.WriteLine($"Created optimal list (capacity: {optimalList.Capacity}) and dictionary");

// Struct enumerator (no allocation)
var testArray = new int[] { 1, 2, 3, 4, 5 };
var enumerator = new CollectionOptimizations.ArrayEnumerator<int>(testArray);

Console.Write("Struct enumerator: ");
while (enumerator.MoveNext())
{
    Console.Write($"{enumerator.Current} ");
}
Console.WriteLine();

// Pooled array processing
CollectionOptimizations.ProcessWithPooledArray<int>(100, array =>
{
    for (int i = 0; i < 10; i++) // Only use first 10 elements
    {
        array[i] = i * i;
    }
    Console.WriteLine($"Processed pooled array: [{string.Join(", ", array.Take(10))}]");
});

// Batch processing
var largeList = Enumerable.Range(1, 20).ToList();
Console.WriteLine("Batch processing results:");
CollectionOptimizations.ProcessInBatches(largeList, 5, item => Console.Write($"{item} "));
Console.WriteLine();

// Example 8: Performance measurement
Console.WriteLine("\nPerformance Measurement Examples:");

// Benchmark simple operations
var simpleTime = PerformanceMeasurement.BenchmarkAction(() =>
{
    var temp = 0;
    for (int i = 0; i < 1000; i++)
    {
        temp += i;
    }
}, 100);

Console.WriteLine($"Simple loop benchmark: {simpleTime.TotalMicroseconds:F2} μs average");

// Benchmark with allocation tracking
var (time, allocations) = PerformanceMeasurement.BenchmarkWithAllocations(() =>
{
    var list = new List<int>();
    for (int i = 0; i < 100; i++)
    {
        list.Add(i);
    }
}, 100);

Console.WriteLine($"List allocation benchmark: {time.TotalMicroseconds:F2} μs, {allocations} bytes allocated");

// Example 9: JIT optimization
Console.WriteLine("\nJIT Optimization Examples:");

// Warmup methods to avoid first-call JIT overhead
JitOptimizations.WarmupMethod(() => ArithmeticOptimizations.MultiplyBy3(5));

var evenTest = 42;
var oddTest = 43;
Console.WriteLine($"{evenTest} is even: {JitOptimizations.IsEven(evenTest)}");
Console.WriteLine($"{oddTest} is odd: {JitOptimizations.IsOdd(oddTest)}");

// Generic specialization
var floatArray = new float[] { 1.1f, 2.2f, 3.3f };
JitOptimizations.ProcessGeneric(floatArray);
Console.WriteLine("Processed float array with generic specialization");

// Example 10: Memory optimizations
Console.WriteLine("\nMemory Optimization Examples:");

// Object pooling
var objectPool = new MemoryOptimizations.ExpensiveObjectPool(() => new MemoryOptimizations.ExpensiveObject());

var expensiveObj = objectPool.Rent();
// Use the object...
objectPool.Return(expensiveObj);
Console.WriteLine("Demonstrated object pooling pattern");

// Stack allocation
MemoryOptimizations.ProcessOnStack();
Console.WriteLine("Processed data on stack using stackalloc");

// Span-based operations
var filePath = @"C:\temp\example.txt".AsSpan();
var fileName = MemoryOptimizations.GetFileNameWithoutExtension(filePath);
Console.WriteLine($"File name without extension: {fileName.ToString()}");

// Example 11: Advanced benchmarking
Console.WriteLine("\nAdvanced Performance Comparisons:");

// Compare different approaches
var data = Enumerable.Range(1, 10000).ToArray();

// Standard LINQ
var linqTime = PerformanceMeasurement.BenchmarkAction(() =>
{
    _ = data.Where(x => x % 2 == 0).Sum();
}, 100);

// Optimized loop
var optimizedTime = PerformanceMeasurement.BenchmarkAction(() =>
{
    var sum = 0;
    for (int i = 0; i < data.Length; i++)
    {
        if (JitOptimizations.IsEven(data[i]))
        {
            sum += data[i];
        }
    }
}, 100);

Console.WriteLine($"LINQ approach: {linqTime.TotalMicroseconds:F2} μs");
Console.WriteLine($"Optimized loop: {optimizedTime.TotalMicroseconds:F2} μs");
Console.WriteLine($"Performance improvement: {linqTime.TotalMicroseconds / optimizedTime.TotalMicroseconds:F2}x faster");

// Cache performance profiling
Console.WriteLine("\nCache Performance Analysis:");
PerformanceMeasurement.ProfileCachePerformance();

// Example 12: Real-world optimization scenarios
Console.WriteLine("\nReal-world Optimization Scenarios:");

// String processing optimization
var csvLine = "John,Doe,30,Engineer,New York";
var fields = new List<string>();

// Traditional approach (allocates many strings)
var traditionalTime = PerformanceMeasurement.BenchmarkAction(() =>
{
    fields.Clear();
    fields.AddRange(csvLine.Split(','));
}, 1000);

// Optimized approach using Span<char>
var optimizedCsvTime = PerformanceMeasurement.BenchmarkAction(() =>
{
    var span = csvLine.AsSpan();
    int fieldCount = 0;
    int start = 0;
    
    for (int i = 0; i <= span.Length; i++)
    {
        if (i == span.Length || span[i] == ',')
        {
            // Process field without allocation (in real scenario)
            fieldCount++;
            start = i + 1;
        }
    }
}, 1000);

Console.WriteLine($"Traditional CSV parsing: {traditionalTime.TotalMicroseconds:F2} μs");
Console.WriteLine($"Span-optimized CSV parsing: {optimizedCsvTime.TotalMicroseconds:F2} μs");
Console.WriteLine($"CSV parsing improvement: {traditionalTime.TotalMicroseconds / optimizedCsvTime.TotalMicroseconds:F2}x faster");

// Mathematical computation optimization
var mathData = Enumerable.Range(1, 1000).Select(x => (double)x).ToArray();

var standardMathTime = PerformanceMeasurement.BenchmarkAction(() =>
{
    double sum = 0;
    foreach (var value in mathData)
    {
        sum += Math.Sqrt(value * value + 1);
    }
}, 100);

var optimizedMathTime = PerformanceMeasurement.BenchmarkAction(() =>
{
    double sum = 0;
    for (int i = 0; i < mathData.Length; i++)
    {
        var temp = mathData[i];
        sum += Math.Sqrt(temp * temp + 1);
    }
}, 100);

Console.WriteLine($"Standard math loop: {standardMathTime.TotalMicroseconds:F2} μs");
Console.WriteLine($"Optimized math loop: {optimizedMathTime.TotalMicroseconds:F2} μs");

Console.WriteLine("\nMicro-optimization examples completed!");
```

**Notes**:

- Micro-optimizations should only be applied to performance-critical hot paths
- Always measure actual performance impact - micro-optimizations can sometimes hurt performance
- Modern JIT compilers are sophisticated and may already perform many optimizations
- Profile first, optimize second - focus on actual bottlenecks
- Cache-friendly code often provides bigger gains than algorithmic micro-optimizations
- Readability and maintainability should not be sacrificed for marginal performance gains
- Different hardware architectures may respond differently to optimizations
- Branch prediction misses can be more expensive than the optimization saves
- Memory allocation patterns often have more impact than computational optimizations
- Use performance profilers to identify real bottlenecks before applying micro-optimizations
- Some optimizations may become obsolete with newer .NET versions
- Always validate optimizations with realistic workloads and data sets

**Prerequisites**:

- Deep understanding of computer architecture (CPU, cache hierarchy, branch prediction)
- Knowledge of .NET JIT compilation and runtime behavior
- Proficiency with performance profiling tools (PerfView, dotTrace, BenchmarkDotNet)
- Understanding of memory management and garbage collection
- Familiarity with assembly language concepts
- Knowledge of SIMD and vectorization principles
- Understanding of modern CPU instruction sets (SSE, AVX)

**Related Snippets**:

- [Vectorization](vectorization.md) - SIMD operations and hardware acceleration
- [Memory Pools](memory-pools.md) - Advanced memory management strategies
- [Span Operations](span-operations.md) - High-performance memory operations
- [Performance LINQ](performance-linq.md) - Optimized LINQ operations
