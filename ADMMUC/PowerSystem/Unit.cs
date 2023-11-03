using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADMMUC
{
    public static class GLOBALDEBUG {
        public static List<string> Lines1 = new List<string>();
        public static List<string> Lines2 = new List<string>();
    }


    public class Unit
    {
        //names
        public int ID;
        public int NodeID;
        public string Name;
        //public string FuelName;

        //number of units
        public int Count;

        //generation
        public double PMin, PMax;

        //generation cost as a quadratic function f(p) = a + bp + c^2
        public double A, B, C;

        //piecewise function approximating the quadratic function
        //it is assumed the PieceWiseCost are monotonic increasing
        // public int Segments;
        public double[] PiecewiseCost;
        public double[] PiecewiseLengths;
        public double[] PiecewiseCumalative;

        public double[] PiecewiseCvl;
        public double[] PiecewiseCwl;

        //cycle info
        public double RampUp, RampDown;

        public double StartUp;

        public double ShutDown;
        public int MinUpTime;
        public int MinDownTime;

        //discretised timedependent startupcosts
        public double[] StartCostInterval;
        public int[] StartInterval;

        //time dependent as exponentail function
        public double FSC;
        public double VSC;
        public double Lambda;


        public void Fix() {
            PMin = (int)PMin;
            PMax = (int)PMax;

            RampUp = (int)RampUp;
            RampUp = (int)RampDown;
            ShutDown = (int)Math.Max(PMin, ShutDown);
            StartUp = (int)Math.Max(PMin, StartUp);
        }

        public Unit(int id, int count)
        {
            ID = id;
            Count = count;
            Name = "Generator" + ID;
        }

        public void SetGenerationLimits(double pMin, double pMax)
        {

            PMin = pMin;
            PMax = pMax;
        }

        public void SetGenerationCost(double a, double b, double c)
        {
            A = a;
            B = b;
            if (!GLOBAL.LINEAR)
            {
                C = c;
            }
        }

        public void SetRampLimits(double rampUp, double rampDown, double startUp, double shutDown)
        {

            RampUp = Math.Min(rampUp, PMax);
            RampDown = Math.Min(rampDown, PMax);
            StartUp = Math.Min(startUp, PMax);
            ShutDown = Math.Min(shutDown, PMax);
        }

        public int RealMinUptime;
        public int RealMinDowntime;
        public void SetMinTime(int minUpTime, int minDownTime, bool ConstraintActive)
        {
            MinUpTime = Math.Max(2, minUpTime);
            MinDownTime = Math.Max(2, minDownTime);
            RealMinUptime = MinUpTime;
            RealMinDowntime = MinDownTime;
        }

        public void SetSUInterval(double[] startCostInterval, int[] startInterval)
        {
            StartCostInterval = startCostInterval;
            StartInterval = startInterval;
        }

        public void SetSUFunction(double f, double v, double lambda)
        {
            FSC = f;
            VSC = v;
            Lambda = lambda;
            DiscretiseTimeDependantStartupCost();
        }

        private void DiscretiseTimeDependantStartupCost()
        {
            StartInterval = new int[] { 0, 10, 20 };
            StartCostInterval = GetCostInterval(StartInterval);
        }

        private double[] GetCostInterval(int[] interval)
        {
            return interval.Select(t => StartupCost(t)).ToArray();
        }

        private double[] GetCostAVG(int[] interval)
        {
            double[] costs = new double[interval.Length];
            for (int i = 0; i < interval.Length - 1; i++)
            {
                costs[i] = StartupCost(interval[i + 1] - interval[i]);
            }
            costs[interval.Length - 1] = StartupCost(interval[interval.Length - 1]);
            return costs;
        }

        private double StartupCost(int timePast)
        {
            return FSC + VSC * (1 - Math.Exp(timePast * -Lambda));
        }

        public double DetermineStartupCost(int timePast)
        {
            for (int i = 0; i < StartInterval.Length - 1; i++)
            {
                if (StartInterval[i] <= timePast && timePast <= StartInterval[i + 1])
                    return StartCostInterval[i];
            }
            return StartCostInterval.Last();
        }

        public double DeterminePiecewiseCost(double p)
        {
            if (p < PMin) { return GetCost(p); }
            double totalP = p - PMin;
            double totalCost = GetCost(PMin);
            for (int s = 0; s < PiecewiseLengths.Length; s++)
            {
                double length = Math.Min(totalP, PiecewiseLengths[s]);
                totalCost += PiecewiseCost[s] * length;
                totalP -= length;
            }
            return totalCost;
        }

        //for creating piece-wsie functions
        public void CreateUniformPiecewiseFunction(int segments)
        {
            //Segments = segments;
            double segementLength = (PMax - PMin) / segments;
            PiecewiseLengths = new double[segments];
            PiecewiseCost = new double[segments];
            PiecewiseCumalative = new double[segments];
            PiecewiseCvl = new double[segments];
            PiecewiseCwl = new double[segments];
            double cumulativeLength = 0;
            for (int s = 0; s < segments; s++)
            {
                PiecewiseLengths[s] = segementLength;

                PiecewiseCost[s] = GetSlope(cumulativeLength + PMin, cumulativeLength + PiecewiseLengths[s] + PMin);
                var prevCumulativeLength = cumulativeLength + PMin;
                cumulativeLength += PiecewiseLengths[s];
                PiecewiseCumalative[s] = cumulativeLength + PMin;
                PiecewiseCvl[s] = GetCul(prevCumulativeLength, PiecewiseCumalative[s], StartUp);
                PiecewiseCwl[s] = GetCul(prevCumulativeLength, PiecewiseCumalative[s], ShutDown);
            }
            MonotonicityCheck(PiecewiseCost);
        }

        private void MonotonicityCheck(double[] piecewiseCost)
        {
            for (int s = 0; s < piecewiseCost.Length - 1; s++)
            {
                if (piecewiseCost[s] > piecewiseCost[s + 1] + 0.0001)
                {
                    Console.WriteLine("{0} - {1} =  {2}", piecewiseCost[s], piecewiseCost[s + 1], piecewiseCost[s + 1] - piecewiseCost[s]);
                    throw new Exception("Error quadractic function not convex, piecewisefunction not monotonic");
                }
            }
        }

        private double GetSlope(double startP, double endP)
        {
            double startCost = GetCost(startP);
            double endCost = GetCost(endP);

            return (endCost - startCost) / (endP - startP);
        }

        public double GetCost(double p)
        {
            return B * p + C * p * p;
        }

        public double GetCul(double PiecewiseCumalativeMaxPrev, double PiecewiseCumalativeMax, double limit)
        {

            if (PiecewiseCumalativeMax <= limit)
            {
                return 0;
            }
            else if (PiecewiseCumalativeMaxPrev < limit && limit < PiecewiseCumalativeMax)
            {
                //GetInfo();
                //Console.ReadLine();
                return PiecewiseCumalativeMax - limit;
            }
            else if (PiecewiseCumalativeMaxPrev >= limit)
            {
                return PiecewiseCumalativeMax - PiecewiseCumalativeMaxPrev;
            }
            throw new Exception("Piecewisecase error");
        }
        public void GetInfo()
        {
            Console.WriteLine(Name);
            Console.WriteLine("GEN:{0} - {1}", PMin, PMax);
            Console.WriteLine("COS:{0}+{1}p+{2}P^2", A, B, C);
            Console.WriteLine("PWS:{0}", "[" + String.Join(":", PiecewiseLengths) + "]");
            Console.WriteLine("PWC:{0}", "[" + String.Join(":", PiecewiseCost) + "]");
            Console.WriteLine("PCU:{0}", "[" + String.Join(":", PiecewiseCumalative) + "]");
            Console.WriteLine("CvL:{0}", "[" + String.Join(":", PiecewiseCvl) + "]");
            Console.WriteLine("CwL:{0}", "[" + String.Join(":", PiecewiseCwl) + "]");
            Console.WriteLine("DIF:{0}", "[" + String.Join(":", PiecewiseCvl.Zip(PiecewiseCwl, (a, b) => b - a)) + "]");
            Console.WriteLine("RAM:{0} - {1}", RampUp, RampDown);
            Console.WriteLine("STA:{0} - {1}", StartUp, ShutDown);
            Console.WriteLine("MIN:{0} - {1}", MinUpTime, MinDownTime);
            Console.WriteLine("EXP:{0} + {1} * (1 - e^-{2}l)", FSC, VSC, Lambda);
            Console.WriteLine("INT:{0}", "[" + String.Join(":", StartInterval) + "]");
            Console.WriteLine("COS:{0}", "[" + String.Join(":", StartCostInterval) + "]");
        }
    }
}
