using MinCostFlow.Core.Gort;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MinCostFlow.Tests.Gort;

public class SVectorTests
{
    #region Basic Operations Tests

    [Fact]
    public void EmptyState_NewSVector_HasSizeZeroCapacityZero()
    {
        var sv = new SVector<int>();
        
        Assert.Equal(0, sv.Size);
        Assert.Equal(0, sv.Capacity);
    }

    [Fact]
    public void Growth_FromEmptyState_IncreasesCapacityAppropriately()
    {
        var sv = new SVector<int>();
        
        sv.Grow(1, 2);
        
        Assert.Equal(1, sv.Size);
        Assert.True(sv.Capacity >= 1);
        Assert.Equal(1, sv[-1]);
        Assert.Equal(2, sv[0]);
    }

    [Fact]
    public void Growth_MultipleGrows_MaintainsElements()
    {
        var sv = new SVector<int>();
        
        sv.Grow(1, 2);
        sv.Grow(3, 4);
        sv.Grow(5, 6);
        
        Assert.Equal(3, sv.Size);
        Assert.Equal(5, sv[-3]);
        Assert.Equal(3, sv[-2]);
        Assert.Equal(1, sv[-1]);
        Assert.Equal(2, sv[0]);
        Assert.Equal(4, sv[1]);
        Assert.Equal(6, sv[2]);
    }

    [Fact]
    public void Indexing_PositiveIndices_WorkCorrectly()
    {
        var sv = new SVector<int>();
        sv.Grow(10, 20);
        sv.Grow(30, 40);
        
        Assert.Equal(20, sv[0]);
        Assert.Equal(40, sv[1]);
    }

    [Fact]
    public void Indexing_NegativeIndices_WorkCorrectly()
    {
        var sv = new SVector<int>();
        sv.Grow(10, 20);
        sv.Grow(30, 40);
        
        Assert.Equal(30, sv[-2]);
        Assert.Equal(10, sv[-1]);
    }

    [Fact]
    public void Indexing_SetValues_UpdatesCorrectly()
    {
        var sv = new SVector<int>();
        sv.Grow(0, 0);
        sv.Grow(0, 0);
        
        sv[-2] = 100;
        sv[-1] = 200;
        sv[0] = 300;
        sv[1] = 400;
        
        Assert.Equal(100, sv[-2]);
        Assert.Equal(200, sv[-1]);
        Assert.Equal(300, sv[0]);
        Assert.Equal(400, sv[1]);
    }

    #endregion

    #region Copy Semantics Tests

    [Fact]
    public void CopyConstructor_CreatesIndependentCopy()
    {
        var original = new SVector<int>();
        original.Grow(1, 2);
        original.Grow(3, 4);
        
        var copy = new SVector<int>(original);
        
        Assert.Equal(original.Size, copy.Size);
        Assert.Equal(original.Capacity, copy.Capacity);
        
        // Verify elements are copied
        for (int i = -original.Size; i < original.Size; i++)
        {
            Assert.Equal(original[i], copy[i]);
        }
        
        // Verify independence
        original[0] = 999;
        Assert.NotEqual(original[0], copy[0]);
    }

    [Fact]
    public void CopyConstructor_EmptyVector_CopiesCorrectly()
    {
        var original = new SVector<int>();
        var copy = new SVector<int>(original);
        
        Assert.Equal(0, copy.Size);
        Assert.Equal(0, copy.Capacity);
    }

    #endregion

    #region Resize Operations Tests

    [Fact]
    public void Resize_GrowSize_NewElementsDefaultInitialized()
    {
        var sv = new SVector<int>();
        sv.Grow(1, 2);
        
        sv.Resize(3);
        
        Assert.Equal(3, sv.Size);
        Assert.Equal(0, sv[-3]); // New negative element
        Assert.Equal(0, sv[-2]); // New negative element
        Assert.Equal(1, sv[-1]); // Existing
        Assert.Equal(2, sv[0]);  // Existing
        Assert.Equal(0, sv[1]);  // New positive element
        Assert.Equal(0, sv[2]);  // New positive element
    }

    [Fact]
    public void Resize_ShrinkSize_ExcessElementsDestroyed()
    {
        var sv = new SVector<int>();
        for (int i = 0; i < 5; i++)
        {
            sv.Grow(i * 10, i * 10 + 5);
        }
        
        sv.Resize(2);
        
        Assert.Equal(2, sv.Size);
        Assert.Equal(10, sv[-2]);
        Assert.Equal(0, sv[-1]);
        Assert.Equal(5, sv[0]);
        Assert.Equal(15, sv[1]);
    }

    [Fact]
    public void Resize_ToZero_ResultsInEmptyVector()
    {
        var sv = new SVector<int>();
        sv.Grow(1, 2);
        sv.Grow(3, 4);
        
        sv.Resize(0);
        
        Assert.Equal(0, sv.Size);
        
        // Can resize back up
        sv.Resize(1);
        Assert.Equal(1, sv.Size);
        Assert.Equal(0, sv[-1]);
        Assert.Equal(0, sv[0]);
    }

    #endregion

    #region Memory Management Tests

    [Fact]
    public void Reserve_IncreasesCapacityWithoutChangingSize()
    {
        var sv = new SVector<int>();
        sv.Grow(1, 2);
        
        int originalSize = sv.Size;
        sv.Reserve(10);
        
        Assert.Equal(originalSize, sv.Size);
        Assert.True(sv.Capacity >= 10);
        Assert.Equal(1, sv[-1]);
        Assert.Equal(2, sv[0]);
    }

    [Fact]
    public void Reserve_RequestedLessThanCurrent_NoOp()
    {
        var sv = new SVector<int>();
        sv.Reserve(10);
        int capacity = sv.Capacity;
        
        sv.Reserve(5);
        
        Assert.Equal(capacity, sv.Capacity);
    }

    [Fact]
    public void GrowthPattern_FollowsExpectedFactor()
    {
        var sv = new SVector<int>();
        var capacities = new List<int>();
        
        for (int i = 0; i < 20; i++)
        {
            sv.Grow(i, i);
            if (sv.Capacity != (capacities.Count > 0 ? capacities.Last() : 0))
            {
                capacities.Add(sv.Capacity);
            }
        }
        
        // Verify growth factor is approximately 1.3x
        for (int i = 1; i < capacities.Count; i++)
        {
            double factor = (double)capacities[i] / capacities[i - 1];
            Assert.True(factor >= 1.2 && factor <= 1.5, 
                $"Growth factor {factor} outside expected range");
        }
    }

    [Fact]
    public void ClearAndDealloc_ReleasesAllMemory()
    {
        var sv = new SVector<int>();
        for (int i = 0; i < 10; i++)
        {
            sv.Grow(i, i);
        }
        
        sv.ClearAndDealloc();
        
        Assert.Equal(0, sv.Size);
        Assert.Equal(0, sv.Capacity);
        
        // Object still usable
        sv.Grow(100, 200);
        Assert.Equal(1, sv.Size);
        Assert.Equal(100, sv[-1]);
        Assert.Equal(200, sv[0]);
    }

    [Fact]
    public void Clear_RemovesElementsKeepsCapacity()
    {
        var sv = new SVector<int>();
        for (int i = 0; i < 5; i++)
        {
            sv.Grow(i, i);
        }
        int originalCapacity = sv.Capacity;
        
        sv.Clear();
        
        Assert.Equal(0, sv.Size);
        Assert.Equal(originalCapacity, sv.Capacity);
    }

    #endregion

    #region Complex Types Tests

    [Fact]
    public void StringElements_ProperConstructionDestruction()
    {
        var sv = new SVector<string>();
        
        sv.Grow("left1", "right1");
        sv.Grow("left2", "right2");
        
        Assert.Equal("left2", sv[-2]);
        Assert.Equal("left1", sv[-1]);
        Assert.Equal("right1", sv[0]);
        Assert.Equal("right2", sv[1]);
        
        sv.Resize(1);
        Assert.Equal("left1", sv[-1]);
        Assert.Equal("right1", sv[0]);
    }

    [Fact]
    public void ComplexType_TrackConstructorDestructor()
    {
        var sv = new SVector<TrackedObject>();
        TrackedObject.ResetCounters();
        
        sv.Grow(new TrackedObject(1), new TrackedObject(2));
        sv.Grow(new TrackedObject(3), new TrackedObject(4));
        
        Assert.Equal(4, TrackedObject.ConstructorCount);
        
        sv.Resize(1);
        // GC will handle destruction in C#
        
        sv.ClearAndDealloc();
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void SelfReferentialGrow_WorksCorrectly()
    {
        var sv = new SVector<int>();
        sv.Grow(10, 20);
        sv.Grow(30, 40);
        
        // Store original values
        int val1 = sv[-1];
        int val2 = sv[0];
        
        // Grow with self-reference
        sv.Grow(sv[-1], sv[0]);
        
        Assert.Equal(3, sv.Size);
        Assert.Equal(val1, sv[-3]);
        Assert.Equal(val2, sv[2]);
    }

    [Fact]
    public void SelfReferentialGrow_WithReallocation_WorksCorrectly()
    {
        var sv = new SVector<int>();
        
        // Fill to capacity
        while (sv.Size < sv.Capacity || sv.Capacity == 0)
        {
            sv.Grow(sv.Size * 10, sv.Size * 10 + 1);
        }
        
        int lastNeg = sv[-1];
        int lastPos = sv[sv.Size - 1];
        
        // This should trigger reallocation
        sv.Grow(sv[-1], sv[sv.Size - 1]);
        
        Assert.Equal(lastNeg, sv[-sv.Size]);
        Assert.Equal(lastPos, sv[sv.Size - 1]);
    }

    [Fact]
    public void Swap_ExchangesContents()
    {
        var sv1 = new SVector<int>();
        sv1.Grow(1, 2);
        sv1.Grow(3, 4);
        
        var sv2 = new SVector<int>();
        sv2.Grow(10, 20);
        
        int sv1Size = sv1.Size;
        int sv2Size = sv2.Size;
        
        sv1.Swap(sv2);
        
        Assert.Equal(sv2Size, sv1.Size);
        Assert.Equal(sv1Size, sv2.Size);
        
        Assert.Equal(10, sv1[-1]);
        Assert.Equal(20, sv1[0]);
        
        Assert.Equal(3, sv2[-2]);
        Assert.Equal(1, sv2[-1]);
        Assert.Equal(2, sv2[0]);
        Assert.Equal(4, sv2[1]);
    }

    #endregion

    #region Iterator Tests

    [Fact]
    public void DefaultIteration_FromNegativeToPositive()
    {
        var sv = new SVector<int>();
        sv.Grow(1, 2);
        sv.Grow(3, 4);
        
        var values = sv.ToList();
        
        Assert.Equal(4, values.Count);
        Assert.Equal(3, values[0]); // sv[-2]
        Assert.Equal(1, values[1]); // sv[-1]
        Assert.Equal(2, values[2]); // sv[0]
        Assert.Equal(4, values[3]); // sv[1]
    }

    [Fact]
    public void PositiveRange_IteratesPositiveOnly()
    {
        var sv = new SVector<int>();
        sv.Grow(1, 2);
        sv.Grow(3, 4);
        
        var values = sv.PositiveRange().ToList();
        
        Assert.Equal(2, values.Count);
        Assert.Equal(2, values[0]); // sv[0]
        Assert.Equal(4, values[1]); // sv[1]
    }

    [Fact]
    public void NegativeRange_IteratesNegativeOnly()
    {
        var sv = new SVector<int>();
        sv.Grow(1, 2);
        sv.Grow(3, 4);
        
        var values = sv.NegativeRange().ToList();
        
        Assert.Equal(2, values.Count);
        Assert.Equal(3, values[0]); // sv[-2]
        Assert.Equal(1, values[1]); // sv[-1]
    }

    #endregion

    #region Performance and Stress Tests

    [Fact]
    public void LargeSize_HandlesCorrectly()
    {
        var sv = new SVector<int>();
        const int size = 10000;
        
        for (int i = 0; i < size; i++)
        {
            sv.Grow(i * 2, i * 2 + 1);
        }
        
        Assert.Equal(size, sv.Size);
        Assert.Equal((size - 1) * 2, sv[-size]);  // Last added left value
        Assert.Equal((size - 1) * 2 + 1, sv[size - 1]);  // Last added right value
    }

    [Fact]
    public void RepeatedGrowShrinkCycles_MaintainsIntegrity()
    {
        var sv = new SVector<int>();
        
        for (int cycle = 0; cycle < 5; cycle++)
        {
            // Grow
            for (int i = 0; i < 100; i++)
            {
                sv.Grow(i, i);
            }
            
            // Verify
            Assert.Equal(100, sv.Size);
            
            // Shrink
            sv.Resize(10);
            Assert.Equal(10, sv.Size);
            
            // Clear
            sv.Clear();
            Assert.Equal(0, sv.Size);
        }
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_EmptyVector_ShowsEmpty()
    {
        var sv = new SVector<int>();
        string str = sv.ToString();
        
        Assert.Contains("SVector<Int32>: []", str);
    }

    [Fact]
    public void ToString_WithElements_ShowsIndexedValues()
    {
        var sv = new SVector<int>();
        sv.Grow(10, 20);
        sv.Grow(30, 40);
        
        string str = sv.ToString();
        
        Assert.Contains("-2:30", str);
        Assert.Contains("-1:10", str);
        Assert.Contains("0:20", str);
        Assert.Contains("1:40", str);
    }

    #endregion

    #region Helper Classes

    private class TrackedObject
    {
        public int Value { get; }
        public static int ConstructorCount { get; private set; }
        public static int DestructorCount { get; private set; }

        public TrackedObject(int value)
        {
            Value = value;
            ConstructorCount++;
        }

        ~TrackedObject()
        {
            DestructorCount++;
        }

        public static void ResetCounters()
        {
            ConstructorCount = 0;
            DestructorCount = 0;
        }
    }

    #endregion
}