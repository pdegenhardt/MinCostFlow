# Epsilon scaling below 1 in cost scaling push-relabel algorithms

Epsilon scaling in cost scaling push-relabel algorithms presents unique challenges when epsilon approaches or drops below 1. The handling of this critical transition point determines whether the algorithm achieves exact optimality or merely approximation, with significant differences between integer and real-valued cost implementations.

## Academic foundations define precise termination criteria

The foundational papers by Goldberg and Tarjan (1990) in "Finding Minimum-Cost Circulations by Successive Approximation" established the theoretical framework for epsilon scaling. Their approach **measures solution quality by the amount that complementary slackness conditions are violated**, with epsilon representing this violation threshold. The key insight is that for integer cost networks, when epsilon < 1/n (where n is the number of vertices), an epsilon-optimal solution becomes exactly optimal because complementary slackness violations less than 1/n cannot exist with integer costs.

Ahuja, Goldberg, Orlin, and Tarjan's subsequent work on "Finding minimum-cost flows by double scaling" (1992) refined these concepts by combining multiple scaling techniques. Their algorithm achieves **O(nm(log log U) log(nC))** complexity by incorporating both capacity and cost scaling, providing better handling of small epsilon values through dual scaling parameters.

The textbook "Network Flows: Theory, Algorithms, and Applications" by Ahuja, Magnanti, and Orlin serves as the primary reference, detailing how epsilon-optimality is defined: a price function p is epsilon-optimal if for all residual arcs (i,j), the reduced cost c_p(i,j) ≥ -epsilon. The book emphasizes that **termination occurs when epsilon = 1** after appropriate cost scaling.

## Integer implementations dominate practical libraries

Real-world implementations overwhelmingly favor integer-based epsilon handling to avoid floating-point precision issues. Google's OR-Tools exemplifies this approach by **multiplying all costs by (n+1)** at initialization, setting initial epsilon to (n+1) × C (where C is the maximum absolute arc cost). The algorithm then divides epsilon by a factor α (default 5) at each iteration until epsilon reaches 1.

This design ensures that when epsilon = 1 in the scaled problem, the solution satisfies: for all residual arcs (v,w), (n+1) × c_p(v,w) ≥ -1, thus c_p(v,w) ≥ -1/(n+1) ≥ 1/n, guaranteeing optimality in the original problem. OR-Tools' extensive code documentation explicitly states: **"The algorithm terminates when epsilon > 1, and divides epsilon by alpha before each iteration."**

LEMON Library similarly focuses on integer arithmetic, noting that "These classes are intended to be used with integer-valued input data," with only CapacityScaling capable of handling real-valued costs. The library recommends NetworkSimplex for small graphs but switches to CostScaling for large sparse networks with hundreds of thousands of nodes.

## Floating-point implementations face significant challenges

NetworkX serves as a cautionary example of floating-point difficulties. The library's documentation explicitly warns: **"This algorithm is not guaranteed to work if edge weights or demands are floating point numbers"** due to overflow and roundoff errors causing infinite loops. As a workaround, NetworkX recommends multiplying all values by a constant factor (e.g., 100) to convert to integers.

Academic implementations often use fractional epsilon with termination when **epsilon < 1/N**, dividing epsilon by 2 at each iteration. This approach maintains theoretical elegance but requires careful numerical analysis to avoid precision loss when epsilon becomes very small relative to machine epsilon (approximately 2.22 × 10^-16 for double precision).

## Termination conditions vary by problem type

For **integer-cost problems**, the standard termination condition is straightforward:
- Terminate when epsilon ≤ 1 (after cost scaling)
- This guarantees exact optimality due to complementary slackness
- No numerical precision concerns with integer arithmetic

For **real-cost problems**, termination becomes more nuanced:
- Use relative tolerance: epsilon < tolerance × max_cost
- Monitor numerical stability indicators
- Implement secondary feasibility checks
- Consider switching to alternative methods when epsilon approaches machine precision

The distinction is crucial because integer problems allow clean termination with theoretical guarantees, while real-valued problems require careful handling of floating-point precision and may only achieve approximate solutions.

## Implementation patterns reveal best practices

Analysis of major libraries reveals consistent implementation patterns:

**Scaling parameters:**
- Initial epsilon: (n+1) × C for integer problems
- Division factor α: typically 2-5 (OR-Tools uses 5)
- Minimum epsilon: 1 or 1/n for termination

**Design decisions:**
- Integer scaling by (n+1) factor ensures theoretical guarantees
- Epsilon division by constant factor maintains geometric convergence
- Termination at epsilon = 1 provides clean optimality condition
- Feasibility checking prevents infinite loops

**Code structure** typically follows:
```
while (epsilon > 1) {
    refine_epsilon_optimal_flow();
    epsilon /= alpha;
}
```

## Conclusion

Epsilon scaling below 1 in cost scaling push-relabel algorithms represents a critical implementation detail that distinguishes robust production systems from theoretical prototypes. The overwhelming consensus from both academic literature and practical implementations favors **integer-based approaches with termination at epsilon = 1** after appropriate cost scaling. This design choice eliminates floating-point precision issues while maintaining theoretical optimality guarantees.

For practitioners implementing these algorithms, the key recommendations are: use integer arithmetic with cost scaling by (n+1), set α = 5 as the epsilon division factor, and terminate when epsilon reaches 1. Real-valued cost problems require additional care with relative tolerances and numerical stability monitoring. The foundational work by Goldberg, Tarjan, Ahuja, and Orlin remains the theoretical bedrock, while modern implementations like OR-Tools provide battle-tested approaches for handling epsilon scaling in practice.