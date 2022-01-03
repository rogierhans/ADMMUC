
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using Gurobi;
using ADMMUC.Solutions;
using System.Runtime.InteropServices;
namespace ADMMUC;

class Program
{
    static void Main()
    {

        var filename = @"C:\Users\Rogier\Google Drive\Data\Github\" + "RTS26.uc";
        int totalTime = 24;
        var rhoUpdate = 1.01;
        var rho = 0.00001;
        var count = 1;
        for (int iter = 0; iter < 3; iter++)
        {
            var PSS = new PowerSystemSolution(filename, totalTime, rho, rhoUpdate, count, 1);
            PSS.RunIterations(10000);
            Console.WriteLine("{0}", PSS.FinalScore);
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