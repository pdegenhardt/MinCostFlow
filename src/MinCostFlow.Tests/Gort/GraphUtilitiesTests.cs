using System;
using FluentAssertions;
using Xunit;
using MinCostFlow.Core.Gort;

namespace MinCostFlow.Tests.Gort;

public class GraphUtilitiesTests
{
    [Fact]
    public void Permute_Array_ShouldReorderCorrectly()
    {
        // Arrange
        var array = new[] { "A", "B", "C", "D" };
        var permutation = new[] { 2, 0, 3, 1 }; // A->2, B->0, C->3, D->1

        // Act
        GraphUtilities.Permute(array, permutation);

        // Assert
        array.Should().Equal("B", "D", "A", "C");
    }

    [Fact]
    public void InversePermutation_ShouldCreateCorrectInverse()
    {
        // Arrange
        var permutation = new[] { 2, 0, 3, 1 };

        // Act
        var inverse = GraphUtilities.InversePermutation(permutation);

        // Assert
        inverse.Should().Equal(1, 3, 0, 2);
        
        // Verify it's actually an inverse
        var identity = GraphUtilities.ComposePermutations(permutation, inverse);
        GraphUtilities.IsIdentityPermutation(identity).Should().BeTrue();
    }

    [Fact]
    public void Permute_WithInvalidPermutation_ShouldThrow()
    {
        // Arrange
        var array = new[] { 1, 2, 3 };
        var invalidPermutation = new[] { 0, 0, 1 }; // Duplicate value

        // Act & Assert
        Action act = () => GraphUtilities.Permute(array, invalidPermutation);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid permutation*");
    }
}