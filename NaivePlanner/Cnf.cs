using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace NaivePlanner
{

    public class Cnf
    {
        public Cnf()
        {

        }

        public Cnf(IEnumerable<IEnumerable<int>> clauses)
        {
            foreach (var clause in clauses)
            {
                AddClause(clause);
            }
            assignment = new bool?[Variables.Count];
        }

        private List<SortedSet<int>> clauses = new List<SortedSet<int>>();
        public IReadOnlyList<SortedSet<int>> Clauses => clauses;
        private bool?[] assignment = null;
        private SortedSet<int> unitClauses = new SortedSet<int>();
        private SortedSet<int> emptyClauses = new SortedSet<int>();
        private SortedSet<int> pureLiterals = new SortedSet<int>();
        private SortedSet<int> satisfiedClauses = new SortedSet<int>();

        private SortedDictionary<int, SortedSet<int>> literalsToClauses = new SortedDictionary<int, SortedSet<int>>();
        public IReadOnlyDictionary<int, SortedSet<int>> ClausesWithLiteral => literalsToClauses;

        private SortedDictionary<int, SortedSet<int>> variablesToClauses = new SortedDictionary<int, SortedSet<int>>();
        public IReadOnlyDictionary<int, SortedSet<int>> ClausesWithVariable => variablesToClauses;

        public ICollection<int> Variables => variablesToClauses.Keys;
        public ICollection<int> Literals => literalsToClauses.Keys;
        public ICollection<int> UnitClauses => unitClauses;
        public ICollection<int> EmptyClauses => emptyClauses;
        public ICollection<int> PureLiterals => pureLiterals;
        public ICollection<int> SatisfiedClauses => satisfiedClauses;
        public bool?[] Assingnment { get => assignment; }

        private void AddClause(IEnumerable<int> literals)
        {
            var clause = new SortedSet<int>();
            foreach (var l in literals)
            {
                if (clause.Add(l))
                {
                    if (clause.Count == 1)
                    {
                        unitClauses.Add(clauses.Count);
                    }
                    else
                    {
                        unitClauses.Remove(clauses.Count);
                    }


                    if (!literalsToClauses.ContainsKey(-l))
                    {
                        pureLiterals.Add(l);
                    }
                    else
                    {
                        pureLiterals.Remove(-l);
                    }


                    if (!literalsToClauses.TryGetValue(l, out var clausesWithLiteral))
                    {
                        clausesWithLiteral = new SortedSet<int>();
                        literalsToClauses[l] = clausesWithLiteral;
                    }
                    clausesWithLiteral.Add(clauses.Count);



                    var v = (l < 0) ? -l : l;
                    if (!variablesToClauses.TryGetValue(v, out var clausesWithVariable))
                    {
                        clausesWithVariable = new SortedSet<int>();
                        variablesToClauses[v] = clausesWithVariable;
                    }
                    clausesWithVariable.Add(clauses.Count);


                }
            }
            clauses.Add(clause);
        }

        private void RemoveLiteralFromClauses(int l)
        {
            if (literalsToClauses.TryGetValue(l, out var clausesWithLiteral))
            {

                foreach (var c in clausesWithLiteral)
                {
                    clauses[c].Remove(l);
                    if (clauses[c].Count == 1)
                    {
                        unitClauses.Add(c);
                    }
                    else if (clauses[c].Count == 0)
                    {
                        unitClauses.Remove(c);
                        emptyClauses.Add(c);
                    }

                    variablesToClauses[l < 0 ? -l : l].Remove(c);
                }

                literalsToClauses.Remove(l);
                pureLiterals.Remove(l);
                if (literalsToClauses.ContainsKey(-l))
                {
                    pureLiterals.Add(-l);
                }

                if (variablesToClauses[l < 0 ? -l : l].Count == 0)
                {
                    variablesToClauses.Remove(l < 0 ? -l : l);
                }
            }


        }

        private void RemoveClause(int index)
        {
            if (index < 0 || index > Clauses.Count)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }

            var c = clauses[index];
            foreach (var l in c)
            {
                if (literalsToClauses.TryGetValue(l, out var clausesWithL))
                {
                    clausesWithL.Remove(index);
                    if (clausesWithL.Count == 0)
                    {
                        literalsToClauses.Remove(l);
                        pureLiterals.Remove(l);

                        if (literalsToClauses.ContainsKey(-l))
                        {
                            pureLiterals.Add(-l);
                        }
                    }
                }

                var v = (l < 0) ? -l : l;
                if (variablesToClauses.TryGetValue(v, out var clausesWithV))
                {
                    clausesWithV.Remove(index);
                    if (clausesWithV.Count == 0)
                    {
                        variablesToClauses.Remove(v);
                    }
                }
            }
            unitClauses.Remove(index);
            emptyClauses.Remove(index);
            clauses[index] = null;
            satisfiedClauses.Add(index);
        }

        private void AddLiteralToClause(int clause, int literal)
        {
            if (clauses[clause].Add(literal))
            {
                emptyClauses.Remove(clause);
                if (clauses[clause].Count == 1)
                {
                    unitClauses.Add(clause);
                }
                else
                {
                    unitClauses.Remove(clause);
                }


                if (!literalsToClauses.ContainsKey(-literal))
                {
                    pureLiterals.Add(literal);
                }
                else
                {
                    pureLiterals.Remove(-literal);
                }


                if (!literalsToClauses.TryGetValue(literal, out var clausesWithLiteral))
                {
                    clausesWithLiteral = new SortedSet<int>();
                    literalsToClauses[literal] = clausesWithLiteral;
                }
                clausesWithLiteral.Add(clause);




                if (!variablesToClauses.TryGetValue((literal < 0) ? -literal : literal, out var clausesWithVariable))
                {
                    clausesWithVariable = new SortedSet<int>();
                    variablesToClauses[literal] = clausesWithVariable;
                }
                clausesWithVariable.Add(clause);


            }
        }

        public bool CheckAssignment(IEnumerable<bool?> assignment, out SortedSet<int> unsatisfiedSet)
        {
            unsatisfiedSet = new SortedSet<int>(Enumerable.Range(0, Clauses.Count));
            var v = 1;
            foreach (var a in assignment)
            {
                if (a != null)
                {
                    foreach (var c_i in variablesToClauses[v])
                    {
                        if (unsatisfiedSet.Contains(c_i) && Clauses[c_i].Contains(a.Value ? v : -v))
                        {
                            unsatisfiedSet.Remove(c_i);
                            if (unsatisfiedSet.Count == 0)
                            {
                                return true;
                            }
                        }
                    }
                }
                v++;
            }
            return false;
        }

        public bool CheckAssignment(IEnumerable<bool?> assignment)
        {
            SortedSet<int> unsatisfied = new SortedSet<int>(Enumerable.Range(0, Clauses.Count));

            var v = 1;
            foreach (var a in assignment)
            {
                if (a != null)
                {
                    foreach (var c_i in variablesToClauses[v])
                    {
                        if (!unsatisfied.Contains(c_i) && Clauses[c_i].Contains(a.Value ? v : -v))
                        {
                            unsatisfied.Remove(c_i);
                            if (unsatisfied.Count == 0)
                            {
                                return true;
                            }
                        }
                    }
                }
                v++;
            }

            return false;
        }

        static public Cnf LoadFile(string filename)
        {
            var fi = new System.IO.FileInfo(filename);
            if (!fi.Exists)
            {
                throw new System.IO.FileNotFoundException(filename);
            }
            using (var sr = new System.IO.StreamReader(fi.OpenRead()))
            {
                var splitDelims = new char[] { ' ', '\t' };
                string line;
                int numVariables = 0;
                int numClauses = 0;

                bool parsedProblemLine = false;
                while (!sr.EndOfStream && !parsedProblemLine)
                {
                    line = sr.ReadLine().Trim();
                    switch (line[0])
                    {
                        case 'C':
                        case 'c':
                            continue;
                        case 'P':
                        case 'p':
                            if (parsedProblemLine)
                            {
                                throw new ApplicationException("Already have the problem line");
                            }
                            var tokens = line.Split(splitDelims, StringSplitOptions.RemoveEmptyEntries);
                            if (tokens.Length == 4 && tokens[1].Equals("cnf", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!int.TryParse(tokens[2], out numVariables) || !int.TryParse(tokens[3], out numClauses))
                                {
                                    throw new ApplicationException("Unable to parse problem line");
                                }
                                parsedProblemLine = true;
                                break;
                            }
                            throw new ApplicationException("Unable to parse problem line");
                        default:
                            if (char.IsDigit(line[0]) || line[0] == '-')
                                throw new ApplicationException("Clauses appear before problem line");
                            break;
                    }
                }

                var cnf = new Cnf();
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine().Trim();
                    var clause = new SortedSet<int>();

                    switch (line[0])
                    {
                        case 'C':
                        case 'c':
                            continue;
                        case 'P':
                        case 'p':
                            throw new ApplicationException("Multiple problem lines");
                        default:
                            if (!char.IsDigit(line[0]) && line[0] != '-')
                                throw new ApplicationException("Unknown line type");
                            var tokens = line.Split(splitDelims, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var token in tokens)
                            {
                                if (int.TryParse(token, out var v))
                                {
                                    if (v == 0)
                                    {
                                        cnf.AddClause(clause);
                                        clause = new SortedSet<int>();
                                    }
                                    else
                                    {
                                        clause.Add(v);
                                    }
                                }
                                else
                                {
                                    throw new ApplicationException("unknown token");
                                }
                            }
                            break;
                    }


                }

                if (cnf.Clauses.Count != numClauses || cnf.Variables.Count != numVariables)
                {
                    throw new ApplicationException("missing clauses or variables");
                }
                cnf.assignment = new bool?[cnf.Variables.Count];
                return cnf;
            }

        }

        public bool SetLiteral(int l)
        {
            var v = l < 0 ? -l : l;
            assignment[v - 1] = l < 0 ? false : true;

            while (ClausesWithLiteral.TryGetValue(l, out var clauses))
            {
                var cl = clauses.First();
                RemoveClause(cl);
            }

            RemoveLiteralFromClauses(-l);

            return EmptyClauses.Count == 0;
        }

        public bool SetVariable(int v, bool value)
        {
            return SetLiteral(value ? v : -v);
        }

        public bool ResolveUnitClauses()
        {
            while (UnitClauses.Count > 0)
            {
                int c = UnitClauses.First();
                var l = Clauses[c].First();
                if (!SetLiteral(l)) return false;
            }
            return EmptyClauses.Count == 0;
        }

        public bool ResolvePureLiterals()
        {
            while (PureLiterals.Count > 0)
            {
                int l = PureLiterals.First();
                if (!SetLiteral(l)) return false;
            }
            return EmptyClauses.Count == 0;

        }

        public Cnf Copy()
        {
            var newCnf = new Cnf();

            newCnf.clauses = new List<SortedSet<int>>(clauses.Count);
            newCnf.satisfiedClauses = new SortedSet<int>(satisfiedClauses);
            for (int i = 0; i < clauses.Count; i++)
            {
                if (clauses[i] == null)
                {
                    newCnf.clauses.Add(null);
                }
                else
                {
                    newCnf.clauses.Add(new SortedSet<int>(clauses[i]));
                }

            }


            newCnf.unitClauses = new SortedSet<int>(unitClauses);
            newCnf.variablesToClauses = new SortedDictionary<int, SortedSet<int>>();
            foreach (var kvp in variablesToClauses)
            {
                newCnf.variablesToClauses[kvp.Key] = new SortedSet<int>(kvp.Value);
            }

            newCnf.literalsToClauses = new SortedDictionary<int, SortedSet<int>>();
            foreach (var kvp in literalsToClauses)
            {
                newCnf.literalsToClauses[kvp.Key] = new SortedSet<int>(kvp.Value);
            }

            newCnf.emptyClauses = new SortedSet<int>(emptyClauses);
            newCnf.pureLiterals = new SortedSet<int>(pureLiterals);
            newCnf.assignment = assignment.ToArray();

            return newCnf;
        }

        public void SaveToFile(string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                SaveToStream(stream);
            }
        }

        public void SaveToStream(Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine($"p cnf {Variables.Count} {Clauses.Count}");
                foreach (var clause in Clauses)
                {
                    foreach (var literal in clause)
                    {
                        writer.Write(literal);
                        writer.Write(' ');
                    }
                    writer.WriteLine('0');
                }
            }
        }


    }

}
