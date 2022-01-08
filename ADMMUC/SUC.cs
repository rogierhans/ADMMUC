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

    internal void PrintStats()
    {
        Console.WriteLine("[{0},{1}] +{2}  -{3}   {4}  {5}  {6} {7}", pMin, pMax, RampUp, RampDown, SU, SD, MinUpTime, MinDownTime);
    }
    public SUC() { }

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
        var objects = new List<object>(){
            Objective,
            TotalTime,
            pMax,
            pMin,
            RampUp,
            RampDown,
            SU,
            SD,
            MinDownTime,
            MinUpTime,
            StartCost,
            A,
            B,
            C,
            string.Join("\t",LagrangeMultipliers),
            string.Join("\t",BM),
            string.Join("\t",CM)
            };
        File.WriteAllLines(filename, objects.Select(x => x.ToString()).ToList());
    }
    public static SUC ReadFromFile(string filename)
    {
        var suc = new SUC();
        var lines = File.ReadAllLines(filename);
        int i = 0;
        suc.Objective = double.Parse(lines[i++]);
        suc.TotalTime = int.Parse(lines[i++]);
        suc.pMax = int.Parse(lines[i++]);
        suc.pMin = int.Parse(lines[i++]);
        suc.RampUp = int.Parse(lines[i++]);
        suc.RampDown = int.Parse(lines[i++]);
        suc.SU = int.Parse(lines[i++]);
        suc.SD = int.Parse(lines[i++]);
        suc.MinDownTime = int.Parse(lines[i++]);
        suc.MinUpTime = int.Parse(lines[i++]);
        suc.StartCost = double.Parse(lines[i++]);
        suc.A = double.Parse(lines[i++]);
        suc.B = double.Parse(lines[i++]);
        suc.C = double.Parse(lines[i++]);
        suc.LagrangeMultipliers = lines[i++].Split('\t').Select(x => double.Parse(x)).ToList();
        suc.BM = lines[i++].Split('\t').Select(x => double.Parse(x)).ToArray();
        suc.CM = lines[i++].Split('\t').Select(x => double.Parse(x)).ToArray();
        //string.Join("\t" = int.Parse(lines[i++]); LagrangeMultipliers)= int.Parse(lines[i++]);
        //string.Join("\t" = int.Parse(lines[i++]); BM)= int.Parse(lines[i++]);
        //string.Join("\t" = int.Parse(lines[i++]); CM)
        return suc;
    }
}
