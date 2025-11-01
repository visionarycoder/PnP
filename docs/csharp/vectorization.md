# Vectorization and SIMD Operations

**Description**: High-performance numerical computations using SIMD (Single Instruction, Multiple Data) operations with Vector<T>, hardware acceleration, and vectorized algorithms for maximum throughput in mathematical and data processing operations.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

// Core vectorization utilities and extensions
public static class VectorExtensions
{
    // Check if vectorization is available and beneficial
    public static bool IsVectorizationBeneficial<T>(int length) where T : struct
    {
        return Vector.IsHardwareAccelerated && 
               length >= Vector<T>.Count * 4; // Minimum threshold for benefit
    }

    // Get optimal chunk size for vectorization
    public static int GetOptimalChunkSize<T>() where T : struct
    {
        return Vector<T>.Count * 8; // Process multiple vectors at once
    }

    // Convert array to vectors with remainder handling
    public static (ReadOnlySpan<Vector<T>> vectors, ReadOnlySpan<T> remainder) AsVectors<T>(
        this ReadOnlySpan<T> span) where T : struct
    {
        var vectorCount = span.Length / Vector<T>.Count;
        var vectorBytes = vectorCount * Vector<T>.Count;
        
        var vectors = MemoryMarshal.Cast<T, Vector<T>>(span.Slice(0, vectorBytes));
        var remainder = span.Slice(vectorBytes);
        
        return (vectors, remainder);
    }

    // Convert mutable span to vectors
    public static (Span<Vector<T>> vectors, Span<T> remainder) AsVectors<T>(
        this Span<T> span) where T : struct
    {
        var vectorCount = span.Length / Vector<T>.Count;
        var vectorBytes = vectorCount * Vector<T>.Count;
        
        var vectors = MemoryMarshal.Cast<T, Vector<T>>(span.Slice(0, vectorBytes));
        var remainder = span.Slice(vectorBytes);
        
        return (vectors, remainder);
    }

    // Vectorized element-wise operations
    public static void Add<T>(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<T> result) 
        where T : struct
    {
        if (left.Length != right.Length || left.Length != result.Length)
            throw new ArgumentException("All spans must have the same length");

        if (!IsVectorizationBeneficial<T>(left.Length))
        {
            AddScalar(left, right, result);
            return;
        }

        var (leftVectors, leftRemainder) = left.AsVectors();
        var (rightVectors, rightRemainder) = right.AsVectors();
        var (resultVectors, resultRemainder) = result.AsVectors();

        // Vectorized processing
        for (int i = 0; i < leftVectors.Length; i++)
        {
            resultVectors[i] = leftVectors[i] + rightVectors[i];
        }

        // Process remainder scalar
        AddScalar(leftRemainder, rightRemainder, resultRemainder);
    }

    private static void AddScalar<T>(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<T> result) 
        where T : struct
    {
        if (typeof(T) == typeof(int))
        {
            var leftInt = MemoryMarshal.Cast<T, int>(left);
            var rightInt = MemoryMarshal.Cast<T, int>(right);
            var resultInt = MemoryMarshal.Cast<T, int>(result);
            
            for (int i = 0; i < leftInt.Length; i++)
            {
                resultInt[i] = leftInt[i] + rightInt[i];
            }
        }
        else if (typeof(T) == typeof(float))
        {
            var leftFloat = MemoryMarshal.Cast<T, float>(left);
            var rightFloat = MemoryMarshal.Cast<T, float>(right);
            var resultFloat = MemoryMarshal.Cast<T, float>(result);
            
            for (int i = 0; i < leftFloat.Length; i++)
            {
                resultFloat[i] = leftFloat[i] + rightFloat[i];
            }
        }
        else if (typeof(T) == typeof(double))
        {
            var leftDouble = MemoryMarshal.Cast<T, double>(left);
            var rightDouble = MemoryMarshal.Cast<T, double>(right);
            var resultDouble = MemoryMarshal.Cast<T, double>(result);
            
            for (int i = 0; i < leftDouble.Length; i++)
            {
                resultDouble[i] = leftDouble[i] + rightDouble[i];
            }
        }
    }

    // Similar methods for other operations
    public static void Multiply<T>(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<T> result) 
        where T : struct
    {
        if (left.Length != right.Length || left.Length != result.Length)
            throw new ArgumentException("All spans must have the same length");

        if (!IsVectorizationBeneficial<T>(left.Length))
        {
            MultiplyScalar(left, right, result);
            return;
        }

        var (leftVectors, leftRemainder) = left.AsVectors();
        var (rightVectors, rightRemainder) = right.AsVectors();
        var (resultVectors, resultRemainder) = result.AsVectors();

        for (int i = 0; i < leftVectors.Length; i++)
        {
            resultVectors[i] = leftVectors[i] * rightVectors[i];
        }

        MultiplyScalar(leftRemainder, rightRemainder, resultRemainder);
    }

    private static void MultiplyScalar<T>(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Span<T> result) 
        where T : struct
    {
        if (typeof(T) == typeof(int))
        {
            var leftInt = MemoryMarshal.Cast<T, int>(left);
            var rightInt = MemoryMarshal.Cast<T, int>(right);
            var resultInt = MemoryMarshal.Cast<T, int>(result);
            
            for (int i = 0; i < leftInt.Length; i++)
            {
                resultInt[i] = leftInt[i] * rightInt[i];
            }
        }
        else if (typeof(T) == typeof(float))
        {
            var leftFloat = MemoryMarshal.Cast<T, float>(left);
            var rightFloat = MemoryMarshal.Cast<T, float>(right);
            var resultFloat = MemoryMarshal.Cast<T, float>(result);
            
            for (int i = 0; i < leftFloat.Length; i++)
            {
                resultFloat[i] = leftFloat[i] * rightFloat[i];
            }
        }
        else if (typeof(T) == typeof(double))
        {
            var leftDouble = MemoryMarshal.Cast<T, double>(left);
            var rightDouble = MemoryMarshal.Cast<T, double>(right);
            var resultDouble = MemoryMarshal.Cast<T, double>(result);
            
            for (int i = 0; i < leftDouble.Length; i++)
            {
                resultDouble[i] = leftDouble[i] * rightDouble[i];
            }
        }
    }
}

// Vectorized mathematical operations
public static class VectorMath
{
    // Vectorized sum with different data types
    public static int Sum(ReadOnlySpan<int> values)
    {
        if (!VectorExtensions.IsVectorizationBeneficial<int>(values.Length))
        {
            return SumScalar(values);
        }

        var (vectors, remainder) = values.AsVectors();
        var accumulator = Vector<int>.Zero;

        // Process vectors
        for (int i = 0; i < vectors.Length; i++)
        {
            accumulator += vectors[i];
        }

        // Sum vector elements
        int result = Vector.Dot(accumulator, Vector<int>.One);

        // Add remainder
        result += SumScalar(remainder);

        return result;
    }

    private static int SumScalar(ReadOnlySpan<int> values)
    {
        int sum = 0;
        for (int i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }
        return sum;
    }

    public static float Sum(ReadOnlySpan<float> values)
    {
        if (!VectorExtensions.IsVectorizationBeneficial<float>(values.Length))
        {
            return SumScalar(values);
        }

        var (vectors, remainder) = values.AsVectors();
        var accumulator = Vector<float>.Zero;

        for (int i = 0; i < vectors.Length; i++)
        {
            accumulator += vectors[i];
        }

        float result = Vector.Dot(accumulator, Vector<float>.One);
        result += SumScalar(remainder);

        return result;
    }

    private static float SumScalar(ReadOnlySpan<float> values)
    {
        float sum = 0f;
        for (int i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }
        return sum;
    }

    public static double Sum(ReadOnlySpan<double> values)
    {
        if (!VectorExtensions.IsVectorizationBeneficial<double>(values.Length))
        {
            return SumScalar(values);
        }

        var (vectors, remainder) = values.AsVectors();
        var accumulator = Vector<double>.Zero;

        for (int i = 0; i < vectors.Length; i++)
        {
            accumulator += vectors[i];
        }

        double result = Vector.Dot(accumulator, Vector<double>.One);
        result += SumScalar(remainder);

        return result;
    }

    private static double SumScalar(ReadOnlySpan<double> values)
    {
        double sum = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            sum += values[i];
        }
        return sum;
    }

    // Vectorized dot product
    public static float DotProduct(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
        if (left.Length != right.Length)
            throw new ArgumentException("Vectors must have the same length");

        if (!VectorExtensions.IsVectorizationBeneficial<float>(left.Length))
        {
            return DotProductScalar(left, right);
        }

        var (leftVectors, leftRemainder) = left.AsVectors();
        var (rightVectors, rightRemainder) = right.AsVectors();

        var accumulator = Vector<float>.Zero;

        for (int i = 0; i < leftVectors.Length; i++)
        {
            accumulator += leftVectors[i] * rightVectors[i];
        }

        float result = Vector.Dot(accumulator, Vector<float>.One);
        result += DotProductScalar(leftRemainder, rightRemainder);

        return result;
    }

    private static float DotProductScalar(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
        float sum = 0f;
        for (int i = 0; i < left.Length; i++)
        {
            sum += left[i] * right[i];
        }
        return sum;
    }

    // Vectorized distance calculations
    public static float EuclideanDistance(ReadOnlySpan<float> left, ReadOnlySpan<float> right)
    {
        if (left.Length != right.Length)
            throw new ArgumentException("Vectors must have the same length");

        var pool = ArrayPool<float>.Shared;
        var diffArray = pool.Rent(left.Length);

        try
        {
            var diff = diffArray.AsSpan(0, left.Length);
            
            // Calculate differences
            VectorExtensions.Add(left, right.ToArray().AsSpan().Select(x => -x).ToArray(), diff);
            
            // Square the differences
            VectorExtensions.Multiply(diff, diff, diff);
            
            // Sum and take square root
            return MathF.Sqrt(Sum(diff));
        }
        finally
        {
            pool.Return(diffArray);
        }
    }

    // Vectorized normalization
    public static void Normalize(Span<float> vector)
    {
        var length = MathF.Sqrt(DotProduct(vector, vector));
        
        if (length == 0f)
            return;

        var (vectors, remainder) = vector.AsVectors();
        var lengthVector = new Vector<float>(length);

        // Vectorized division
        for (int i = 0; i < vectors.Length; i++)
        {
            vectors[i] /= lengthVector;
        }

        // Handle remainder
        for (int i = 0; i < remainder.Length; i++)
        {
            remainder[i] /= length;
        }
    }

    // Vectorized matrix operations
    public static void MatrixMultiply(ReadOnlySpan<float> matrixA, ReadOnlySpan<float> matrixB, 
        Span<float> result, int rowsA, int colsA, int colsB)
    {
        if (matrixA.Length != rowsA * colsA || 
            matrixB.Length != colsA * colsB || 
            result.Length != rowsA * colsB)
            throw new ArgumentException("Matrix dimensions don't match");

        // Simple vectorized matrix multiplication (can be optimized further)
        for (int i = 0; i < rowsA; i++)
        {
            for (int j = 0; j < colsB; j++)
            {
                float sum = 0f;
                
                var rowA = matrixA.Slice(i * colsA, colsA);
                
                // Extract column B (non-contiguous, so scalar for now)
                for (int k = 0; k < colsA; k++)
                {
                    sum += rowA[k] * matrixB[k * colsB + j];
                }
                
                result[i * colsB + j] = sum;
            }
        }
    }

    // Vectorized statistical operations
    public static (float mean, float variance) MeanAndVariance(ReadOnlySpan<float> values)
    {
        if (values.Length == 0)
            return (0f, 0f);

        var mean = Sum(values) / values.Length;
        
        var pool = ArrayPool<float>.Shared;
        var deviationsArray = pool.Rent(values.Length);

        try
        {
            var deviations = deviationsArray.AsSpan(0, values.Length);
            
            // Calculate deviations from mean
            var meanVector = new Vector<float>(mean);
            var (vectors, remainder) = values.AsVectors();
            var (devVectors, devRemainder) = deviations.AsVectors();

            // Vectorized subtraction
            for (int i = 0; i < vectors.Length; i++)
            {
                devVectors[i] = vectors[i] - meanVector;
            }

            // Handle remainder
            for (int i = 0; i < remainder.Length; i++)
            {
                devRemainder[i] = remainder[i] - mean;
            }

            // Square deviations
            VectorExtensions.Multiply(deviations, deviations, deviations);
            
            var variance = Sum(deviations) / (values.Length - 1);
            
            return (mean, variance);
        }
        finally
        {
            pool.Return(deviationsArray);
        }
    }
}

// Advanced vectorization with hardware intrinsics
public static class IntrinsicOperations
{
    // Check for specific CPU features
    public static bool SupportsAVX2 => Avx2.IsSupported;
    public static bool SupportsAVX => Avx.IsSupported;
    public static bool SupportsSSE41 => Sse41.IsSupported;
    public static bool SupportsSSE2 => Sse2.IsSupported;

    // High-performance sum using AVX2 when available
    public static unsafe int SumAVX2(ReadOnlySpan<int> values)
    {
        if (!Avx2.IsSupported || values.Length < 8)
        {
            return VectorMath.Sum(values);
        }

        fixed (int* ptr = values)
        {
            var sum = Vector256<int>.Zero;
            int i = 0;

            // Process 8 integers at a time
            for (; i <= values.Length - 8; i += 8)
            {
                var data = Avx2.LoadVector256(ptr + i);
                sum = Avx2.Add(sum, data);
            }

            // Horizontal sum of vector elements
            var result = 0;
            var sumArray = stackalloc int[8];
            Avx2.Store(sumArray, sum);

            for (int j = 0; j < 8; j++)
            {
                result += sumArray[j];
            }

            // Add remaining elements
            for (; i < values.Length; i++)
            {
                result += values[i];
            }

            return result;
        }
    }

    // Vectorized comparison operations
    public static unsafe int CountGreaterThan(ReadOnlySpan<int> values, int threshold)
    {
        if (!Avx2.IsSupported || values.Length < 8)
        {
            return CountGreaterThanScalar(values, threshold);
        }

        fixed (int* ptr = values)
        {
            var thresholdVec = Vector256.Create(threshold);
            var count = 0;
            int i = 0;

            for (; i <= values.Length - 8; i += 8)
            {
                var data = Avx2.LoadVector256(ptr + i);
                var comparison = Avx2.CompareGreaterThan(data, thresholdVec);
                
                // Count set bits (each represents a true comparison)
                var mask = Avx2.MoveMask(comparison.AsByte());
                count += System.Numerics.BitOperations.PopCount((uint)mask) / 4; // 4 bytes per int
            }

            // Handle remainder
            for (; i < values.Length; i++)
            {
                if (values[i] > threshold)
                    count++;
            }

            return count;
        }
    }

    private static int CountGreaterThanScalar(ReadOnlySpan<int> values, int threshold)
    {
        int count = 0;
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] > threshold)
                count++;
        }
        return count;
    }

    // Vectorized memory copy
    public static unsafe void MemoryCopyAVX(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (source.Length != destination.Length)
            throw new ArgumentException("Source and destination must have the same length");

        if (!Avx.IsSupported || source.Length < 32)
        {
            source.CopyTo(destination);
            return;
        }

        fixed (byte* srcPtr = source)
        fixed (byte* dstPtr = destination)
        {
            int i = 0;

            // Copy 32 bytes at a time using AVX
            for (; i <= source.Length - 32; i += 32)
            {
                var data = Avx.LoadVector256(srcPtr + i);
                Avx.Store(dstPtr + i, data);
            }

            // Copy remaining bytes
            source.Slice(i).CopyTo(destination.Slice(i));
        }
    }

    // Vectorized string comparison
    public static unsafe bool StringEqualsVectorized(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        if (left.Length != right.Length)
            return false;

        if (!Avx2.IsSupported || left.Length < 16)
        {
            return left.SequenceEqual(right);
        }

        fixed (char* leftPtr = left)
        fixed (char* rightPtr = right)
        {
            int i = 0;

            // Compare 16 chars at a time
            for (; i <= left.Length - 16; i += 16)
            {
                var leftVec = Avx2.LoadVector256((short*)(leftPtr + i));
                var rightVec = Avx2.LoadVector256((short*)(rightPtr + i));
                var comparison = Avx2.CompareEqual(leftVec, rightVec);

                if (!Avx2.TestZ(comparison, comparison)) // If not all bits are zero
                {
                    var mask = Avx2.MoveMask(comparison.AsByte());
                    if (mask != -1) // Not all comparisons were true
                        return false;
                }
            }

            // Compare remaining chars
            return left.Slice(i).SequenceEqual(right.Slice(i));
        }
    }
}

// Vectorized algorithms for common operations
public static class VectorizedAlgorithms
{
    // Vectorized linear search
    public static int IndexOf<T>(ReadOnlySpan<T> span, T value) where T : struct, IEquatable<T>
    {
        if (Vector.IsHardwareAccelerated && span.Length >= Vector<T>.Count)
        {
            var searchVector = new Vector<T>(value);
            var (vectors, remainder) = span.AsVectors();

            for (int i = 0; i < vectors.Length; i++)
            {
                var equals = Vector.Equals(vectors[i], searchVector);
                
                if (Vector<T>.Zero != equals)
                {
                    // Found match, find exact index
                    var startIndex = i * Vector<T>.Count;
                    for (int j = 0; j < Vector<T>.Count; j++)
                    {
                        if (span[startIndex + j].Equals(value))
                            return startIndex + j;
                    }
                }
            }

            // Check remainder
            var remainderStart = vectors.Length * Vector<T>.Count;
            for (int i = 0; i < remainder.Length; i++)
            {
                if (remainder[i].Equals(value))
                    return remainderStart + i;
            }
        }
        else
        {
            // Fallback to scalar search
            for (int i = 0; i < span.Length; i++)
            {
                if (span[i].Equals(value))
                    return i;
            }
        }

        return -1;
    }

    // Vectorized fill operation
    public static void Fill<T>(Span<T> span, T value) where T : struct
    {
        if (Vector.IsHardwareAccelerated && span.Length >= Vector<T>.Count)
        {
            var valueVector = new Vector<T>(value);
            var (vectors, remainder) = span.AsVectors();

            for (int i = 0; i < vectors.Length; i++)
            {
                vectors[i] = valueVector;
            }

            // Fill remainder
            remainder.Fill(value);
        }
        else
        {
            span.Fill(value);
        }
    }

    // Vectorized copy with transformation
    public static void CopyWithMultiplier(ReadOnlySpan<float> source, Span<float> destination, float multiplier)
    {
        if (source.Length != destination.Length)
            throw new ArgumentException("Source and destination must have the same length");

        if (Vector.IsHardwareAccelerated && source.Length >= Vector<float>.Count)
        {
            var multiplierVector = new Vector<float>(multiplier);
            var (sourceVectors, sourceRemainder) = source.AsVectors();
            var (destVectors, destRemainder) = destination.AsVectors();

            for (int i = 0; i < sourceVectors.Length; i++)
            {
                destVectors[i] = sourceVectors[i] * multiplierVector;
            }

            // Process remainder
            for (int i = 0; i < sourceRemainder.Length; i++)
            {
                destRemainder[i] = sourceRemainder[i] * multiplier;
            }
        }
        else
        {
            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = source[i] * multiplier;
            }
        }
    }

    // Vectorized clamp operation
    public static void Clamp<T>(Span<T> values, T min, T max) 
        where T : struct, IComparable<T>
    {
        if (Vector.IsHardwareAccelerated && values.Length >= Vector<T>.Count && 
            (typeof(T) == typeof(float) || typeof(T) == typeof(int)))
        {
            var (vectors, remainder) = values.AsVectors();
            
            if (typeof(T) == typeof(float))
            {
                var minVec = new Vector<float>(Unsafe.As<T, float>(ref min));
                var maxVec = new Vector<float>(Unsafe.As<T, float>(ref max));
                var floatVectors = MemoryMarshal.Cast<Vector<T>, Vector<float>>(vectors);

                for (int i = 0; i < floatVectors.Length; i++)
                {
                    floatVectors[i] = Vector.Min(Vector.Max(floatVectors[i], minVec), maxVec);
                }
            }
            else if (typeof(T) == typeof(int))
            {
                var minVec = new Vector<int>(Unsafe.As<T, int>(ref min));
                var maxVec = new Vector<int>(Unsafe.As<T, int>(ref max));
                var intVectors = MemoryMarshal.Cast<Vector<T>, Vector<int>>(vectors);

                for (int i = 0; i < intVectors.Length; i++)
                {
                    intVectors[i] = Vector.Min(Vector.Max(intVectors[i], minVec), maxVec);
                }
            }

            // Clamp remainder
            ClampScalar(remainder, min, max);
        }
        else
        {
            ClampScalar(values, min, max);
        }
    }

    private static void ClampScalar<T>(Span<T> values, T min, T max) where T : IComparable<T>
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i].CompareTo(min) < 0)
                values[i] = min;
            else if (values[i].CompareTo(max) > 0)
                values[i] = max;
        }
    }

    // Vectorized histogram calculation
    public static void CalculateHistogram(ReadOnlySpan<int> values, Span<int> histogram, int minValue, int maxValue)
    {
        if (histogram.Length != (maxValue - minValue + 1))
            throw new ArgumentException("Histogram size doesn't match value range");

        histogram.Clear();

        // For now, use scalar implementation as vectorized histograms are complex
        // In practice, you'd want specialized vectorized histogram algorithms
        foreach (var value in values)
        {
            if (value >= minValue && value <= maxValue)
            {
                histogram[value - minValue]++;
            }
        }
    }
}

// Benchmarking and performance measurement
public static class VectorizationBenchmarks
{
    public static void BenchmarkSum(int[] data, int iterations = 1000)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Scalar sum
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            var sum = 0;
            for (int j = 0; j < data.Length; j++)
            {
                sum += data[j];
            }
            _ = sum; // Prevent optimization
        }
        stopwatch.Stop();
        Console.WriteLine($"Scalar sum: {stopwatch.ElapsedMilliseconds}ms");

        // LINQ sum
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            _ = data.Sum();
        }
        stopwatch.Stop();
        Console.WriteLine($"LINQ sum: {stopwatch.ElapsedMilliseconds}ms");

        // Vectorized sum
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            _ = VectorMath.Sum(data.AsSpan());
        }
        stopwatch.Stop();
        Console.WriteLine($"Vectorized sum: {stopwatch.ElapsedMilliseconds}ms");

        // AVX2 sum (if supported)
        if (IntrinsicOperations.SupportsAVX2)
        {
            stopwatch.Restart();
            for (int i = 0; i < iterations; i++)
            {
                _ = IntrinsicOperations.SumAVX2(data.AsSpan());
            }
            stopwatch.Stop();
            Console.WriteLine($"AVX2 sum: {stopwatch.ElapsedMilliseconds}ms");
        }
    }

    public static void BenchmarkDotProduct(float[] left, float[] right, int iterations = 1000)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Scalar dot product
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            float sum = 0f;
            for (int j = 0; j < left.Length; j++)
            {
                sum += left[j] * right[j];
            }
            _ = sum;
        }
        stopwatch.Stop();
        Console.WriteLine($"Scalar dot product: {stopwatch.ElapsedMilliseconds}ms");

        // Vectorized dot product
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            _ = VectorMath.DotProduct(left.AsSpan(), right.AsSpan());
        }
        stopwatch.Stop();
        Console.WriteLine($"Vectorized dot product: {stopwatch.ElapsedMilliseconds}ms");
    }

    public static void BenchmarkArrayOperations(int arraySize = 1000000, int iterations = 100)
    {
        var left = Enumerable.Range(1, arraySize).ToArray();
        var right = Enumerable.Range(1, arraySize).Select(x => x * 2).ToArray();
        var result = new int[arraySize];

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Scalar addition
        stopwatch.Restart();
        for (int iter = 0; iter < iterations; iter++)
        {
            for (int i = 0; i < arraySize; i++)
            {
                result[i] = left[i] + right[i];
            }
        }
        stopwatch.Stop();
        Console.WriteLine($"Scalar addition: {stopwatch.ElapsedMilliseconds}ms");

        // Vectorized addition
        stopwatch.Restart();
        for (int iter = 0; iter < iterations; iter++)
        {
            VectorExtensions.Add(left.AsSpan(), right.AsSpan(), result.AsSpan());
        }
        stopwatch.Stop();
        Console.WriteLine($"Vectorized addition: {stopwatch.ElapsedMilliseconds}ms");
    }

    public static void ShowHardwareCapabilities()
    {
        Console.WriteLine("Hardware Vectorization Capabilities:");
        Console.WriteLine($"Vector.IsHardwareAccelerated: {Vector.IsHardwareAccelerated}");
        Console.WriteLine($"Vector<int>.Count: {Vector<int>.Count}");
        Console.WriteLine($"Vector<float>.Count: {Vector<float>.Count}");
        Console.WriteLine($"Vector<double>.Count: {Vector<double>.Count}");
        Console.WriteLine();
        
        Console.WriteLine("CPU Instruction Set Support:");
        Console.WriteLine($"SSE2: {IntrinsicOperations.SupportsSSE2}");
        Console.WriteLine($"SSE41: {IntrinsicOperations.SupportsSSE41}");
        Console.WriteLine($"AVX: {IntrinsicOperations.SupportsAVX}");
        Console.WriteLine($"AVX2: {IntrinsicOperations.SupportsAVX2}");
    }
}

// Real-world vectorized applications
public static class VectorizedApplications
{
    // Image processing with vectorization
    public static void AdjustBrightness(Span<byte> imageData, float brightnessMultiplier)
    {
        var floatData = new float[imageData.Length];
        
        // Convert to float for processing
        for (int i = 0; i < imageData.Length; i++)
        {
            floatData[i] = imageData[i];
        }

        // Apply brightness adjustment
        VectorizedAlgorithms.CopyWithMultiplier(floatData.AsSpan(), floatData.AsSpan(), brightnessMultiplier);

        // Clamp and convert back
        VectorizedAlgorithms.Clamp(floatData.AsSpan(), 0f, 255f);

        for (int i = 0; i < imageData.Length; i++)
        {
            imageData[i] = (byte)floatData[i];
        }
    }

    // Financial calculations
    public static void CalculateReturns(ReadOnlySpan<float> prices, Span<float> returns)
    {
        if (prices.Length < 2 || returns.Length != prices.Length - 1)
            throw new ArgumentException("Invalid array sizes for return calculation");

        var pool = ArrayPool<float>.Shared;
        var currentPrices = pool.Rent(returns.Length);
        var previousPrices = pool.Rent(returns.Length);

        try
        {
            // Copy price arrays
            prices.Slice(1).CopyTo(currentPrices.AsSpan(0, returns.Length));
            prices.Slice(0, returns.Length).CopyTo(previousPrices.AsSpan(0, returns.Length));

            // Calculate returns: (current - previous) / previous
            VectorExtensions.Add(currentPrices.AsSpan(0, returns.Length), 
                                previousPrices.AsSpan(0, returns.Length).ToArray().Select(x => -x).ToArray(), 
                                returns);

            // Divide by previous prices (element-wise)
            for (int i = 0; i < returns.Length; i++)
            {
                returns[i] /= previousPrices[i];
            }
        }
        finally
        {
            pool.Return(currentPrices);
            pool.Return(previousPrices);
        }
    }

    // Signal processing - moving average
    public static void CalculateMovingAverage(ReadOnlySpan<float> signal, Span<float> result, int windowSize)
    {
        if (result.Length != signal.Length - windowSize + 1)
            throw new ArgumentException("Result array size incorrect for moving average");

        for (int i = 0; i < result.Length; i++)
        {
            var window = signal.Slice(i, windowSize);
            result[i] = VectorMath.Sum(window) / windowSize;
        }
    }

    // Machine learning - vector operations
    public static float CosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
    {
        if (vectorA.Length != vectorB.Length)
            throw new ArgumentException("Vectors must have the same length");

        var dotProduct = VectorMath.DotProduct(vectorA, vectorB);
        var magnitudeA = MathF.Sqrt(VectorMath.DotProduct(vectorA, vectorA));
        var magnitudeB = MathF.Sqrt(VectorMath.DotProduct(vectorB, vectorB));

        if (magnitudeA == 0f || magnitudeB == 0f)
            return 0f;

        return dotProduct / (magnitudeA * magnitudeB);
    }

    // Physics simulation - particle updates
    public static void UpdateParticleVelocities(Span<float> velocitiesX, Span<float> velocitiesY, 
        ReadOnlySpan<float> accelerationsX, ReadOnlySpan<float> accelerationsY, float deltaTime)
    {
        if (velocitiesX.Length != accelerationsX.Length || velocitiesY.Length != accelerationsY.Length)
            throw new ArgumentException("Velocity and acceleration arrays must have same length");

        var pool = ArrayPool<float>.Shared;
        var deltaAccelX = pool.Rent(velocitiesX.Length);
        var deltaAccelY = pool.Rent(velocitiesY.Length);

        try
        {
            // Calculate deltaTime * acceleration
            VectorizedAlgorithms.CopyWithMultiplier(accelerationsX, deltaAccelX.AsSpan(0, velocitiesX.Length), deltaTime);
            VectorizedAlgorithms.CopyWithMultiplier(accelerationsY, deltaAccelY.AsSpan(0, velocitiesY.Length), deltaTime);

            // Add to velocities: v = v + a * dt
            VectorExtensions.Add(velocitiesX, deltaAccelX.AsSpan(0, velocitiesX.Length), velocitiesX);
            VectorExtensions.Add(velocitiesY, deltaAccelY.AsSpan(0, velocitiesY.Length), velocitiesY);
        }
        finally
        {
            pool.Return(deltaAccelX);
            pool.Return(deltaAccelY);
        }
    }
}
```

**Usage**:

```csharp
// Example 1: Hardware capabilities check
Console.WriteLine("Checking hardware vectorization capabilities:");
VectorizationBenchmarks.ShowHardwareCapabilities();
Console.WriteLine();

// Example 2: Basic vectorized operations
Console.WriteLine("Basic vectorized arithmetic operations:");

var array1 = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
var array2 = new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };
var result = new int[array1.Length];

Console.WriteLine($"Array 1: [{string.Join(", ", array1)}]");
Console.WriteLine($"Array 2: [{string.Join(", ", array2)}]");

// Vectorized addition
VectorExtensions.Add(array1.AsSpan(), array2.AsSpan(), result.AsSpan());
Console.WriteLine($"Addition result: [{string.Join(", ", result)}]");

// Vectorized multiplication
VectorExtensions.Multiply(array1.AsSpan(), array2.AsSpan(), result.AsSpan());
Console.WriteLine($"Multiplication result: [{string.Join(", ", result)}]");

// Example 3: Vectorized sum operations
Console.WriteLine("\nVectorized sum operations:");

var largeArray = Enumerable.Range(1, 1000).ToArray();
Console.WriteLine($"Summing array of {largeArray.Length} elements...");

// Compare different sum implementations
var scalarSum = largeArray.Sum(); // LINQ
var vectorSum = VectorMath.Sum(largeArray.AsSpan());

Console.WriteLine($"LINQ sum: {scalarSum}");
Console.WriteLine($"Vectorized sum: {vectorSum}");
Console.WriteLine($"Results match: {scalarSum == vectorSum}");

// Example 4: Floating-point operations
Console.WriteLine("\nFloating-point vector operations:");

var floats1 = new float[] { 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f, 8.5f };
var floats2 = new float[] { 0.5f, 1.5f, 2.5f, 3.5f, 4.5f, 5.5f, 6.5f, 7.5f };

Console.WriteLine($"Vector 1: [{string.Join(", ", floats1)}]");
Console.WriteLine($"Vector 2: [{string.Join(", ", floats2)}]");

var dotProduct = VectorMath.DotProduct(floats1.AsSpan(), floats2.AsSpan());
Console.WriteLine($"Dot product: {dotProduct:F2}");

var euclideanDist = VectorMath.EuclideanDistance(floats1.AsSpan(), floats2.AsSpan());
Console.WriteLine($"Euclidean distance: {euclideanDist:F2}");

// Normalize vector
var normalizeTest = (float[])floats1.Clone();
VectorMath.Normalize(normalizeTest.AsSpan());
Console.WriteLine($"Normalized vector 1: [{string.Join(", ", normalizeTest.Select(x => x.ToString("F3")))}]");

// Example 5: Statistical operations
Console.WriteLine("\nStatistical operations:");

var samples = new float[] { 1.2f, 2.3f, 3.1f, 2.8f, 4.5f, 3.7f, 2.9f, 3.4f, 4.1f, 2.6f };
Console.WriteLine($"Sample data: [{string.Join(", ", samples.Select(x => x.ToString("F1")))}]");

var (mean, variance) = VectorMath.MeanAndVariance(samples.AsSpan());
Console.WriteLine($"Mean: {mean:F3}");
Console.WriteLine($"Variance: {variance:F3}");
Console.WriteLine($"Standard deviation: {MathF.Sqrt(variance):F3}");

// Example 6: Vectorized algorithms
Console.WriteLine("\nVectorized algorithm demonstrations:");

var searchArray = new int[] { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };
var searchValue = 25;

Console.WriteLine($"Search array: [{string.Join(", ", searchArray)}]");
Console.WriteLine($"Searching for: {searchValue}");

var index = VectorizedAlgorithms.IndexOf(searchArray.AsSpan(), searchValue);
Console.WriteLine($"Found at index: {index}");

// Fill operation
var fillArray = new int[12];
VectorizedAlgorithms.Fill(fillArray.AsSpan(), 42);
Console.WriteLine($"Filled array: [{string.Join(", ", fillArray)}]");

// Copy with multiplier
var sourceFloat = new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f };
var destFloat = new float[sourceFloat.Length];
VectorizedAlgorithms.CopyWithMultiplier(sourceFloat.AsSpan(), destFloat.AsSpan(), 2.5f);
Console.WriteLine($"Source: [{string.Join(", ", sourceFloat)}]");
Console.WriteLine($"Multiplied by 2.5: [{string.Join(", ", destFloat.Select(x => x.ToString("F1")))}]");

// Example 7: Clamping values
Console.WriteLine("\nValue clamping:");

var clampData = new float[] { -2.5f, 0.5f, 3.5f, 8.5f, 12.0f, -1.0f, 7.5f, 15.5f };
Console.WriteLine($"Original: [{string.Join(", ", clampData.Select(x => x.ToString("F1")))}]");

VectorizedAlgorithms.Clamp(clampData.AsSpan(), 0.0f, 10.0f);
Console.WriteLine($"Clamped [0.0, 10.0]: [{string.Join(", ", clampData.Select(x => x.ToString("F1")))}]");

// Example 8: Performance benchmarking
Console.WriteLine("\nPerformance benchmarking:");

var benchmarkData = Enumerable.Range(1, 10000).ToArray();
Console.WriteLine($"Benchmarking with {benchmarkData.Length} elements:");

VectorizationBenchmarks.BenchmarkSum(benchmarkData, 100);

var leftVec = Enumerable.Range(1, 1000).Select(x => (float)x).ToArray();
var rightVec = Enumerable.Range(1, 1000).Select(x => (float)x * 0.5f).ToArray();

Console.WriteLine("\nDot product benchmark:");
VectorizationBenchmarks.BenchmarkDotProduct(leftVec, rightVec, 1000);

Console.WriteLine("\nArray operations benchmark:");
VectorizationBenchmarks.BenchmarkArrayOperations(100000, 50);

// Example 9: Hardware intrinsics (if available)
Console.WriteLine("\nHardware intrinsics operations:");

if (IntrinsicOperations.SupportsAVX2)
{
    var intArray = Enumerable.Range(-5, 20).ToArray();
    Console.WriteLine($"Test array: [{string.Join(", ", intArray)}]");
    
    var avx2Sum = IntrinsicOperations.SumAVX2(intArray.AsSpan());
    Console.WriteLine($"AVX2 sum: {avx2Sum}");
    
    var countGreater = IntrinsicOperations.CountGreaterThan(intArray.AsSpan(), 5);
    Console.WriteLine($"Count > 5: {countGreater}");
}
else
{
    Console.WriteLine("AVX2 not supported on this hardware");
}

// String comparison with vectorization
var string1 = "Hello, vectorized world!";
var string2 = "Hello, vectorized world!";
var string3 = "Hello, different world!";

Console.WriteLine($"String 1: '{string1}'");
Console.WriteLine($"String 2: '{string2}'");
Console.WriteLine($"String 3: '{string3}'");

var equals12 = IntrinsicOperations.StringEqualsVectorized(string1.AsSpan(), string2.AsSpan());
var equals13 = IntrinsicOperations.StringEqualsVectorized(string1.AsSpan(), string3.AsSpan());

Console.WriteLine($"String 1 == String 2: {equals12}");
Console.WriteLine($"String 1 == String 3: {equals13}");

// Example 10: Real-world applications
Console.WriteLine("\nReal-world vectorized applications:");

// Image brightness adjustment simulation
var imagePixels = new byte[100];
for (int i = 0; i < imagePixels.Length; i++)
{
    imagePixels[i] = (byte)(i * 2 % 256);
}

Console.WriteLine($"Original pixels (first 10): [{string.Join(", ", imagePixels.Take(10))}]");

VectorizedApplications.AdjustBrightness(imagePixels.AsSpan(), 1.2f);
Console.WriteLine($"Brightened pixels (first 10): [{string.Join(", ", imagePixels.Take(10))}]");

// Financial calculations
var prices = new float[] { 100f, 102f, 98f, 105f, 103f, 107f, 110f, 108f };
var returns = new float[prices.Length - 1];

Console.WriteLine($"Stock prices: [{string.Join(", ", prices.Select(p => p.ToString("F0")))}]");

VectorizedApplications.CalculateReturns(prices.AsSpan(), returns.AsSpan());
Console.WriteLine($"Returns: [{string.Join(", ", returns.Select(r => r.ToString("F3")))}]");

// Moving average
var signal = new float[] { 1f, 4f, 2f, 8f, 5f, 7f, 3f, 6f, 9f, 4f };
var movingAvg = new float[signal.Length - 2]; // Window size 3

Console.WriteLine($"Signal: [{string.Join(", ", signal)}]");

VectorizedApplications.CalculateMovingAverage(signal.AsSpan(), movingAvg.AsSpan(), 3);
Console.WriteLine($"Moving average (window=3): [{string.Join(", ", movingAvg.Select(x => x.ToString("F2")))}]");

// Cosine similarity
var vecA = new float[] { 1f, 2f, 3f, 4f };
var vecB = new float[] { 2f, 3f, 4f, 5f };
var vecC = new float[] { -1f, -2f, -3f, -4f };

Console.WriteLine($"Vector A: [{string.Join(", ", vecA)}]");
Console.WriteLine($"Vector B: [{string.Join(", ", vecB)}]");
Console.WriteLine($"Vector C: [{string.Join(", ", vecC)}]");

var simAB = VectorizedApplications.CosineSimilarity(vecA.AsSpan(), vecB.AsSpan());
var simAC = VectorizedApplications.CosineSimilarity(vecA.AsSpan(), vecC.AsSpan());

Console.WriteLine($"Cosine similarity A-B: {simAB:F3}");
Console.WriteLine($"Cosine similarity A-C: {simAC:F3}");

// Physics simulation
var velocitiesX = new float[] { 1.0f, 2.0f, -1.5f, 3.0f };
var velocitiesY = new float[] { 0.5f, -1.0f, 2.0f, -0.5f };
var accelerationsX = new float[] { 0.1f, -0.2f, 0.3f, -0.1f };
var accelerationsY = new float[] { -0.98f, -0.98f, -0.98f, -0.98f }; // Gravity

Console.WriteLine("Physics simulation - updating particle velocities:");
Console.WriteLine($"Initial vel X: [{string.Join(", ", velocitiesX.Select(v => v.ToString("F2")))}]");
Console.WriteLine($"Initial vel Y: [{string.Join(", ", velocitiesY.Select(v => v.ToString("F2")))}]");

VectorizedApplications.UpdateParticleVelocities(
    velocitiesX.AsSpan(), velocitiesY.AsSpan(), 
    accelerationsX.AsSpan(), accelerationsY.AsSpan(), 
    0.1f); // 0.1 second time step

Console.WriteLine($"Updated vel X: [{string.Join(", ", velocitiesX.Select(v => v.ToString("F2")))}]");
Console.WriteLine($"Updated vel Y: [{string.Join(", ", velocitiesY.Select(v => v.ToString("F2")))}]");

// Example 11: Matrix operations
Console.WriteLine("\nMatrix multiplication:");

// 2x3 matrix A
var matrixA = new float[] { 
    1f, 2f, 3f,  // Row 1
    4f, 5f, 6f   // Row 2
};

// 3x2 matrix B  
var matrixB = new float[] { 
    7f, 8f,   // Row 1
    9f, 10f,  // Row 2
    11f, 12f  // Row 3
};

// Result will be 2x2
var matrixResult = new float[4];

Console.WriteLine("Matrix A (2x3):");
Console.WriteLine($"  [{matrixA[0]}, {matrixA[1]}, {matrixA[2]}]");
Console.WriteLine($"  [{matrixA[3]}, {matrixA[4]}, {matrixA[5]}]");

Console.WriteLine("Matrix B (3x2):");
Console.WriteLine($"  [{matrixB[0]}, {matrixB[1]}]");
Console.WriteLine($"  [{matrixB[2]}, {matrixB[3]}]");
Console.WriteLine($"  [{matrixB[4]}, {matrixB[5]}]");

VectorMath.MatrixMultiply(matrixA.AsSpan(), matrixB.AsSpan(), matrixResult.AsSpan(), 2, 3, 2);

Console.WriteLine("Result A × B (2x2):");
Console.WriteLine($"  [{matrixResult[0]}, {matrixResult[1]}]");
Console.WriteLine($"  [{matrixResult[2]}, {matrixResult[3]}]");

// Example 12: Histogram calculation
Console.WriteLine("\nHistogram calculation:");

var histogramData = new int[] { 1, 2, 2, 3, 3, 3, 4, 4, 5, 1, 2, 3, 4, 5, 5, 5 };
var histogram = new int[5]; // For values 1-5

Console.WriteLine($"Data: [{string.Join(", ", histogramData)}]");

VectorizedAlgorithms.CalculateHistogram(histogramData.AsSpan(), histogram.AsSpan(), 1, 5);

Console.WriteLine("Histogram:");
for (int i = 0; i < histogram.Length; i++)
{
    Console.WriteLine($"  Value {i + 1}: {histogram[i]} occurrences");
}

Console.WriteLine("\nVectorization examples completed!");
```

**Notes**:

- Vector<T> provides cross-platform SIMD operations that work on different CPU architectures
- Hardware intrinsics (AVX2, SSE) offer maximum performance but are CPU-specific
- Vectorization is most beneficial for large arrays (typically > 100 elements)
- Always provide scalar fallbacks for small arrays or unsupported hardware
- Memory layout matters - contiguous data enables better vectorization
- Branch prediction and cache locality can impact vectorized performance
- Vector operations work best with aligned memory and power-of-2 sizes
- Floating-point operations typically vectorize better than integer operations
- Modern CPUs can process 4-8 floats or 8-16 integers simultaneously
- Profile actual performance gains as vectorization overhead can negate benefits for small datasets
- Consider using libraries like MathNet.Numerics for production mathematical operations
- GPU computing (CUDA, OpenCL) may be better for extremely parallel workloads

**Prerequisites**:

- .NET Core 3.0+ or .NET Framework 4.7.2+ for hardware intrinsics support
- Understanding of SIMD concepts and parallel processing
- Knowledge of CPU architecture and instruction sets (SSE, AVX)
- Familiarity with memory alignment and cache optimization
- Performance profiling tools to measure actual speedup gains
- Understanding of floating-point precision and numerical stability

**Related Snippets**:

- [Span Operations](span-operations.md) - Memory operations with Span<T>
- [Memory Pools](memory-pools.md) - Memory management for vectorized operations
- [Parallel Patterns](parallel-patterns.md) - CPU parallelization strategies  
- [Micro Optimizations](micro-optimizations.md) - Low-level performance techniques
