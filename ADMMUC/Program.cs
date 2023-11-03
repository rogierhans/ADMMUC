
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Gurobi;
using ADMMUC._1UC;
using ADMMUC.Solutions;
using System.Runtime.InteropServices;
namespace ADMMUC;

class Program
{
    static void Main()
    {
        var SW = new Stopwatch();
        double rho = 0.00001;
        double alpha = 1.01;
        var fileNames = new DirectoryInfo(@"C:\Instances\").GetFiles();
        int totalTime = 0;
        foreach (var filename in fileNames)
            for (int i = 0; i < 100; i++)
            {
                GLOBAL.Times = new List<double>();
                GLOBAL.DPTimes = new List<double>();
                GLOBAL.RemovalTime = new List<double>();
                GLOBAL.Ms = new List<double>();
                GLOBAL.Ks = new List<double>();
                totalTime += 24;
                var PSS = new PowerSystemSolution(filename.FullName, totalTime, rho, alpha, 1, 1);
                SW.Restart();
                PSS.RunIterations(10000);
            }
    }

}



public static class GLOBAL
{
    public static List<double> Times = new List<double>();
    public static List<double> DPTimes = new List<double>();
    public static List<double> RemovalTime = new List<double>();

    public static List<double> Ms = new List<double>();
    public static List<double> Ks = new List<double>();
    
    public static bool PRINTIteration = false;
    public static bool ResolveInteration = false;
    public static bool ForceEnding = false;
    public static bool WeirdTrans = false;
    public static bool UseGurobi = false;
    public static bool RelaxGurobi = false;
    public static bool LINEAR = false;
    public static bool IncreaseRho = true;

}
