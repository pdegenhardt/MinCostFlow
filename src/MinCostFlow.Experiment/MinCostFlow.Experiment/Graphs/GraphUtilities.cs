namespace MinCostFlow.Experiment.Graphs;

// Utility function to permute arrays
public static class GraphUtilities
{
    public static void Permute<T, TIndex>(IList<TIndex> permutation, IList<T> arrayToPermute)
        where TIndex : struct
    {
        if (permutation.Count == 0) return;

        var temp = new List<T>(arrayToPermute.Take(permutation.Count));
        for (int i = 0; i < permutation.Count; i++)
        {
            arrayToPermute[Convert.ToInt32(permutation[i])] = temp[i];
        }
    }
}
