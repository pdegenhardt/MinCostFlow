using System.Linq;
using FluentAssertions;
using MinCostFlow.Problems;
using MinCostFlow.Problems.Loaders;
using MinCostFlow.Problems.Sets;
using Xunit;

namespace MinCostFlow.Tests
{
    /// <summary>
    /// Tests for embedded resource functionality.
    /// </summary>
    public class EmbeddedResourceTests
    {
        [Fact]
        public void EmbeddedResourceLoader_Should_FindAllEmbeddedProblems()
        {
            // Arrange
            var loader = new EmbeddedResourceLoader();

            // Act
            var problems = loader.GetAvailableProblems();
            var solutions = loader.GetAvailableSolutions();

            // Assert
            problems.Should().NotBeEmpty();
            solutions.Should().NotBeEmpty();
            
            // Should have problems from different categories
            problems.Any(p => p.Contains("small")).Should().BeTrue();
            problems.Any(p => p.Contains("dimacs")).Should().BeTrue();
            problems.Any(p => p.Contains("lemon")).Should().BeTrue();
        }

        [Fact]
        public void StandardProblems_Small_Should_LoadAllProblems()
        {
            // Act & Assert - each property access loads the problem
            var path5 = StandardProblems.Small.Path5Node;
            path5.Should().NotBeNull();
            path5.NodeCount.Should().Be(5);
            path5.ArcCount.Should().Be(4);
            path5.Metadata.Should().NotBeNull();
            path5.Metadata!.Category.Should().Be("Small");
            path5.Metadata.Source.Should().Be("Embedded");

            var diamond = StandardProblems.Small.DiamondGraph;
            diamond.Should().NotBeNull();
            diamond.NodeCount.Should().BeGreaterThan(0);

            // Test that All() returns all problems
            var allSmall = StandardProblems.Small.All().ToList();
            allSmall.Should().HaveCount(9);
            allSmall.Should().AllSatisfy(p => p.Should().NotBeNull());
        }

        [Fact]
        public void StandardProblems_Dimacs_Should_LoadNetgenProblems()
        {
            // Act
            var netgen = StandardProblems.Dimacs.Netgen8_08a;

            // Assert
            netgen.Should().NotBeNull();
            netgen.NodeCount.Should().BeGreaterThan(0);
            netgen.ArcCount.Should().BeGreaterThan(0);
            netgen.Metadata!.Category.Should().Be("DIMACS");
        }

        [Fact]
        public void StandardProblems_Solutions_Should_LoadSolutionFiles()
        {
            // Act
            var solution = StandardProblems.Solutions.GetSolution("small.path_5node");

            // Assert
            solution.Should().NotBeNull();
            solution!.OptimalCost.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ProblemRepository_Should_LoadEmbeddedProblems()
        {
            // Arrange
            var repo = new ProblemRepository();

            // Act
            var problem = repo.LoadEmbeddedProblem("small/diamond_graph.min");

            // Assert
            problem.Should().NotBeNull();
            problem.Metadata!.Name.Should().Be("diamond_graph");
            problem.Metadata.Category.Should().Be("Small");
        }

        [Fact]
        public void ProblemRepository_Should_CacheEmbeddedProblems()
        {
            // Arrange
            var repo = new ProblemRepository();

            // Act
            var problem1 = repo.LoadEmbeddedProblem("small/simple_4node.min");
            var problem2 = repo.LoadEmbeddedProblem("small/simple_4node.min");

            // Assert
            problem1.Should().BeSameAs(problem2); // Same instance due to caching
        }

        [Fact]
        public void EmbeddedResourceLoader_Should_ConvertPathsCorrectly()
        {
            // Arrange
            var loader = new EmbeddedResourceLoader();

            // Act & Assert - various path formats should work
            loader.CanLoad("small/path_5node.min").Should().BeTrue();
            loader.CanLoad("Resources/small/path_5node.min").Should().BeTrue();
            loader.CanLoad("Resources.small.path_5node.min").Should().BeTrue();
        }

        [Theory]
        [InlineData("small/assignment_3x3.min", 6, 9)] // 3 sources + 3 sinks = 6 nodes, 3x3 = 9 arcs
        [InlineData("small/path_10node.min", 10, 9)]   // 10 nodes, 9 arcs in a path
        [InlineData("small/grid_2x2.min", 4, 4)]       // 2x2 = 4 nodes, edges depend on structure
        public void EmbeddedProblems_Should_HaveExpectedStructure(string resourcePath, int expectedNodes, int minArcs)
        {
            // Arrange
            var repo = new ProblemRepository();

            // Act
            var problem = repo.LoadEmbeddedProblem(resourcePath);

            // Assert
            problem.NodeCount.Should().Be(expectedNodes);
            problem.ArcCount.Should().BeGreaterThanOrEqualTo(minArcs);
            problem.Validate().Should().BeTrue("problem should be valid");
        }

        [Fact]
        public void StandardProblems_Should_LoadOptimalCostsFromSolutions()
        {
            // Arrange & Act
            var path5 = StandardProblems.Small.Path5Node;

            // Assert - the StandardProblems loader should set OptimalCost from solution files
            path5.Metadata!.OptimalCost.Should().NotBeNull();
            path5.Metadata.OptimalCost.Should().BeGreaterThan(0);
        }
    }
}