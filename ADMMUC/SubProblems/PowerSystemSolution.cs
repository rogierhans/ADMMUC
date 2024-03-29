﻿using System;
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
        public GenerationSubproblem[] GenerationSubproblems;
        public RESSubProblem[] RESSubProblems;
        public TransmissionSubproblem TransmisssionSubproblems;
        readonly protected int totalTime;
        readonly protected int totalNodes;
        readonly protected int totalRes;
        readonly protected int totalUnits;
        protected double Rho;
        readonly protected double RhoMultiplier;
        readonly protected double multiplierMultiplier;
        readonly protected int rhoUpdateCounter;
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
            TransmisssionSubproblems = new TransmissionSubproblem(PowerSystem, totalTime);
            CreateResSolutions(totalTime);
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
                    NodeMultipliers[n, t] = node.UnitsIndex.Count == 0 ? 0 : node.UnitsIndex.Average(x => PowerSystem.Units[x].B);
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
        bool Converged()
        {
            if ((GenerationSubproblems.Sum(g => g.ReevalCost) > 100 && AbsoluteResidualLoad() < 0.001) && ConvergedObjective())
            {
                solutioncounter++;
            }
            else
            {
                solutioncounter = 0;
            }
            return Rho > (long)1 << 50 || solutioncounter >= 10;
        }
        protected int counter = 0;
        public virtual void RunIterations(int maxIterations)
        {
              
            while (i++ < maxIterations && !Converged())
            {
                Iteration(rhoUpdateCounter);
            }

            FinalIteration = i;
            FinalScore = GenerationSubproblems.Sum(g => g.ReevalCost);
        }


        public double GetScore()
        {

            return AbsoluteResidualLoad() * PowerSystem.VOLL + GenerationSubproblems.Sum(g => g.ReevalCost);
        }

        protected readonly Random RNG = new();
        protected readonly List<double> Values = new();


        public List<double> Deltas = new List<double>();

        public virtual void Iteration(int rhoUpdateCounter)
        {
            var CurrentDemand = GetDemand();
            foreach (var g in Enumerable.Range(0, RESSubProblems.Length).OrderBy(i => RNG.NextDouble()).ToList())
            {
                RESSubProblems[g].Reevaluate(NodeMultipliers, CurrentDemand, Rho, totalTime);
            }
            foreach (var g in Enumerable.Range(0, GenerationSubproblems.Length).OrderBy(i => RNG.NextDouble()).ToList())
            {
                GenerationSubproblems[g].Reevaluate(NodeMultipliers, CurrentDemand, Rho, totalTime);
            }

            if (PowerSystem.Nodes.Count > 1)
            {
                TransmisssionSubproblems.Reevaluate(NodeMultipliers, CurrentDemand, Rho);
            }
            UpdateMultiplers(Rho);
            if (counter++ % rhoUpdateCounter == 0 && GLOBAL.IncreaseRho)
            {
                Rho *= RhoMultiplier;
                if (ConvergedObjective() && AbsoluteResidualLoad()<1 && GLOBAL.ForceEnding)
                {
                    Rho *= 100;
                    rhoUpdateCounter = 1;
                }
            }
            Values.Add(GenerationSubproblems.Sum(g => g.ReevalCost));
        }
        protected void CreateResSolutions(int totalTime)
        {
            RESSubProblems = new RESSubProblem[totalRes];
            for (int r = 0; r < totalRes; r++)
            {
                RESSubProblems[r] = new RESSubProblem(PowerSystem.Res[r].ResValues.ToList().Take(totalTime).ToArray(), PowerSystem.Nodes.First(n => n.RESindex.Contains(r)).ID, totalTime);
            }
        }

        protected void CreateGenerationSolution(int totalTime, string name)
        {
            GenerationSubproblems = new GenerationSubproblem[totalUnits + totalNodes];
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
                var SGU = new SUC(unit.A, unit.B, unit.C, unit.StartCostInterval.First(), pMax, pMin, RU, RD, MinUp, minDownTime, SU, SD, totalTime);
                GenerationSubproblems[u] = new GenerationSubproblem(SGU, totalTime, PowerSystem.Nodes.First(node => node.UnitsIndex.Contains(u)).ID, name);
            }
            for (int n = 0; n < totalNodes; n++)
            {
                int index = totalUnits + n;
                var max = 10000;
                var UC = new SUC(0, 10000, 0, 0, max, 0, max, max, 2, 2, max, max, totalTime);
                GenerationSubproblems[index] = new GenerationSubproblem(UC, totalTime, n, name);
                PowerSystem.Nodes[n].UnitsIndex.Add(index);
            }
        }

        protected bool ConvergedObjective()
        {
            int k = 10;
            var LastK = Values.Skip(Values.Count - k).Take(k).ToList();
            bool p = true;
            for (int i = 0; i < LastK.Count - 1; i++)
            {
                p &= (Math.Abs(LastK[i] - LastK[i + 1]) / LastK[i]) < 0.0001;
            }
            return p && i>50 ;
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
                var genSolutions = PowerSystem.Nodes[n].UnitsIndex.Select(g => GenerationSubproblems[g]);
                var resSolutions = PowerSystem.Nodes[n].RESindex.Select(g => RESSubProblems[g]); ;
                for (int t = 0; t < totalTime; t++)
                {
                    Demand[n, t] = -genSolutions.Sum(g => g.CurrentDispatchAtTime[t]);
                    Demand[n, t] += -resSolutions.Sum(g => g.Dispatch[t]);
                    Demand[n, t] += TransmisssionSubproblems.CurrentExport[n, t];
                    Demand[n, t] += PowerSystem.Nodes[n].NodalDemand(t);
                }
            }
            return Demand;
        }

    }

}
