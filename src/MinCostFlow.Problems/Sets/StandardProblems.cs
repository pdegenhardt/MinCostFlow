using System;
using System.Collections.Generic;
using MinCostFlow.Problems.Loaders;
using MinCostFlow.Problems.Models;

namespace MinCostFlow.Problems.Sets
{
    /// <summary>
    /// Provides easy access to standard embedded test problems.
    /// </summary>
    public static class StandardProblems
    {
        private static readonly EmbeddedResourceLoader _loader = new();
        private static readonly Dictionary<string, MinCostFlowProblem> _cache = new();
        private static readonly object _cacheLock = new();
        private static readonly DynamicProblems _dynamicProblems = new(_loader);

        /// <summary>
        /// Small test problems suitable for unit tests and debugging.
        /// </summary>
        public static class Small
        {
            /// <summary>
            /// 3x3 assignment problem.
            /// </summary>
            public static MinCostFlowProblem Assignment3x3 => LoadCached("Resources.small.assignment_3x3.min");

            /// <summary>
            /// Simple cycle with shortcut problem.
            /// </summary>
            public static MinCostFlowProblem CycleShortcut => LoadCached("Resources.small.cycle_shortcut.min");

            /// <summary>
            /// Diamond-shaped graph problem.
            /// </summary>
            public static MinCostFlowProblem DiamondGraph => LoadCached("Resources.small.diamond_graph.min");

            /// <summary>
            /// 2x2 grid network problem.
            /// </summary>
            public static MinCostFlowProblem Grid2x2 => LoadCached("Resources.small.grid_2x2.min");

            /// <summary>
            /// 10-node path problem.
            /// </summary>
            public static MinCostFlowProblem Path10Node => LoadCached("Resources.small.path_10node.min");

            /// <summary>
            /// 5-node path problem.
            /// </summary>
            public static MinCostFlowProblem Path5Node => LoadCached("Resources.small.path_5node.min");

            /// <summary>
            /// Simple 4-node problem.
            /// </summary>
            public static MinCostFlowProblem Simple4Node => LoadCached("Resources.small.simple_4node.min");

            /// <summary>
            /// Star-shaped graph problem.
            /// </summary>
            public static MinCostFlowProblem StarGraph => LoadCached("Resources.small.star_graph.min");

            /// <summary>
            /// 2x3 transportation problem.
            /// </summary>
            public static MinCostFlowProblem Transport2x3 => LoadCached("Resources.small.transport_2x3.min");

            /// <summary>
            /// Gets all small problems.
            /// </summary>
            public static IEnumerable<MinCostFlowProblem> All()
            {
                // Use dynamic enumeration to get all small problems
                return _dynamicProblems.GetByCategory("Small");
            }
        }

        /// <summary>
        /// DIMACS benchmark problems.
        /// </summary>
        public static class Dimacs
        {
            /// <summary>
            /// NETGEN 8_08a problem.
            /// </summary>
            public static MinCostFlowProblem Netgen8_08a => LoadCached("Resources.dimacs.netgen_8_08a.min");

            /// <summary>
            /// NETGEN 8_10a problem.
            /// </summary>
            public static MinCostFlowProblem Netgen8_10a => LoadCached("Resources.dimacs.netgen_8_10a.min");

            /// <summary>
            /// NETGEN 8_13a problem.
            /// </summary>
            public static MinCostFlowProblem Netgen8_13a => LoadCached("Resources.dimacs.netgen_8_13a.min");

            /// <summary>
            /// NETGEN 8_14a problem.
            /// </summary>
            public static MinCostFlowProblem Netgen8_14a => LoadCached("Resources.dimacs.netgen_8_14a.min");

            /// <summary>
            /// NETGEN 8_15a problem.
            /// </summary>
            public static MinCostFlowProblem Netgen8_15a => LoadCached("Resources.dimacs.netgen_8_15a.min");

            /// <summary>
            /// Gets all DIMACS problems.
            /// </summary>
            public static IEnumerable<MinCostFlowProblem> All()
            {
                // Use dynamic enumeration to get all DIMACS problems
                return _dynamicProblems.GetByCategory("DIMACS");
            }
        }

        /// <summary>
        /// LEMON test problems.
        /// </summary>
        public static class Lemon
        {
            /// <summary>
            /// 12-node test problem from LEMON.
            /// </summary>
            public static MinCostFlowProblem Test12Node => LoadCached("Resources.lemon.test_12node.min");

            /// <summary>
            /// Gets all LEMON problems.
            /// </summary>
            public static IEnumerable<MinCostFlowProblem> All()
            {
                // Use dynamic enumeration to get all LEMON problems
                return _dynamicProblems.GetByCategory("LEMON");
            }
        }

        /// <summary>
        /// Solutions for problems that have known optimal solutions.
        /// </summary>
        public static class Solutions
        {
            private static readonly Dictionary<string, SolutionLoader.Solution> _solutionCache = new();
            private static readonly object _solutionCacheLock = new();

            /// <summary>
            /// Gets the solution for a problem if available.
            /// </summary>
            /// <param name="problemName">The problem resource name (e.g., "small.path_5node").</param>
            /// <returns>The solution if available, null otherwise.</returns>
            public static SolutionLoader.Solution? GetSolution(string problemName)
            {
                try
                {
                    // Ensure we have the full resource name
                    if (!problemName.StartsWith("Resources.") && !problemName.StartsWith("MinCostFlow.Problems."))
                    {
                        // If it's missing the Resources prefix, add it
                        problemName = "Resources." + problemName;
                    }
                    
                    // Ensure it ends with .sol
                    if (!problemName.EndsWith(".sol"))
                    {
                        // If it ends with .min, replace it; otherwise add .sol
                        if (problemName.EndsWith(".min"))
                        {
                            problemName = problemName.Replace(".min", ".sol");
                        }
                        else
                        {
                            problemName = problemName + ".sol";
                        }
                    }
                    
                    lock (_solutionCacheLock)
                    {
                        if (_solutionCache.TryGetValue(problemName, out var cached))
                            return cached;
                        
                        var solution = _loader.LoadSolutionFromResource(problemName);
                        _solutionCache[problemName] = solution;
                        return solution;
                    }
                }
                catch
                {
                    return null;
                }
            }

            /// <summary>
            /// Gets the expected optimal cost for a problem if known.
            /// </summary>
            /// <param name="problemName">The problem resource name.</param>
            /// <returns>The optimal cost if known, null otherwise.</returns>
            public static long? GetOptimalCost(string problemName)
            {
                var solution = GetSolution(problemName);
                return solution?.OptimalCost;
            }

            /// <summary>
            /// Checks if a solution exists for the given problem.
            /// </summary>
            public static bool HasSolution(string problemName)
            {
                return GetSolution(problemName) != null;
            }

            /// <summary>
            /// Gets the solution for a specific DIMACS problem.
            /// </summary>
            public static SolutionLoader.Solution? GetDimacsSolution(string problemName)
            {
                return GetSolution($"dimacs.{problemName}");
            }

            /// <summary>
            /// Gets the Gurobi solution for netgen_8_08a.
            /// </summary>
            public static SolutionLoader.Solution? Netgen8_08a => GetSolution("dimacs.netgen_8_08a");

            /// <summary>
            /// Gets the Gurobi solution for netgen_8_13a.
            /// </summary>
            public static SolutionLoader.Solution? Netgen8_13a => GetSolution("dimacs.netgen_8_13a");
        }

        /// <summary>
        /// Gets all available embedded problems grouped by category.
        /// </summary>
        public static Dictionary<string, List<MinCostFlowProblem>> GetAllByCategory()
        {
            // Use dynamic enumeration to get all problems by category
            return _dynamicProblems.GetAllByCategory();
        }
        
        /// <summary>
        /// Medium test problems.
        /// </summary>
        public static class Medium
        {
            /// <summary>
            /// Gets all medium problems.
            /// </summary>
            public static IEnumerable<MinCostFlowProblem> All()
            {
                // Use dynamic enumeration to get all medium problems
                return _dynamicProblems.GetByCategory("Medium");
            }
        }
        
        /// <summary>
        /// Large test problems.
        /// </summary>
        public static class Large
        {
            /// <summary>
            /// Gets all large problems.
            /// </summary>
            public static IEnumerable<MinCostFlowProblem> All()
            {
                // Use dynamic enumeration to get all large problems
                return _dynamicProblems.GetByCategory("Large");
            }
        }

        /// <summary>
        /// Loads a problem from embedded resources with caching.
        /// </summary>
        private static MinCostFlowProblem LoadCached(string resourceName)
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(resourceName, out var cached))
                    return cached;

                var problem = _loader.LoadFromResource(resourceName);
                
                // Try to set optimal cost from solution if available
                var optimalCost = Solutions.GetOptimalCost(resourceName);
                if (optimalCost.HasValue && problem.Metadata != null)
                {
                    problem.Metadata.OptimalCost = optimalCost.Value;
                }

                _cache[resourceName] = problem;
                return problem;
            }
        }

        /// <summary>
        /// Clears the internal cache.
        /// </summary>
        public static void ClearCache()
        {
            lock (_cacheLock)
            {
                _cache.Clear();
            }
        }
    }
}