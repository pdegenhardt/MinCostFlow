using System;
using MinCostFlow.Core.Gort;
using Xunit;

namespace MinCostFlow.Tests.Gort;

public class ZVectorTests
{
    [Fact]
    public void Constructor_ValidRange_CreatesVector()
    {
        using var vector = new ZVector<long>(-10, 10);
        Assert.Equal(-10, vector.MinIndex);
        Assert.Equal(10, vector.MaxIndex);
    }

    [Fact]
    public void Constructor_InvalidRange_ThrowsException()
    {
        Assert.Throws<ArgumentException>(() => new ZVector<long>(10, -10));
    }

    [Fact]
    public void ForArcs_CreatesCorrectRange()
    {
        using var vector = ZVector<long>.ForArcs(100);
        Assert.Equal(-100, vector.MinIndex);
        Assert.Equal(99, vector.MaxIndex);
    }

    [Fact]
    public void Indexer_PositiveIndex_WorksCorrectly()
    {
        using var vector = new ZVector<long>(-10, 10);
        
        vector[5] = 42;
        Assert.Equal(42, vector[5]);
        Assert.Equal(42, vector.Value(5));
    }

    [Fact]
    public void Indexer_NegativeIndex_WorksCorrectly()
    {
        using var vector = new ZVector<long>(-10, 10);
        
        vector[-7] = 123;
        Assert.Equal(123, vector[-7]);
        Assert.Equal(123, vector.Value(-7));
    }

    [Fact]
    public void Indexer_BoundaryIndices_WorkCorrectly()
    {
        using var vector = new ZVector<long>(-10, 10);
        
        vector[-10] = 1;
        vector[10] = 2;
        
        Assert.Equal(1, vector[-10]);
        Assert.Equal(2, vector[10]);
    }

#if DEBUG
    [Fact]
    public void Indexer_OutOfRange_ThrowsException()
    {
        using var vector = new ZVector<long>(-10, 10);
        
        Assert.Throws<IndexOutOfRangeException>(() => vector[-11]);
        Assert.Throws<IndexOutOfRangeException>(() => vector[11]);
        Assert.Throws<IndexOutOfRangeException>(() => vector[-11] = 1);
        Assert.Throws<IndexOutOfRangeException>(() => vector[11] = 1);
    }
#endif

    [Fact]
    public void SetAll_SetsAllValues()
    {
        using var vector = new ZVector<long>(-5, 5);
        vector.SetAll(77);
        
        for (long i = -5; i <= 5; i++)
        {
            Assert.Equal(77, vector[i]);
        }
    }

    [Fact]
    public void Clear_ResetsAllValues()
    {
        using var vector = new ZVector<long>(-5, 5);
        
        // Set some values
        for (long i = -5; i <= 5; i++)
        {
            vector[i] = i * 10;
        }
        
        // Clear
        vector.Clear();
        
        // Verify all are zero
        for (long i = -5; i <= 5; i++)
        {
            Assert.Equal(0, vector[i]);
        }
    }

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        using var original = new ZVector<long>(-3, 3);
        
        // Set values
        for (long i = -3; i <= 3; i++)
        {
            original[i] = i * 2;
        }
        
        // Clone
        using var clone = original.Clone();
        
        // Verify values match
        for (long i = -3; i <= 3; i++)
        {
            Assert.Equal(original[i], clone[i]);
        }
        
        // Modify clone
        clone[0] = 999;
        
        // Verify original unchanged
        Assert.NotEqual(original[0], clone[0]);
    }

    [Fact]
    public void CopyFrom_CopiesValues()
    {
        using var source = new ZVector<long>(-2, 2);
        using var target = new ZVector<long>(-2, 2);
        
        // Set source values
        for (long i = -2; i <= 2; i++)
        {
            source[i] = i * 3;
        }
        
        // Copy
        target.CopyFrom(source);
        
        // Verify
        for (long i = -2; i <= 2; i++)
        {
            Assert.Equal(source[i], target[i]);
        }
    }

    [Fact]
    public void CopyFrom_DifferentRanges_ThrowsException()
    {
        using var source = new ZVector<long>(-2, 2);
        using var target = new ZVector<long>(-3, 3);
        
        Assert.Throws<ArgumentException>(() => target.CopyFrom(source));
    }

    [Fact]
    public void AsSpan_ReturnsCorrectSpan()
    {
        using var vector = new ZVector<long>(-2, 2);
        
        // Set values
        vector[-2] = 10;
        vector[-1] = 20;
        vector[0] = 30;
        vector[1] = 40;
        vector[2] = 50;
        
        // Get span
        var span = vector.AsSpan();
        
        // Verify span has correct values in storage order
        Assert.Equal(5, span.Length);
        Assert.Equal(10, span[0]); // Index -2 in ZVector
        Assert.Equal(20, span[1]); // Index -1 in ZVector
        Assert.Equal(30, span[2]); // Index 0 in ZVector
        Assert.Equal(40, span[3]); // Index 1 in ZVector
        Assert.Equal(50, span[4]); // Index 2 in ZVector
    }

    [Fact]
    public void IntIndexer_WorksCorrectly()
    {
        using var vector = new ZVector<int>(-100, 100);
        
        vector[50] = 123;
        vector[-50] = 456;
        
        Assert.Equal(123, vector[50]);
        Assert.Equal(456, vector[-50]);
    }

    [Fact]
    public unsafe void GetPointer_ReturnsValidPointer()
    {
        using var vector = new ZVector<long>(-5, 5);
        
        vector[3] = 42;
        long* ptr = vector.GetPointer(3);
        
        Assert.Equal(42, *ptr);
        
        // Modify through pointer
        *ptr = 84;
        Assert.Equal(84, vector[3]);
    }

    [Fact]
    public void SetAll_ZeroValue_UsesArrayClear()
    {
        using var vector = new ZVector<long>(-1000, 1000);
        
        // Set non-zero values
        vector.SetAll(123);
        
        // Set to zero (should use Array.Clear optimization)
        vector.SetAll(0);
        
        // Verify
        Assert.Equal(0, vector[-1000]);
        Assert.Equal(0, vector[0]);
        Assert.Equal(0, vector[1000]);
    }
}