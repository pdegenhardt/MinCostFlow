using System.IO;
using System.Threading.Tasks;
using MinCostFlow.Problems.Models;

namespace MinCostFlow.Problems.Loaders;

/// <summary>
/// Interface for problem loaders.
/// </summary>
public interface IProblemLoader
{
    /// <summary>
    /// Loads a problem from a file.
    /// </summary>
    /// <param name="filePath">Path to the problem file.</param>
    /// <returns>The loaded problem.</returns>
    MinCostFlowProblem LoadFromFile(string filePath);

    /// <summary>
    /// Loads a problem from a stream.
    /// </summary>
    /// <param name="stream">Stream containing the problem data.</param>
    /// <returns>The loaded problem.</returns>
    MinCostFlowProblem LoadFromStream(Stream stream);

    /// <summary>
    /// Asynchronously loads a problem from a file.
    /// </summary>
    /// <param name="filePath">Path to the problem file.</param>
    /// <returns>The loaded problem.</returns>
    Task<MinCostFlowProblem> LoadFromFileAsync(string filePath);

    /// <summary>
    /// Asynchronously loads a problem from a stream.
    /// </summary>
    /// <param name="stream">Stream containing the problem data.</param>
    /// <returns>The loaded problem.</returns>
    Task<MinCostFlowProblem> LoadFromStreamAsync(Stream stream);

    /// <summary>
    /// Determines if this loader can handle the given file.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <returns>True if the loader can handle the file, false otherwise.</returns>
    bool CanLoad(string filePath);
}