using System;
using System.Collections.Generic;
using System.Diagnostics;
using Google.OrTools.Graph;
using MinCostFlow.Core.Algorithms;
using MinCostFlow.Core.Graphs;
using MinCostFlow.Core.Types;
using GoogleMinCostFlow = Google.OrTools.Graph.MinCostFlow;

namespace MinCostFlow.Benchmarks.Solvers
{
    /// <summary>
    /// Wrapper for Google OR-Tools MinCostFlow solver.
    /// </summary>
    public class OrToolsSolver
    {
        private readonly IGraph _graph;
        private readonly GoogleMinCostFlow _solver;
        private readonly Dictionary<int, long> _nodeSupplies = new();
        private readonly Dictionary<int, (long lower, long upper)> _arcBounds = new();
        private readonly Dictionary<int, long> _arcCosts = new();
        private readonly Dictionary<(int from, int to), int> _arcLookup = new();
        private readonly Dictionary<int, (long lower, long cost)> _arcLowerBoundCosts = new();
        private SolverStatus _lastStatus = SolverStatus.NotSolved;
        private bool _isPrepared = false;
        private long _lowerBoundCostAdjustment = 0;

        public OrToolsSolver(IGraph graph)
        {
            _graph = graph;
            _solver = new GoogleMinCostFlow();
        }

        public SolverStatus Status => _lastStatus;

        public void SetNodeSupply(Node node, long supply)
        {
            if (_isPrepared)
                throw new InvalidOperationException("Cannot modify solver after preparation");
            _nodeSupplies[node.Id] = supply;
        }

        public void SetArcBounds(Arc arc, long lower, long upper)
        {
            if (_isPrepared)
                throw new InvalidOperationException("Cannot modify solver after preparation");
            _arcBounds[arc.Id] = (lower, upper);
        }

        public void SetArcCost(Arc arc, long cost)
        {
            if (_isPrepared)
                throw new InvalidOperationException("Cannot modify solver after preparation");
            _arcCosts[arc.Id] = cost;
        }

        public SolverStatus Solve()
        {
            if (!_isPrepared)
                PrepareSolver();

            var result = _solver.Solve();
            _lastStatus = ConvertStatus(result);
            return _lastStatus;
        }

        public SolverStatus Solve(PivotRule pivotRule)
        {
            // OR-Tools doesn't expose pivot rule selection, just use regular solve
            return Solve();
        }

        public long GetNodePotential(Node node)
        {
            if (_lastStatus != SolverStatus.Optimal)
                throw new InvalidOperationException("Solution not available");
            
            // OR-Tools doesn't directly expose node potentials
            // We would need to compute them from the dual solution
            return 0; // Placeholder
        }

        public long GetPotential(Node node)
        {
            return GetNodePotential(node);
        }

        public long GetFlow(Arc arc)
        {
            if (_lastStatus != SolverStatus.Optimal)
                throw new InvalidOperationException("Solution not available");
            
            var source = _graph.Source(arc);
            var target = _graph.Target(arc);
            var key = (source.Id, target.Id);
            
            if (_arcLookup.TryGetValue(key, out var orToolsArcId))
            {
                var (lower, _) = GetArcBounds(arc);
                // Add back the lower bound to get the actual flow
                return _solver.Flow(orToolsArcId) + lower;
            }
            
            return 0;
        }

        public long GetTotalCost()
        {
            if (_lastStatus != SolverStatus.Optimal)
                throw new InvalidOperationException("Solution not available");
            
            // Add back the cost of lower bound flows
            return _solver.OptimalCost() + _lowerBoundCostAdjustment;
        }

        public SupplyType GetSupplyType()
        {
            long totalSupply = 0;
            foreach (var supply in _nodeSupplies.Values)
            {
                totalSupply += supply;
            }

            if (totalSupply > 0) return SupplyType.Geq;
            if (totalSupply < 0) return SupplyType.Leq;
            return SupplyType.Geq; // Default
        }

        public long GetNodeSupply(Node node)
        {
            return _nodeSupplies.TryGetValue(node.Id, out var supply) ? supply : 0;
        }

        public (long lower, long upper) GetArcBounds(Arc arc)
        {
            return _arcBounds.TryGetValue(arc.Id, out var bounds) ? bounds : (0, long.MaxValue);
        }

        public long GetArcCost(Arc arc)
        {
            return _arcCosts.TryGetValue(arc.Id, out var cost) ? cost : 0;
        }

        private void PrepareSolver()
        {
            // First pass: Add all arcs
            var adjustedSupplies = new Dictionary<int, long>(_nodeSupplies);
            
            for (int arcId = 0; arcId < _graph.ArcCount; arcId++)
            {
                var arc = new Arc(arcId);
                var source = _graph.Source(arc);
                var target = _graph.Target(arc);
                
                var (lower, upper) = GetArcBounds(arc);
                var cost = GetArcCost(arc);
                
                // OR-Tools MinCostFlow doesn't support lower bounds directly
                // We transform the problem by:
                // 1. Setting capacity to (upper - lower)
                // 2. Adjusting node supplies by the lower bound
                // 3. Adjusting the objective by lower * cost
                
                long capacity = upper - lower;
                
                // OR-Tools uses int for arc indices, long for capacities and costs
                int orToolsArcId = _solver.AddArcWithCapacityAndUnitCost(
                    source.Id, 
                    target.Id, 
                    capacity, 
                    cost);
                
                // Store mapping for later flow retrieval
                _arcLookup[(source.Id, target.Id)] = orToolsArcId;
                
                // Adjust supplies for lower bounds
                if (lower != 0)
                {
                    if (!adjustedSupplies.ContainsKey(source.Id))
                        adjustedSupplies[source.Id] = 0;
                    if (!adjustedSupplies.ContainsKey(target.Id))
                        adjustedSupplies[target.Id] = 0;
                    
                    adjustedSupplies[source.Id] -= lower;
                    adjustedSupplies[target.Id] += lower;
                    
                    // Track cost adjustment for lower bounds
                    _lowerBoundCostAdjustment += lower * cost;
                    _arcLowerBoundCosts[arcId] = (lower, cost);
                }
            }
            
            // Set adjusted node supplies
            for (int nodeId = 0; nodeId < _graph.NodeCount; nodeId++)
            {
                long supply = adjustedSupplies.TryGetValue(nodeId, out var s) ? s : 0;
                _solver.SetNodeSupply(nodeId, supply);
            }
            
            _isPrepared = true;
        }

        private SolverStatus ConvertStatus(GoogleMinCostFlow.Status orToolsStatus)
        {
            switch (orToolsStatus)
            {
                case GoogleMinCostFlow.Status.OPTIMAL:
                    return SolverStatus.Optimal;
                case GoogleMinCostFlow.Status.INFEASIBLE:
                    return SolverStatus.Infeasible;
                case GoogleMinCostFlow.Status.UNBALANCED:
                    return SolverStatus.Infeasible; // Unbalanced is a type of infeasibility
                case GoogleMinCostFlow.Status.BAD_COST_RANGE:
                case GoogleMinCostFlow.Status.BAD_RESULT:
                case GoogleMinCostFlow.Status.NOT_SOLVED:
                default:
                    return SolverStatus.NotSolved;
            }
        }
    }
}