using System.Diagnostics;
using System.Numerics;
using System.Xml.Linq;

namespace MinCostFlow.Experiment;

/// <summary>
/// Generic MaxFlow implementation using push-relabel algorithm.
/// Works on graphs with the notion of "reverse" arcs.
/// </summary>
public class GenericMaxFlow<TGraph, TArcFlowType, TFlowSumType>
    where TGraph : IMaxFlowGraph
    where TArcFlowType : struct, INumber<TArcFlowType>
    where TFlowSumType : struct, INumber<TFlowSumType>
{
    private readonly TFlowSumType[] _nodeExcess;
    private readonly int[] _nodePotential;
    private readonly TArcFlowType[] _residualArcCapacity;
    private readonly TArcFlowType[] _initialCapacity;
    private readonly int[] _firstAdmissibleArc;

    private readonly PriorityQueueWithRestrictedPush<int, int> _activeNodeByHeight = new();

    private readonly bool[] _nodeInBfsQueue;
    private readonly List<int> _bfsQueue = [];

    // Maximum manageable flow
    private readonly TFlowSumType _maxFlowSum;

    public GenericMaxFlow(TGraph graph, int source, int sink)
    {
        Graph = graph ?? throw new ArgumentNullException(nameof(graph));
        SourceNodeIndex = source;
        SinkNodeIndex = sink;

        // Initialize maxFlowSum based on the numeric type
        if (typeof(TFlowSumType) == typeof(long))
        {
            _maxFlowSum = (TFlowSumType)(object)long.MaxValue;
        }
        else if (typeof(TFlowSumType) == typeof(int))
        {
            _maxFlowSum = (TFlowSumType)(object)int.MaxValue;
        }
        else
        {
            throw new NotSupportedException($"Flow sum type {typeof(TFlowSumType)} is not supported");
        }

        if (!graph.IsNodeValid(source))
        {
            throw new ArgumentException("Source node is not valid.", nameof(source));
        }

        if (!graph.IsNodeValid(sink))
        {
            throw new ArgumentException("Sink node is not valid.", nameof(sink));
        }

        int maxNumNodes = graph.NodeCapacity;
        if (maxNumNodes > 0)
        {
            _nodeInBfsQueue = new bool[maxNumNodes];
            _nodeExcess = new TFlowSumType[maxNumNodes];           
            _nodePotential = new int[maxNumNodes];
            _firstAdmissibleArc = new int[maxNumNodes];
            _bfsQueue.Capacity = maxNumNodes;
        }

        int maxNumArcs = graph.ArcCapacity;
        if (maxNumArcs > 0)
        {
            if (graph.HasNegativeReverseArcs)
            {
                // For graphs with negative reverse arcs, we need to handle negative indices
                // This would require a custom indexing solution in C#
                _residualArcCapacity = new TArcFlowType[maxNumArcs * 2];
            }
            else
            {
                _initialCapacity = new TArcFlowType[maxNumArcs];
                _residualArcCapacity = new TArcFlowType[maxNumArcs];
            }
        }
    }

    public TGraph Graph { get; }
    public MaxFlowStatus Status { get; private set; } = MaxFlowStatus.NotSolved;
    public int SourceNodeIndex { get; }
    public int SinkNodeIndex { get; }

    public void SetArcCapacity(int arc, TArcFlowType newCapacity)
    {
        if (Comparer<TArcFlowType>.Default.Compare(newCapacity, default) < 0)
        {
            throw new ArgumentException("Arc capacity must be non-negative.", nameof(newCapacity));
        }

        if (!IsArcDirect(arc))
        {
            throw new ArgumentException("Arc must be direct.", nameof(arc));
        }

        // Skip self-loops
        if (Head(arc).Equals(Tail(arc)))
        {
            return;
        }

        Status = MaxFlowStatus.NotSolved;
        int arcIndex = arc;
        _residualArcCapacity[arcIndex] = newCapacity;

        if (!Graph.HasNegativeReverseArcs)
        {
            _initialCapacity[arcIndex] = newCapacity;
        }
        else
        {
            int oppositeIndex = Opposite(arc);
            _residualArcCapacity[oppositeIndex] = default;
        }
    }

    public bool Solve()
    {
        Status = MaxFlowStatus.NotSolved;
        InitializePreflow();

        // Handle case when source or sink is not inside graph
        var numNodes = Graph.NumNodes;
        if (SinkNodeIndex >= numNodes || SourceNodeIndex >= numNodes)
        {
            Status = MaxFlowStatus.Optimal;
            return true;
        }

        RefineWithGlobalUpdate();

        Status = MaxFlowStatus.Optimal;
        if (!CheckResult())
        {
            throw new InvalidOperationException("MaxFlow result check failed.");
        }

        if (GetOptimalFlow().Equals(_maxFlowSum) && AugmentingPathExists())
        {
            Status = MaxFlowStatus.IntOverflow;
        }

        return true;
    }

    public TFlowSumType GetOptimalFlow()
    {
        return _nodeExcess[SinkNodeIndex];
    }

    public TFlowSumType Flow(int arc)
    {
        if (Graph.HasNegativeReverseArcs)
        {
            if (IsArcDirect(arc))
            {
                return ConvertToFlowSum(_residualArcCapacity[Opposite(arc)]);
            }
            return -(ConvertToFlowSum(_residualArcCapacity[arc]));
        }

        int arcIndex = arc;
        return ConvertToFlowSum(_initialCapacity[arcIndex]) - ConvertToFlowSum(_residualArcCapacity[arcIndex]);
    }

    public TArcFlowType Capacity(int arc)
    {
        if (Graph.HasNegativeReverseArcs)
        {
            if (!IsArcDirect(arc))
            {
                return default;
            }

            return _residualArcCapacity[arc] + _residualArcCapacity[Opposite(arc)];
        }
        return _initialCapacity[arc];
    }

    public void GetSourceSideMinCut(List<int> result)
    {
        ComputeReachableNodes(SourceNodeIndex, result, false);
    }

    public void GetSinkSideMinCut(List<int> result)
    {
        ComputeReachableNodes(SinkNodeIndex, result, true);
    }

    private void InitializePreflow()
    {
        int numNodes = Graph.NumNodes;
        int maxNumNodes = Graph.NodeCapacity;

        // Clear node excess
        Array.Clear(_nodeExcess, 0, maxNumNodes);

        // Restart from clear state with no flow
        int numArcs = Graph.NumArcs;
        if (Graph.HasNegativeReverseArcs)
        {
            for (int arc = 0; arc < numArcs; arc++)
            {
                var opposite = Opposite(arc);

                _residualArcCapacity[arc] = _residualArcCapacity[arc] + _residualArcCapacity[opposite];
                _residualArcCapacity[opposite] = default;
            }
        }
        else
        {
            Array.Copy(_initialCapacity, _residualArcCapacity, numArcs);
        }

        // Initialize node potentials
        Array.Clear(_nodePotential, 0, maxNumNodes);
        _nodePotential[SourceNodeIndex] = numNodes;

        // Initialize first admissible arcs
        for (int node = 0; node < numNodes; node++)
        {
            _firstAdmissibleArc[node] = Graph.NilArc;

            foreach (var arc in Graph.OutgoingOrOppositeIncomingArcs(node))
            {
                _firstAdmissibleArc[node] = arc;
                break;
            }
        }
    }

    private void RefineWithGlobalUpdate()
    {
        int numNodes = Graph.NumNodes;
        var skipActiveNode = new int[numNodes];

        while (SaturateOutgoingArcsFromSource())
        {
            int numSkipped;
            do
            {
                numSkipped = 0;
                Array.Clear(skipActiveNode, 0, numNodes);
                skipActiveNode[SinkNodeIndex] = 2;
                skipActiveNode[SourceNodeIndex] = 2;
                GlobalUpdate();

                while (!_activeNodeByHeight.IsEmpty)
                {
                    var node = _activeNodeByHeight.Pop();

                    if (skipActiveNode[node] > 1)
                    {
                        if (!node.Equals(SinkNodeIndex) && !node.Equals(SourceNodeIndex))
                        {
                            numSkipped++;
                        }
                        continue;
                    }

                    var oldHeight = _nodePotential[node];
                    Discharge(node);

                    if (_nodePotential[node] > oldHeight + 1)
                    {
                        skipActiveNode[node]++;
                    }
                }
            } while (numSkipped > 0);

            PushFlowExcessBackToSource();
        }
    }

    private void GlobalUpdate()
    {
        _bfsQueue.Clear();
        int queueIndex = 0;
        int numNodes = Graph.NumNodes;
        Array.Fill(_nodeInBfsQueue, false);

        _nodeInBfsQueue[SinkNodeIndex] = true;
        _nodeInBfsQueue[SourceNodeIndex] = true;
        _bfsQueue.Add(SinkNodeIndex);

        while (queueIndex != _bfsQueue.Count)
        {
            var node = _bfsQueue[queueIndex];
            queueIndex++;

            int candidateDistance = _nodePotential[node] + 1;

            foreach (var arc in Graph.OutgoingOrOppositeIncomingArcs(node))
            {
                var head = Head(arc);

                if (_nodeInBfsQueue[head])
                {
                    continue;
                }

                var oppositeArc = Opposite(arc);
                if (TArcFlowType.IsPositive(_residualArcCapacity[oppositeArc]))
                {
                    if (TFlowSumType.IsPositive(_nodeExcess[head]))
                    {
                        PushAsMuchFlowAsPossible(head, oppositeArc);
                        if (TArcFlowType.IsZero(_residualArcCapacity[oppositeArc]))
                        {
                            continue;
                        }
                    }

                    _nodePotential[head] = candidateDistance;
                    _nodeInBfsQueue[head] = true;
                    _bfsQueue.Add(head);
                }
            }
        }

        // Set unreachable nodes to high potential
        for (int i = 0; i < numNodes; i++)
        {
            if (!_nodeInBfsQueue[i])
            {
                _nodePotential[i] = 2 * numNodes - 1;
            }
        }

        // Reset active nodes
        if (!_activeNodeByHeight.IsEmpty)
        {
            throw new InvalidOperationException("Active node queue should be empty at start of GlobalUpdate.");
        }

        for (int i = 1; i < _bfsQueue.Count; i++)
        {
            var node = _bfsQueue[i];
            if (TFlowSumType.IsPositive(_nodeExcess[node]))
            {
                if (!IsActive(node))
                {
                    throw new InvalidOperationException($"Node {node} should be active.");
                }

                _activeNodeByHeight.Push(node, _nodePotential[node]);
            }
        }
    }

    private bool SaturateOutgoingArcsFromSource()
    {
        int numNodes = Graph.NumNodes;

        if (_nodeExcess[SinkNodeIndex].Equals(_maxFlowSum))
        {
            return false;
        }

        if (_nodeExcess[SourceNodeIndex].Equals(-_maxFlowSum))
        {
            return false;
        }

        bool flowPushed = false;
        foreach (var arc in Graph.OutgoingArcs(SourceNodeIndex))
        {
            var flow = _residualArcCapacity[arc];

            if (TArcFlowType.IsZero(flow) || _nodePotential[Head(arc)] >= numNodes)
            {
                continue;
            }

            var currentFlowOutOfSource = -_nodeExcess[SourceNodeIndex];
            if (!IsPositiveOrZero(flow))
            {
                throw new InvalidOperationException("Flow should be positive or zero.");
            }

            if (!IsPositiveOrZero(currentFlowOutOfSource))
            {
                throw new InvalidOperationException("Current flow out of source should be positive or zero.");
            }

            var cappedFlow = _maxFlowSum - currentFlowOutOfSource;
            if (cappedFlow < ConvertToFlowSum(flow))
            {
                if (TFlowSumType.IsZero(cappedFlow))
                {
                    return true;
                }

                PushFlow(ConvertToArcFlow(cappedFlow), SourceNodeIndex, arc);
                return true;
            }

            PushFlow(flow, SourceNodeIndex, arc);
            flowPushed = true;
        }

        if (!IsNegativeOrZero(_nodeExcess[SourceNodeIndex]))
        {
            throw new InvalidOperationException("Source node excess should be negative or zero after saturation.");
        }

        return flowPushed;
    }

    private void Discharge(int node)
    {
        int numNodes = Graph.NumNodes;

        if (!IsActive(node))
        {
            throw new InvalidOperationException($"Node {node} should be active at start of Discharge.");
        }

        while (true)
        {
            if (!IsActive(node))
            {
                throw new InvalidOperationException($"Node {node} should be active inside Discharge.");
            }

            foreach (var arc in Graph.OutgoingOrOppositeIncomingArcsStartingFrom(node, _firstAdmissibleArc[node]))
            {
                if (IsAdmissible(node, arc))
                {
                    if (!IsActive(node))
                    {
                        throw new InvalidOperationException($"Node {node} should be active inside Discharge.");
                    }

                    var head = Head(arc);

                    if (TFlowSumType.IsZero(_nodeExcess[head]))
                    {
                        _activeNodeByHeight.Push(head, _nodePotential[head]);
                    }

                    PushAsMuchFlowAsPossible(node, arc);
                    if (TFlowSumType.IsZero(_nodeExcess[node]))
                    {
                        _firstAdmissibleArc[node] = arc;
                        return;
                    }
                }
            }

            Relabel(node);

            if (_nodePotential[node] >= numNodes)
            {
                break;
            }
        }
    }

    private void Relabel(int node)
    {
        int minHeight = int.MaxValue;
        int firstAdmissibleArcFound = Graph.NilArc;

        foreach (var arc in Graph.OutgoingOrOppositeIncomingArcs(node))
        {
            if (TArcFlowType.IsPositive(_residualArcCapacity[arc]))
            {
                int headHeight = _nodePotential[Head(arc)];
                if (headHeight < minHeight)
                {
                    minHeight = headHeight;
                    firstAdmissibleArcFound = arc;

                    if (minHeight + 1 == _nodePotential[node])
                    {
                        break;
                    }
                }
            }
        }

        if (firstAdmissibleArcFound.Equals(Graph.NilArc))
        {
            throw new InvalidOperationException($"No admissible arc found for node {node} during relabel.");
        }

        _nodePotential[node] = minHeight + 1;
        _firstAdmissibleArc[node] = firstAdmissibleArcFound;
    }

    private void PushFlow(TArcFlowType flow, int tail, int arc)
    {
        if (TArcFlowType.IsZero(flow))
        {
            throw new ArgumentException("Flow to push must be non-zero.", nameof(flow));
        }

        int oppIdx = Opposite(arc);

        _residualArcCapacity[arc] = _residualArcCapacity[arc]- flow;
        _residualArcCapacity[oppIdx] = _residualArcCapacity[oppIdx]+ flow;

        if (!IsPositiveOrZero(_residualArcCapacity[arc]))
        {
            throw new InvalidOperationException("Residual arc capacity must be positive or zero after push.");
        }

        if (!IsPositiveOrZero(_residualArcCapacity[oppIdx]))
        {
            throw new InvalidOperationException("Opposite residual arc capacity must be positive or zero after push.");
        }

        int headIdx = Head(arc);

        _nodeExcess[tail] -= ConvertToFlowSum(flow);
        _nodeExcess[headIdx] += ConvertToFlowSum(flow);
    }

    private void PushAsMuchFlowAsPossible(int tail, int arc)
    {
        var flow = TArcFlowType.Min(_residualArcCapacity[arc],
                      ConvertToArcFlow(_nodeExcess[tail]));

        PushFlow(flow, tail, arc);
    }

    private void PushFlowExcessBackToSource()
    {
        int numNodes = Graph.NumNodes;

        var stored = new bool[numNodes];
        stored[SinkNodeIndex] = true;

        var visited = new bool[numNodes];
        visited[SinkNodeIndex] = true;

        var arcStack = new List<int>();
        var indexBranch = new List<int>();
        var reverseTopologicalOrder = new List<int>();

        // Start DFS from source
        foreach (var arc in Graph.OutgoingArcs(SourceNodeIndex))
        {
            if (TFlowSumType.IsPositive(Flow(arc)))
            {
                arcStack.Add(arc);
            }
        }
        visited[SourceNodeIndex] = true;

        // DFS on subgraph with positive flow
        while (arcStack.Count > 0)
        {
            var node = Head(arcStack[^1]);

            if (visited[node])
            {
                if (!stored[node])
                {
                    stored[node] = true;
                    reverseTopologicalOrder.Add(node);
                    if (indexBranch.Count <= 0)
                    {
                        throw new InvalidOperationException("indexBranch should not be empty when storing node.");
                    }

                    indexBranch.RemoveAt(indexBranch.Count - 1);
                }
                arcStack.RemoveAt(arcStack.Count - 1);
                continue;
            }

            if (stored[node])
            {
                throw new InvalidOperationException($"Node {node} should not be stored before visiting.");
            }

            visited[node] = true;
            indexBranch.Add(arcStack.Count - 1);

            foreach (var arc in Graph.OutgoingArcs(node))
            {
                var flow = Flow(arc);
                var head = Head(arc);

                if (TFlowSumType.IsPositive(flow) && !stored[head])
                {
                    if (!visited[head])
                    {
                        arcStack.Add(arc);
                    }
                    else
                    {
                        // Handle cycle
                        int cycleBegin = indexBranch.Count;
                        while (cycleBegin > 0 &&
                               !Head(arcStack[indexBranch[cycleBegin - 1]]).Equals(head))
                        {
                            cycleBegin--;
                        }

                        var flowOnCycle = ConvertToArcFlow(flow);
                        int firstSaturatedIndex = indexBranch.Count;

                        for (int i = indexBranch.Count - 1; i >= cycleBegin; i--)
                        {
                            var arcOnCycle = arcStack[indexBranch[i]];
                            var arcFlow = ConvertToArcFlow(Flow(arcOnCycle));
                            if (arcFlow <= flowOnCycle)
                            {
                                flowOnCycle = arcFlow;
                                firstSaturatedIndex = i;
                            }
                        }

                        var excess = _nodeExcess[head];

                        // Cancel flow on cycle
                        PushFlow(-flowOnCycle, node, arc);
                        for (int i = indexBranch.Count - 1; i >= cycleBegin; i--)
                        {
                            var arcOnCycle = arcStack[indexBranch[i]];
                            PushFlow(-flowOnCycle, Tail(arcOnCycle), arcOnCycle);
                            if (i >= firstSaturatedIndex)
                            {
                                if (!visited[Head(arcOnCycle)])
                                {
                                    throw new InvalidOperationException("Head of arc in cycle should be visited.");
                                }

                                visited[Head(arcOnCycle)] = false;
                            }
                            else
                            {
                                if (!TFlowSumType.IsPositive(Flow(arcOnCycle)))
                                {
                                    throw new InvalidOperationException("Flow on arc in cycle should be positive.");
                                }
                            }
                        }

                        if (!excess.Equals(_nodeExcess[head]))
                        {
                            throw new InvalidOperationException("Excess should be unchanged after canceling cycle.");
                        }

                        // Backtrack
                        if (firstSaturatedIndex < indexBranch.Count)
                        {
                            arcStack.RemoveRange(indexBranch[firstSaturatedIndex],
                                               arcStack.Count - indexBranch[firstSaturatedIndex]);
                            indexBranch.RemoveRange(firstSaturatedIndex,
                                                  indexBranch.Count - firstSaturatedIndex);
                            break;
                        }
                    }
                }
            }
        }

        // Return flow to source
        foreach (var node in reverseTopologicalOrder)
        {
            if (TFlowSumType.IsZero(_nodeExcess[node]))
            {
                continue;
            }

            foreach (var arc in Graph.OutgoingOrOppositeIncomingArcs(node))
            {
                var flow = Flow(arc);
                if (TFlowSumType.IsNegative(flow))
                {
                    if (!TArcFlowType.IsPositive(_residualArcCapacity[arc]))
                    {
                        throw new InvalidOperationException("Residual arc capacity should be positive for negative flow.");
                    }

                    var toPush = TArcFlowType.Min(ConvertToArcFlow(_nodeExcess[node]),
                                    ConvertToArcFlow(-flow));
                    PushFlow(toPush, node, arc);
                    if (TFlowSumType.IsZero(_nodeExcess[node]))
                    {
                        break;
                    }
                }
            }
            if (!TFlowSumType.IsZero(_nodeExcess[node]))
            {
                throw new InvalidOperationException("Node excess should be zero after pushing flow back to source.");
            }
        }

        if (!(-_nodeExcess[SourceNodeIndex]).Equals(_nodeExcess[SinkNodeIndex]))
        {
            throw new InvalidOperationException("Source and sink excesses should be equal and opposite after push back.");
        }
    }

    private void ComputeReachableNodes(int start, List<int> result, bool reverse)
    {
        int numNodes = Graph.NumNodes;
        if (start >= numNodes)
        {
            result.Clear();
            result.Add(start);
            return;
        }

        _bfsQueue.Clear();
        Array.Fill(_nodeInBfsQueue, false);

        int queueIndex = 0;
        _bfsQueue.Add(start);
        _nodeInBfsQueue[start] = true;

        while (queueIndex != _bfsQueue.Count)
        {
            var node = _bfsQueue[queueIndex];
            queueIndex++;

            foreach (var arc in Graph.OutgoingOrOppositeIncomingArcs(node))
            {
                var head = Head(arc);
                int headIdx = head;

                if (_nodeInBfsQueue[headIdx])
                {
                    continue;
                }

                var checkArc = reverse ? Opposite(arc) : arc;
                if (TArcFlowType.IsZero(_residualArcCapacity[checkArc]))
                {
                    continue;
                }

                _nodeInBfsQueue[headIdx] = true;
                _bfsQueue.Add(head);
            }
        }

        result.Clear();
        result.AddRange(_bfsQueue);
    }

    private bool AugmentingPathExists()
    {
        int numNodes = Graph.NumNodes;
        var isReached = new bool[numNodes];
        var toProcess = new Stack<int>();

        toProcess.Push(SourceNodeIndex);
        isReached[SourceNodeIndex] = true;

        while (toProcess.Count > 0)
        {
            var node = toProcess.Pop();
            foreach (var arc in Graph.OutgoingOrOppositeIncomingArcs(node))
            {
                if (TArcFlowType.IsPositive(_residualArcCapacity[arc]))
                {
                    var head = Head(arc);
                    int headIdx = head;
                    if (!isReached[headIdx])
                    {
                        isReached[headIdx] = true;
                        toProcess.Push(head);
                    }
                }
            }
        }

        return isReached[SinkNodeIndex];
    }

    private bool CheckResult()
    {
        if (!(-_nodeExcess[SourceNodeIndex]).Equals(_nodeExcess[SinkNodeIndex]))
        {
            return false;
        }

        for (int node = 0; node < Graph.NumNodes; node++)
        {
            if (!node.Equals(SourceNodeIndex) && !node.Equals(SinkNodeIndex))
            {
                if (!TFlowSumType.IsZero(_nodeExcess[node]))
                {
                    return false;
                }
            }
        }

        for (int arc = 0; arc < Graph.NumArcs; arc++)
        {
            var opposite = Opposite(arc);
            var directCapacity = _residualArcCapacity[arc];
            var oppositeCapacity = _residualArcCapacity[opposite];

            if (TArcFlowType.IsNegative(directCapacity) || TArcFlowType.IsNegative(oppositeCapacity))
            {
                return false;
            }

            if (TArcFlowType.IsNegative(directCapacity + oppositeCapacity))
            {
                return false;
            }
        }

        if (GetOptimalFlow()< _maxFlowSum && AugmentingPathExists())
        {
            return false;
        }

        return true;
    }

    // Helper methods
    private bool IsActive(int node)
    {
        return !node.Equals(SourceNodeIndex) && !node.Equals(SinkNodeIndex) &&
               TFlowSumType.IsPositive(_nodeExcess[node]);
    }

    private bool IsAdmissible(int tail, int arc)
    {
        if (!tail.Equals(Tail(arc)))
        {
            throw new ArgumentException("Tail does not match arc tail.", nameof(tail));
        }


        return TArcFlowType.IsPositive(_residualArcCapacity[arc]) &&
               _nodePotential[tail] == _nodePotential[Head(arc)] + 1;
    }

    private int Head(int arc) => Graph.Head(arc);
    private int Tail(int arc) => Graph.Tail(arc);
    private int Opposite(int arc) => Graph.OppositeArc(arc);

    private bool IsArcDirect(int arc)
    {
        return IsArcValid(arc) && arc >= 0;
    }

    private bool IsArcValid(int arc) => Graph.IsArcValid(arc);

    private static TFlowSumType ConvertToFlowSum(TArcFlowType value) => TFlowSumType.CreateChecked(value);
    private static TArcFlowType ConvertToArcFlow(TFlowSumType value) => TArcFlowType.CreateChecked(value);

    private static bool IsPositiveOrZero<T>(T value) where T : struct, IComparable<T> => value.CompareTo(default) >= 0;
    private static bool IsNegativeOrZero<T>(T value) where T : struct, IComparable<T> => value.CompareTo(default) <= 0;
}
