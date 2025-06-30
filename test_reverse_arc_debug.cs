using System;
using System.Collections.Generic;

// Minimal reproduction of the ReverseArcListGraph issue
public class TestReverseArc
{
    public static void Main()
    {
        // Test the GetNextIndex logic
        Console.WriteLine("Testing GetNextIndex logic:");
        
        // When adding arc 3 (1->3)
        int arc = 3;
        int reverseArc = ~arc;  // ~3 = -4
        Console.WriteLine($"Arc {arc}: reverseArc = ~{arc} = {reverseArc}");
        
        // Initial state: _arcCapacity = 0 (no arcs yet)
        // After adding arcs 1,2: _arcCapacity = 2
        // When adding arc 3: _arcCapacity increases to 3
        
        Console.WriteLine("\nScenario: Adding arc 3 when _arcCapacity = 2");
        int arcCapacity = 2;
        
        // GetNextIndex calculation
        // For negative arc -4: index = _arcCapacity - arc = 2 - (-4) = 2 + 4 = 6
        int nextIndex = arcCapacity - reverseArc;
        Console.WriteLine($"GetNextIndex({reverseArc}) = {arcCapacity} - ({reverseArc}) = {nextIndex}");
        
        // But array was sized for: 2 * arcCapacity + 2 = 2 * 2 + 2 = 6
        int arraySize = 2 * arcCapacity + 2;
        Console.WriteLine($"Array size = 2 * {arcCapacity} + 2 = {arraySize}");
        Console.WriteLine($"Index {nextIndex} is {(nextIndex < arraySize ? "VALID" : "OUT OF BOUNDS")}!");
        
        Console.WriteLine("\nAfter ReserveArcs increases capacity to 3:");
        arcCapacity = 3;
        nextIndex = arcCapacity - reverseArc;
        arraySize = 2 * arcCapacity + 2;
        Console.WriteLine($"GetNextIndex({reverseArc}) = {arcCapacity} - ({reverseArc}) = {nextIndex}");
        Console.WriteLine($"Array size = 2 * {arcCapacity} + 2 = {arraySize}");
        Console.WriteLine($"Index {nextIndex} is {(nextIndex < arraySize ? "VALID" : "OUT OF BOUNDS")}!");
    }
}