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
    public class SUC
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
        public double[] BM;
        public double[] CM;

        internal void PrintStats()
        {
            Console.WriteLine("[{0},{1}] +{2}  -{3}   {4}  {5}  {6} {7}", pMin, pMax, RampUp, RampDown, SU, SD, minUpTime, minDownTime);
        }


        public SUC(double a, double b, double c, double start, int pMax, int pMin, int rampUp, int rampDown, int minUpTime, int minDownTime, int su, int sd, int totalTime)
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

        }

        public void SetLM(List<double> LM, double[] bm, double[] cm)
        {
            LagrangeMultipliers = LM; BM = bm; CM = cm;

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

        public double Objective = 0;

        public void WriteToFile(string filename)
        {
            BinarySerialization.WriteToBinaryFile(filename, this);
        }
        public static SUC ReadFromFile(string filename)
        {
            return BinarySerialization.ReadFromBinaryFile<SUC>(filename);
        }

    }
}
