
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
        CreateMILPs();
        //CreateMILPs(); return;
        //List<double> scores = new List<double>();
        //foreach (var (key, (na, bl, score)) in GetRhoAndStuff())
        //{
        //    Console.WriteLine("{0} {1} {2} {3}", key[..5], na, bl, score);
        //}
        //Console.ReadLine();
        //Test6(1.2);
        //Test6(1.1);
        //Test100(1.1);
        //Test6(1.01);
        //Console.WriteLine(scores.Average());

        // return;
        //  RhoIncreaseTest();
        //var filename = @"C:\Users\Rogier\Google Drive\Data\Github\" + "GA10.uc";
        //int totalTime = 24;
        //var rhoUpdate = 1.1;
        //var rho = 0.0001;
        //var count = 10;
        //var sw = new Stopwatch();
        //sw.Start();
        //for (int iter = 0; iter < 10; iter++)
        //{
        //    var PSS = new PowerSystemSolution(filename, totalTime, rho, rhoUpdate, count, 1);
        //    // PSS.Test1UC = true;
        //    PSS.RunIterations(10000);
        //    Console.WriteLine("{0}", PSS.FinalScore);
        //    //  Console.ReadLine();
        //    Console.WriteLine(sw.Elapsed.TotalSeconds / (iter + 1));
        //}
        //sw.Stop();
        //Console.WriteLine(sw.Elapsed.TotalSeconds);
        //Console.ReadLine();

    }


    private static Dictionary<string, (double, int, double)> GetRhoAndStuff()
    {
        var filename = @"C:\Users\Rogier\Desktop\Looking.txt";
        var dict = new Dictionary<string, (double, int, double)>();

        var lines = File.ReadAllLines(filename);
        foreach (string line in lines)
        {
            var input = line.Split('\t');
            var name = input[0];
            var alpha = double.Parse(input[1][1..^1].Split(',').First());
            var m = int.Parse(input[1][1..^1].Split(',')[2]);
            var score = double.Parse(input[5]);
            if (!dict.ContainsKey(name))
            {
                dict[name] = (alpha, m, score);
            }
            else
            {
                if (dict[name].Item3 < score)
                {
                    dict[name] = (alpha, m, score);
                }
            }
        }

        return dict;

    }

    private static void DoMILP(PowerSystem PS, int totalTime)
    {
        var Resolve = new ResolveTrans(PS, totalTime, false, false);
        List<(double, double, double)> Snaps = Resolve.Solve(60 * 60);
        var filename = @"C:\Users\" + Environment.UserName + @"\OneDrive - Universiteit Utrecht\UCSolutionOptimalScores\" + totalTime + @"\";
        //create directory
        if (!Directory.Exists(filename))
        {
            Directory.CreateDirectory(filename);
        }   

        File.WriteAllLines(filename + PS.Name, Snaps.Select(x => x.Item1 + "\t" + x.Item2 + "\t" + x.Item3 + "\t"));
    }


    private static void CreateMILPs()
    {
        var fileNames = new DirectoryInfo(@"C:\Users\Rogier\Google Drive\Data\Github\").GetFiles();

        foreach (var filename in fileNames.OrderBy(x => x.Name.GetNumbers().First()).Take(8))
        {
            if (filename.Name.Contains("RTS")) continue;
            Console.WriteLine(filename.Name);
            int totalTime = 24 * 7 * 4;

            var ConstraintConfiguration = new ConstraintConfiguration(false, false, "", false, false, false, 1);
            ConstraintConfiguration.SetLimits(0, -1, -1, -1);
            var PowerSystem = IOUtils.GetPowerSystem(filename.FullName, ConstraintConfiguration);
            DoMILP(PowerSystem, totalTime);

        }

    }

    private static double GetScore(List<(double, double, double)> snaps, double algTime, double algScore)
    {
        foreach (var (objective, bound, time) in snaps)
        {
            if (time >= algTime)
            {

                return (objective - algScore) / objective;
            }
        }
        return (snaps.Last().Item1 - algScore) / snaps.Last().Item1;
    }

    private static double GetTime(List<(double, double, double)> snaps, double algTime, double algScore)
    {
        foreach (var (objective, bound, time) in snaps)
        {
            if (objective <= algScore)
            {

                return time;
            }
        }
        return snaps.Last().Item3;
    }

    private static double GetObjectiveRatio(List<(double, double, double)> snaps, double algScore)
    {
        return (snaps.Last().Item1 - algScore) / snaps.Last().Item1;
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

    public static List<int> GetNumbers(this string line)
    {
        // Console.WriteLine(line);
        List<int> list = new List<int>();
        string number = "";
        for (int i = 0; i < line.Length; i++)
        {
            //Console.WriteLine(number);
            if ((number == "") && (line[i] == '-'))
            {
                number = "-";
            }
            else if (line[i] >= '0' && line[i] <= '9')
            {
                number += line[i];
            }
            else if (number.Length > 0 && number != "-")
            {
                _ = int.TryParse(number, out int parsedNumber);
                list.Add(parsedNumber);
                number = "";
            }
            else
            {
                number = "";
            }
        }
        if (number.Length > 0 && number != "-")
        {

            _ = int.TryParse(number, out int parsedNumber);
            list.Add(parsedNumber);

        }
        //Console.ReadLine();
        return list;
        //return ..Where(x => !string.IsNullOrEmpty(x)).Where(x => x.Length < 9).Select(int.Parse).ToList();
    }
}
