
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

        //List<double> scores = new List<double>();
        foreach (var (key, (na, bl, score)) in GetRhoAndStuff())
        {
            Console.WriteLine("{0} {1} {2} {3}", key[..3], na, bl, score);
        }
        Test();
        //Console.WriteLine(scores.Average());
        // CreateMILPs();
        // return;
        // RhoIncreaseTest();
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
            var score = double.Parse(input[4]);
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
        File.WriteAllLines(filename + PS.Name, Snaps.Select(x => x.Item1 + "\t" + x.Item2 + "\t" + x.Item3 + "\t"));
    }


    private static void CreateMILPs()
    {
        var fileNames = new DirectoryInfo(@"C:\Users\Rogier\Google Drive\Data\Github\").GetFiles();
        int totalTime = 24 * 5;
        foreach (var filename in fileNames)
        {

            var ConstraintConfiguration = new ConstraintConfiguration(false, false, "", false, false, false, 1);
            ConstraintConfiguration.SetLimits(0, -1, -1, -1);
            var PowerSystem = IOUtils.GetPowerSystem(filename.FullName, ConstraintConfiguration);
            DoMILP(PowerSystem, totalTime);

        }

    }
    private static void Test()
    {
        var fileNames = new DirectoryInfo(@"C:\Users\Rogier\Google Drive\Data\Github\").GetFiles();
        //  int totalTime = 24;
        var SW = new Stopwatch();
        double rho = 0.00001;
        var dict = GetRhoAndStuff();

        foreach (var filething in fileNames.Skip(2))
        {
            foreach (var totalTime in new List<int>() { 24 * 5 })
            {

                double alpha = dict[filething.Name].Item1;
                int count = dict[filething.Name].Item2;
                string id = string.Format("({0},{1},{2})", alpha, rho, count);
                //Console.WriteLine(id);  

                if (filething.Name != "RTS54.uc") continue;
                var filenameScore = @"C:\Users\" + Environment.UserName + @"\OneDrive - Universiteit Utrecht\UCSolutionOptimalScores\" + totalTime + @"\" + filething.Name;

                List<(double, double, double)> MILPSnaps = File.ReadAllLines(filenameScore).Select(x =>
                {
                    var input = x.Split('\t').Take(3).Select(double.Parse).ToList();
                    return (input[0], input[1], input[2]);
                }).ToList();
                SW.Restart();
                var test = new RealADMM(filething.FullName, totalTime, rho, alpha, count, 1);
                test.WriteAlgorithmToFile(10000);
                var time = (SW.Elapsed.TotalMilliseconds / 1000);
                var score = test.GetScore();

                var ratio = GetScore(MILPSnaps, time, score);
                var optRatio = GetObjectiveRatio(MILPSnaps, score);
                Console.WriteLine("{0} {1} {2} {3} {4}", filething.Name[..3], Math.Round(time, 1), score, ratio, optRatio);
                score = test.FinalScore;

                // ratio = GetScore(MILPSnaps, time, score);
                //optRatio = GetObjectiveRatio(MILPSnaps, score);
                // Console.WriteLine("{0} {1} {2} {3} {4}", filething.Name, time, score, ratio, optRatio);
                // Console.ReadLine();
            }
            Console.WriteLine();
        }
    }

    private static void RhoIncreaseTest()
    {
        var fileNames = new DirectoryInfo(@"C:\Users\Rogier\Google Drive\Data\Github\").GetFiles();
        int totalTime = 24;
        var SW = new Stopwatch();


        foreach (var rho in new List<double> { 0.000001 })
        {
            for (double rhoUpdate = 1.01; rhoUpdate <= 1.1; rhoUpdate += 0.01)
            {
                for (int count = 1; count <= 10; count++)
                {
                    List<double> Ratios = new List<double>();
                    List<double> OptRatios = new List<double>();
                    List<double> Comptime = new List<double>();
                    string id = string.Format("({0},{1},{2})", rhoUpdate, rho, count);
                    foreach (var filename in fileNames)
                    {
                        var filenameScore = @"C:\Users\" + Environment.UserName + @"\OneDrive - Universiteit Utrecht\UCSolutionOptimalScores\" + totalTime + @"\" + filename.Name;
                        List<(double, double, double)> MILPSnaps = File.ReadAllLines(filenameScore).Select(x =>
                        {
                            //Console.WriteLine(x);
                            var input = x.Split('\t').Take(3).Select(y =>
                        {
                            return double.Parse(y);
                        }).ToList();

                            return (input[0], input[1], input[2]);
                        }).ToList();
                        {
                            SW.Restart();
                            var test = new PowerSystemSolution(filename.FullName, totalTime, rho, rhoUpdate, count, 1);
                            test.RunIterations(10000);
                            var time = (SW.Elapsed.TotalMilliseconds / 1000);
                            var score = test.GetScore();
                            var ratio = GetScore(MILPSnaps, time, score);
                            var optRatio = GetObjectiveRatio(MILPSnaps, score);
                            Console.WriteLine(SW.Elapsed.TotalMilliseconds);
                            Console.WriteLine(ratio);
                            Ratios.Add(ratio);
                            OptRatios.Add(optRatio);
                            Comptime.Add(time);
                            File.AppendAllText(@"C:\Users\Rogier\Desktop\Looking.txt", filename.Name + "\t" + id + "\t" + (SW.Elapsed.TotalMilliseconds / 1000) + "\t" + test.GetScore() + "\t" + ratio + "\t" + optRatio + "\n");
                        }
                    }
                    File.AppendAllText(@"C:\Users\Rogier\Desktop\Ratios.txt", string.Format("{0} {1} {2} {3} {4} {5} {6}", id, OptRatios.Average(), Ratios.Average(), Comptime.Average(), Ratios.Min(), Ratios.Max(), Comptime.Min(), Comptime.Max()) + "\n");
                    File.AppendAllText(@"C:\Users\Rogier\Desktop\ForR.txt", string.Format("{0} {1} {2} {3} {4}", rhoUpdate, count, OptRatios.Average(), Ratios.Average(), Comptime.Average()) + "\n");
                }
            }
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
    public static void LinearModelTest()
    {
        ForceEnding = false;
        UseGurobi = true;
        RelaxGurobi = true;
        ResolveInteration = false;
    }

    public static void Reverse()
    {
        ForceEnding = true;
        UseGurobi = false;
        RelaxGurobi = false;
        ResolveInteration = false;

    }
}
