using System.Diagnostics;

namespace MinCostFlow.Experiment.Graphs;

// Base class of all Graphs implemented here
public abstract class BaseGraph(bool hasNegativeReverseArcs = false)
{
    public bool HasNegativeReverseArcs { get; protected set; } = hasNegativeReverseArcs;

    protected int NumNodesInternal = 0;
    protected int NodeCapacityInternal = 0;
    protected int NumArcsInternal = 0;
    protected int ArcCapacityInternal = 0;
    protected bool ConstCapacitiesInternal = false;

    // Constants for nil values
    public static readonly int NilNode = int.MaxValue;
    public static readonly int NilArc = int.MaxValue;

    public int NumNodes => NumNodesInternal;
    public int Size => NumNodesInternal; // Prefer NumNodes
    public int NumArcs => NumArcsInternal;

    // Range-based iteration support
    public IEnumerable<int> AllNodes()
    {
        for (int i = 0; i < NumNodesInternal; i++)
        {
            yield return i;
        }
    }

    public IEnumerable<int> AllForwardArcs()
    {
        for (int i = 0; i < NumArcsInternal; i++)
        {
            yield return i;
        }
    }

    public bool IsNodeValid(int node)
    {
        return node >= 0 && node < NumNodesInternal;
    }

    public bool IsArcValid(int arc)
    {
        int lowerBound = HasNegativeReverseArcs ? -NumArcsInternal : 0;
        return arc >= lowerBound && arc < NumArcsInternal;
    }

    public int NodeCapacity => NodeCapacityInternal > NumNodesInternal ? NodeCapacityInternal : NumNodesInternal;
    public int ArcCapacity => ArcCapacityInternal > NumArcsInternal ? ArcCapacityInternal : NumArcsInternal;

    public virtual void ReserveNodes(int bound)
    {
        Debug.Assert(!ConstCapacitiesInternal);
        Debug.Assert(bound >= NumNodesInternal);
        if (bound <= NumNodesInternal) return;
        NodeCapacityInternal = bound;
    }

    public virtual void ReserveArcs(int bound)
    {
        Debug.Assert(!ConstCapacitiesInternal);
        Debug.Assert(bound >= NumArcsInternal);
        if (bound <= NumArcsInternal) return;
        ArcCapacityInternal = bound;
    }

    public void Reserve(int nodeCapacity, int arcCapacity)
    {
        ReserveNodes(nodeCapacity);
        ReserveArcs(arcCapacity);
    }

    public void FreezeCapacities()
    {
        ConstCapacitiesInternal = true;
        NodeCapacityInternal = Math.Max(NodeCapacityInternal, NumNodesInternal);
        ArcCapacityInternal = Math.Max(ArcCapacityInternal, NumArcsInternal);
    }

    protected static int Increment(int value) => value + 1;
    protected static int Decrement(int value) => value - 1;
    protected static int Add(int a, int b) => a + b;
    protected static int Subtract(int a, int b) => a - b;
    protected static int Max(int a, int b) => Math.Max(a, b);
    protected static int Negate(int value) => -value;
    protected static int BitwiseNot(int value) => ~value;
    protected static int ToInt32(int value) => value;
    protected static long ToInt64(int value) => value;

    protected void ComputeCumulativeSum(List<int> v)
    {
        Debug.Assert(v.Count == NumNodesInternal + 1);
        int sum = 0;
        for (int i = 0; i < NumNodesInternal; i++)
        {
            int temp = v[i];
            v[i] = sum;
            sum += temp;
        }
        Debug.Assert(sum == NumArcsInternal);
        v[NumNodesInternal] = sum; // Sentinel
    }
}
