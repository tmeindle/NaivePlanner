using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace NaivePlanner
{
    internal static class Ex
    {
        public static Random rnd = new Random();
        public static IEnumerable<T> Randomize<T>(this IEnumerable<T> source)
        {
            var list = source.ToList();
            for (var k = 0; k < list.Count; k += 1)
            {
                var j = rnd.Next(k, list.Count);
                var temp = list[k];
                list[k] = list[j];
                list[j] = temp;
            }
            return list;
        }

        static public IEnumerable<IEnumerable<T>> GetPermutations<T>(this IEnumerable<T> source, int length)
        {
            if (length == 1) return source.Select(t => new T[] { t });

            return GetPermutations(source, length - 1)
                .SelectMany(t => source.Where(e => !t.Contains(e)),
                    (t1, t2) => t1.Concat(new T[] { t2 }));
        }
    }

    public static class PddlUtils
    {
        static public IEnumerable<string> GetInitialState(PddlDomain domain, PddlProblem problem)
        {
            StringBuilder sb = new StringBuilder(64);
            foreach (var predicate in problem.InitialState)
            {
                sb.Length = 0;

                if (predicate.Negated)
                {
                    sb.Append("NOT ");
                }
                sb.Append(predicate.Name);
                foreach (var p in predicate.Parameters)
                {
                    sb.Append('_');
                    sb.Append(p.Name);
                }

                yield return sb.ToString();
            }
        }

        static public IEnumerable<string> GetGoalState(PddlDomain domain, PddlProblem problem)
        {
            StringBuilder sb = new StringBuilder(64);
            foreach (var predicate in problem.GoalState)
            {
                sb.Length = 0;

                if (predicate.Negated)
                {
                    sb.Append("NOT ");
                }
                sb.Append(predicate.Name);
                foreach (var p in predicate.Parameters)
                {
                    sb.Append('_');
                    sb.Append(p.Name);
                }

                yield return sb.ToString();
            }
        }



        static public IEnumerable<string> GetPredicateVariables(PddlDomain domain, PddlProblem problem)
        {
            foreach (var predicate in domain.Predicates)
            {
                foreach (var v in GetPredicateVariables(domain, problem, predicate))
                {
                    yield return v;
                }
            }
        }

        static public IEnumerable<string> GetPredicateVariables(PddlDomain domain, PddlProblem problem, string predicateName)
        {
            var predicate = domain.Predicates.FirstOrDefault(x => x.Name.Equals(predicateName));
            if (predicate != null) return GetPredicateVariables(domain, problem, predicate);
            else return Enumerable.Empty<string>();
        }

        static public IEnumerable<string> GetPredicateVariables(PddlDomain domain, PddlProblem problem, PddlPredicate predicate)
        {

            var paramOrder = new Dictionary<string, int>();
            foreach (var kvp in predicate.Parameters.Select((x, i) => new KeyValuePair<string, int>(x.Name, i)))
            {
                paramOrder.Add(kvp.Key, kvp.Value);
            }

            var paramTypeDict = predicate.Parameters.GroupBy(x => x.Type).ToDictionary(x => x.Key, x => x.ToList());

            var permutations = new Dictionary<string, List<List<string>>>();
            List<List<string>> perms;
            foreach (var kvp in paramTypeDict)
            {
                var variables = string.IsNullOrEmpty(kvp.Key) ? problem.Objects.Union(domain.Objects).Select(x => x.Name).ToList()
                       : problem.Objects.Union(domain.Objects).Where(v => v.Type.Equals(kvp.Key)).Select(x => x.Name).OrderBy(x => x).ToList();

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
                var paramValues = new string[predicate.Parameters.Count];

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

                var sb = new StringBuilder(predicate.Name);
                sb.Append('_');
                sb.Append(string.Join("_", paramValues));

                yield return sb.ToString();

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




        static public IEnumerable<string> GetActionVariables(PddlDomain domain, PddlProblem problem)
        {
            foreach (var action in domain.Actions)
            {
                foreach (var v in GetActionVariables(domain, problem, action))
                    yield return v;
            }
        }

        static public IEnumerable<string> GetActionVariables(PddlDomain domain, PddlProblem problem, string name)
        {
            var action = domain.Actions.FirstOrDefault(x => x.Name.Equals(name));
            if (action != null) return GetActionVariables(domain, problem, action);
            else return Enumerable.Empty<string>();
        }

        static public IEnumerable<string> GetActionVariables(PddlDomain domain, PddlProblem problem, PddlAction action)
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
                var variables = string.IsNullOrEmpty(kvp.Key) ? problem.Objects.Union(domain.Objects).Select(x => x.Name).ToList()
                       : problem.Objects.Union(domain.Objects).Where(v => v.Type.Equals(kvp.Key)).Select(x => x.Name).OrderBy(x => x).ToList();

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

                var sb = new StringBuilder(action.Name);
                sb.Append('_');
                sb.Append(string.Join("_", paramValues));

                yield return sb.ToString();

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

        static public IEnumerable<IList<IList<string>>> GetActionEffects(PddlDomain domain, PddlProblem problem)
        {
            foreach (var action in domain.Actions)
            {
                foreach (var v in GetActionEffects(domain, problem, action))
                    yield return v;
            }
        }


        static public IEnumerable<IList<IList<string>>> GetActionEffects(PddlDomain domain, PddlProblem problem, string name)
        {
            var action = domain.Actions.FirstOrDefault(x => x.Name.Equals(name));
            if (action != null) return GetActionEffects(domain, problem, action);
            else return Enumerable.Empty<IList<IList<string>>>();
        }

        static public IEnumerable<IList<IList<string>>> GetActionEffects(PddlDomain domain, PddlProblem problem, PddlAction action)
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
                var variables = string.IsNullOrEmpty(kvp.Key) ? problem.Objects.Union(domain.Objects).Select(x => x.Name).ToList()
                       : problem.Objects.Union(domain.Objects).Where(v => v.Type.Equals(kvp.Key)).Select(x => x.Name).OrderBy(x => x).ToList();

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
                var actionEffects = new List<IList<string>>();
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

                var sb = new StringBuilder(action.Name);
                foreach (var v in paramValues)
                {
                    sb.Append('_');
                    sb.Append(v);
                }

                var actionLiteral = sb.ToString();

                foreach (var effect in action.Effects)
                {
                    sb.Length = 0;
                    if (effect.Negated)
                    {
                        sb.Append("NOT ");
                    }
                    sb.Append(effect.Name);

                    foreach (var p in effect.Parameters)
                    {
                        sb.Append('_').Append(paramValues[paramOrder[p.Name]]);
                    }
                    actionEffects.Add(new List<string>(2) { actionLiteral, sb.ToString() });
                }

                yield return actionEffects;

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

        static public IEnumerable<IList<IList<string>>> GetActionPreconditions(PddlDomain domain, PddlProblem problem)
        {
            foreach (var action in domain.Actions)
            {
                foreach (var v in GetActionPreconditions(domain, problem, action))
                    yield return v;
            }
        }

        static public IEnumerable<IList<IList<string>>> GetActionPreconditions(PddlDomain domain, PddlProblem problem, string name)
        {
            var action = domain.Actions.FirstOrDefault(x => x.Name.Equals(name));
            if (action != null) return GetActionPreconditions(domain, problem, action);
            else return Enumerable.Empty<IList<IList<string>>>();
        }


        static public IEnumerable<IList<IList<string>>> GetActionPreconditions(PddlDomain domain, PddlProblem problem, PddlAction action)
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
                var variables = string.IsNullOrEmpty(kvp.Key) ? problem.Objects.Union(domain.Objects).Select(x => x.Name).ToList()
                       : problem.Objects.Union(domain.Objects).Where(v => v.Type.Equals(kvp.Key)).Select(x => x.Name).OrderBy(x => x).ToList();

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
                var actionsPreconditions = new List<IList<string>>();
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

                var sb = new StringBuilder(action.Name);
                foreach (var v in paramValues)
                {
                    sb.Append('_');
                    sb.Append(v);
                }

                var actionLiteral = sb.ToString();

                foreach (var precondition in action.Preconditions)
                {
                    sb.Length = 0;
                    if (precondition.Negated)
                    {
                        throw new ApplicationException("preconditions cannot be negative");
                        //sb.Append("NOT ");
                    }
                    sb.Append(precondition.Name);

                    foreach (var p in precondition.Parameters)
                    {
                        sb.Append('_').Append(paramValues[paramOrder[p.Name]]);
                    }
                    actionsPreconditions.Add(new List<string>(2) { actionLiteral, sb.ToString() });
                }

                yield return actionsPreconditions;

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

    public class PddlObject
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class PddlPredicate
    {
        public bool Negated { get; set; }
        public string Name { get; set; }
        public List<PddlObject> Parameters { get; set; }
    }

    public class PddlAction
    {
        public string Name { get; set; }
        public List<PddlObject> Parameters { get; set; }
        public List<PddlPredicate> Preconditions { get; set; }
        public List<PddlPredicate> Effects { get; set; }
    }


    public class PddlProblem
    {
        public string Name { get; set; }
        public string Domain { get; set; }
        public List<PddlObject> Objects { get; set; }
        public List<PddlPredicate> InitialState { get; set; }
        public List<PddlPredicate> GoalState { get; set; }

        static public PddlProblem Load(string filename)
        {
            string factsText;
            using (var reader = new StreamReader(filename))
            {
                factsText = reader.ReadToEnd();
            }

            AntlrInputStream inputStream = new AntlrInputStream(factsText);
            PddlLexer pddlLexer = new PddlLexer(inputStream);
            pddlLexer.RemoveErrorListener(ConsoleErrorListener<int>.Instance);

            CommonTokenStream commonTokenStream = new CommonTokenStream(pddlLexer);
            PddlParser pddlParser = new PddlParser(commonTokenStream);
            pddlParser.RemoveErrorListener(ConsoleErrorListener<IToken>.Instance);

            pddlParser.BuildParseTree = true;

            var problem = new PddlProblem
            {
                InitialState = new List<PddlPredicate>(),
                GoalState = new List<PddlPredicate>(),
                Objects = new List<PddlObject>(),
            };

            var listener = new PddlProblemListener(problem);
            var pd = pddlParser.problem();
            Antlr4.Runtime.Tree.ParseTreeWalker.Default.Walk(listener, pd);

            return problem;
        }

        class PddlProblemListener : PddlBaseListener
        {
            PddlProblem Problem = new PddlProblem();

            public PddlProblemListener(PddlProblem problem)
            {
                Problem = problem;
            }

            public PddlProblemListener()
            {
                Problem.InitialState = new List<PddlPredicate>();
                Problem.GoalState = new List<PddlPredicate>();
                Problem.Objects = new List<PddlObject>();
            }

            public override void EnterProblemDecl([NotNull] PddlParser.ProblemDeclContext context)
            {
                var n = context.NAME().GetText();
                Problem.Name = n; 

                base.EnterProblemDecl(context);
            }


            public override void EnterProblemDomain([NotNull] PddlParser.ProblemDomainContext context)
            {
                Problem.Domain = context.NAME().GetText();
                base.EnterProblemDomain(context);
            }

            public override void EnterInit([NotNull] PddlParser.InitContext initcontext)
            {
                foreach (var context in initcontext.initEl())
                {
                    var fHead = context.fHead();
                    var nameLiteral = context.nameLiteral();

                    var anf = nameLiteral.atomicNameFormula();

                    var proposition = new PddlPredicate
                    {
                        Name = anf.predicate().GetText(),
                        Parameters = anf.NAME().Select(n => new PddlObject { Name = n.GetText() }).ToList(),
                    };
                    Problem.InitialState.Add(proposition);
                }
                base.EnterInit(initcontext);
            }

            public override void EnterObjectDecl([NotNull] PddlParser.ObjectDeclContext context)
            {
                var tnl = context.typedNameList();
                var stvl = tnl.singleTypeNameList();
                foreach (var stv in stvl)
                {
                    var t = stv.type().GetText();
                    foreach (var v in stv.NAME())
                    {
                        var obj = new PddlObject();
                        obj.Name = v.Symbol.Text;
                        obj.Type = t;
                        Problem.Objects.Add(obj);
                    }
                }

                foreach (var v in tnl.NAME())
                {
                    var obj = new PddlObject();
                    obj.Name = v.Symbol.Text;
                    obj.Type = "";
                    Problem.Objects.Add(obj);
                }

                base.EnterObjectDecl(context);
            }

            public override void EnterGoal([NotNull] PddlParser.GoalContext context)
            {
                var gd = context.goalDesc();
                var c1 = gd.children[1].GetText();
                foreach (var goal in gd.goalDesc())
                {

                    var a = goal.atomicTermFormula();
                    var p = a.predicate();
                    var prop = new PddlPredicate() { Name = p.GetText(), Parameters = new List<PddlObject>() };
                    foreach (var term in a.term())
                    {
                        var t = term.GetText();
                        
                        prop.Parameters.Add(new PddlObject { Name = t });
                    }
                    Problem.GoalState.Add(prop);
                }
                base.EnterGoal(context);
            }
        }
    }

    public class PddlDomain
    {
        public string Name { get; set; }
        public List<PddlObject> Objects { get; set; }
        public List<PddlPredicate> Predicates { get; set; }
        public List<PddlAction> Actions { get; set; }

        static public PddlDomain Load(string filename)
        {
            string factsText;
            using (var reader = new StreamReader(filename))
            {
                factsText = reader.ReadToEnd();
            }

            AntlrInputStream inputStream = new AntlrInputStream(factsText);
            PddlLexer pddlLexer = new PddlLexer(inputStream);
            pddlLexer.RemoveErrorListener(ConsoleErrorListener<int>.Instance);

            CommonTokenStream commonTokenStream = new CommonTokenStream(pddlLexer);
            PddlParser pddlParser = new PddlParser(commonTokenStream);
            pddlParser.RemoveErrorListener(ConsoleErrorListener<IToken>.Instance);

            pddlParser.BuildParseTree = true;

            var domain = new PddlDomain { Objects = new List<PddlObject>(), Predicates = new List<PddlPredicate>(), Actions = new List<PddlAction>() };
            var listener = new PddlDomainListener(domain);
            var pd = pddlParser.domain();
            Antlr4.Runtime.Tree.ParseTreeWalker.Default.Walk(listener, pd);

            return domain;
        }


        class PddlDomainListener : PddlBaseListener
        {
            PddlDomain Domain;

            public PddlDomainListener(PddlDomain domain)
            {
                Domain = domain;
            }

            public PddlDomainListener()
            {
                Domain = new PddlDomain { Objects = new List<PddlObject>(), Predicates = new List<PddlPredicate>(), Actions = new List<PddlAction>() };
            }

            public override void EnterDomain([NotNull] PddlParser.DomainContext context)
            {
                Domain.Name = context.domainName().NAME().GetText();
                base.EnterDomain(context);
            }
            public override void EnterObjectDecl([NotNull] PddlParser.ObjectDeclContext context)
            {
                var tnl = context.typedNameList();
                var stvl = tnl.singleTypeNameList();
                foreach (var stv in stvl)
                {
                    var t = stv.type().GetText();
                    foreach (var v in stv.NAME())
                    {
                        var obj = new PddlObject();
                        obj.Name = v.Symbol.Text;
                        obj.Type = t;
                        Domain.Objects.Add(obj);
                    }
                }

                foreach (var v in tnl.NAME())
                {
                    var obj = new PddlObject();
                    obj.Name = v.Symbol.Text;
                    obj.Type = "";
                    Domain.Objects.Add(obj);
                }

                base.EnterObjectDecl(context);
            }

            public override void EnterPredicatesDef([NotNull] PddlParser.PredicatesDefContext context)
            {
                foreach (var afs in context.atomicFormulaSkeleton())
                {
                    var pred = new PddlPredicate
                    {
                        Name = afs.predicate().GetText(),
                        Parameters = new List<PddlObject>()
                    };
                    var parameters = afs.typedVariableList();

                    var stvl = parameters.singleTypeVarList();
                    foreach (var stv in stvl)
                    {
                        var t = stv.type().GetText();
                        foreach (var v in stv.VARIABLE())
                        {
                            var param = new PddlObject();
                            param.Name = v.Symbol.Text.TrimStart('?');
                            param.Type = t;
                            pred.Parameters.Add(param);
                        }
                    }

                    foreach (var v in parameters.VARIABLE())
                    {
                        var param = new PddlObject();
                        param.Name = v.Symbol.Text.TrimStart('?');
                        param.Type = "";
                        pred.Parameters.Add(param);
                    }
                    Domain.Predicates.Add(pred);
                }
                base.EnterPredicatesDef(context);
            }

            public override void EnterActionDef([NotNull] PddlParser.ActionDefContext context)
            {
                var action = new PddlAction();
                action.Parameters = new List<PddlObject>();
                action.Preconditions = new List<PddlPredicate>();
                action.Effects = new List<PddlPredicate>();

                action.Name = context.actionSymbol().NAME().GetText();
                var body = context.actionDefBody();
                var parameters = context.typedVariableList();

                var stvl = parameters.singleTypeVarList();
                foreach (var stv in stvl)
                {
                    var t = stv.type().GetText();
                    foreach (var v in stv.VARIABLE())
                    {
                        var param = new PddlObject();
                        param.Name = v.Symbol.Text.TrimStart('?');
                        param.Type = t;
                        action.Parameters.Add(param);
                    }
                }

                foreach (var v in parameters.VARIABLE())
                {
                    var param = new PddlObject();
                    param.Name = v.Symbol.Text.TrimStart('?');
                    param.Type = "";
                    action.Parameters.Add(param);
                }


                




                var bodyGoalDesc = body.goalDesc();
                var c1 = bodyGoalDesc.children[1].GetText();

                foreach (var g in bodyGoalDesc.goalDesc())
                {
                    var pred = new PddlPredicate();
                    pred.Parameters = new List<PddlObject>();
                    var atf = g.atomicTermFormula();
                    pred.Name = atf.predicate().NAME().GetText();

                    foreach (var term in atf.term())
                    {
                        pred.Parameters.Add(new PddlObject
                        {
                            Name = term.GetText().TrimStart('?'),
                        });
                    }

                    action.Preconditions.Add(pred);
                }

                var effect = body.effect();
                var effects = effect.cEffect();
                c1 = effect.children[1].GetText();
                foreach (var ce in effects)
                {
                    var pred = new PddlPredicate() { Parameters = new List<PddlObject>() };
                    var peffect = ce.pEffect();
                    var atf = peffect.atomicTermFormula();
                    if (peffect.children.Count == 4)
                    {
                        if (!peffect.children[1].GetText().Equals("not", StringComparison.OrdinalIgnoreCase))
                        {
                            throw new ApplicationException("parsing failed");
                        }
                        else
                        {
                            pred.Negated = true;
                        }

                    }
                    pred.Name = atf.predicate().NAME().GetText();

                    foreach (var term in atf.term())
                    {
                        pred.Parameters.Add(new PddlObject
                        {
                            Name = term.GetText().TrimStart('?'),
                        });
                    }
                    action.Effects.Add(pred);
                }

                Domain.Actions.Add(action);
                base.EnterActionDef(context);
            }
        }

    }
}
