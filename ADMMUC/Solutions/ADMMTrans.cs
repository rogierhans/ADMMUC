using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;
using System.Diagnostics;
using System.IO;
namespace ADMMUC.Solutions
{
    class ADMMTrans
    {
        private readonly List<TransmissionLine> Lines;

        //public double ADMMCost;
        private readonly int totalTime;
        private readonly int totalNodes;
        private readonly PowerSystem PS;
        private readonly ADMMSingleTrans[] TransSubproblem;
        private double[,] _currentExport;
        public double[,] CurrentExport { get => _currentExport; set => _currentExport = value; }

        public ADMMTrans(PowerSystem ps, int totalTime)
        {

            totalNodes = ps.Nodes.Count;
            CurrentExport = new double[totalNodes, totalTime];
            Lines = ps.Lines;
            this.totalTime = totalTime;
            PS = ps;
            TransSubproblem = new ADMMSingleTrans[totalTime];
            for (int t = 0; t < totalTime; t++)
            {
                TransSubproblem[t] = new ADMMSingleTrans(PS, t);
            }

        }

        public void Reevaluate(double[,] LagrangeMultipliers, double[,] demand, double rho)
        {
            double[,] Demand = new double[totalNodes, totalTime];
            for (int t = 0; t < totalTime; t++)
                for (int n = 0; n < totalNodes; n++)
                {
                    Demand[n, t] = demand[n, t] - CurrentExport[n, t];
                }
            var value = Calculate(LagrangeMultipliers, rho, Demand);
            for (int t = 0; t < totalTime; t++)
                for (int n = 0; n < totalNodes; n++)
                {
                    CurrentExport[n, t] = TransSubproblem[t].export[n];
                }
        }

        private double Calculate(double[,] LagrangeMultipliers, double rho, double[,] Demand)
        {
            double total = 0;
            for (int t = 0; t < totalTime; t++)
            {
                TransSubproblem[t].rho = rho;
                double[] Bs = new double[totalNodes];
                double[] Cs = new double[totalNodes];

                for (int n = 0; n < totalNodes; n++)
                {
                    total += LagrangeMultipliers[n, t] * Demand[n, t] + 0.5 * rho * (Demand[n, t] * Demand[n, t]);

                    Bs[n] = (LagrangeMultipliers[n, t] + rho * Demand[n, t]);
                    Cs[n] = 0.5 * rho;
                }
                total += TransSubproblem[t].Calculate(Bs, Cs);
            }
            return total;
        }
    }
}
