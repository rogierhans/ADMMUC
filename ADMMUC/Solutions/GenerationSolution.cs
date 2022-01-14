using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ADMMUC._1UC;

namespace ADMMUC.Solutions
{
    [Serializable]
    public class GenerationSolution
    {
        readonly int NodeID;


        public double ReevalCost;
        public double ADMMCost;
        public SUC SGUC;
        public double[] CurrentDispatchAtTime;
        public SUCSolution OldSolution;
        // readonly bool DebugCheck = true;
        private Gurobi1UC G1UC;

        private string Name;
        public GenerationSolution(SUC SGUC, int time, int nodeID, string name)
        {
            this.SGUC = SGUC;
            CurrentDispatchAtTime = new double[time];
            NodeID = nodeID;
            Name = name;
            G1UC = new Gurobi1UC(SGUC,GLOBAL.RelaxGurobi);
        }
        public void Print()
        {
            Console.WriteLine(string.Join("", CurrentDispatchAtTime.Select(x => x > 0.0001 ? 1 : 0)));
        }
        public double LR(double[,] Multipliers, int totalTime)
        {
            var LagrangeMultipliers = new double[totalTime];
            double[] Bmultiplier = new double[totalTime];
            double[] Cmultiplier = new double[totalTime];
            for (int t = 0; t < totalTime; t++)
            {
                LagrangeMultipliers[t] = Multipliers[NodeID, t];
                Bmultiplier[t] = 0;
                Cmultiplier[t] = 0;
            }
            SGUC.SetLM(LagrangeMultipliers.ToList(), Bmultiplier, Cmultiplier);
            var solution = new RRF(SGUC, true).GetSolution();
            //Check(solution);
            return solution.CostADMM;
        }
        static int counter = 0;
        static int extraCounter = 0;
        public double Reevaluate(double[,] Multipliers, double[,] Demand, double rho, int totalTime, bool test = false)
        {
            Substract(Demand);
            var LagrangeMultipliers = new double[totalTime];
            double[] Bmultiplier = new double[totalTime];
            double[] Cmultiplier = new double[totalTime];
            for (int t = 0; t < totalTime; t++)
            {
                LagrangeMultipliers[t] = Multipliers[NodeID, t];
                Bmultiplier[t] = rho * -Demand[NodeID, t];


                Cmultiplier[t] = rho / 2;
            }

            SGUC.SetLM(LagrangeMultipliers.ToList(), Bmultiplier, Cmultiplier);
            var solution = new RRF(SGUC, true).GetSolution();
            ADMMCost = solution.CostADMM;
            ReevalCost = ReevalSolution(SGUC, solution.Steps);
            for (int t = 0; t < Demand.GetLength(1); t++)
            {
                CurrentDispatchAtTime[t] = solution.Steps[t].P;
            }
            OldSolution = solution;
            Add(Demand);

            if (!test) return 0;
            return ExtractForTesting(LagrangeMultipliers, Bmultiplier, Cmultiplier);
        }

        private double ExtractForTesting(double[] LagrangeMultipliers, double[] Bmultiplier, double[] Cmultiplier)
        {
            var (gscore, _, _) = G1UC.CalcOptimum();
            if (Math.Abs(gscore - ADMMCost) > 0.001)
            {

                Console.WriteLine("L max:{0}", LagrangeMultipliers.Max());
                Console.WriteLine("B max:{0}", Bmultiplier.Max());
                Console.WriteLine("C max:{0}", Cmultiplier.Max());
                Console.WriteLine(Math.Abs(gscore - ADMMCost));
                G1UC.Print();
                SGUC.PrintStats();
                SGUC.Objective = gscore;
                SGUC.WriteToFile(@"C:\Users\Rogier\OneDrive - Universiteit Utrecht\1UCTest\Counter\" + (extraCounter++) + ".suc");
            }
            else
            {
                if (counter < 10000)
                {
                    if (!Directory.Exists(@"C:\Users\Rogier\OneDrive - Universiteit Utrecht\1UCTest\" + Name + @"\"))
                    {
                        // Try to create the directory.
                        DirectoryInfo di = Directory.CreateDirectory(@"C:\Users\Rogier\OneDrive - Universiteit Utrecht\1UCTest\" + Name + @"\");
                    }
                    SGUC.Objective = gscore;
                    SGUC.WriteToFile(@"C:\Users\Rogier\OneDrive - Universiteit Utrecht\1UCTest\" + Name + @"\" + (counter++) + ".suc");
                }
            }
            return Math.Abs(gscore - ADMMCost);
        }

        public void GurobiDispose()
        {
            G1UC.Dispose();
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

            SGUC.SetLM(LagrangeMultipliers.ToList(), Bmultiplier, Cmultiplier);
            (ADMMCost, ReevalCost, CurrentDispatchAtTime) = G1UC.CalcOptimum();

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
        private static double ReevalSolution(SUC UC, List<DPQSolution> solution)
        {
            double startCost = solution.Skip(1).Where(step => step.On && step.Tau == 0).Sum(step => UC.StartCost);
            double generationCost = solution.Sum(step => (step.On ? UC.A : 0) + UC.B * step.P + UC.C * step.P * step.P);
            return startCost + generationCost;
        }
    }
}
