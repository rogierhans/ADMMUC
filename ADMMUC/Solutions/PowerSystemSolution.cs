using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;
using System.IO;
using System.Diagnostics;

namespace ADMMUC.Solutions
{

    public class PowerSystemSolution
    {
        protected readonly PowerSystem PowerSystem;
        protected readonly double[,] NodeMultipliers;
        public GenerationSolution[] GSolutions;
        public ResSolution[] RSolutions;
        public ADMMTrans TSolution;
        readonly protected int totalTime;
        readonly protected int totalNodes;
        readonly protected int totalRes;
        readonly protected int totalUnits;
        protected double Rho;
        readonly protected double RhoMultiplier;
        readonly protected double multiplierMultiplier;
        readonly protected int rhoUpdateCounter;
        readonly ResolveTrans Resolve;
        public PowerSystemSolution(string fileName, int totalTime, double rho, double rhoMultiplier, int rhoUpdateCounter, double multiplierMultiplier)
        {

            var ConstraintConfiguration = new ConstraintConfiguration(false, false, "", false, false, false, 1);
            ConstraintConfiguration.SetLimits(0, -1, -1, -1);
            PowerSystem = IOUtils.GetPowerSystem(fileName, ConstraintConfiguration);
            totalNodes = PowerSystem.Nodes.Count;
            totalUnits = PowerSystem.Units.Count;
            totalRes = PowerSystem.Res.Count;
            NodeMultipliers = new double[totalNodes, totalTime];
            Demand = new double[totalNodes, totalTime];
            Rho = rho;
            RhoMultiplier = rhoMultiplier;
            this.totalTime = totalTime;
            this.multiplierMultiplier = multiplierMultiplier;

            SetMultipliers();
            CreateGenerationSolution(totalTime, fileName.Split('\\').Last().Split('.').First());
            TSolution = new ADMMTrans(PowerSystem, totalTime);
            CreateResSolutions(totalTime);
          //  Resolve = new ResolveTrans(PowerSystem, totalTime, true, false);
            this.rhoUpdateCounter = rhoUpdateCounter;
        }

        private void SetMultipliers()
        {
            for (int t = 0; t < totalTime; t++)
            {
                double price = PowerSystem.Units.Min(x => x.B);
                for (int n = 0; n < totalNodes; n++)
                {
                    var node = PowerSystem.Nodes[n];
                    NodeMultipliers[n, t] =  node.UnitsIndex.Count == 0 ? 0 : node.UnitsIndex.Average(x => PowerSystem.Units[x].B);
                }
            }
        }
        public void UpdateMultiplers(double rho)
        {
            var UpdateMultipliers = GetDemand();
            for (int n = 0; n < totalNodes; n++)
            {
                for (int t = 0; t < totalTime; t++)
                {
                    NodeMultipliers[n, t] = NodeMultipliers[n, t] + rho * multiplierMultiplier * UpdateMultipliers[n, t];
                }
            }
        }
        public double FinalScore;
        public int FinalIteration;
        public int i = 0;
        protected int solutioncounter = 0;
        protected int counter = 0;
        public virtual void RunIterations(int maxIterations)
        {

            while (i++ < maxIterations && !Converged())
            {
                Go(rhoUpdateCounter);
                if (i % 100 == 0 && GSolutions.Sum(g => g.ReevalCost) > 1)
                {
                    if (GLOBAL.ResolveInteration)
                    {

                        ResolveSolutionWithMILP();
                    }
                }
            }

            FinalIteration = i;
            FinalScore = GSolutions.Sum(g => g.ReevalCost);
          //  Resolve.KILL();
            //GSolutions.ToList().ForEach(x => x.GurobiDispose());
            bool Converged()
            {
                if ((GSolutions.Sum(g => g.ReevalCost) > 100 && AbsoluteResidualLoad() < 0.001) && ConvergedObjective())
                {
                    solutioncounter++;
                }
                else
                {
                    solutioncounter = 0;
                }
                return Rho > (long)1 << 50 || solutioncounter >= 10;
            }
        }


        public double GetScore()
        {

            return AbsoluteResidualLoad() * PowerSystem.VOLL + GSolutions.Sum(g => g.ReevalCost);
        }

        protected readonly Random RNG = new();
        protected readonly List<double> Values = new();

        public bool Test1UC = false;

        public List<double> Deltas = new List<double>();

        public virtual void Go(int rhoUpdateCounter)
        {
          //  Console.WriteLine(GSolutions.Sum(g => g.ReevalCost) + " " + AbsoluteResidualLoad() + " " + Rho);
            //PrintMultipliers();
            var CurrentDemand = GetDemand();
            foreach (var g in Enumerable.Range(0, RSolutions.Length).OrderBy(i => RNG.NextDouble()).ToList())
            {
                RSolutions[g].Reevaluate(NodeMultipliers, CurrentDemand, Rho, totalTime);
            }
            foreach (var g in Enumerable.Range(0, GSolutions.Length).OrderBy(i => RNG.NextDouble()).ToList())
            {
                if (GLOBAL.UseGurobi)
                    GSolutions[g].ReGurobi(NodeMultipliers, CurrentDemand, Rho, totalTime);
                else
                {
                    var delta = GSolutions[g].Reevaluate(NodeMultipliers, CurrentDemand, Rho, totalTime, Test1UC);
                    Deltas.Add(delta);

                }
            }
            if (PowerSystem.Nodes.Count > 1)
            {
                TSolution.Reevaluate(NodeMultipliers, CurrentDemand, Rho);
            }
            UpdateMultiplers(Rho);
            if (counter++ % rhoUpdateCounter == 0 && GLOBAL.IncreaseRho)
            {
                Rho *= RhoMultiplier;
                if (ConvergedObjective() && GLOBAL.ForceEnding)
                {
                    Rho *= 10;
                    rhoUpdateCounter = 1;
                }
            }
            Values.Add(GSolutions.Sum(g => g.ReevalCost));
        }
        protected void CreateResSolutions(int totalTime)
        {
            RSolutions = new ResSolution[totalRes];
            for (int r = 0; r < totalRes; r++)
            {
                RSolutions[r] = new ResSolution(PowerSystem.Res[r].ResValues.ToList().Take(totalTime).ToArray(), PowerSystem.Nodes.First(n => n.RESindex.Contains(r)).ID, totalTime);
            }
        }

        protected void CreateGenerationSolution(int totalTime, string name)
        {
            GSolutions = new GenerationSolution[totalUnits + totalNodes];
            for (int u = 0; u < totalUnits; u++)
            {
                var unit = PowerSystem.Units[u];
                int pMin = (int)unit.PMin;
                int pMax = (int)unit.PMax;
                int RU = (int)unit.RampUp;
                int RD = (int)unit.RampDown;
                int MinUp = unit.MinUpTime;
                int minDownTime = unit.MinDownTime;
                int SD = (int)Math.Max(pMin, unit.ShutDown);
                int SU = (int)Math.Max(pMin, unit.StartUp);
                if (pMin >= pMax || SD > pMax || SU > pMax || MinUp < 1 || minDownTime < 1 || SD < pMin || SU < pMin || pMin < 0)
                {
                    Console.WriteLine("[{0},{1}] SD:{2} SU:{3}    up:{4} down:{5}", pMin, pMax, SD, SU, MinUp, minDownTime);
                    throw new Exception();
                }
                else
                {
                    var SGU = new SUC(unit.A, unit.B, unit.C, unit.StartCostInterval.First(), pMax, pMin, RU, RD, MinUp, minDownTime, SU, SD, totalTime);
                    GSolutions[u] = new GenerationSolution(SGU, totalTime, PowerSystem.Nodes.First(node => node.UnitsIndex.Contains(u)).ID, name);
                    // SGU.CreateEnv(GLOBAL.RelaxGurobi);
                }
            }
            for (int n = 0; n < totalNodes; n++)
            {
                int index = totalUnits + n;
                var max = 10000;
                var UC = new SUC(0, 10000, 0, 0, max, 0, max, max, 2, 2, max, max, totalTime);
                GSolutions[index] = new GenerationSolution(UC, totalTime, n, name);
                PowerSystem.Nodes[n].UnitsIndex.Add(index);
                //UC.CreateEnv(GLOBAL.RelaxGurobi);
            }
        }

        protected bool ConvergedObjective()
        {
            int k = 10;
            var LastK = Values.Skip(Values.Count - k).Take(k).ToList();
            bool p = true;
            for (int i = 0; i < LastK.Count - 1; i++)
            {
                p &= (Math.Abs(LastK[i] - LastK[i + 1]) / LastK[i]) < 0.0000001;
            }
            return p && counter > 500;
        }


        protected double AbsoluteResidualLoad()
        {
            double total = 0;
            var demand = GetDemand();
            for (int t = 0; t < totalTime; t++)
            {
                for (int n = 0; n < totalNodes; n++)
                {
                    total += Math.Abs(demand[n, t]);
                }
            }
            return total;
        }


        public double[,] Demand;
        public double[,] GetDemand()
        {
            for (int n = 0; n < totalNodes; n++)
            {
                var genSolutions = PowerSystem.Nodes[n].UnitsIndex.Select(g => GSolutions[g]);
                var resSolutions = PowerSystem.Nodes[n].RESindex.Select(g => RSolutions[g]); ;
                for (int t = 0; t < totalTime; t++)
                {
                    Demand[n, t] = -genSolutions.Sum(g => g.CurrentDispatchAtTime[t]);
                    Demand[n, t] += -resSolutions.Sum(g => g.Dispatch[t]);
                    Demand[n, t] += TSolution.CurrentExport[n, t];
                    Demand[n, t] += PowerSystem.Nodes[n].NodalDemand(t);
                }
            }
            return Demand;
        }



        //private double LRReeval()
        //{
        //    double cost = 0;
        //    foreach (var g in Enumerable.Range(0, GSolutions.Length).OrderBy(i => RNG.NextDouble()).ToList())
        //    {
        //        cost += GSolutions[g].LR(NodeMultipliers, totalTime);
        //    }
        //    // Console.WriteLine(cost);
        //    for (int t = 0; t < totalTime; t++)
        //    {
        //        for (int n = 0; n < totalNodes; n++)
        //        {
        //            cost += NodeMultipliers[n, t] * PowerSystem.Nodes[n].NodalDemand(t);
        //        }
        //    }
        //    // Console.WriteLine(cost);
        //    for (int t = 0; t < totalTime; t++)
        //    {
        //        //    cost += TSolutions[t].LR(NodeMultipliers);
        //    }
        //    foreach (var g in Enumerable.Range(0, RSolutions.Length).OrderBy(i => RNG.NextDouble()).ToList())
        //    {

        //        cost += RSolutions[g].LR(NodeMultipliers, totalTime);
        //    }
        //    return cost;
        //}

        //public double FinalResolveTime;
        //public double FinalResolveScore;
        private void PrintMultipliers() {

            for (int n = 0; n < totalNodes; n++)
            {
                List<double> multies = new List<double>();
                for (int t = 0; t < totalTime; t++)
                {
                    multies.Add(Math.Round(NodeMultipliers[n, t],1));
                }
                Console.WriteLine(String.Join("\t",multies));
                //Console.ReadLine();
            }
        }

        private double ResolveSolutionWithMILP()
        {
            var commits = new int[totalTime, totalUnits];
            var Ps = new double[totalTime, totalUnits];
            for (int t = 0; t < totalTime; t++)
            {
                for (int g = 0; g < totalUnits; g++)
                {
                    commits[t, g] = GSolutions[g].OldSolution.Steps[t].On ? 1 : 0;
                    Ps[t, g] = GSolutions[g].CurrentDispatchAtTime[t];
                }
            }

            (double val, double ms, double cost, double lol) = Resolve.Solve(commits);
            Console.WriteLine("RESOLVE "+ val + " " + cost + " " + lol);
            return val;
        }
    }

    //private void Check()
    //{
    //    var commits = new int[totalTime, totalUnits];
    //    var Ps = new double[totalTime, totalUnits];
    //    for (int t = 0; t < totalTime; t++)
    //    {
    //        for (int g = 0; g < totalUnits; g++)
    //        {
    //            commits[t, g] = GSolutions[g].OldSolution.Steps[t].On ? 1 : 0;
    //            Ps[t, g] = GSolutions[g].CurrentDispatchAtTime[t];
    //        }
    //    }
    //    Resolve.Check(commits, Ps);
    //}

}
