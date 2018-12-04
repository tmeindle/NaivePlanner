using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NaivePlanner
{

    class PlanGraph
    {
        public PddlDomain Domain { get; }
        public PddlProblem Problem { get; }


        public PlanGraph(PddlDomain domain, PddlProblem problem)
        {
            Domain = domain;
            Problem = problem;
        }




        public bool BuildCnf(int t, out IList<string> predicateVariables, out IList<string> actionVariables, out IList<IList<string>> clauses, IncludedClauses include = IncludedClauses.Serial, bool verbose = true)
        {
            if (t < 1)
            {
                predicateVariables = null;
                actionVariables = null;
                clauses = null;
                return false;
            }

            if (verbose)
            {
                Console.WriteLine($"Assembling Cnf Formula for plan of length {t}");
                Console.WriteLine($"Domain : {Domain.Name}");
                Console.WriteLine($"Problem : {Problem.Name}");
                Console.WriteLine();
            }

            var predicate_variables = new List<string>();
            var action_variables = new List<string>();

            var clauses_list = new List<IList<string>>();
            var pg = ConstructConsecutiveLayers(Problem.InitialState, t).ToArray();

            var variableCount = 0;
            var clauseCount = 0;

            foreach (var l in Problem.InitialState)
            {
                predicate_variables.Add(string.Concat(l.LiteralName, "_0"));
                clauses_list.Add(new List<string> { string.Concat(l.LiteralName, "_0")} );
            }

            variableCount = predicate_variables.Count;
            clauseCount = clauses_list.Count;
            Console.WriteLine($"Added: {variableCount} variables and {clauseCount} clauses for initial fact layer 0");

            foreach (var l in Problem.GoalState)
            {
                predicate_variables.Add(string.Concat(l.LiteralName, "_", t));
                clauses_list.Add(new List<string> { string.Concat(l.LiteralName, "_", t) });
            }
            Console.WriteLine($"Added: {predicate_variables.Count - variableCount} variables and {clauses_list.Count - clauseCount} for fact goal layer {t}");
            variableCount  = predicate_variables.Count;
            clauseCount = clauses_list.Count;


            for (int i = 0; i < t; i++)
            {
                var layer = pg[i];
                foreach (var l in layer.Item2)
                {
                    var predicateName = string.Concat(l.LiteralName, "_", i + 1);
                    if (!predicate_variables.Contains(predicateName))
                    predicate_variables.Add(predicateName);
                }


                Console.WriteLine($"Added: {predicate_variables.Count - variableCount} variables for fact layer {i+1}");
                variableCount = predicate_variables.Count;
                clauseCount = clauses_list.Count;
            }


            for (int i = 0; i < t; i++)
            {
                var layer = pg[i];
                foreach (var a in layer.Item1)
                {
                    var actionName = string.Concat(a.LiteralName, "_", i + 1);
                    if (!action_variables.Contains(actionName))
                        action_variables.Add(actionName);
                }

                Console.WriteLine($"Added: {action_variables.Count} action variables for action layer {i + 1}");
                variableCount = action_variables.Count + predicate_variables.Count;
                clauseCount = clauses_list.Count;
            }

            for (int i = 0; i < t; i++)
            {
                var layer = pg[i];
                foreach (var a in layer.Item1)
                {
                    var action_string = string.Concat("NOT ",a.LiteralName, '_', i + 1);
                    foreach (var p in a.Preconditions)
                    {
                        var pre_string = string.Concat(p.LiteralName, '_', i);
                        var clause = new string[2];
                        clause[0] = action_string;
                        clause[1] = pre_string;
                        clauses_list.Add(clause);
                    }
                }

                Console.WriteLine($"Added: {clauses_list.Count - clauseCount} clauses for effects imply preconditions on action layer {i + 1}");
                clauseCount = clauses_list.Count;
            }


            for (int i = 0; i < t; i++)
            {
                var layer = pg[i];
                foreach (var p in layer.Item2)
                {
                    var predicate_string = string.Concat("NOT ", p.LiteralName, '_', i + 1);
                    var frameClause = new List<string>(2);
                    frameClause.Add(predicate_string);

                    foreach (var a in layer.Item1)
                    {
                        foreach (var e in a.Effects)
                        {
                            if (!e.Negated && e.LiteralName.Equals(p.LiteralName, StringComparison.Ordinal))
                            {
                                var action_string = string.Concat(a.LiteralName, '_', i + 1);
                                frameClause.Add(action_string);
                                break;
                            }
                        }
                    }
                    clauses_list.Add(frameClause);
                }

                Console.WriteLine($"Added: {clauses_list.Count - clauseCount} framing axiom clauses for facts imply actions effects on action layer {i + 1}");
                clauseCount = clauses_list.Count;

            }

            for (int i = 0; i < t; i++)
            {
                var layer = pg[i];
                foreach (var exclusion in layer.Item3)
                {
                    var clause = new List<string>(2);
                    clause.Add(string.Concat("NOT ", exclusion.Item1.LiteralName, "_", i + 1));
                    clause.Add(string.Concat("NOT ", exclusion.Item2.LiteralName, "_", i + 1));

                    clauses_list.Add(clause);
                }
                Console.WriteLine($"Added: {clauses_list.Count - clauseCount} axiom exclusion clauses for action layer {i + 1}");
                clauseCount = clauses_list.Count;

            }


            actionVariables = action_variables;
            predicateVariables = predicate_variables;
            clauses = clauses_list;

            Console.WriteLine($"Cnf complete added: {actionVariables.Count + predicateVariables.Count} variables and {clauses.Count} clauses total");

            return true;

        }


        public IEnumerable<PddlAction> PossibleActions(HashSet<PddlPredicate> state)
        {
            HashSet<string> seen = new HashSet<string>();

            foreach (var action in Domain.Actions)
            {
                var paramOrder = new Dictionary<string, int>();
                foreach (var kvp in action.Parameters.Select((x, i) => new KeyValuePair<string, int>(x.Name, i)))
                {
                    paramOrder.Add(kvp.Key, kvp.Value);
                }

                var paramTypeDict = action.Parameters.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.ToList());

                var permutations = new Dictionary<string, List<List<string>>>();
                List<List<string>> perms;
                foreach (var kvp in paramTypeDict)
                {
                    var variables = string.IsNullOrEmpty(kvp.Key) ? Problem.Objects.Union(Domain.Objects).Select(x => x.Name).ToList()
                           : Problem.Objects.Union(Domain.Objects).Where(v => v.Type.Equals(kvp.Key)).Select(x => x.Name).OrderBy(x => x).ToList();

                    perms = new List<List<string>>();
                    foreach (var perm in variables.GetPermutations(kvp.Value.Count))
                    {
                        perms.Add(perm.ToList());
                    }
                    permutations.Add(kvp.Key, perms);
                }


                var listsIndexes = new int[permutations.Count];
                var listLengths = new int[permutations.Count];

                var keyOrder = permutations.Keys.ToArray();
                for (int k = 0; k < keyOrder.Length; k++)
                {
                    listLengths[k] = permutations[keyOrder[k]].Count();
                }


                var done = false;
                do
                {
                    var paramValues = new string[action.Parameters.Count];

                    for (int k = 0; k < keyOrder.Length; k++)
                    {
                        var key = keyOrder[k];
                        var perm = permutations[key][listsIndexes[k]];

                        for (int p = 0; p < perm.Count; p++)
                        {
                            var v = paramTypeDict[key][p].Name;
                            var v_i = paramOrder[v];
                            paramValues[v_i] = perm[p];
                        }


                    }


                    var stateContainsMatch = true;
                    foreach (var precondition in action.Preconditions)
                    {
                        var resolved = new PddlPredicate();
                        resolved.Name = precondition.Name;
                        resolved.Parameters = new List<PddlObject>(precondition.Parameters.Count);

                        for (int i = 0; i < precondition.Parameters.Count; i++)
                        {
                            var param = precondition.Parameters[i];
                            var paramValue = paramValues[paramOrder[param.Name]];
                            var resolvedParam = new PddlObject();
                            resolvedParam.Name = paramValue;
                            resolved.Parameters.Add(resolvedParam);
                        }


                        if (!((IEnumerable<PddlPredicate>)state).Contains(resolved))
                        {
                            stateContainsMatch = false;
                            break;
                        }

                    }

                    if (stateContainsMatch)
                    {
                        var a = action.Copy();


                        for (int i = 0; i < a.Parameters.Count; i++)
                        {
                            var param = a.Parameters[i];
                            a.Parameters[i].Name = paramValues[paramOrder[param.Name]];
                        }

                        for (int i = 0; i < a.Preconditions.Count; i++)
                        {
                            for (int j = 0; j < a.Preconditions[i].Parameters.Count; j++)
                            {
                                var param = a.Preconditions[i].Parameters[j];
                                a.Preconditions[i].Parameters[j].Name = paramValues[paramOrder[param.Name]];
                            }
                        }

                        for (int i = 0; i < a.Effects.Count; i++)
                        {
                            for (int j = 0; j < a.Effects[i].Parameters.Count; j++)
                            {
                                var param = a.Effects[i].Parameters[j];
                                a.Effects[i].Parameters[j].Name = paramValues[paramOrder[param.Name]];
                            }
                        }


                        if (!seen.Contains(a.LiteralName))
                        {
                            seen.Add(a.LiteralName);
                            yield return a;
                        }

                    }



                    for (int i = 0; i <= listsIndexes.Length; i++)
                    {
                        if (i == listsIndexes.Length)
                        {
                            done = true;
                            break;
                        }
                        else
                        {
                            listsIndexes[listsIndexes.Length - 1 - i]++;
                            if (listsIndexes[listsIndexes.Length - 1 - i] == listLengths[listsIndexes.Length - 1 - i])
                            {
                                listsIndexes[listsIndexes.Length - 1 - i] = 0;
                                continue;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                } while (!done);


            }



        }

        public Cnf GetCnf(IEnumerable<string> variables, IList<IList<string>> clauses)
        {
            var list = CreateCnfClauses(variables.ToList(), clauses).ToList();
            return new Cnf(list);
        }

        private IEnumerable<IEnumerable<int>> CreateCnfClauses(IList<string> variables, IList<IList<string>> clauses)
        {
            
            var mapping = new Dictionary<string, int>();
            for (int i = 0; i < variables.Count; i++)
            {
                mapping.Add(variables[i], i + 1);
            }

            for (var c = 0; c < clauses.Count; c++)
            {
                var clause = clauses[c];
                var carray = new int[clause.Count];

                for (int i = 0; i < clause.Count; i++)
                {

                    if (clause[i].StartsWith("NOT ", StringComparison.OrdinalIgnoreCase))
                    {
                        carray[i] = -(mapping[clause[i].Substring(4)]);
                    }
                    else
                    {
                        carray[i] = (mapping[clause[i]]);
                    }
                }
                yield return carray;
            }
        }

        public Tuple<HashSet<PddlAction>, HashSet<PddlPredicate>, HashSet<Tuple<PddlAction, PddlAction>>> ConstructNextLayerFromState(HashSet<PddlPredicate> currentState)
        {
            HashSet<string> seen = new HashSet<string>();
            var possibleActions = new HashSet<PddlAction>();
            foreach (var a in PossibleActions(currentState))
            {
                if (!seen.Contains(a.LiteralName))
                {
                    seen.Add(a.LiteralName);
                    possibleActions.Add(a);
                }
            }

            foreach (var s in currentState)
            {
                PddlAction a = new PddlAction();
                a.Name = string.Concat("NoOP_", s.Name);
                a.Parameters = new List<PddlObject>(s.Parameters.Select(p => p.Copy()));
                a.Preconditions = new List<PddlPredicate>(1);
                a.Preconditions.Add(s.Copy());
                a.Effects = new List<PddlPredicate>(1);
                a.Effects.Add(s.Copy());
                if (!seen.Contains(a.LiteralName))
                {
                    seen.Add(a.LiteralName);
                    possibleActions.Add(a);
                }
            }


            var nextState = new HashSet<PddlPredicate>();
            

            foreach (var a in possibleActions)
            {
                foreach (var e in a.Effects)
                {
                    if (!e.Negated)
                    {
                        var c = e.Copy();
                        c.Negated = false;
                        seen.Add(c.LiteralName);
                        nextState.Add(c);
                    }
                }
            }

            var exclusions = new HashSet<Tuple<PddlAction, PddlAction>>(CalculateActionExclusions(possibleActions));

            return Tuple.Create(possibleActions, nextState, exclusions);
        }

        public IEnumerable<Tuple<PddlAction, PddlAction>> CalculateActionExclusions(HashSet<PddlAction> actions)
        {
            var actionlist = actions.ToArray();

            for(int i = 0; i<actionlist.Length - 1; i++)
            {
                var mutex = false;
                var action_i = actionlist[i];
                var action_i_negated_preconditions = new HashSet<string>();
                foreach (var p in action_i.Preconditions)
                {
                    var name = p.Name;
                    action_i_negated_preconditions.Add(p.Name.StartsWith("Not", StringComparison.Ordinal) ? p.Name.Substring(3) : string.Concat("Not", p.Name));
                }

                for (int j = i + 1; j < actionlist.Length; j++)
                {
                    var action_j = actionlist[j];
                    var action_j_preconditions = new HashSet<PddlPredicate>(action_j.Preconditions);
                    var action_j_effects = new HashSet<PddlPredicate>(action_j.Effects);
                    foreach(var p in action_i.Effects)
                    {
                        var notP = p.Copy();
                        notP.Negated = !notP.Negated;
                        if (action_j_effects.Contains(notP)) //i and j have inconsistent effects
                        {
                            yield return Tuple.Create(action_i, action_j);
                            mutex = true;
                            break;
                        }
                        else if (action_j_preconditions.Contains(notP)) //interference : i negates a precondition of j
                        {
                            yield return Tuple.Create(action_i, action_j);
                            mutex = true;
                            break;
                        }
                    }
                    if (mutex) continue;

                    foreach (var negated in action_i_negated_preconditions)
                    {
                        foreach (var precondion in action_j_preconditions)
                        {
                            if (precondion.Name == negated)
                            {
                                yield return Tuple.Create(action_i, action_j);
                                mutex = true;
                                break;
                            }
                        }
                        if (mutex) break;
                    }

                }
            }
        }

        public IEnumerable<Tuple<HashSet<PddlAction>, HashSet<PddlPredicate>, HashSet<Tuple<PddlAction, PddlAction>>>> ConstructConsecutiveLayers(IEnumerable<PddlPredicate> initialState, int tMax = int.MaxValue)
        {

            var currentState = new HashSet<PddlPredicate>(initialState);
            var currentActions = new HashSet<PddlAction>();

            var t = 0;
            do
            {
                var layer = ConstructNextLayerFromState(currentState);
                yield return layer;

                t++;
                if (t == tMax)
                {
                    break;
                }

                currentState = layer.Item2;

                /*
                                int g = 0;
                                for (; g < goalState.Count; g++)
                                {
                                    if (!currentState.Contains(goalState[g]))
                                    {
                                        break;
                                    }
                                }
                                if (g == goalState.Count)
                                {
                                    break;
                                }
                                */
            } while (true);

        }

        public IEnumerable<string> ExtractPlan(Assignment assignment, IList<string> variables, int actionVariableCount, bool includeNoOPs = true)
        {
            for (int i = 0; i < actionVariableCount; i++)
            {
                if (assignment[i] == true )
                {
                    if (!includeNoOPs && variables[i].StartsWith("NoOP_", StringComparison.Ordinal))
                        continue;
                    yield return variables[i];
                }
            }
        }

        public void WriteLiteralsToStream(Stream stream, IList<string> variables, IList<IList<string>> clauses)
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("Variables: ");
                foreach (var v in variables)
                {
                    writer.WriteLine(v);
                }

                writer.WriteLine("\nClauses: ");
                foreach (var c in clauses)
                {
                    writer.Write(c[0]);
                    foreach (var v in c.Skip(1))
                    {
                        writer.Write(" ");
                        writer.Write(v);
                    }
                    writer.WriteLine();
                }
            }
        }
        public void WriteLiteralsToFile(string filename, IList<string> variables, IList<IList<string>> clauses)
        {
            using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                WriteLiteralsToStream(stream, variables, clauses);
            }
        }

    }
}
