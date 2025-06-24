using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MinCostFlow.Problems.Loaders
{
    /// <summary>
    /// Loads solution files for validation purposes.
    /// </summary>
    public class SolutionLoader
    {
        /// <summary>
        /// Represents a solution to a minimum cost flow problem.
        /// </summary>
        public class Solution
        {
            /// <summary>
            /// Gets or sets the optimal cost value.
            /// </summary>
            public long OptimalCost { get; set; }

            /// <summary>
            /// Gets or sets the arc flows. Key is arc index, value is flow.
            /// </summary>
            public Dictionary<int, long> ArcFlows { get; set; } = new Dictionary<int, long>();

            /// <summary>
            /// Gets or sets arc flows by source/target pairs. Key is (source, target), value is flow.
            /// </summary>
            public Dictionary<(int source, int target), long> ArcFlowsByEndpoints { get; set; } = new Dictionary<(int, int), long>();

            /// <summary>
            /// Gets or sets node potentials (dual variables).
            /// </summary>
            public Dictionary<int, long>? NodePotentials { get; set; }

            /// <summary>
            /// Gets or sets the source of the solution (e.g., "Gurobi", "LEMON", "NetworkSimplex").
            /// </summary>
            public string Source { get; set; } = "Unknown";

            /// <summary>
            /// Gets or sets when the solution was generated.
            /// </summary>
            public DateTime? GeneratedAt { get; set; }

            /// <summary>
            /// Gets or sets additional metadata about the solution.
            /// </summary>
            public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        }

        /// <summary>
        /// Loads a solution from a file.
        /// Supports DIMACS .sol format.
        /// </summary>
        public static Solution LoadFromFile(string filePath)
        {
            using var reader = new StreamReader(filePath);
            return LoadFromStream(reader);
        }

        /// <summary>
        /// Loads a solution from a stream.
        /// </summary>
        public static Solution LoadFromStream(StreamReader reader)
        {
            var solution = new Solution();
            string? line;
            bool hasGurobiComment = false;

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                    continue;

                switch (tokens[0])
                {
                    case "c":
                        // Comment - check for special markers
                        if (line.Contains("Gurobi", StringComparison.OrdinalIgnoreCase))
                        {
                            solution.Source = "Gurobi";
                            hasGurobiComment = true;
                        }
                        else if (line.Contains("Generated on", StringComparison.OrdinalIgnoreCase))
                        {
                            // Try to parse the date
                            var dateMatch = System.Text.RegularExpressions.Regex.Match(line, @"(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})");
                            if (dateMatch.Success && DateTime.TryParse(dateMatch.Groups[1].Value, out var date))
                            {
                                solution.GeneratedAt = date;
                            }
                        }
                        break;

                    case "s":
                        // Solution value: s COST
                        if (tokens.Length >= 2)
                        {
                            solution.OptimalCost = long.Parse(tokens[1], CultureInfo.InvariantCulture);
                        }
                        break;

                    case "f":
                        // Flow value: f ARC_ID FLOW or f SRC DST FLOW
                        if (tokens.Length >= 3)
                        {
                            if (tokens.Length == 3)
                            {
                                // Format: f ARC_ID FLOW
                                int arcId = int.Parse(tokens[1], CultureInfo.InvariantCulture);
                                long flow = long.Parse(tokens[2], CultureInfo.InvariantCulture);
                                solution.ArcFlows[arcId] = flow;
                            }
                            else if (tokens.Length >= 4)
                            {
                                // Format: f SRC DST FLOW
                                int src = int.Parse(tokens[1], CultureInfo.InvariantCulture) - 1; // Convert to 0-based
                                int dst = int.Parse(tokens[2], CultureInfo.InvariantCulture) - 1; // Convert to 0-based
                                long flow = long.Parse(tokens[3], CultureInfo.InvariantCulture);
                                
                                // Store in endpoint format
                                solution.ArcFlowsByEndpoints[(src, dst)] = flow;
                                
                                // Also create a pseudo arc ID for backward compatibility
                                int arcId = src * 100000 + dst; // Simple encoding
                                solution.ArcFlows[arcId] = flow;
                            }
                        }
                        break;

                    case "p":
                        // Node potential: p NODE_ID POTENTIAL
                        if (tokens.Length >= 3)
                        {
                            if (solution.NodePotentials == null)
                                solution.NodePotentials = new Dictionary<int, long>();

                            int nodeId = int.Parse(tokens[1], CultureInfo.InvariantCulture);
                            long potential = long.Parse(tokens[2], CultureInfo.InvariantCulture);
                            solution.NodePotentials[nodeId] = potential;
                        }
                        break;
                }
            }

            // If we didn't find a specific source but found Gurobi comments, set it
            if (solution.Source == "Unknown" && hasGurobiComment)
            {
                solution.Source = "Gurobi";
            }

            return solution;
        }

        /// <summary>
        /// Asynchronously loads a solution from a file.
        /// </summary>
        public static async Task<Solution> LoadFromFileAsync(string filePath)
        {
            return await Task.Run(() => LoadFromFile(filePath)).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves a solution to a file.
        /// </summary>
        public static void SaveToFile(Solution solution, string filePath)
        {
            using var writer = new StreamWriter(filePath);
            
            // Write optimal cost
            writer.WriteLine($"s {solution.OptimalCost}");

            // Write arc flows
            foreach (var (arcId, flow) in solution.ArcFlows.OrderBy(kvp => kvp.Key))
            {
                if (flow != 0)
                {
                    writer.WriteLine($"f {arcId} {flow}");
                }
            }

            // Write node potentials if available
            if (solution.NodePotentials != null)
            {
                foreach (var (nodeId, potential) in solution.NodePotentials.OrderBy(kvp => kvp.Key))
                {
                    writer.WriteLine($"p {nodeId} {potential}");
                }
            }
        }
    }
}