using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaivePlanner
{
    public class Solvers
    {
        static Random rnd = new Random();
        static public Assignment WalkSat(Cnf cnf, double p = 0.2, int maxFlips = 1000, int maxTries = 100000, bool verbose = true)
        {
            if (verbose)
            {
                Console.WriteLine($"Performing WalkSat with {nameof(p)} = {p}, {nameof(maxFlips)} = {maxFlips}, and  {nameof(maxTries)} = {maxTries}.\r\n");
            }

            int numVars = cnf.Variables.Count;
            int numClauses = cnf.Clauses.Count;
            for (int restart = 0; restart < maxTries; restart++)
            {
                var currentAssignment = new Assignment(numVars, true);
                if (cnf.CheckAssignment(currentAssignment, out var unsatisfiedClauses))
                {
                    if (verbose)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Found satisfying assignment before any flips after {restart} restarts");
                    }

                    return currentAssignment;
                }
                var currentUnsatisfied = unsatisfiedClauses.ToList();

                for (int flip = 0; flip < maxFlips; flip++)
                {
                    var clause = currentUnsatisfied[rnd.Next(currentUnsatisfied.Count)];
                    var currentSatisfied = new SortedSet<int>(Enumerable.Range(0, numClauses).Except(currentUnsatisfied));
                    var canidates = new Tuple<Assignment, int, List<int>>[cnf.Clauses[clause].Count];
                    int k = 0;
                    foreach (var v in cnf.Clauses[clause].Select(l => l < 0 ? -l : l))
                    {
                        var newAssignment = currentAssignment.Copy();
                        newAssignment[v - 1] = !currentAssignment[v - 1];

                        if (cnf.CheckAssignment(newAssignment, out var uClauses))
                        {
                            if (verbose)
                            {
                                Console.WriteLine();
                                Console.WriteLine($"Found satisfying assignment in {flip+1} flips after {restart} restarts");
                            }

                            return newAssignment;
                        }

                        var breakSet = currentSatisfied.Intersect(uClauses).ToList();
                        var breakCount = breakSet.Count;
                        canidates[k] = new Tuple<Assignment, int, List<int>>(newAssignment, breakCount, uClauses.ToList());
                        k++;
                    }

                    var freeFlips = canidates.Where(c => c.Item2 == 0).ToList();
                    if (freeFlips.Count > 0)
                    {
                        var f = freeFlips[rnd.Next(freeFlips.Count)];

                        currentAssignment = f.Item1;
                        currentUnsatisfied = f.Item3;
                        continue;
                    }
                    else
                    {
                        var n = rnd.NextDouble();
                        if (n <= p)
                        {
                            var f = canidates[rnd.Next(canidates.Length)];

                            currentAssignment = f.Item1;
                            currentUnsatisfied = f.Item3;
                            continue;
                        }
                        else
                        {
                            Array.Sort(canidates, (a, b) =>
                            {
                                if (a.Item2 < b.Item2)
                                {
                                    return -1;
                                }
                                else if (a.Item2 == b.Item2)
                                {
                                    return 0;
                                }
                                return 1;
                            });

                            var f = canidates[0];

                            currentAssignment = f.Item1;
                            currentUnsatisfied = f.Item3;
                            continue;
                        }
                    }
                }
                Console.Write('.');
            }

            if (verbose)
            {
                Console.WriteLine();
                Console.WriteLine($"No satisfying assignment found in {maxTries + 1} searches with {maxFlips} flips in each search");
                Console.WriteLine($"This does not necessarily mean the problem is unsatisfable as WalkSat is not a complete search");
            }

            return null;
        }


        static public Assignment GSat(Cnf cnf, double p = 0.5, int maxFlips = 1000, int maxTries = 10000, bool verbose = true)
        {
            int numVars = cnf.Variables.Count;
            int numClauses = cnf.Clauses.Count;
            if (verbose)
            {
                Console.WriteLine($"Performing GSat with {nameof(p)} = {p}, {nameof(maxFlips)} = {maxFlips}, and  {nameof(maxTries)} = {maxTries}.\r\n");
            }

            for (int restart = 0; restart < maxTries; restart++)
            {
                var currentAssignment = new Assignment(numVars, true);
                if (cnf.CheckAssignment(currentAssignment, out var unsatisfiedClauses))
                {
                    if (verbose)
                    {
                        Console.WriteLine($"Found satisfying assignment after 0 flips on run {restart + 1}");
                    }
                    return currentAssignment;
                }
                var currentUnsatisfied = unsatisfiedClauses.ToList();

                var canidates = new Tuple<Assignment, List<int>>[cnf.Variables.Count];

                for (int flip = 0; flip < maxFlips; flip++)
                {
                    //var currentSatisfied = new SortedSet<int>(Enumerable.Range(0, numClauses).Except(currentUnsatisfied));
                    int k = 0;
                    foreach (var v in cnf.Variables)
                    {
                        var newAssignment = currentAssignment.Copy();
                        newAssignment[v - 1] = !currentAssignment[v - 1];

                        if (cnf.CheckAssignment(newAssignment, out var uClauses))
                        {
                            if (verbose)
                            {
                                Console.WriteLine($"Found satisfying assignment after {flip} flips on run {restart + 1}");
                            }

                            return newAssignment;
                        }
                        canidates[k] = new Tuple<Assignment, List<int>>(newAssignment, uClauses.ToList());
                        k++;
                    }


                    var n = rnd.NextDouble();
                    if (n <= p)
                    {
                        var c = currentUnsatisfied[rnd.Next(currentUnsatisfied.Count)];
                        var v = cnf.Clauses[c].ToList()[rnd.Next(cnf.Clauses[c].Count)];
                        if (v < 0) v = -v;

                        var newAssignment = currentAssignment.Copy();
                        newAssignment[v - 1] = !currentAssignment[v - 1];

                        if (cnf.CheckAssignment(newAssignment, out var uClauses))
                        {
                            if (verbose)
                            {
                                Console.WriteLine($"Found satisfying assignment after {flip} flips on run {restart + 1}");
                            }

                            return newAssignment;
                        }
                        currentUnsatisfied = uClauses.ToList();
                        currentAssignment = newAssignment;
                        continue;
                    }
                    else
                    {
                        Array.Sort(canidates, (a, b) =>
                        {
                            if (a.Item2.Count < b.Item2.Count)
                            {
                                return -1;
                            }
                            else if (a.Item2.Count == b.Item2.Count)
                            {
                                return 0;
                            }
                            return 1;
                        });

                        var lowest = canidates.Where(can => can.Item2.Count == canidates[0].Item2.Count).ToList();
                        var r = rnd.Next(lowest.Count);
                        var f = canidates[r];

                        currentAssignment = f.Item1;
                        currentUnsatisfied = f.Item2;
                        continue;
                    }

                }
            }

            if (verbose)
            {
                Console.WriteLine($"No satisfying assignment found in {maxTries + 1} searches with {maxFlips} flips in each search");
                Console.WriteLine($"This does not necessarily mean the problem is unsatisfable as GSat is not a complete search");
            }

            return null;
        }

    }

}
