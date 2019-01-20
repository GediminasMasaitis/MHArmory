using System;
using MHArmory.Search.Default;
using MHArmory.Search.Testing;
using MHArmory.Search.Contracts;
using MHArmory.Search.MHOSEF;

namespace MHArmory
{
    public static class AvailableExtensions
    {
        public static readonly ISolverData[] SolverData = new ISolverData[]
        {
            new SolverData(),
            new TestSolverData(),
        };

        public static readonly ISolver[] Solvers = new ISolver[]
        {
            new Solver(),
            new MHOSEFSolver(),
            new TestSolver(),
        };
    }
}
