using System;
using System.Collections.Generic;

namespace MinCostFlow.Core.Gort;

/// <summary>
/// Utility functions for graph operations.
/// </summary>
public static class GraphUtilities
{
    /// <summary>
    /// Reorders array elements according to a permutation.
    /// Element at position i moves to position permutation[i].
    /// </summary>
    /// <typeparam name="T">Type of array elements</typeparam>
    /// <param name="array">Array to be permuted</param>
    /// <param name="permutation">Permutation array where element i moves to position permutation[i]</param>
    public static void Permute<T>(T[] array, int[] permutation)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));
        if (permutation == null)
            throw new ArgumentNullException(nameof(permutation));
        if (array.Length != permutation.Length)
            throw new ArgumentException("Array and permutation must have the same length");
        
        // Validate permutation
        ValidatePermutation(permutation);
        
        // Create a copy of the original array
        T[] temp = new T[array.Length];
        Array.Copy(array, temp, array.Length);
        
        // Apply permutation
        for (int i = 0; i < array.Length; i++)
        {
            array[permutation[i]] = temp[i];
        }
    }
    
    /// <summary>
    /// Reorders list elements according to a permutation.
    /// Element at position i moves to position permutation[i].
    /// </summary>
    public static void Permute<T>(IList<T> list, int[] permutation)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));
        if (permutation == null)
            throw new ArgumentNullException(nameof(permutation));
        if (list.Count != permutation.Length)
            throw new ArgumentException("List and permutation must have the same length");
        
        // Validate permutation
        ValidatePermutation(permutation);
        
        // Create a copy of the original list
        T[] temp = new T[list.Count];
        for (int i = 0; i < list.Count; i++)
        {
            temp[i] = list[i];
        }
        
        // Apply permutation
        for (int i = 0; i < list.Count; i++)
        {
            list[permutation[i]] = temp[i];
        }
    }
    
    /// <summary>
    /// Creates the inverse of a permutation.
    /// If permutation[i] = j, then inverse[j] = i.
    /// </summary>
    public static int[] InversePermutation(int[] permutation)
    {
        if (permutation == null)
            throw new ArgumentNullException(nameof(permutation));
        
        ValidatePermutation(permutation);
        
        int[] inverse = new int[permutation.Length];
        for (int i = 0; i < permutation.Length; i++)
        {
            inverse[permutation[i]] = i;
        }
        
        return inverse;
    }
    
    /// <summary>
    /// Composes two permutations: result[i] = second[first[i]].
    /// </summary>
    public static int[] ComposePermutations(int[] first, int[] second)
    {
        if (first == null)
            throw new ArgumentNullException(nameof(first));
        if (second == null)
            throw new ArgumentNullException(nameof(second));
        if (first.Length != second.Length)
            throw new ArgumentException("Permutations must have the same length");
        
        ValidatePermutation(first);
        ValidatePermutation(second);
        
        int[] result = new int[first.Length];
        for (int i = 0; i < first.Length; i++)
        {
            result[i] = second[first[i]];
        }
        
        return result;
    }
    
    /// <summary>
    /// Checks if a permutation is the identity permutation.
    /// </summary>
    public static bool IsIdentityPermutation(int[] permutation)
    {
        if (permutation == null)
            throw new ArgumentNullException(nameof(permutation));
        
        for (int i = 0; i < permutation.Length; i++)
        {
            if (permutation[i] != i)
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Validates that an array represents a valid permutation.
    /// </summary>
    private static void ValidatePermutation(int[] permutation)
    {
        bool[] seen = new bool[permutation.Length];
        
        for (int i = 0; i < permutation.Length; i++)
        {
            int value = permutation[i];
            
            if (value < 0 || value >= permutation.Length)
            {
                throw new ArgumentException(
                    $"Invalid permutation: value {value} at index {i} is out of range [0, {permutation.Length})");
            }
            
            if (seen[value])
            {
                throw new ArgumentException(
                    $"Invalid permutation: value {value} appears multiple times");
            }
            
            seen[value] = true;
        }
    }
}