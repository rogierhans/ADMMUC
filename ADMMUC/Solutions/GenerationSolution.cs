using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ADMMUC._1UC;

namespace ADMMUC.Solutions
{
    class GenerationSolution
    {
        readonly int NodeID;


        public double ReevalCost;
        public double ADMMCost;
        public  GeneratorQuadratic SGUC;
        public double[] CurrentDispatchAtTime;
        // readonly bool DebugCheck = true;
        public GenerationSolution(GeneratorQuadratic SGUC, int time, int nodeID)
        {
            this.SGUC = SGUC;
            CurrentDispatchAtTime = new double[time];
            NodeID = nodeID;

        }
        public void Print()
        {
            Console.WriteLine(string.Join("", CurrentDispatchAtTime.Select(x => x > 0.0001 ? 1 : 0)));
        }
        public double LR(double[,] Multipliers, int totalTime)
        {
            var LagrangeMultipliers = new double[totalTime];
            // int totalTime = LagrangeMultipliersLagrangeMultipliers.Count();
            double[] Bmultiplier = new double[totalTime];
            double[] Cmultiplier = new double[totalTime];
            for (int t = 0; t < totalTime; t++)
            {
                LagrangeMultipliers[t] = Multipliers[NodeID, t];
                Bmultiplier[t] = 0;
                Cmultiplier[t] = 0;
            }
            SGUC.SetLM(LagrangeMultipliers.ToList());
            SGUC.SetBM(Bmultiplier);
            SGUC.SetCM(Cmultiplier);
            var solution = new RRF(SGUC, true).GetSolution();


            //Check(solution);

            return solution.CostADMM;
        }
        public SUCSolution OldSolution;
        public void Reevaluate(double[,] Multipliers, double[,] Demand, double rho, int totalTime)
        {
            Substract(Demand);
            var LagrangeMultipliers = new double[totalTime];
            // int totalTime = LagrangeMultipliersLagrangeMultipliers.Count();
            double[] Bmultiplier = new double[totalTime];
            double[] Cmultiplier = new double[totalTime];
            for (int t = 0; t < totalTime; t++)
            {
                LagrangeMultipliers[t] = Multipliers[NodeID, t];
                Bmultiplier[t] = rho * -Demand[NodeID, t];


                Cmultiplier[t] = rho / 2;
            }

            SGUC.SetLM(LagrangeMultipliers.ToList());
            SGUC.SetBM(Bmultiplier);
            SGUC.SetCM(Cmultiplier);
            var solution = new RRF(SGUC, true).GetSolution();
            ADMMCost = solution.CostADMM;
            ReevalCost = ReevalSolution(SGUC, solution.Steps);
            for (int t = 0; t < Demand.GetLength(1); t++)
            {
                CurrentDispatchAtTime[t] = solution.Steps[t].P;
            }
            OldSolution = solution;
            Add(Demand);
            //var test = SGUC.CalcOptimum();

            //if (Math.Abs(test - ADMMCost) > 0.001)
            //{
            //    Console.WriteLine("{0} {1}", test, ADMMCost);
            //    Console.ReadLine();
            //}

        }

        public void GurobiDispose() {
            SGUC.Dispose();
        }

        public void ReGurobi(double[,] Multipliers, double[,] Demand, double rho, int totalTime)
        {
            Substract(Demand);
            var LagrangeMultipliers = new double[totalTime];
            // int totalTime = LagrangeMultipliersLagrangeMultipliers.Count();
            double[] Bmultiplier = new double[totalTime];
            double[] Cmultiplier = new double[totalTime];
            for (int t = 0; t < totalTime; t++)
            {
                LagrangeMultipliers[t] = Multipliers[NodeID, t];
                Bmultiplier[t] = rho * -Demand[NodeID, t];


                Cmultiplier[t] = rho / 2;
            }

            SGUC.SetLM(LagrangeMultipliers.ToList());
            SGUC.SetBM(Bmultiplier);
            SGUC.SetCM(Cmultiplier);
            (ADMMCost, ReevalCost, CurrentDispatchAtTime) = SGUC.CalcOptimum();

            Add(Demand);

        }
        private void Substract(double[,] Demand)
        {
            for (int t = 0; t < CurrentDispatchAtTime.Length; t++)
            {

                Demand[NodeID, t] = Demand[NodeID, t] + CurrentDispatchAtTime[t];
            }
        }
        private void Add(double[,] Demand)
        {
            for (int t = 0; t < CurrentDispatchAtTime.Length; t++)
            {
                Demand[NodeID, t] = Demand[NodeID, t] - CurrentDispatchAtTime[t];
            }
        }
        private static double ReevalSolution(GeneratorQuadratic UC, List<DPQSolution> solution)
        {
            double startCost = solution.Skip(1).Where(step => step.On && step.Tau == 0).Sum(step => UC.startCost);
            double generationCost = solution.Sum(step => (step.On ? UC.A : 0) + UC.B * step.P + UC.C * step.P * step.P);
            return startCost + generationCost;
        }

        internal void PrintStats()
        {
            SGUC.PrintStats();
        }
    }
}
