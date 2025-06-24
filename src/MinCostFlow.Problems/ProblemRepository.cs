using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MinCostFlow.Problems.Generators;
using MinCostFlow.Problems.Loaders;
using MinCostFlow.Problems.Models;

namespace MinCostFlow.Problems
{
    /// <summary>
    /// Central repository for accessing minimum cost flow problems.
    /// </summary>
    public class ProblemRepository
    {
        private readonly Dictionary<string, MinCostFlowProblem> _cache = new();
        private readonly List<IProblemLoader> _loaders = new();
        private readonly object _cacheLock = new();

        /// <summary>
        /// Creates a new instance of ProblemRepository.
        /// </summary>
        public ProblemRepository()
        {
            // Register default loaders
            RegisterLoader(new DimacsLoader());
            RegisterLoader(new EmbeddedResourceLoader());
        }

        /// <summary>
        /// Registers a problem loader.
        /// </summary>
        public void RegisterLoader(IProblemLoader loader)
        {
            _loaders.Add(loader);
        }

        /// <summary>
        /// Loads a problem from a file.
        /// </summary>
        public MinCostFlowProblem LoadFromFile(string filePath, bool useCache = true)
        {
            if (useCache)
            {
                lock (_cacheLock)
                {
                    if (_cache.TryGetValue(filePath, out var cached))
                        return cached;
                }
            }

            var loader = _loaders.FirstOrDefault(l => l.CanLoad(filePath))
                ?? throw new NotSupportedException($"No loader found for file: {filePath}");

            var problem = loader.LoadFromFile(filePath);

            if (useCache)
            {
                lock (_cacheLock)
                {
                    _cache[filePath] = problem;
                }
            }

            return problem;
        }

        /// <summary>
        /// Loads a problem from a stream.
        /// </summary>
        public MinCostFlowProblem LoadFromStream(Stream stream, string format = "dimacs")
        {
            var loader = format.ToLowerInvariant() switch
            {
                "dimacs" => new DimacsLoader(),
                _ => throw new NotSupportedException($"Unsupported format: {format}")
            };

            return loader.LoadFromStream(stream);
        }

        /// <summary>
        /// Asynchronously loads a problem from a file.
        /// </summary>
        public async Task<MinCostFlowProblem> LoadFromFileAsync(string filePath, bool useCache = true)
        {
            if (useCache)
            {
                lock (_cacheLock)
                {
                    if (_cache.TryGetValue(filePath, out var cached))
                        return cached;
                }
            }

            var loader = _loaders.FirstOrDefault(l => l.CanLoad(filePath))
                ?? throw new NotSupportedException($"No loader found for file: {filePath}");

            var problem = await loader.LoadFromFileAsync(filePath).ConfigureAwait(false);

            if (useCache)
            {
                lock (_cacheLock)
                {
                    _cache[filePath] = problem;
                }
            }

            return problem;
        }

        /// <summary>
        /// Loads all problems from a directory.
        /// </summary>
        public List<MinCostFlowProblem> LoadFromDirectory(string directoryPath, string searchPattern = "*.*", bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(directoryPath, searchPattern, searchOption);
            var problems = new List<MinCostFlowProblem>();

            foreach (var file in files)
            {
                if (_loaders.Any(l => l.CanLoad(file)))
                {
                    try
                    {
                        var problem = LoadFromFile(file);
                        problems.Add(problem);
                    }
                    catch (Exception ex)
                    {
                        // Log or handle error
                        Console.WriteLine($"Failed to load {file}: {ex.Message}");
                    }
                }
            }

            return problems;
        }

        /// <summary>
        /// Gets problems by category.
        /// </summary>
        public List<MinCostFlowProblem> GetByCategory(string category)
        {
            lock (_cacheLock)
            {
                return _cache.Values
                    .Where(p => p.Metadata?.Category?.Equals(category, StringComparison.OrdinalIgnoreCase) == true)
                    .ToList();
            }
        }

        /// <summary>
        /// Gets problems by size range.
        /// </summary>
        public List<MinCostFlowProblem> GetBySize(int minNodes = 0, int maxNodes = int.MaxValue, 
            int minArcs = 0, int maxArcs = int.MaxValue)
        {
            lock (_cacheLock)
            {
                return _cache.Values
                    .Where(p => p.NodeCount >= minNodes && p.NodeCount <= maxNodes &&
                               p.ArcCount >= minArcs && p.ArcCount <= maxArcs)
                    .ToList();
            }
        }

        /// <summary>
        /// Clears the problem cache.
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// Loads a problem from an embedded resource.
        /// </summary>
        /// <param name="resourcePath">The resource path (e.g., "small/path_5node.min").</param>
        /// <param name="useCache">Whether to cache the loaded problem.</param>
        /// <returns>The loaded problem.</returns>
        public MinCostFlowProblem LoadEmbeddedProblem(string resourcePath, bool useCache = true)
        {
            var cacheKey = "embedded:" + resourcePath;
            
            if (useCache)
            {
                lock (_cacheLock)
                {
                    if (_cache.TryGetValue(cacheKey, out var cached))
                        return cached;
                }
            }

            var embeddedLoader = new EmbeddedResourceLoader();
            var problem = embeddedLoader.LoadFromFile(resourcePath);

            if (useCache)
            {
                lock (_cacheLock)
                {
                    _cache[cacheKey] = problem;
                }
            }

            return problem;
        }

        /// <summary>
        /// Gets all available embedded problems.
        /// </summary>
        /// <returns>Dictionary of problem names to resource paths.</returns>
        public Dictionary<string, string> GetAvailableEmbeddedProblems()
        {
            var embeddedLoader = new EmbeddedResourceLoader();
            var resources = embeddedLoader.GetAvailableProblems();
            var problems = new Dictionary<string, string>();

            foreach (var resource in resources)
            {
                // Extract a friendly name from the resource path
                var parts = resource.Split('.');
                if (parts.Length >= 3)
                {
                    var category = parts[parts.Length - 3]; // e.g., "small", "dimacs"
                    var name = parts[parts.Length - 2];     // e.g., "path_5node"
                    var key = $"{category}/{name}";
                    problems[key] = resource;
                }
            }

            return problems;
        }

        /// <summary>
        /// Generates a standard transportation problem.
        /// </summary>
        public MinCostFlowProblem GenerateTransportationProblem(int sources, int sinks, long supply, 
            int minCost = 1, int maxCost = 100)
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                ProblemGenerator.GenerateTransportationProblem(tempFile, sources, sinks, supply, minCost, maxCost);
                var problem = LoadFromFile(tempFile, useCache: false);
                
                if (problem.Metadata != null)
                {
                    problem.Metadata.Name = $"Transport_{sources}x{sinks}";
                    problem.Metadata.Category = "Transportation";
                    problem.Metadata.Source = "Generated";
                }
                
                return problem;
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Generates a circulation problem.
        /// </summary>
        public MinCostFlowProblem GenerateCirculationProblem(int nodes, double density, 
            int minCost = -50, int maxCost = 100)
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                ProblemGenerator.GenerateCirculationProblem(tempFile, nodes, density, minCost, maxCost);
                var problem = LoadFromFile(tempFile, useCache: false);
                
                if (problem.Metadata != null)
                {
                    problem.Metadata.Name = $"Circulation_{nodes}_{density:F2}";
                    problem.Metadata.Category = "Circulation";
                    problem.Metadata.Source = "Generated";
                }
                
                return problem;
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Generates a grid network problem.
        /// </summary>
        public MinCostFlowProblem GenerateGridProblem(int rows, int cols, 
            int sourceRow, int sourceCol, int sinkRow, int sinkCol, long supply)
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                ProblemGenerator.GenerateGridProblem(tempFile, rows, cols, sourceRow, sourceCol, sinkRow, sinkCol, supply);
                var problem = LoadFromFile(tempFile, useCache: false);
                
                if (problem.Metadata != null)
                {
                    problem.Metadata.Name = $"Grid_{rows}x{cols}";
                    problem.Metadata.Category = "Grid";
                    problem.Metadata.Source = "Generated";
                }
                
                return problem;
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }

        /// <summary>
        /// Generates a simple path problem.
        /// </summary>
        public MinCostFlowProblem GeneratePathProblem(int nodes, long supply)
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                ProblemGenerator.GeneratePathProblem(tempFile, nodes, supply);
                var problem = LoadFromFile(tempFile, useCache: false);
                
                if (problem.Metadata != null)
                {
                    problem.Metadata.Name = $"Path_{nodes}";
                    problem.Metadata.Category = "Path";
                    problem.Metadata.Source = "Generated";
                }
                
                return problem;
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
    }
}