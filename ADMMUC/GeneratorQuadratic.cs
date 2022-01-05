using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;
using System.Diagnostics;
using System.IO;

namespace ADMMUC
{
    [Serializable]
    public class GeneratorQuadratic
    {
        public int RampUp;
        public int RampDown;
        public int pMax;
        public int pMin;

        public int minDownTime;
        public int minUpTime;

        public int SU;
        public int SD;

        public double startCost;

        public double A;
        public double B;
        public double C;


        public List<double> LagrangeMultipliers = new List<double>();
        public int totalTime;
        // readonly Stopwatch sw = new Stopwatch();
        public double[] BM;
        public double[] CM;

        internal void PrintStats()
        {
            Console.WriteLine("[{0},{1}] +{2}  -{3}   {4}  {5}  {6} {7}", pMin, pMax, RampUp, RampDown, SU, SD, minUpTime, minDownTime);
        }

        private double totaltime = 0;



        public GeneratorQuadratic(double a, double b, double c, double start, int pMax, int pMin, int rampUp, int rampDown, int minUpTime, int minDownTime, int su, int sd, int totalTime)
        {

            this.pMax = pMax;
            this.pMin = pMin;
            SU = su;
            SD = sd;
            RampUp = Math.Max(rampUp, 1);
            RampDown = Math.Max(rampDown, 1);
            //SU = pMax;
            //SD = pMax;
            //RampUp = pMax;
            //RampDown = pMax;
            this.minDownTime = minDownTime;
            this.minUpTime = minUpTime;
            startCost = start;
            A = a;
            B = b;
            C = c;
            this.totalTime = totalTime;
            BM = new double[totalTime];
            CM = new double[totalTime];
            CreateEnv(false);
        }

        public void SetLM(List<double> LM)
        {
            LagrangeMultipliers = LM;

        }

        public void SetBM(double[] lm)
        {
            BM = lm;

        }
        public void SetCM(double[] lm)
        {
            CM = lm;

        }
        public void SetRandomLM()
        {
            LagrangeMultipliers = new List<double>();
            Random rng = new Random();
            for (int i = 0; i < totalTime; i++)
            {
                LagrangeMultipliers.Add(B * (rng.NextDouble() * 3));

            }
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
            var type = Relax ? GRB.CONTINUOUS : GRB.BINARY;
            env = new GRBEnv();
            model = new GRBModel(env);
            Model = model;
            model.Set("OutputFlag", "0");
            model.Set("MIPGap", "0.000000000001");
            model.Set("IntFeasTol", "0.000000001");
            model.Set("TimeLimit", "100");
            P = new GRBVar[totalTime];
            Commit = new GRBVar[totalTime];
            Start = new GRBVar[totalTime];
            Stop = new GRBVar[totalTime];
            for (int t = 0; t < totalTime; t++)
            {
                P[t] = model.AddVar(double.MinValue, double.MaxValue, 0.0, GRB.CONTINUOUS, "");
                Commit[t] = model.AddVar(0.0, 1.0, 0.0, type, "U" + t);
                Start[t] = model.AddVar(0.0, 1.0, 0.0, type, "V" + t);
                Stop[t] = model.AddVar(0.0, 1.0, 0.0, type, "W" + t);
                var maxGeneration = pMax * Commit[t];
                var minGerneation = pMin * Commit[t];
                Model.AddConstr(P[t] <= maxGeneration, "");
                Model.AddConstr(P[t] >= minGerneation, "");
                if (t > 0)
                {
                    GRBLinExpr downwardRampingLimitNormal = RampDown * Commit[t - 1];
                    GRBLinExpr downwardRampingLimitShutdown = Stop[t] * (SD - RampDown);
                    GRBLinExpr downwardRampingLimit = downwardRampingLimitNormal + downwardRampingLimitShutdown;
                    Model.AddConstr(P[t - 1] - P[t] <= downwardRampingLimit, "t" + t);


                    GRBLinExpr upwardRampingLimitNormal = RampUp * Commit[t];
                    GRBLinExpr upwardRampingLimitStartup = Start[t] * (SU - RampUp);
                    GRBLinExpr upwardRampingLimit = upwardRampingLimitNormal + upwardRampingLimitStartup;
                    Model.AddConstr(P[t] - P[t - 1] <= upwardRampingLimit, "t" + t);
                }

            }
            for (int t = 0; t < totalTime; t++)
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
            for (int t = 0; t < totalTime; t++)
            {
                ob += (P[t] * P[t] * (CM[t] + C)) + Commit[t] * A + (Start[t] * startCost) + (P[t] * ((B + BM[t]) - LagrangeMultipliers[t]));
            }
            model.SetObjective(ob, GRB.MINIMIZE);
            var Sw = new Stopwatch();
            Sw.Start();
            model.Optimize();
            totaltime += Sw.Elapsed.TotalMilliseconds;
            double returnvalue = ob.Value - totalTime;
            return (returnvalue, ReevalSolution(), P.Select(x => x.X).ToArray()); 
        }


        private double ReevalSolution()
        {

            double CycleCost = Start.Skip(1).Sum(step => step.X * startCost);
            double generationCost = 0;
            for (int t = 0; t < totalTime; t++)
            {
                generationCost += Commit[t].X * A + B * P[t].X + P[t].X * P[t].X * C;
            }
            return CycleCost + generationCost;
        }
        private void AddMinimumUpTime()
        {
            for (int t = 0; t < totalTime; t++)
            {
                var amountOfTimeStartedInPeriod = new GRBLinExpr();
                for (int t2 = Math.Max(0, (t + 1) - minUpTime); t2 <= t; t2++)
                {
                    amountOfTimeStartedInPeriod += Start[t2];
                }
                Model.AddConstr(Commit[t] >= amountOfTimeStartedInPeriod, "MinUpTime" + t);
            }
        }

        private void AddMinimumDownTime()
        {
            for (int t = 0; t < totalTime; t++)
            {

                var amountOfTimeStopped = new GRBLinExpr();
                for (int t2 = Math.Max(0, (t + 1) - minDownTime); t2 <= t; t2++)
                {
                    amountOfTimeStopped += Stop[t2];
                }

                Model.AddConstr(1 - Commit[t] >= amountOfTimeStopped, "MinDownTime" + t);


            }
        }
        public void Print()
        {
            var results = P.Select(var => var.X).ToList();
            double tot = pMax;
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
