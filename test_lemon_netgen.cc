#include <iostream>
#include <fstream>
#include <lemon/list_graph.h>
#include <lemon/network_simplex.h>
#include <lemon/dimacs.h>

using namespace lemon;
using namespace std;

int main(int argc, char* argv[]) {
    if (argc < 2) {
        cerr << "Usage: " << argv[0] << " <dimacs_file>" << endl;
        return 1;
    }

    // Read DIMACS file
    ListDigraph g;
    ListDigraph::ArcMap<int> lower(g);
    ListDigraph::ArcMap<int> upper(g);
    ListDigraph::ArcMap<int> cost(g);
    ListDigraph::NodeMap<int> supply(g);
    
    ifstream input(argv[1]);
    if (!input) {
        cerr << "Cannot open file: " << argv[1] << endl;
        return 1;
    }
    
    readDimacsMin(input, g, lower, upper, cost, supply);
    input.close();
    
    // Create and run network simplex
    NetworkSimplex<ListDigraph> ns(g);
    ns.lowerMap(lower)
      .upperMap(upper)
      .costMap(cost)
      .supplyMap(supply);
    
    NetworkSimplex<ListDigraph>::ProblemType status = ns.run();
    
    if (status == NetworkSimplex<ListDigraph>::OPTIMAL) {
        cout << "Optimal solution found!" << endl;
        cout << "Total cost: " << ns.totalCost() << endl;
    } else if (status == NetworkSimplex<ListDigraph>::INFEASIBLE) {
        cout << "Problem is infeasible" << endl;
    } else if (status == NetworkSimplex<ListDigraph>::UNBOUNDED) {
        cout << "Problem is unbounded" << endl;
    }
    
    return 0;
}