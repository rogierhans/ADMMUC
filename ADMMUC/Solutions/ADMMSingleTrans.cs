using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;
using System.Diagnostics;
namespace ADMMUC.Solutions
{
    class ADMMSingleTrans
    {
        readonly int T;
        readonly List<TransmissionLine> Lines;
        readonly PowerSystem PS;
        readonly int totalNodes;

        public ADMMSingleTrans(PowerSystem ps, int t)
        {
            totalNodes = ps.Nodes.Count;
            Lines = ps.Lines;
            T = t;
            PS = ps;
            flows = new double[Lines.Count];
            export = new double[totalNodes];
            exportMin = new double[totalNodes];
            exportMax = new double[totalNodes];
            FlowTotal = new double[totalNodes];
            Lagrange = RandomMultipliers();
            for (int n = 0; n < totalNodes; n++)
            {
                var node = PS.Nodes[n];
                exportMin[n] = -node.NodalDemand(T);
                exportMax[n] = node.PotentialExport(PS);
            }
        }

        readonly public double[] flows;
        readonly public double[] export;
        readonly public double[] exportMin;
        readonly public double[] exportMax;
        readonly public double[] FlowTotal;




        readonly  double[] Lagrange;
        public double rho = 1;
        public double Calculate(double[] Bs, double[] Cs)
        {
            double currentValue = Iteration(Bs, Cs);
            while ((ResidualLoad() > 0.000001 || rho > 1) && ResidualLoad() > 0.01)// && counter++< 30)
            {
                for (int n = 0; n < totalNodes; n++)
                {
                    Lagrange[n] = Lagrange[n] + (export[n] - FlowTotal[n]) * rho;
                }
                currentValue = Iteration(Bs, Cs);
            }
            return currentValue;
        }

        private double ResidualLoad()
        {
            double total = 0;
            for (int n = 0; n < PS.Nodes.Count; n++)
            {
                total += Math.Abs(export[n] - FlowTotal[n]);
            }
            return total;
        }

        public double RecalcPrimal(double[] Bs, double[] Cs)
        {
            double total = 0;
            for (int n = 0; n < PS.Nodes.Count; n++)
            {
                total += Bs[n] * (FlowTotal[n]) + Cs[n] * FlowTotal[n] * FlowTotal[n];
            }
            return total;
        }

        readonly Random rng = new ();
        private double[] RandomMultipliers()
        {
            double[] Lagrange = new double[totalNodes];
            for (int n = 0; n < PS.Nodes.Count; n++)
            {
                Lagrange[n] = rng.NextDouble() * 10 - 5;

            }

            return Lagrange;
        }

        private double Iteration(double[] Bs, double[] Cs)
        {
            double value = 0;
            // GRBQuadExpr objective = 0;
            for (int l = 0; l < Lines.Count; l++)
            {
                var line = Lines[l];
                FlowTotal[line.From.ID] -= flows[l];
                FlowTotal[line.To.ID] += flows[l];

                double b1 = -(export[line.From.ID] - FlowTotal[line.From.ID]) * rho;
                double b2 = (export[line.To.ID] - FlowTotal[line.To.ID]) * rho;

                flows[l] = MinimumAtInterval(-Lagrange[line.From.ID] + Lagrange[line.To.ID] + b1 + b2, rho, line.MinCapacity, line.MaxCapacity);
                FlowTotal[line.From.ID] += flows[l];
                FlowTotal[line.To.ID] -= flows[l];
            }
            for (int n = 0; n < totalNodes; n++)
            {
                export[n] = 0;
                export[n] = MinimumAtInterval(Bs[n] + Lagrange[n] - (rho * FlowTotal[n]), Cs[n] + rho / 2, exportMin[n], exportMax[n]);
                value += (Bs[n] + Lagrange[n]) * export[n] + Cs[n] * export[n] * export[n];
                value += -Lagrange[n] * FlowTotal[n];
            }

            //model.SetObjective(objective, GRB.MINIMIZE);
            //model.Optimize();
            //value += objective.Value;


            return value;
        }

        public static double MinimumAtInterval(double B, double C, double min, double max)
        {
            if (C == 0)
            {
                if (B > 0) return min;
                else return max;
            }

            double minimum = (-B / (2 * C));

            if (minimum < min) return min;
            else if (minimum > max) return max;
            else return minimum;
        }
    }
}
