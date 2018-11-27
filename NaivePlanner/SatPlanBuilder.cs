using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NaivePlanner
{
    [Flags]
    public enum IncludedClauses
    {
        InitalState = 1,
        GoalState = 2,
        ActionsImplyEffects = 4,
        ActionsImplyPreconditions = 8,
        PredicatesOnlyChangeThroughActions = 16,
        AtLeastOneActionPerTime = 32,
        Parallel = 63,
        AtMostOneActionPerTime = 64,
        Serial = 127

    }

    public class SatPlanBuilder
    {
        PddlDomain Domain { get; }
        PddlProblem Problem { get; }
        bool _init = false;

        private List<string> predicateVariables;
        private List<string> actionVariables;
        private List<IList<IList<string>>> actionsEffects;
        private List<IList<IList<string>>> actionPreconditions;
        private List<string> goalState;
        private List<string> initialState;

        public SatPlanBuilder(PddlDomain domain, PddlProblem problem)
        {
            Domain = domain;
            Problem = problem;
            Initialize();
        }

        public void Initialize()
        {
            if (_init) return;
            _init = true;

            predicateVariables = PddlUtils.GetPredicateVariables(Domain, Problem).ToList();

            actionVariables = PddlUtils.GetActionVariables(Domain, Problem).ToList();

            actionsEffects = PddlUtils.GetActionEffects(Domain, Problem).ToList();

            actionPreconditions = PddlUtils.GetActionPreconditions(Domain, Problem).ToList();

            goalState = PddlUtils.GetGoalState(Domain, Problem).ToList();

            initialState = PddlUtils.GetInitialState(Domain, Problem).ToList();
        }



        public Cnf GetCnf(IList<string> variables, IList<IList<string>> clauses)
        {
            var list = CreateCnfClauses(variables, clauses).ToList();
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
                var carry = new int[clause.Count];

                for (int i = 0; i < clause.Count; i++)
                {

                    if (clause[i].StartsWith("NOT", StringComparison.OrdinalIgnoreCase))
                    {
                        carry[i] = -(mapping[clause[i].Substring(4)]);
                    }
                    else
                    {
                        carry[i] = (mapping[clause[i]]); 
                    }
                }
                yield return carry;
            }
        }

        public IEnumerable<string> ExtractPlan(Assignment assignment, IList<string> variables, int actionVariableCount)
        {
            for(int i = 0; i < actionVariableCount; i++)
            {
                if (assignment[i] == true)
                {
                    yield return variables[i];
                }
            }
        }

        public void WriteSatPlanToStream(Stream stream, IList<string> variables, IList<IList<string>> clauses)
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
        public void WriteSatPlanToFile(string filename, IList<string> variables, IList<IList<string>> clauses)
        {
            using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan))
            {
                WriteSatPlanToStream(stream, variables, clauses);
            }
        }
        public bool TryBuildSatPlan(int t, out IList<string> variables, out IList<IList<string>> clauses, IncludedClauses include = IncludedClauses.Serial, bool verbose = true)
        {
            if (t < 1)
            {
                variables = null;
                clauses = null;
                return false;
            }

            if (verbose)
            {
                Console.WriteLine($"Assembling SAT Plan");
                Console.WriteLine();
            }

            var variables_list = new List<string>();

            for (int i = 1; i <= t; i++)
            {
                variables_list.AddRange(ActionVariables(i));
            }

            if (verbose)
            {
                Console.WriteLine($"Added {variables_list.Count} Action Variables to the plan");
            }

            int c = variables_list.Count;
            for (int i = 0; i <= t; i++)
            {
                variables_list.AddRange(PredicateVariables(i));
            }

            if (verbose)
            {
                Console.WriteLine($"Added {variables_list.Count - c} Predicate Variables to the plan");
            }

            var clauses_list = new List<IList<string>>();
            c = 0;
            if (include.HasFlag(IncludedClauses.InitalState))
            {
                clauses_list.AddRange(InitialStateClauses());
            
                if (verbose)
                {
                    Console.WriteLine($"Added {clauses_list.Count - c} Initial State clauses to the plan");
                }
                c = clauses_list.Count;
            }

            if (include.HasFlag(IncludedClauses.GoalState))
            {
                clauses_list.AddRange(GoalStateClauses(t));
                if (verbose)
                {
                    Console.WriteLine($"Added {clauses_list.Count - c} Goal State clauses to the plan");
                }
                c = clauses_list.Count;
            }

            if (include.HasFlag(IncludedClauses.ActionsImplyEffects))
            {
                for (int i = 1; i <= t; i++)
                {
                    clauses_list.AddRange(ActionsImplyEffectsClauses(i));
                }
                if (verbose)
                {
                    Console.WriteLine($"Added {clauses_list.Count - c} Actions Imply Effects Clauses to the plan");
                }
                c = clauses_list.Count;
            }

            if (include.HasFlag(IncludedClauses.ActionsImplyPreconditions))
            {
                for (int i = 1; i <= t; i++)
                {
                    clauses_list.AddRange(ActionsImplyPreconditionsClauses(i));
                }
                if (verbose)
                {
                    Console.WriteLine($"Added {clauses_list.Count - c} Actions Imply Precondition clauses to the plan");
                }
                c = clauses_list.Count;
            }


            if (include.HasFlag(IncludedClauses.AtLeastOneActionPerTime))
            {
                for (int i = 1; i <= t; i++)
                {
                    clauses_list.AddRange(AtLeastOneActionPerTime(i));
                }
                if (verbose)
                {
                    Console.WriteLine($"Added {clauses_list.Count - c} At Least One Action Per Time clauses to the plan");
                }
                c = clauses_list.Count;

            }

            if (include.HasFlag(IncludedClauses.AtMostOneActionPerTime))
            {
                for (int i = 1; i <= t; i++)
                {
                    clauses_list.AddRange(AtMostOneActionPerTime(i));
                }
                if (verbose)
                {
                    Console.WriteLine($"Added {clauses_list.Count - c} At Most One Action Per Time clauses to the plan");
                }
                c = clauses_list.Count;
            }

            if (include.HasFlag(IncludedClauses.PredicatesOnlyChangeThroughActions))
            {
                for (int i = 1; i <= t; i++)
                {
                    var list = PredicatesOnlyChangeThroughActions(i).ToList();
                    clauses_list.AddRange(list);
                }

                if (verbose)
                {
                    Console.WriteLine($"Added {clauses_list.Count - c} Predicate Only Change Through Actions clauses to the plan");
                }
                c = clauses_list.Count;
            }

            variables = variables_list;
            clauses = clauses_list;
            if (verbose)
            {
                Console.WriteLine();
                Console.WriteLine($"Created SAT Plan cnf with {variables.Count} variables and {clauses.Count} clauses");
            }
            return true;
        }

        public IEnumerable<string> PredicateVariables(int t)
        {
            foreach (var predicate in predicateVariables)
            {
                yield return string.Concat(predicate, "_", t);
            }
        }

        public IEnumerable<string> ActionVariables(int t)
        {
            foreach (var action in actionVariables)
            {
                yield return string.Concat(action, "_", t);
            }
        }

        public IEnumerable<string[]> ActionsImplyEffectsClauses(int t)
        {
            foreach (var a in actionsEffects)
            {
                foreach (var e in a)
                {
                    var clause = new string[2];
                    clause[0] = string.Concat("NOT ", e[0], "_", t);
                    clause[1] = string.Concat(e[1], "_", t);
                    yield return clause;
                }
            }
        }

        public IEnumerable<string[]> ActionsImplyPreconditionsClauses(int t)
        {
            foreach (var a in actionPreconditions)
            {
                foreach (var e in a)
                {
                    var clause = new string[2];
                    clause[0] = string.Concat("NOT ", e[0], "_", t);
                    clause[1] = string.Concat(e[1], "_", t - 1);
                    yield return clause;
                }
            }
        }

        public IEnumerable<string[]> InitialStateClauses()
        {
            foreach (var p in predicateVariables)
            {
                yield return new string[1] { string.Concat(!initialState.Contains(p) ? string.Concat("NOT ", p) : p, "_0") };
            }
        }

        public IEnumerable<string[]> GoalStateClauses(int k)
        {
            foreach (var g in goalState)
            {
                yield return new string[1] { string.Concat(g, "_", k) };
            }
        }

        public IEnumerable<string[]> AtLeastOneActionPerTime(int t)
        {
            yield return actionVariables.Select(x => string.Concat(x, "_", t)).ToArray();
        }

        public IEnumerable<string[]> AtMostOneActionPerTime(int t)
        {
            for (int i = 0; i < actionVariables.Count - 1; i++)
            {
                for (int j = i + 1; j < actionVariables.Count; j++)
                {
                    var clause = new string[2];
                    clause[0] = string.Concat("NOT ", actionVariables[i], "_", t);
                    clause[1] = string.Concat("NOT ", actionVariables[j], "_", t);
                    yield return clause;
                }
            }
        }

        public IEnumerable<string[]> PredicatesOnlyChangeThroughActions(int t)
        {
            var predicateEffects = new Dictionary<string, IList<string>>();

            foreach (var a in actionsEffects)
            {
                foreach (var e in a)
                {
                    if (!predicateEffects.TryGetValue(e[1], out var effects))
                    {
                        effects = new List<string>();
                        predicateEffects.Add(e[1], effects);
                    }
                    effects.Add(e[0]);
                }
            }

            foreach (var key in predicateEffects.Keys)
            {
                var clause = new string[2 + predicateEffects[key].Count];
                var inverse = key.StartsWith("NOT ", StringComparison.OrdinalIgnoreCase) ? key.Substring(4) : string.Concat("NOT ", key);

                //  (inverse_t-1 ^ key_t) => value_t
                //  (inverse_t V key_t-1 V value_t

                clause[0] = string.Concat(key, "_", t - 1);
                clause[1] = string.Concat(inverse, "_", t);
                var count = predicateEffects[key].Count;
                for (int i = 0; i < count; i++)
                {
                    clause[i + 2] = string.Concat(predicateEffects[key][i], '_', t);
                }

                yield return clause;


            }
        }


        public IEnumerable<string[]> SuccessorStateAxioms(int t)
        {
            for (int i = 0; i < actionVariables.Count - 1; i++)
            {
                for (int j = i + 1; j < actionVariables.Count; j++)
                {
                    var clause = new string[2];
                    clause[0] = string.Concat("NOT ", actionVariables[i], "_", t);
                    clause[1] = string.Concat("NOT ", actionVariables[j], "_", t);
                    yield return clause;
                }
            }
        }

    }
}
