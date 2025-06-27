using System;
using System.IO;
using System.Linq;
using MinCostFlow.Core.Lemon.Algorithms;
using MinCostFlow.Core.Lemon.Types;
using MinCostFlow.Problems;
using MinCostFlow.Problems.Generators;
using MinCostFlow.Problems.Models;

namespace MinCostFlow.Benchmarks;

/// <summary>
/// Handles command-line problem generation.
/// </summary>
public static class ProblemGeneratorCommand
{
    private static readonly ProblemRepository _repository = new();
    
    public static void Execute(string[] args)
    {
        if (args.Length < 2)
        {
            PrintGenerateHelp();
            return;
        }
        
        var problemType = args[1].ToLowerInvariant();
        
        try
        {
            switch (problemType)
            {
                case "assignment":
                    GenerateAssignmentProblem(args.Skip(2).ToArray());
                    break;
                case "transport":
                case "transportation":
                    GenerateTransportationProblem(args.Skip(2).ToArray());
                    break;
                case "circulation":
                    GenerateCirculationProblem(args.Skip(2).ToArray());
                    break;
                case "path":
                    GeneratePathProblem(args.Skip(2).ToArray());
                    break;
                case "grid":
                    GenerateGridProblem(args.Skip(2).ToArray());
                    break;
                default:
                    Console.WriteLine($"Unknown problem type: {problemType}");
                    PrintGenerateHelp();
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error generating problem: {ex.Message}");
            Environment.Exit(1);
        }
    }
    
    private static void PrintGenerateHelp()
    {
        Console.WriteLine("Problem Generation Usage:");
        Console.WriteLine();
        Console.WriteLine("  dotnet run -- --generate <type> <parameters>");
        Console.WriteLine();
        Console.WriteLine("Problem Types:");
        Console.WriteLine("  assignment <size>                    - Generate assignment problem");
        Console.WriteLine("  transport <sources> <sinks>          - Generate transportation problem");
        Console.WriteLine("  circulation <nodes> <density>        - Generate circulation problem");
        Console.WriteLine("  path <nodes>                         - Generate path problem");
        Console.WriteLine("  grid <rows> <cols>                   - Generate grid problem");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run -- --generate assignment 100");
        Console.WriteLine("  dotnet run -- --generate transport 50 50");
        Console.WriteLine("  dotnet run -- --generate circulation 1000 0.05");
        Console.WriteLine("  dotnet run -- --generate path 5000");
        Console.WriteLine("  dotnet run -- --generate grid 100 100");
    }
    
    private static void GenerateAssignmentProblem(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: --generate assignment <size>");
            return;
        }
        
        if (!int.TryParse(args[0], out var size) || size <= 0)
        {
            Console.WriteLine("Size must be a positive integer");
            return;
        }
        
        var problemType = "assignment";
        var category = GetSizeCategory(size * 2); // Total nodes = 2 * size
        var fileName = $"assignment_{size}x{size}";
        var (minCost, maxCost) = GetCostRange(category);
        
        Console.WriteLine($"Generating {size}x{size} assignment problem...");
        
        var tempFile = Path.GetTempFileName();
        try
        {
            // Generate the problem
            ProblemGenerator.GenerateAssignmentProblem(tempFile, size, size, minCost, maxCost);
            
            // Load and solve it
            var problem = _repository.LoadFromFile(tempFile, useCache: false);
            var (solution, optimalCost) = SolveProblem(problem);
            
            // Save to resources
            var resourceDir = GetResourceDirectory(problemType);
            var problemPath = Path.Combine(resourceDir, fileName + ".min");
            var solutionPath = Path.Combine(resourceDir, fileName + ".sol");
            
            File.Copy(tempFile, problemPath, overwrite: true);
            WriteSolutionFile(solutionPath, solution, optimalCost, fileName);
            
            Console.WriteLine($"Generated files:");
            Console.WriteLine($"  Problem:  {problemPath}");
            Console.WriteLine($"  Solution: {solutionPath}");
            Console.WriteLine($"  Optimal cost: {optimalCost}");
            Console.WriteLine($"  Category: {category}");
            Console.WriteLine();
            Console.WriteLine("Files will be included as embedded resources in the next build.");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
    
    private static void GenerateTransportationProblem(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: --generate transport <sources> <sinks>");
            return;
        }
        
        if (!int.TryParse(args[0], out var sources) || sources <= 0 ||
            !int.TryParse(args[1], out var sinks) || sinks <= 0)
        {
            Console.WriteLine("Sources and sinks must be positive integers");
            return;
        }
        
        var problemType = "transport";
        var totalNodes = sources + sinks;
        var category = GetSizeCategory(totalNodes);
        var fileName = $"transport_{sources}x{sinks}";
        var (minCost, maxCost) = GetCostRange(category);
        
        Console.WriteLine($"Generating {sources}x{sinks} transportation problem...");
        
        var tempFile = Path.GetTempFileName();
        try
        {
            // Generate the problem
            var supply = 1000L * Math.Max(sources, sinks);
            ProblemGenerator.GenerateTransportationProblem(tempFile, sources, sinks, supply, minCost, maxCost);
            
            // Load and solve it
            var problem = _repository.LoadFromFile(tempFile, useCache: false);
            var (solution, optimalCost) = SolveProblem(problem);
            
            // Save to resources
            var resourceDir = GetResourceDirectory(problemType);
            var problemPath = Path.Combine(resourceDir, fileName + ".min");
            var solutionPath = Path.Combine(resourceDir, fileName + ".sol");
            
            File.Copy(tempFile, problemPath, overwrite: true);
            WriteSolutionFile(solutionPath, solution, optimalCost, fileName);
            
            Console.WriteLine($"Generated files:");
            Console.WriteLine($"  Problem:  {problemPath}");
            Console.WriteLine($"  Solution: {solutionPath}");
            Console.WriteLine($"  Optimal cost: {optimalCost}");
            Console.WriteLine($"  Category: {category}");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
    
    private static void GenerateCirculationProblem(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: --generate circulation <nodes> <density>");
            return;
        }
        
        if (!int.TryParse(args[0], out var nodes) || nodes <= 0)
        {
            Console.WriteLine("Nodes must be a positive integer");
            return;
        }
        
        if (!double.TryParse(args[1], out var density) || density <= 0 || density > 1)
        {
            Console.WriteLine("Density must be between 0 and 1");
            return;
        }
        
        var problemType = "circulation";
        var category = GetSizeCategory(nodes);
        var fileName = $"circulation_{nodes}_{density:F2}".Replace(".", "_");
        
        Console.WriteLine($"Generating circulation problem with {nodes} nodes, density {density:F2}...");
        
        var tempFile = Path.GetTempFileName();
        try
        {
            // Generate the problem
            ProblemGenerator.GenerateCirculationProblem(tempFile, nodes, density);
            
            // Load and solve it
            var problem = _repository.LoadFromFile(tempFile, useCache: false);
            var (solution, optimalCost) = SolveProblem(problem);
            
            // Save to resources
            var resourceDir = GetResourceDirectory(problemType);
            var problemPath = Path.Combine(resourceDir, fileName + ".min");
            var solutionPath = Path.Combine(resourceDir, fileName + ".sol");
            
            File.Copy(tempFile, problemPath, overwrite: true);
            WriteSolutionFile(solutionPath, solution, optimalCost, fileName);
            
            Console.WriteLine($"Generated files:");
            Console.WriteLine($"  Problem:  {problemPath}");
            Console.WriteLine($"  Solution: {solutionPath}");
            Console.WriteLine($"  Optimal cost: {optimalCost}");
            Console.WriteLine($"  Category: {category}");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
    
    private static void GeneratePathProblem(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: --generate path <nodes>");
            return;
        }
        
        if (!int.TryParse(args[0], out var nodes) || nodes <= 0)
        {
            Console.WriteLine("Nodes must be a positive integer");
            return;
        }
        
        var problemType = "path";
        var category = GetSizeCategory(nodes);
        var fileName = $"path_{nodes}node";
        
        Console.WriteLine($"Generating path problem with {nodes} nodes...");
        
        var tempFile = Path.GetTempFileName();
        try
        {
            // Generate the problem
            var supply = 1000L;
            ProblemGenerator.GeneratePathProblem(tempFile, nodes, supply);
            
            // Load and solve it
            var problem = _repository.LoadFromFile(tempFile, useCache: false);
            var (solution, optimalCost) = SolveProblem(problem);
            
            // Save to resources
            var resourceDir = GetResourceDirectory(problemType);
            var problemPath = Path.Combine(resourceDir, fileName + ".min");
            var solutionPath = Path.Combine(resourceDir, fileName + ".sol");
            
            File.Copy(tempFile, problemPath, overwrite: true);
            WriteSolutionFile(solutionPath, solution, optimalCost, fileName);
            
            Console.WriteLine($"Generated files:");
            Console.WriteLine($"  Problem:  {problemPath}");
            Console.WriteLine($"  Solution: {solutionPath}");
            Console.WriteLine($"  Optimal cost: {optimalCost}");
            Console.WriteLine($"  Category: {category}");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
    
    private static void GenerateGridProblem(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: --generate grid <rows> <cols>");
            return;
        }
        
        if (!int.TryParse(args[0], out var rows) || rows <= 0 ||
            !int.TryParse(args[1], out var cols) || cols <= 0)
        {
            Console.WriteLine("Rows and columns must be positive integers");
            return;
        }
        
        var problemType = "grid";
        var totalNodes = rows * cols;
        var category = GetSizeCategory(totalNodes);
        var fileName = $"grid_{rows}x{cols}";
        
        Console.WriteLine($"Generating {rows}x{cols} grid problem...");
        
        var tempFile = Path.GetTempFileName();
        try
        {
            // Generate the problem
            var supply = 1000L;
            ProblemGenerator.GenerateGridProblem(tempFile, rows, cols, 0, 0, rows - 1, cols - 1, supply);
            
            // Load and solve it
            var problem = _repository.LoadFromFile(tempFile, useCache: false);
            var (solution, optimalCost) = SolveProblem(problem);
            
            // Save to resources
            var resourceDir = GetResourceDirectory(problemType);
            var problemPath = Path.Combine(resourceDir, fileName + ".min");
            var solutionPath = Path.Combine(resourceDir, fileName + ".sol");
            
            File.Copy(tempFile, problemPath, overwrite: true);
            WriteSolutionFile(solutionPath, solution, optimalCost, fileName);
            
            Console.WriteLine($"Generated files:");
            Console.WriteLine($"  Problem:  {problemPath}");
            Console.WriteLine($"  Solution: {solutionPath}");
            Console.WriteLine($"  Optimal cost: {optimalCost}");
            Console.WriteLine($"  Category: {category}");
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
    
    private static string GetSizeCategory(int nodeCount)
    {
        if (nodeCount <= 100)
        {
            return "small";
        }
        else if (nodeCount <= 5000)
        {
            return "medium";
        }
        else
        {
            return "large";
        }
    }
    
    private static (int minCost, int maxCost) GetCostRange(string category)
    {
        return category switch
        {
            "small" => (1, 10),
            "medium" => (1, 100),
            "large" => (1, 1000),
            _ => (1, 100)
        };
    }
    
    private static string GetResourceDirectory(string problemType)
    {
        var baseDir = Path.GetDirectoryName(typeof(ProblemGeneratorCommand).Assembly.Location);
        var projectRoot = Path.GetFullPath(Path.Combine(baseDir!, "..", "..", "..", "..", "MinCostFlow.Problems"));
        var resourceDir = Path.Combine(projectRoot, "Resources", problemType);
        
        if (!Directory.Exists(resourceDir))
        {
            Directory.CreateDirectory(resourceDir);
        }
        
        return resourceDir;
    }
    
    private static (long[] flows, long optimalCost) SolveProblem(MinCostFlowProblem problem)
    {
        var solver = new NetworkSimplex(problem.Graph);
        
        // Set supplies
        for (int i = 0; i < problem.NodeCount; i++)
        {
            solver.SetNodeSupply(new Node(i), problem.NodeSupplies[i]);
        }
        
        // Set arc data
        for (int i = 0; i < problem.ArcCount; i++)
        {
            var arc = new Arc(i);
            solver.SetArcCost(arc, problem.ArcCosts[i]);
            solver.SetArcBounds(arc, problem.ArcLowerBounds[i], problem.ArcUpperBounds[i]);
        }
        
        var status = solver.Solve();
        if (status != SolverStatus.Optimal)
        {
            throw new InvalidOperationException($"Failed to solve problem. Status: {status}");
        }
        
        var flows = new long[problem.ArcCount];
        for (int i = 0; i < problem.ArcCount; i++)
        {
            flows[i] = solver.GetFlow(new Arc(i));
        }
        
        return (flows, solver.GetTotalCost());
    }
    
    private static void WriteSolutionFile(string path, long[] flows, long optimalCost, string problemName)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine($"c Solution file for {problemName}.min");
        writer.WriteLine("c");
        writer.WriteLine("c Optimal solution");
        writer.WriteLine($"s {optimalCost}");
        writer.WriteLine("c");
        writer.WriteLine("c Non-zero flows (SRC DST FLOW)");
        
        var problem = Path.ChangeExtension(path, ".min");
        if (File.Exists(problem))
        {
            var lines = File.ReadAllLines(problem);
            var pLine = lines.FirstOrDefault(l => l.StartsWith("p min"));
            if (pLine != null)
            {
                var parts = pLine.Split(' ');
                if (parts.Length >= 4 && int.TryParse(parts[3], out var arcCount))
                {
                    // Find arc definitions to map flows
                    var arcIndex = 0;
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("a "))
                        {
                            if (arcIndex < flows.Length && flows[arcIndex] > 0)
                            {
                                var arcParts = line.Split(' ');
                                if (arcParts.Length >= 3)
                                {
                                    writer.WriteLine($"f {arcParts[1]} {arcParts[2]} {flows[arcIndex]}");
                                }
                            }
                            arcIndex++;
                        }
                    }
                }
            }
        }
        
        writer.WriteLine("c");
        writer.WriteLine("c End of file");
    }
}