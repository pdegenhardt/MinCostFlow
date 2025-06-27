namespace MinCostFlow.Experiment.Test;

// SVector Tests with multiple type combinations
public class SVectorTypedTests
{
    public static IEnumerable<object[]> TypeCombinations =>
        new List<object[]>
        {
                new object[] { typeof(int), typeof(int) }
        };

    [Theory]
    [MemberData(nameof(TypeCombinations))]
    public void CopyAndIterate(Type indexType, Type valueType)
    {
        // Use dynamic to handle different type combinations
        dynamic CreateVector()
        {
            var vectorType = typeof(SVector<,>).MakeGenericType(indexType, valueType);
            return Activator.CreateInstance(vectorType);
        }

        dynamic v = CreateVector();
        v.Resize(2);
        v[0] = 1;
        v[1] = 2;

        // Test copy-like behavior
        dynamic v2 = CreateVector();
        v2.Resize(v.Size);
        for (int i = -v.Size; i < v.Size; i++)
        {
            v2[i] = v[i];
        }

        Assert.Equal(v[0], v2[0]);
        Assert.Equal(v[1], v2[1]);
    }

    [Theory]
    [MemberData(nameof(TypeCombinations))]
    public void DynamicGrowth(Type indexType, Type valueType)
    {
        dynamic v = Activator.CreateInstance(typeof(SVector<,>).MakeGenericType(indexType, valueType));
        Assert.Equal(0, v.Size);
        Assert.Equal(0, v.Capacity);

        for (int i = 0; i < 100; i++)
        {
            v.Grow(-i, i);
        }

        Assert.Equal(100, v.Size);
        Assert.True(v.Capacity >= 100);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(-i, (int)v[-i - 1]);
            Assert.Equal(i, (int)v[i]);
        }
    }

    [Theory]
    [MemberData(nameof(TypeCombinations))]
    public void Reserve(Type indexType, Type valueType)
    {
        dynamic v = Activator.CreateInstance(typeof(SVector<,>).MakeGenericType(indexType, valueType));
        v.Reserve(100);

        Assert.Equal(0, v.Size);
        Assert.True(v.Capacity >= 100);

        for (int i = 0; i < 100; i++)
        {
            v.Grow(-i, i);
        }

        Assert.Equal(100, v.Size);
        Assert.True(v.Capacity >= 100);

        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(-i, (int)v[-i - 1]);
            Assert.Equal(i, (int)v[i]);
        }
    }

    [Theory]
    [MemberData(nameof(TypeCombinations))]
    public void Resize(Type indexType, Type valueType)
    {
        dynamic v = Activator.CreateInstance(typeof(SVector<,>).MakeGenericType(indexType, valueType));
        v.Resize(100);

        Assert.Equal(100, v.Size);
        Assert.True(v.Capacity >= 100);

        // Check default values
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(0, (int)v[-i - 1]);
            Assert.Equal(0, (int)v[i]);
        }
    }

    [Theory]
    [MemberData(nameof(TypeCombinations))]
    public void ResizeToZero(Type indexType, Type valueType)
    {
        dynamic v = Activator.CreateInstance(typeof(SVector<,>).MakeGenericType(indexType, valueType));
        v.Resize(1);
        v.Resize(0);
        Assert.Equal(0, v.Size);
    }
}