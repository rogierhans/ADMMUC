using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADMMUC.Solutions
{
    public class RESSubProblem
    {
        public double[] MaxDisptach;
        public double[] Dispatch;
        readonly int NodeID;
        readonly int TotalDispatchHorizon;
        public RESSubProblem(double[] maxDisptach, int node, int totaltime)
        {
            NodeID = node;
            MaxDisptach = maxDisptach;
            TotalDispatchHorizon = maxDisptach.Length;
            Dispatch = new double[totaltime];
        }
        public void Reevaluate(double[,] Multipliers, double[,] Demand, double rho, int totalTime)
        {
            Substract(Demand);
            for (int t = 0; t < totalTime; t++)
            {
                var B = -Multipliers[NodeID, t] + rho * -Demand[NodeID, t];
                var C = rho / 2;
                Dispatch[t] = MinimumAtInterval(t, B, C);
            }
            Add(Demand);
        }
        public double MinimumAtInterval(int t, double B, double C)
        {
            var max = MaxDisptach[t % TotalDispatchHorizon];
            if (C == 0)
            {
                if (B > 0)
                {
                    return 0;
                }
                else
                {
                    return max;
                }
            }
            double minimum = (-B / (2 * C));
            if (minimum < 0)
            {
                return 0;
            }
            else if (minimum > max)
            {
                return max;
            }
            else
            {
                return minimum;
            }
        }
        private void Substract(double[,] Demand)
        {
            for (int t = 0; t < Dispatch.Length; t++)
            {
                Demand[NodeID, t] = Demand[NodeID, t] + Dispatch[t];
            }
        }
        private void Add(double[,] Demand)
        {
            for (int t = 0; t < Dispatch.Length; t++)
            {
                Demand[NodeID, t] = Demand[NodeID, t] - Dispatch[t];
            }
        }
        internal double LR(double[,] nodeMultipliers, int totalTime)
        {
            double totalCost = 0;
            for (int t = 0; t < totalTime; t++)
            {
                if (nodeMultipliers[NodeID, t] >= 0)
                {
                    totalCost += -nodeMultipliers[NodeID, t] * MaxDisptach[t % TotalDispatchHorizon];
                }
            }
            return totalCost;
        }
    }
}
