using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;

namespace ADMMUC.Solutions
{
    public class Gurobi1UC
    {
        SUC GQ;
        public Gurobi1UC(SUC gq) {
            GQ = gq;
            CreateEnv(false);
        }


        public int MaxIntervals = 0;
        public GRBVar[] P; // time 
        public GRBVar[] Commit; // time
        public GRBVar[] Start; // time 
        public GRBVar[] Stop; // time 
        private GRBModel Model;
        private GRBEnv env;
        GRBModel model;
        public void Dispose()
        {
            model.Dispose();
        }

        public void CreateEnv(bool Relax)
        {
           // Console.WriteLine("???");
            var type = Relax ? GRB.CONTINUOUS : GRB.BINARY;
            env = new GRBEnv();
            model = new GRBModel(env);
            Model = model;
            model.Set("OutputFlag", "0");
            model.Set("MIPGap", "0.000000000001");
            model.Set("IntFeasTol", "0.000000001");
            model.Set("TimeLimit", "100");
            P = new GRBVar[GQ.totalTime];
            Commit = new GRBVar[GQ.totalTime];
            Start = new GRBVar[GQ.totalTime];
            Stop = new GRBVar[GQ.totalTime];
            for (int t = 0; t < GQ.totalTime; t++)
            {
                P[t] = model.AddVar(double.MinValue, double.MaxValue, 0.0, GRB.CONTINUOUS, "");
                Commit[t] = model.AddVar(0.0, 1.0, 0.0, type, "U" + t);
                Start[t] = model.AddVar(0.0, 1.0, 0.0, type, "V" + t);
                Stop[t] = model.AddVar(0.0, 1.0, 0.0, type, "W" + t);
                var maxGeneration = GQ.pMax * Commit[t];
                var minGerneation = GQ.pMin * Commit[t];
                Model.AddConstr(P[t] <= maxGeneration, "");
                Model.AddConstr(P[t] >= minGerneation, "");
                if (t > 0)
                {
                    GRBLinExpr downwardRampingLimitNormal = GQ.RampDown * Commit[t - 1];
                    GRBLinExpr downwardRampingLimitShutdown = Stop[t] * (GQ.SD - GQ.RampDown);
                    GRBLinExpr downwardRampingLimit = downwardRampingLimitNormal + downwardRampingLimitShutdown;
                    Model.AddConstr(P[t - 1] - P[t] <= downwardRampingLimit, "t" + t);


                    GRBLinExpr upwardRampingLimitNormal = GQ.RampUp * Commit[t];
                    GRBLinExpr upwardRampingLimitStartup = Start[t] * (GQ.SU - GQ.RampUp);
                    GRBLinExpr upwardRampingLimit = upwardRampingLimitNormal + upwardRampingLimitStartup;
                    Model.AddConstr(P[t] - P[t - 1] <= upwardRampingLimit, "t" + t);
                }

            }
            for (int t = 0; t < GQ.totalTime; t++)
            {
                if (t != 0)
                {
                    var ppLogic = Commit[t - 1] - Commit[t] + Start[t] - Stop[t] == 0;
                    Model.AddConstr(ppLogic, "Power Plant Logic" + t);
                }

            }
            AddMinimumUpTime();
            AddMinimumDownTime();

            model.Parameters.TimeLimit = 10;
            model.Parameters.Threads = 1;
        }
        public (double, double, double[]) CalcOptimum()
        {
            GRBQuadExpr ob = new GRBQuadExpr();
            for (int t = 0; t < GQ.totalTime; t++)
            {
                ob += (P[t] * P[t] * (GQ.CM[t] + GQ.C)) + Commit[t] * GQ.A + (Start[t] * GQ.startCost) + (P[t] * ((GQ.B + GQ.BM[t]) - GQ.LagrangeMultipliers[t]));
            }
            model.SetObjective(ob, GRB.MINIMIZE);
            model.Optimize();
            double returnvalue = ob.Value - GQ.totalTime;
            return (returnvalue, ReevalSolution(), P.Select(x => x.X).ToArray());
        }


        private double ReevalSolution()
        {

            double CycleCost = Start.Skip(1).Sum(step => step.X * GQ.startCost);
            double generationCost = 0;
            for (int t = 0; t < GQ.totalTime; t++)
            {
                generationCost += Commit[t].X * GQ.A + GQ.B * P[t].X + P[t].X * P[t].X * GQ.C;
            }
            return CycleCost + generationCost;
        }
        private void AddMinimumUpTime()
        {
            for (int t = 0; t < GQ.totalTime; t++)
            {
                var amountOfTimeStartedInPeriod = new GRBLinExpr();
                for (int t2 = Math.Max(0, (t + 1) - GQ.minUpTime); t2 <= t; t2++)
                {
                    amountOfTimeStartedInPeriod += Start[t2];
                }
                Model.AddConstr(Commit[t] >= amountOfTimeStartedInPeriod, "MinUpTime" + t);
            }
        }

        private void AddMinimumDownTime()
        {
            for (int t = 0; t < GQ.totalTime; t++)
            {

                var amountOfTimeStopped = new GRBLinExpr();
                for (int t2 = Math.Max(0, (t + 1) - GQ.minDownTime); t2 <= t; t2++)
                {
                    amountOfTimeStopped += Stop[t2];
                }

                Model.AddConstr(1 - Commit[t] >= amountOfTimeStopped, "MinDownTime" + t);


            }
        }
        public void Print()
        {
            var results = P.Select(var => var.X).ToList();
            double tot = GQ.pMax;
            int height = 25;
            int toInt(double x) => height - (int)((x / tot) * height);
            string getChar(double x, int y, string z) => toInt(x) == y ? z : ".";
            for (int i = 0; i <= height; i++)
            {
                string line = "";
                for (int j = 0; j < results.Count - 1; j++)
                {
                    double first = results[j];
                    double second = results[j + 1];
                    double diff = second - first;
                    double segments = 6;
                    line += getChar(first, i, "O");
                    for (double s = 1; s < segments; s++)
                    {
                        double multiplier = diff * (s / segments);
                        line += getChar(first + multiplier, i, "x");
                    }

                }
                Console.WriteLine(line);
            }
        }

    }
}
