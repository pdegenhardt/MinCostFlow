using FluentAssertions;
using MinCostFlow.Core.Types;
using Xunit;

namespace MinCostFlow.Tests;

public class TypeTests
{
    [Fact]
    public void Node_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var node1 = new Node(5);
        var node2 = new Node(5);
        var node3 = new Node(7);
        
        // Assert
        node1.Should().Be(node2);
        node1.Should().NotBe(node3);
        (node1 == node2).Should().BeTrue();
        (node1 != node3).Should().BeTrue();
    }
    
    [Fact]
    public void Node_Invalid_ShouldHaveNegativeId()
    {
        // Assert
        Node.Invalid.Id.Should().Be(-1);
        Node.Invalid.IsValid.Should().BeFalse();
        new Node(0).IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void Node_Comparison_ShouldWorkCorrectly()
    {
        // Arrange
        var node1 = new Node(3);
        var node2 = new Node(5);
        var node3 = new Node(5);
        
        // Assert
        (node1 < node2).Should().BeTrue();
        (node2 > node1).Should().BeTrue();
        (node2 <= node3).Should().BeTrue();
        (node2 >= node3).Should().BeTrue();
    }
    
    [Fact]
    public void Arc_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var arc1 = new Arc(10);
        var arc2 = new Arc(10);
        var arc3 = new Arc(15);
        
        // Assert
        arc1.Should().Be(arc2);
        arc1.Should().NotBe(arc3);
        (arc1 == arc2).Should().BeTrue();
        (arc1 != arc3).Should().BeTrue();
    }
    
    [Fact]
    public void Arc_Invalid_ShouldHaveNegativeId()
    {
        // Assert
        Arc.Invalid.Id.Should().Be(-1);
        Arc.Invalid.IsValid.Should().BeFalse();
        new Arc(0).IsValid.Should().BeTrue();
    }
    
    [Fact]
    public void Arc_Comparison_ShouldWorkCorrectly()
    {
        // Arrange
        var arc1 = new Arc(8);
        var arc2 = new Arc(12);
        var arc3 = new Arc(12);
        
        // Assert
        (arc1 < arc2).Should().BeTrue();
        (arc2 > arc1).Should().BeTrue();
        (arc2 <= arc3).Should().BeTrue();
        (arc2 >= arc3).Should().BeTrue();
    }
    
    [Fact]
    public void Node_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var validNode = new Node(42);
        var invalidNode = Node.Invalid;
        
        // Assert
        validNode.ToString().Should().Be("Node(42)");
        invalidNode.ToString().Should().Be("Node(Invalid)");
    }
    
    [Fact]
    public void Arc_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var validArc = new Arc(99);
        var invalidArc = Arc.Invalid;
        
        // Assert
        validArc.ToString().Should().Be("Arc(99)");
        invalidArc.ToString().Should().Be("Arc(Invalid)");
    }
}