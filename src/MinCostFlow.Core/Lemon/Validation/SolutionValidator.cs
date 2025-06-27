using System;
using System.Collections.Generic;
using MinCostFlow.Core.Lemon.Algorithms;
using MinCostFlow.Core.Lemon.Graphs;
using MinCostFlow.Core.Lemon.Types;

namespace MinCostFlow.Core.Lemon.Validation;

/// <summary>
/// Validates minimum cost flow solutions for correctness.
/// </summary>
public class SolutionValidator(IGraph graph, NetworkSimplex solver)
{
    private readonly IGraph _graph = graph;
    private readonly NetworkSimplex _solver = solver;

    /// <summary>
    /// Validates the solution and returns a detailed report.
    /// </summary>
    public ValidationResult Validate()
    {
        var result = new ValidationResult();
        
        // Check if solver found a solution
        var status = _solver.Status;
        result.SolverStatus = status;
        result.SupplyType = _solver.SupplyType;
        
        if (status != SolverStatus.Optimal)
        {
            result.IsValid = false;
            result.AddError($"Solver status is {status}, not Optimal");
            return result;
        }

        // Validate flow conservation
        ValidateFlowConservation(result);
        
        // Validate capacity constraints
        ValidateCapacityConstraints(result);
        
        // Validate complementary slackness
        ValidateComplementarySlackness(result);
        
        // Calculate and verify objective value
        ValidateObjectiveValue(result);
        
        // Validate dual cost equals primal cost
        ValidateDualCost(result);
        
        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    private void ValidateFlowConservation(ValidationResult result)
    {
        // Get the supply type from the solver
        var supplyType = _solver.SupplyType;
        
        // Calculate net flow at each node
        long[] netFlow = new long[_graph.NodeCount];
        
        for (int arcId = 0; arcId < _graph.ArcCount; arcId++)
        {
            var arc = new Arc(arcId);
            var flow = _solver.GetFlow(arc);
            var source = _graph.Source(arc);
            var target = _graph.Target(arc);
            
            netFlow[source.Id] += flow;  // Flow out of source (positive)
            netFlow[target.Id] -= flow;  // Flow into target (negative)
        }
        
        // Check against supplies based on supply type
        for (int nodeId = 0; nodeId < _graph.NodeCount; nodeId++)
        {
            var node = new Node(nodeId);
            var supply = _solver.GetNodeSupply(node);
            
            bool satisfied = supplyType switch
            {
                SupplyType.Geq => netFlow[nodeId] >= supply,  // GEQ: net flow >= supply
                SupplyType.Leq => netFlow[nodeId] <= supply,  // LEQ: net flow <= supply
                _ => netFlow[nodeId] == supply                // Default to equality
            };
            
            if (!satisfied)
            {
                string violationType = supplyType switch
                {
                    SupplyType.Geq => "GEQ constraint",
                    SupplyType.Leq => "LEQ constraint",
                    _ => "equality constraint"
                };
                
                result.AddError($"Flow conservation {violationType} violated at node {nodeId}: " +
                              $"net flow = {netFlow[nodeId]}, supply = {supply}");
            }
        }
    }

    private void ValidateCapacityConstraints(ValidationResult result)
    {
        for (int arcId = 0; arcId < _graph.ArcCount; arcId++)
        {
            var arc = new Arc(arcId);
            var flow = _solver.GetFlow(arc);
            var lowerBound = _solver.GetArcLowerBound(arc);
            var upperBound = _solver.GetArcUpperBound(arc);
            
            if (flow < lowerBound)
            {
                result.AddError($"Lower bound violated on arc {arcId}: " +
                              $"flow = {flow}, lower = {lowerBound}");
            }
            
            if (flow > upperBound)
            {
                result.AddError($"Upper bound violated on arc {arcId}: " +
                              $"flow = {flow}, upper = {upperBound}");
            }
        }
    }

    /// <summary>
    /// Validates complementary slackness conditions for optimality.
    /// Based on LEMON's checkPotential implementation:
    /// - If reduced cost > 0, flow must equal lower bound
    /// - If reduced cost &lt; 0, flow must equal upper bound
    /// - If reduced cost = 0, flow can be anywhere in [lower, upper]
    /// For GEQ/LEQ problems, also validates node dual feasibility.
    /// </summary>
    private void ValidateComplementarySlackness(ValidationResult result)
    {
        // Get the supply type from the solver
        var supplyType = _solver.SupplyType;
        
        // Get node potentials
        var potentials = new long[_graph.NodeCount];
        for (int i = 0; i < _graph.NodeCount; i++)
        {
            potentials[i] = _solver.GetPotential(new Node(i));
        }
        
        // Check arc complementary slackness conditions
        for (int arcId = 0; arcId < _graph.ArcCount; arcId++)
        {
            var arc = new Arc(arcId);
            var flow = _solver.GetFlow(arc);
            var source = _graph.Source(arc);
            var target = _graph.Target(arc);
            
            // Calculate reduced cost
            var cost = _solver.GetArcCost(arc);
            long reducedCost = cost + potentials[source.Id] - potentials[target.Id];
            
            // Check conditions based on LEMON's implementation:
            // - If reduced cost > 0, flow MUST equal lower bound
            // - If reduced cost < 0, flow MUST equal upper bound
            // - If reduced cost = 0, flow can be anywhere in [lower, upper]
            var lowerBound = _solver.GetArcLowerBound(arc);
            var upperBound = _solver.GetArcUpperBound(arc);
            
            if (reducedCost > 0 && flow != lowerBound)
            {
                result.AddError($"Complementary slackness violation on arc {arcId}: " +
                                $"reduced cost = {reducedCost} > 0 but flow = {flow} != lower bound = {lowerBound}");
            }
            
            if (reducedCost < 0 && flow != upperBound)
            {
                result.AddError($"Complementary slackness violation on arc {arcId}: " +
                                $"reduced cost = {reducedCost} < 0 but flow = {flow} != upper bound = {upperBound}");
            }
        }
        
        // Check node dual feasibility for GEQ/LEQ problems
        // Calculate net flow at each node for the checks
        long[] netFlow = new long[_graph.NodeCount];
        for (int arcId = 0; arcId < _graph.ArcCount; arcId++)
        {
            var arc = new Arc(arcId);
            var flow = _solver.GetFlow(arc);
            var source = _graph.Source(arc);
            var target = _graph.Target(arc);
            
            netFlow[source.Id] += flow;
            netFlow[target.Id] -= flow;
        }
        
        // Validate node potentials based on supply type
        for (int nodeId = 0; nodeId < _graph.NodeCount; nodeId++)
        {
            var node = new Node(nodeId);
            var supply = _solver.GetNodeSupply(node);
            var pi = potentials[nodeId];
            
            if (supplyType == SupplyType.Geq)
            {
                // For GEQ: pi[n] <= 0, and if pi[n] < 0 then net_flow[n] = supply[n]
                if (pi > 0)
                {
                    result.AddError($"Node dual feasibility violation at node {nodeId}: " +
                                  $"GEQ problem requires pi <= 0, but pi = {pi}");
                }
                else if (pi < 0 && netFlow[nodeId] != supply)
                {
                    result.AddError($"Node complementary slackness violation at node {nodeId}: " +
                                  $"GEQ problem with pi = {pi} < 0 requires net flow = supply, " +
                                  $"but net flow = {netFlow[nodeId]}, supply = {supply}");
                }
            }
            else if (supplyType == SupplyType.Leq)
            {
                // For LEQ: pi[n] >= 0, and if pi[n] > 0 then net_flow[n] = supply[n]
                if (pi < 0)
                {
                    result.AddError($"Node dual feasibility violation at node {nodeId}: " +
                                  $"LEQ problem requires pi >= 0, but pi = {pi}");
                }
                else if (pi > 0 && netFlow[nodeId] != supply)
                {
                    result.AddError($"Node complementary slackness violation at node {nodeId}: " +
                                  $"LEQ problem with pi = {pi} > 0 requires net flow = supply, " +
                                  $"but net flow = {netFlow[nodeId]}, supply = {supply}");
                }
            }
        }
    }

    private void ValidateObjectiveValue(ValidationResult result)
    {
        long calculatedCost = 0;
        
        for (int arcId = 0; arcId < _graph.ArcCount; arcId++)
        {
            var arc = new Arc(arcId);
            var flow = _solver.GetFlow(arc);
            
            if (flow > 0)
            {
                var source = _graph.Source(arc);
                var target = _graph.Target(arc);
                var cost = _solver.GetArcCost(arc);
                result.ArcFlows.Add((arcId, source.Id, target.Id, flow, cost));
            }
            
            calculatedCost += flow * _solver.GetArcCost(arc);
        }
        
        var reportedCost = _solver.GetTotalCost();
        result.ObjectiveValue = reportedCost;
        
        if (calculatedCost != reportedCost)
        {
            result.AddError($"Objective value mismatch: " +
                          $"calculated = {calculatedCost}, reported = {reportedCost}");
        }
    }
    
    /// <summary>
    /// Validates that the dual cost equals the primal cost.
    /// Based on LEMON's checkDualCost implementation.
    /// </summary>
    private void ValidateDualCost(ValidationResult result)
    {
        // Calculate dual cost following LEMON's formula:
        // dual_cost = sum(-supply[i] * pi[i]) + sum(lower[a] * cost[a]) 
        //           - sum((upper[a]-lower[a]) * max(-reduced_cost[a], 0))
        
        long dualCost = 0;
        
        // Get node potentials
        var potentials = new long[_graph.NodeCount];
        for (int i = 0; i < _graph.NodeCount; i++)
        {
            potentials[i] = _solver.GetPotential(new Node(i));
        }
        
        // First term: -sum(supply[i] * pi[i])
        // But we need to adjust supplies by removing the effect of non-zero lower bounds
        long[] adjustedSupply = new long[_graph.NodeCount];
        for (int i = 0; i < _graph.NodeCount; i++)
        {
            adjustedSupply[i] = _solver.GetNodeSupply(new Node(i));
        }
        
        // Adjust supplies for non-zero lower bounds
        for (int arcId = 0; arcId < _graph.ArcCount; arcId++)
        {
            var arc = new Arc(arcId);
            var lower = _solver.GetArcLowerBound(arc);
            if (lower != 0)
            {
                var source = _graph.Source(arc);
                var target = _graph.Target(arc);
                var cost = _solver.GetArcCost(arc);
                
                // Add lower bound cost
                dualCost += lower * cost;
                
                // Adjust supplies
                adjustedSupply[source.Id] -= lower;
                adjustedSupply[target.Id] += lower;
            }
        }
        
        // Add node potential contributions
        for (int i = 0; i < _graph.NodeCount; i++)
        {
            dualCost -= adjustedSupply[i] * potentials[i];
        }
        
        // Second term: -sum((upper[a]-lower[a]) * max(-reduced_cost[a], 0))
        for (int arcId = 0; arcId < _graph.ArcCount; arcId++)
        {
            var arc = new Arc(arcId);
            var source = _graph.Source(arc);
            var target = _graph.Target(arc);
            var cost = _solver.GetArcCost(arc);
            
            // Calculate reduced cost
            long reducedCost = cost + potentials[source.Id] - potentials[target.Id];
            
            if (reducedCost < 0)
            {
                var lower = _solver.GetArcLowerBound(arc);
                var upper = _solver.GetArcUpperBound(arc);
                dualCost -= (upper - lower) * -reducedCost;
            }
        }
        
        // Compare with primal cost
        var primalCost = _solver.GetTotalCost();
        result.DualCost = dualCost;
        
        if (dualCost != primalCost)
        {
            result.AddError($"Dual-primal cost mismatch: " +
                          $"dual cost = {dualCost}, primal cost = {primalCost}");
        }
    }
}

/// <summary>
/// Result of solution validation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public SolverStatus SolverStatus { get; set; }
    public SupplyType SupplyType { get; set; }
    public long ObjectiveValue { get; set; }
    public long DualCost { get; set; }
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    
    public void AddError(string error) => Errors.Add(error);
    public void AddWarning(string warning) => Warnings.Add(warning);
    
    public void PrintReport()
    {
        Console.WriteLine("=== Solution Validation Report ===");
        Console.WriteLine($"Status: {(IsValid ? "VALID" : "INVALID")}");
        Console.WriteLine($"Solver Status: {SolverStatus}");
        Console.WriteLine($"Supply Type: {SupplyType}");
        Console.WriteLine($"Objective Value: {ObjectiveValue}");
        Console.WriteLine($"Dual Cost: {DualCost}");
        
        if (Errors.Count > 0)
        {
            Console.WriteLine("\nErrors:");
            foreach (var error in Errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }
        
        if (Warnings.Count > 0)
        {
            Console.WriteLine("\nWarnings:");
            foreach (var warning in Warnings)
            {
                Console.WriteLine($"  - {warning}");
            }
        }
        
        if (IsValid && Errors.Count == 0 && Warnings.Count == 0)
        {
            Console.WriteLine("\nAll validation checks passed!");
        }
    }
    
    public List<(int arcId, int source, int target, long flow, long cost)> ArcFlows { get; } = [];
}