using System;
using System.IO;
using System.Threading.Tasks;
using MinCostFlow.Problems.Models;

namespace MinCostFlow.Problems.Loaders
{
    /// <summary>
    /// Loader for DIMACS format minimum cost flow problems.
    /// </summary>
    public class DimacsLoader : IProblemLoader
    {
        /// <inheritdoc/>
        public MinCostFlowProblem LoadFromFile(string filePath)
        {
            var problem = DimacsReader.ReadFromFile(filePath);
            
            // Set metadata
            if (problem.Metadata != null)
            {
                problem.Metadata.FilePath = filePath;
                problem.Metadata.Name = Path.GetFileNameWithoutExtension(filePath);
            }
            
            return problem;
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
            if (string.IsNullOrEmpty(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".min" || extension == ".dimacs" || extension == ".txt" || extension == ".tmp";
        }
    }
}