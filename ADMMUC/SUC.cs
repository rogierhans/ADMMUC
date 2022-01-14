using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gurobi;
using System.Diagnostics;
using System.IO;

namespace ADMMUC;

public class SUC
{
    public double Objective;
    public int TotalTime;
    public int pMax;
    public int pMin;
    public int RampUp;
    public int RampDown;
    public int SU;
    public int SD;
    public int MinDownTime;
    public int MinUpTime;
    public double StartCost;
    public double A;
    public double B;
    public double C;
    public List<double> LagrangeMultipliers = new List<double>();
    public double[] BM;
    public double[] CM;


    public SUC() {
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
        this.MinDownTime = minDownTime;
        this.MinUpTime = minUpTime;
        StartCost = start;
        A = a;
        B = b;
        C = c;
        this.TotalTime = totalTime;
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
        for (int i = 0; i < TotalTime; i++)
        {
            LagrangeMultipliers.Add(B * (rng.NextDouble() * 3));

        }
    }

    public void WriteToFile(string filename)
    {
        var objects = new List<string>(){
            Objective.ToString(),
            TotalTime.ToString(),
            pMax.ToString(),
            pMin.ToString(),
            RampUp.ToString(),
            RampDown.ToString(),
            SU.ToString(),
            SD.ToString(),
            MinDownTime.ToString(),
            MinUpTime.ToString(),
            StartCost.ToString(),
            A.ToString(),
            B.ToString(),
            C.ToString(),
            string.Join("\t",LagrangeMultipliers),
            string.Join("\t",BM),
            string.Join("\t",CM)
            };
        File.WriteAllLines(filename, objects);
    }
    public static SUC ReadFromFile(string filename)
    {
        var lines = File.ReadAllLines(filename);
        int i = 0;
        var suc = new SUC
        {
            Objective = double.Parse(lines[i++]),
            TotalTime = int.Parse(lines[i++]),
            pMax = int.Parse(lines[i++]),
            pMin = int.Parse(lines[i++]),
            RampUp = int.Parse(lines[i++]),
            RampDown = int.Parse(lines[i++]),
            SU = int.Parse(lines[i++]),
            SD = int.Parse(lines[i++]),
            MinDownTime = int.Parse(lines[i++]),
            MinUpTime = int.Parse(lines[i++]),
            StartCost = double.Parse(lines[i++]),
            A = double.Parse(lines[i++]),
            B = double.Parse(lines[i++]),
            C = double.Parse(lines[i++]),
            LagrangeMultipliers = lines[i++].Split('\t').Select(x => double.Parse(x)).ToList(),
            BM = lines[i++].Split('\t').Select(x => double.Parse(x)).ToArray(),
            CM = lines[i++].Split('\t').Select(x => double.Parse(x)).ToArray()
        };
        return suc;
    }

    internal void PrintStats()
    {
        Console.WriteLine("[{0},{1}] +{2}  -{3}   {4}  {5}  {6} {7}", pMin, pMax, RampUp, RampDown, SU, SD, MinUpTime, MinDownTime);
    }
}
