
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
        //{
        //    foreach (var file in new DirectoryInfo(@"C:\Users\Rogier\OneDrive - Universiteit Utrecht\1UCTest\GA10").GetFiles())
        //    {
        //        var filename = file.FullName;
        //        var suc = SUC.ReadFromFile(filename);
        //        var solution = new RRF(suc, true).GetSolution();
        //        F.ccounter = 0;
        //     //   Console.WriteLine(suc.Objective);
        //        ///Console.ReadLine();
        //    }
        //    Console.ReadLine();
        //}
        {
            var filename = @"C:\Users\Rogier\Google Drive\Data\Github\" + "GA10.uc";

            int totalTime = 24;
            var rhoUpdate = 1.1;
            var rho = 0.0001;
            var count = 10;
            var sw = new Stopwatch();
            sw.Start();
            for (int iter = 0; iter < 10; iter++)
            {
                var PSS = new PowerSystemSolution(filename, totalTime, rho, rhoUpdate, count, 1);
               // PSS.Test1UC = true;
                PSS.RunIterations(10000);
                Console.WriteLine("{0}", PSS.FinalScore);
                //  Console.ReadLine();
                Console.WriteLine(sw.Elapsed.TotalSeconds / (iter+1));
            }
            sw.Stop();
            Console.WriteLine(sw.Elapsed.TotalSeconds);
            Console.ReadLine();
        }
    }

}

public static class GLOBAL
{
    public static bool PRINTIteration = false;
    public static bool ResolveInteration = false;
    public static bool ForceEnding = false;
    public static bool WeirdTrans = false;
    public static bool UseGurobi = false;
    public static bool RelaxGurobi = false;
    public static bool LINEAR = false;
    public static bool IncreaseRho = true;
    public static void LinearModelTest()
    {
        ForceEnding = false;
        UseGurobi = true;
        RelaxGurobi = true;
        ResolveInteration = false;
        // IncreaseRho = false;
    }

    public static void Reverse()
    {
        ForceEnding = true;
        UseGurobi = false;
        RelaxGurobi = false;
        ResolveInteration = false;

    }
}