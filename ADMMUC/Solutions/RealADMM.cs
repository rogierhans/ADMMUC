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
    class RealADMM
    {
        PowerSystem PowerSystem;
        readonly double[,] NodeMultipliers;
        public GenerationSolution[] GSolutions;
        ResSolution[] RSolutions;
        readonly private int totalTime;
        private int totalNodes;
        private int totalRes;
        private int totalUnits;
        private double Rho = 0.01;
        double RhoMultiplier = 1.1;
        readonly private double rhoInit;
        readonly private double multiplierMultiplier;
        readonly private int rhoUpdateCounter;

        ResolveTrans Resolve;
        public RealADMM(string fileName, int totalTime, double rho, double rhoMultiplier, int rhoUpdateCounter, double multiplierMultiplier)
        {

            SetPowerSystem(fileName);
            Rho = rho;
            rhoInit = rho;
            RhoMultiplier = rhoMultiplier;
            this.totalTime = totalTime;
            this.multiplierMultiplier = multiplierMultiplier;
            NodeMultipliers = new double[totalNodes, totalTime];
            Demand = new double[totalNodes, totalTime];
            SetMultipliers();
            CreateGenerationSolution(totalTime);
            CreateTransmissionSolution(totalTime);
            CreateResSolutions(totalTime);
            Resolve = new ResolveTrans(PowerSystem, totalTime, true, false);
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
                    NodeMultipliers[n, t] = node.UnitsIndex.Count == 0 ? 0 : node.UnitsIndex.Max(x => PowerSystem.Units[x].B);
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
        public double FinalTime;
        public int FinalIteration;
        public Stopwatch SWTrans = new Stopwatch();
        public Stopwatch SWGen = new Stopwatch();
        public List<(double, double)> ResolvesScores = new List<(double, double)>();
        public int i = 0;
        public void WriteAlgorithmToFile(int maxIterations)
        {
            var swALL = new Stopwatch();
            var sw = new Stopwatch();
            var sw2 = new Stopwatch();

            swALL.Start();
            List<string> lines = new List<string>();
            while (i++ < maxIterations && !RhoLargeEnough())
            {
                sw.Start();
                Go(rhoUpdateCounter);
                sw.Stop();
                var line = GSolutions.Sum(g => g.ReevalCost) + "\t" + ResidualLoad();
                lines.Add(line);
                if (i % 10 == 0 && GSolutions.Sum(g => g.ReevalCost) > 1)
                {
                    if (GLOBAL.ResolveInteration)
                    {
                        sw2.Start();
                        ResolvesScores.Add((sw.Elapsed.TotalMilliseconds / 1000, ResolveSolutionWithMILP()));
                        sw2.Stop();
                    }
                }
            }
            FinalIteration = i;
            FinalScore = GSolutions.Sum(g => g.ReevalCost);
            FinalTime = sw.Elapsed.TotalMilliseconds / 1000;
            Resolve.KILL();
            GSolutions.ToList().ForEach(x => x.GurobiDispose());
            bool RhoLargeEnough()
            {
                if ((GSolutions.Sum(g => g.ReevalCost) > 100 && ResidualLoad() < 0.001) && ConvergedObjective())
                {
                    solutioncounter++;
                }
                else
                { solutioncounter = 0; }
                return Rho > (long)1 << 50 || solutioncounter >= 10;
            }
        }


        public double GetScore()
        {

            return ResidualLoad() * PowerSystem.VOLL + GSolutions.Sum(g => g.ReevalCost);
        }
        int solutioncounter = 0;
        int counter = 0;
        Random RNG = new Random();
        List<double> Values = new List<double>();
        public void Go(int rhoUpdateCounter)
        {
            var CurrentDemand = GetDemand();
            foreach (var g in Enumerable.Range(0, RSolutions.Length).OrderBy(i => RNG.NextDouble()).ToList())
            {
                RSolutions[g].Reevaluate(NodeMultipliers, CurrentDemand.Clone() as double[,], Rho, totalTime);
            }
            SWGen.Start();
            foreach (var g in Enumerable.Range(0, GSolutions.Length).OrderBy(i => RNG.NextDouble()).ToList())
            {
                if (GLOBAL.UseGurobi)
                    GSolutions[g].ReGurobi(NodeMultipliers, CurrentDemand.Clone() as double[,], Rho, totalTime);
                else
                    GSolutions[g].Reevaluate(NodeMultipliers, CurrentDemand.Clone() as double[,], Rho, totalTime);
            }
            SWGen.Stop();
            if (PowerSystem.Nodes.Count > 1)
            {
                SWTrans.Start();
                TSolutions.Reevaluate(NodeMultipliers, CurrentDemand.Clone() as double[,], Rho);
                SWTrans.Stop();
            }
            UpdateMultiplers(Rho);
            if (counter++ % rhoUpdateCounter == 0 &&GLOBAL.IncreaseRho)
            {
                Rho = Rho * RhoMultiplier;
                if (ConvergedObjective() && GLOBAL.ForceEnding)
                {
                    Rho = Rho * 10;
                    rhoUpdateCounter = 1;
                }
            }
            Values.Add(GSolutions.Sum(g => g.ReevalCost));
            if (GLOBAL.PRINTIteration)
                Console.WriteLine(GSolutions.Sum(g => g.ReevalCost) + " " +
                    ResidualLoad() + " " +
                    Rho + "                  " +
                    SWGen.Elapsed.TotalSeconds + " " +
                    SWTrans.Elapsed.TotalSeconds + " " +
                    GetScore() + " " +
                    MaxMultiplier() + " " + MinMultiplier());
        }

        private double MaxMultiplier()
        {

            double max = 0;
            for (int t = 0; t < totalTime; t++)
            {
                for (int n = 0; n < totalNodes; n++)
                {
                    max = Math.Max(max, (NodeMultipliers[n, t]));
                }
            }
            return max;
        }

        private double MinMultiplier()
        {

            double Min = 0;
            for (int t = 0; t < totalTime; t++)
            {
                for (int n = 0; n < totalNodes; n++)
                {
                    Min = Math.Min(Min, (NodeMultipliers[n, t]));
                }
            }
            return Min;
        }
        public double FinalResolveTime;
        public double FinalResolveScore;

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
            FinalResolveTime = ms / 1000;
            FinalResolveScore = val;
            Console.WriteLine(val + " " + cost + " " + lol);
            return val;
        }

        private void Check()
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
            //  if (val > GSolutions.Sum(g => g.ReevalCost))
            Resolve.Check(commits, Ps);
        }

        private void CreateResSolutions(int totalTime)
        {
            RSolutions = new ResSolution[totalRes];
            for (int r = 0; r < totalRes; r++)
            {
                RSolutions[r] = new ResSolution(PowerSystem.Res[r].ResValues.ToList().Take(totalTime).ToArray(), PowerSystem.Nodes.First(n => n.RESindex.Contains(r)).ID, totalTime);
            }
        }
        ADMMTrans TSolutions;
        private void CreateTransmissionSolution(int totalTime)
        {
            TSolutions = new ADMMTrans(PowerSystem, totalTime);
        }

        private void CreateGenerationSolution(int totalTime)
        {
            GSolutions = new GenerationSolution[totalUnits + totalNodes];
            for (int u = 0; u < totalUnits; u++)
            {
                var unit = PowerSystem.Units[u];
                int pMin = (int)unit.PMin;
                int pMax = (int)unit.PMax;

                int RU = (int)unit.RampUp;
                int RD = (int)unit.RampDown;
                int MinUp = (int)unit.MinUpTime;
                int minDownTime = unit.MinDownTime;
                int SD = (int)Math.Max(pMin, unit.ShutDown);
                int SU = (int)Math.Max(pMin, unit.StartUp);
                if (pMin >= pMax || SD > pMax || SU > pMax || MinUp < 1 || minDownTime < 1 || SD < pMin || SU < pMin || pMin < 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("[{0},{1}] SD:{2} SU:{3}    up:{4} down:{5}", pMin, pMax, SD, SU, MinUp, minDownTime);
                    Console.WriteLine("wooops");
                    Console.ReadLine();
                }
                else
                {
                    var SGU = new SUC(unit.A, unit.B, unit.C, unit.StartCostInterval.First(), pMax, pMin, RU, RD, MinUp, minDownTime, SU, SD, totalTime);
                    GSolutions[u] = new GenerationSolution(SGU, totalTime, PowerSystem.Nodes.First(node => node.UnitsIndex.Contains(u)).ID , "NoName");
                }
            }
            Console.Write(PowerSystem.Nodes.Count);
            for (int n = 0; n < totalNodes; n++)
            {
                int index = totalUnits + n;
                var max = 10000;
                var UC = new SUC(0, 10000, 0, 0, max, 0, max, max, 2, 2, max, max, totalTime);
                Console.WriteLine(index);
                GSolutions[index] = new GenerationSolution(UC, totalTime, n, "NoName");
                Console.WriteLine();
                Console.Write(PowerSystem.Nodes.Count);
                Console.WriteLine();
                Console.Write(PowerSystem.Nodes[n]);
                PowerSystem.Nodes[n].UnitsIndex.Add(index);
            }
        }

        private void SetPowerSystem(string fileName)
        {
            var ConstraintConfiguration = new ConstraintConfiguration(false, false, "", false, false, false, 1);
            ConstraintConfiguration.SetLimits(0, -1, -1, -1);
            PowerSystem = IOUtils.GetPowerSystem(fileName, ConstraintConfiguration);
            totalNodes = PowerSystem.Nodes.Count();
            totalUnits = PowerSystem.Units.Count;
            totalRes = PowerSystem.Res.Count;
        }


        private bool ConvergedObjective()

        {
            int k = 10;
            var LastK = Values.Skip(Values.Count - k).Take(k).ToList();
            bool p = true;
            for (int i = 0; i < LastK.Count() - 1; i++)
            {
                p &= (Math.Abs(LastK[i] - LastK[i + 1]) / LastK[i]) < 0.0000001;
            }
            return p && counter > 500;
        }

       

        


        private double ResidualLoad()
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

        private double LRReeval()
        {
            double cost = 0;
            foreach (var g in Enumerable.Range(0, GSolutions.Length).OrderBy(i => RNG.NextDouble()).ToList())
            {
                cost += GSolutions[g].LR(NodeMultipliers, totalTime);
            }
            // Console.WriteLine(cost);
            for (int t = 0; t < totalTime; t++)
            {
                for (int n = 0; n < totalNodes; n++)
                {
                    cost += NodeMultipliers[n, t] * PowerSystem.Nodes[n].NodalDemand(t);
                }
            }
            // Console.WriteLine(cost);
            for (int t = 0; t < totalTime; t++)
            {
                //    cost += TSolutions[t].LR(NodeMultipliers);
            }
            foreach (var g in Enumerable.Range(0, RSolutions.Length).OrderBy(i => RNG.NextDouble()).ToList())
            {

                cost += RSolutions[g].LR(NodeMultipliers, totalTime);
            }
            return cost;
        }

        public double[,] Demand = new double[1, 1];
        public double[,] GetDemand2()
        {
            for (int n = 0; n < totalNodes; n++)
            {
                var genSolutions = PowerSystem.Nodes[n].UnitsIndex.Select(g => GSolutions[g]);
                var resSolutions = PowerSystem.Nodes[n].RESindex.Select(g => RSolutions[g]); ;
                for (int t = 0; t < totalTime; t++)
                {
                    Demand[n, t] = -genSolutions.Sum(g => g.CurrentDispatchAtTime[t]);
                    Demand[n, t] += -resSolutions.Sum(g => g.Dispatch[t]);
                    Demand[n, t] += TSolutions.CurrentExport[n, t];
                    Demand[n, t] += PowerSystem.Nodes[n].NodalDemand(t);
                }
            }
            return Demand;
        }
        public double[,] GetDemand()
        {
            for (int n = 0; n < totalNodes; n++)
            {
                int totalAgents = 0;
                totalAgents += PowerSystem.Nodes[n].UnitsIndex.Select(g => GSolutions[g]).Count();
                totalAgents += PowerSystem.Nodes[n].RESindex.Select(g => RSolutions[g]).Count();
                totalAgents += 1 + 1; //demand and trans
                var genSolutions = PowerSystem.Nodes[n].UnitsIndex.Select(g => GSolutions[g]);
                var resSolutions = PowerSystem.Nodes[n].RESindex.Select(g => RSolutions[g]); ;
                for (int t = 0; t < totalTime; t++)
                {
                    Demand[n, t] = -genSolutions.Sum(g => g.CurrentDispatchAtTime[t]);
                    Demand[n, t] += -resSolutions.Sum(g => g.Dispatch[t]);
                    Demand[n, t] += TSolutions.CurrentExport[n, t];
                    Demand[n, t] += PowerSystem.Nodes[n].NodalDemand(t);
                    Demand[n, t] = Demand[n, t] / totalAgents;
                }
            }
            return Demand;
        }
    }
}
