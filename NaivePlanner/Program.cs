﻿using System;
using System.Linq;
using CommandLine;

namespace NaivePlanner
{
    class Program
    {
        [CommandLine.Verb("solve", HelpText = "Try to solve a pddl planning problem using SatPlan.")]
        class SolveOptions
        {
            [CommandLine.Option('p', "problem", Required = true, HelpText = "The input problem pddl file")]
            public string InputProblemFile { get; set; }

            [CommandLine.Option('d', "domain", Required = true, HelpText = "The input domain pddl file")]
            public string InputDomainFile { get; set; }

            [CommandLine.Option('t', "time", Required = true, HelpText = "The number of steps to include in the formula")]
            public int Time { get; set; }

            [CommandLine.Option('v', "verbose", Default = true, HelpText = "Set verbosity")]
            public bool Verbose { get; set; }


            [CommandLine.Option('i', "included", Required = false, Default = 127, HelpText = "Which clauses to include in cnf formula. 1 : Initial state clauses, 2 : Goal state clauses, 4 : Actions imply effects, 8 : Actions imply preconditions, 16 : Predicates change only through actions, 32 : At least one action per time, 64 : At most one action per time")]
            public int IncludedClauses { get; set; }

            [CommandLine.Option('a', "axioms", Required = false, HelpText = "The output axioms information file")]
            public string OutputAxiomsFile { get; set; }

            [CommandLine.Option('l', "literals", Required = false, HelpText = "The output cnf literals information file")]
            public string OutputLiteralsFile { get; set; }

            [CommandLine.Option('c', "cnf", Required = false, HelpText = "The output cnf file")]
            public string OutputCnfFile { get; set; }


            [CommandLine.Option('s', "solver", Required = false, Default = Solver.WalkSat, HelpText = "Select which solver to use: WalkSat, GSat")]
            public Solver Solver { get; set; }


            [CommandLine.Option("max_restarts", Required = false, Default = 10000, HelpText = "The number of times to restart with a random assignment.")]
            public int MaxRestarts { get; set; }

            [CommandLine.Option("max_flips", Required = false, Default = 1000, HelpText = "The max number of flips to allow before restart occurs")]
            public int MaxFlips { get; set; }

            [CommandLine.Option("probability", Required = false, Default = 0.44, HelpText = "The probability of a random walk occuring")]
            public double Probability { get; set; }
        }

        [CommandLine.Verb("graphplansat", HelpText = "Try to solve a pddl planning problem using plangraph to SatPlan.")]
        class GpsOption
        {
            [CommandLine.Option('p', "problem", Required = true, HelpText = "The input problem pddl file")]
            public string InputProblemFile { get; set; }

            [CommandLine.Option('d', "domain", Required = true, HelpText = "The input domain pddl file")]
            public string InputDomainFile { get; set; }

            [CommandLine.Option('t', "time", Required = true, HelpText = "The number of steps to include in the formula")]
            public int Time { get; set; }

            [CommandLine.Option('v', "verbose", Default = true, HelpText = "Set verbosity")]
            public bool Verbose { get; set; }

            [CommandLine.Option('l', "literals", Required = false, HelpText = "The output cnf literals information file")]
            public string OutputLiteralsFile { get; set; }

            [CommandLine.Option('c', "cnf", Required = false, HelpText = "The output cnf file")]
            public string OutputCnfFile { get; set; }

            [CommandLine.Option('s', "solver", Required = false, Default = Solver.WalkSat, HelpText = "Select which solver to use: WalkSat, GSat")]
            public Solver Solver { get; set; }

            [CommandLine.Option("max_restarts", Required = false, Default = 10000, HelpText = "The number of times to restart with a random assignment.")]
            public int MaxRestarts { get; set; }

            [CommandLine.Option("max_flips", Required = false, Default = 1000, HelpText = "The max number of flips to allow before restart occurs")]
            public int MaxFlips { get; set; }

            [CommandLine.Option("probability", Required = false, Default = 0.44, HelpText = "The probability of a random walk occuring")]
            public double Probability { get; set; }

        }


        public enum Solver { WalkSat, GSat}

        static int Main(string[] args)
        {
            return CommandLine.Parser.Default.ParseArguments<SolveOptions, GpsOption>(args)
              .MapResult<SolveOptions, GpsOption, int>(
                (SolveOptions opts) => RunSolveAndExit(opts),
                (GpsOption opts) => RunPlanGraphAndExit(opts),
                errs => 1);

            //CommandLine.Parser.Default.ParseArguments<SolveOptions>(args).MapResult((SolveOptions opts) => RunSolveAndExit(opts), errs => 1);
            //CommandLine.Parser.Default.ParseArguments<GpsOption>(args).MapResult((GpsOption opts) => RunPlanGraphAndExit(opts), errs => 1);

        }

        static int RunSolveAndExit(SolveOptions opts)
        {
            try
            {
                var domain = PddlDomain.Load(opts.InputDomainFile);
                var problem = PddlProblem.Load(opts.InputProblemFile);

                var satPlanBuilder = new SatPlanBuilder(domain, problem);

                if (opts.Verbose)
                {
                    satPlanBuilder.SpewPlanAxioms(Console.Out, (IncludedClauses)opts.IncludedClauses);
                    Console.WriteLine();
                }
                if (!string.IsNullOrWhiteSpace(opts.OutputAxiomsFile))
                {
                    using (var writer = new System.IO.StreamWriter(opts.OutputAxiomsFile))
                    {
                        satPlanBuilder.SpewPlanAxioms(writer, (IncludedClauses)opts.IncludedClauses);
                    }
                }


                satPlanBuilder.TryBuildSatPlan(opts.Time, out var variables, out var clauses, (IncludedClauses)opts.IncludedClauses, opts.Verbose);
                if (!string.IsNullOrWhiteSpace(opts.OutputLiteralsFile))
                {
                    satPlanBuilder.WriteSatPlanToFile(opts.OutputLiteralsFile, variables, clauses);
                }

                var cnf = satPlanBuilder.GetCnf(variables, clauses);
                if (!string.IsNullOrWhiteSpace(opts.OutputCnfFile))
                {
                    cnf.SaveToFile(opts.OutputCnfFile);
                }

                Assignment assignment = null;
                switch (opts.Solver)
                {
                    case Solver.WalkSat:
                        assignment = Solvers.WalkSat(cnf, opts.Probability, opts.MaxFlips, opts.MaxRestarts, opts.Verbose);
                        break;
                    case Solver.GSat:
                        assignment = Solvers.GSat(cnf, opts.Probability, opts.MaxFlips, opts.MaxRestarts, opts.Verbose);
                        break;
                }

                if (assignment != null)
                {
                    foreach (var l in satPlanBuilder.ExtractPlan(assignment, variables, PddlUtils.GetActionVariables(domain, problem).Count() * opts.Time))
                    {
                        Console.WriteLine(l);
                    }
                 }

             }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
                return 1;
            }

            return 0;   
        }


        static int RunPlanGraphAndExit(GpsOption opts)
        {

            var domain = PddlDomain.Load(opts.InputDomainFile);
            var problem = PddlProblem.Load(opts.InputProblemFile);

            var planBuilder = new PlanGraph(domain, problem);
            planBuilder.BuildCnf(opts.Time, out var predicateVariables, out var actionVariables, out var clauses);
            var variables = actionVariables.ToList();
            variables.AddRange(predicateVariables);

            if (!string.IsNullOrWhiteSpace(opts.OutputLiteralsFile))
            {
                planBuilder.WriteLiteralsToFile(opts.OutputLiteralsFile, variables, clauses);
            }

            var cnf = planBuilder.GetCnf(variables, clauses);
            if (!string.IsNullOrWhiteSpace(opts.OutputCnfFile))
            {
                cnf.SaveToFile(opts.OutputCnfFile);
            }

            Assignment assignment = null;
            switch (opts.Solver)
            {
                case Solver.WalkSat:
                    assignment = Solvers.WalkSat(cnf, opts.Probability, opts.MaxFlips, opts.MaxRestarts, opts.Verbose);
                    break;
                case Solver.GSat:
                    assignment = Solvers.GSat(cnf, opts.Probability, opts.MaxFlips, opts.MaxRestarts, opts.Verbose);
                    break;
            }

            if (assignment != null)
            {
                foreach (var l in planBuilder.ExtractPlan(assignment, variables, actionVariables.Count, false))
                {
                    Console.WriteLine(l);
                }
            }

            return 0;
        }

    }
}
