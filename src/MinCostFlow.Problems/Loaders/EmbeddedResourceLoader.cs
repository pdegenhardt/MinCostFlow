using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using MinCostFlow.Problems.Models;

namespace MinCostFlow.Problems.Loaders
{
    /// <summary>
    /// Loader for problems embedded as resources in the assembly.
    /// </summary>
    public class EmbeddedResourceLoader : IProblemLoader
    {
        private readonly Assembly _assembly;
        private readonly string _resourcePrefix;

        /// <summary>
        /// Creates a new instance of EmbeddedResourceLoader.
        /// </summary>
        /// <param name="assembly">The assembly containing the embedded resources. If null, uses the Problems assembly.</param>
        public EmbeddedResourceLoader(Assembly? assembly = null)
        {
            _assembly = assembly ?? typeof(EmbeddedResourceLoader).Assembly;
            _resourcePrefix = _assembly.GetName().Name + ".";
        }

        /// <inheritdoc/>
        public MinCostFlowProblem LoadFromFile(string filePath)
        {
            // Convert file path to resource name
            var resourceName = ConvertPathToResourceName(filePath);
            return LoadFromResource(resourceName);
        }

        /// <inheritdoc/>
        public MinCostFlowProblem LoadFromStream(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return DimacsReader.ReadFromStream(reader);
        }

        /// <inheritdoc/>
        public async Task<MinCostFlowProblem> LoadFromFileAsync(string filePath)
        {
            return await Task.Run(() => LoadFromFile(filePath)).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task<MinCostFlowProblem> LoadFromStreamAsync(Stream stream)
        {
            return await Task.Run(() => LoadFromStream(stream)).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public bool CanLoad(string filePath)
        {
            // This loader handles embedded resources
            var resourceName = ConvertPathToResourceName(filePath);
            return ResourceExists(resourceName);
        }

        /// <summary>
        /// Loads a problem from an embedded resource.
        /// </summary>
        /// <param name="resourceName">The name of the embedded resource.</param>
        /// <returns>The loaded problem.</returns>
        public MinCostFlowProblem LoadFromResource(string resourceName)
        {
            // Ensure resource name has proper prefix
            if (!resourceName.StartsWith(_resourcePrefix))
            {
                resourceName = _resourcePrefix + resourceName;
            }

            using var stream = _assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Embedded resource not found: {resourceName}");

            var problem = LoadFromStream(stream);
            
            // Set metadata
            if (problem.Metadata != null)
            {
                problem.Metadata.FilePath = resourceName;
                problem.Metadata.Source = "Embedded";
                
                // Extract name from resource path
                var parts = resourceName.Split('.');
                if (parts.Length > 2)
                {
                    var name = parts[parts.Length - 2]; // File name without extension
                    problem.Metadata.Name = name;
                    
                    // Set category based on folder
                    if (resourceName.Contains(".small."))
                        problem.Metadata.Category = "Small";
                    else if (resourceName.Contains(".medium."))
                        problem.Metadata.Category = "Medium";
                    else if (resourceName.Contains(".large."))
                        problem.Metadata.Category = "Large";
                    else if (resourceName.Contains(".dimacs."))
                        problem.Metadata.Category = "DIMACS";
                    else if (resourceName.Contains(".lemon."))
                        problem.Metadata.Category = "LEMON";
                }
            }
            
            return problem;
        }

        /// <summary>
        /// Loads a solution from an embedded resource.
        /// </summary>
        /// <param name="resourceName">The name of the solution resource.</param>
        /// <returns>The loaded solution.</returns>
        public SolutionLoader.Solution LoadSolutionFromResource(string resourceName)
        {
            // Ensure resource name has proper prefix
            if (!resourceName.StartsWith(_resourcePrefix))
            {
                resourceName = _resourcePrefix + resourceName;
            }

            using var stream = _assembly.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"Embedded solution resource not found: {resourceName}");
            
            using var reader = new StreamReader(stream);
            return SolutionLoader.LoadFromStream(reader);
        }

        /// <summary>
        /// Gets all available embedded problem resources.
        /// </summary>
        /// <returns>Array of resource names for .min files.</returns>
        public string[] GetAvailableProblems()
        {
            var resources = _assembly.GetManifestResourceNames();
            return Array.FindAll(resources, r => r.EndsWith(".min", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets all available embedded solution resources.
        /// </summary>
        /// <returns>Array of resource names for .sol files.</returns>
        public string[] GetAvailableSolutions()
        {
            var resources = _assembly.GetManifestResourceNames();
            return Array.FindAll(resources, r => r.EndsWith(".sol", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Checks if a resource exists.
        /// </summary>
        private bool ResourceExists(string resourceName)
        {
            if (!resourceName.StartsWith(_resourcePrefix))
            {
                resourceName = _resourcePrefix + resourceName;
            }
            
            var resources = _assembly.GetManifestResourceNames();
            return Array.Exists(resources, r => r.Equals(resourceName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Converts a file path to an embedded resource name.
        /// </summary>
        private string ConvertPathToResourceName(string filePath)
        {
            // Handle various path formats
            var normalized = filePath.Replace('/', '.').Replace('\\', '.');
            
            // Remove leading dots
            while (normalized.StartsWith("."))
            {
                normalized = normalized.Substring(1);
            }
            
            // If it already has the full resource name, return it
            if (normalized.StartsWith(_resourcePrefix))
            {
                return normalized;
            }
            
            // Build the resource name: AssemblyName.Resources.category.filename
            if (!normalized.StartsWith("Resources."))
            {
                normalized = "Resources." + normalized;
            }
            
            return _resourcePrefix + normalized;
        }
    }
}