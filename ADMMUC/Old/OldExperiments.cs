
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

class Program2
{
    static void Main2()
    {
        //CreateMILPs(); return;
        //List<double> scores = new List<double>();
        //foreach (var (key, (na, bl, score)) in GetRhoAndStuff())
        //{
        //    Console.WriteLine("{0} {1} {2} {3}", key[..5], na, bl, score);
        //}
        //Console.ReadLine();
        //Test6(1.2);
        //Test6(1.1);
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
        File.WriteAllLines(filename + PS.Name, Snaps.Select(x => x.Item1 + "\t" + x.Item2 + "\t" + x.Item3 + "\t"));
    }


    private static void CreateMILPs()
    {
        var fileNames = new DirectoryInfo(@"C:\Users\Rogier\Google Drive\Data\Github\").GetFiles();
        for (int i = 1; i <= 7; i++)



            foreach (var filename in fileNames)
            {
                int totalTime = 24 * i;
                if (filename.FullName != @"C:\Users\Rogier\Google Drive\Data\Github\DSET304.uc") continue;
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

        foreach (var filething in fileNames)
        {
            Console.WriteLine(filething);
            foreach (var totalTime in new List<int>() { 24 * 1, 24 * 2, 24 * 3, 24 * 4, 24 * 5, 24 * 6, 24 * 7, })
                for (int i = 0; i < 10; i++)
                {

                    double alpha = dict[filething.Name].Item1;
                    int count = 1;// dict[filething.Name].Item2;
                    string id = string.Format("({0},{1},{2})", alpha, rho, count);
                    //Console.WriteLine(id);  

                    // if (filething.Name != "GA10.uc") continue;
                    var filenameScore = @"C:\Users\" + Environment.UserName + @"\OneDrive - Universiteit Utrecht\UCSolutionOptimalScores\" + totalTime + @"\" + filething.Name;

                    List<(double, double, double)> MILPSnaps = File.ReadAllLines(filenameScore).Select(x =>
                    {
                        var input = x.Split('\t').Take(3).Select(double.Parse).ToList();
                        return (input[0], input[1], input[2]);
                    }).ToList();
                    SW.Restart();
                    var PSS = new PowerSystemSolution(filething.FullName, totalTime, rho, alpha, count, 1);
                    PSS.RunIterations(10000);
                    var time = (SW.Elapsed.TotalMilliseconds / 1000);
                    var score = PSS.GetScore();


                    var gTime = GetTime(MILPSnaps, time, score);
                    var ratio = GetScore(MILPSnaps, time, score);
                    var optRatio = GetObjectiveRatio(MILPSnaps, score);
                    var line = string.Format("{0} {1} {2} {3} {4} {5} {6} {7}", filething.Name.Replace("201409", "").Replace("01", "").Replace("01h", ""), totalTime, Math.Round(time, 1), score, ratio, optRatio, gTime, PSS.i);
                    Console.WriteLine(line);
                    score = PSS.FinalScore;
                    File.AppendAllText(@"C:\Users\Rogier\Desktop\smallLog5.txt", line + "\n");
                    File.AppendAllText(@"C:\Users\Rogier\Dropbox\smallLog5.txt", line + "\n");
                    //
                    // ratio = GetScore(MILPSnaps, time, score);
                    //optRatio = GetObjectiveRatio(MILPSnaps, score);
                    // Console.WriteLine("{0} {1} {2} {3} {4}", filething.Name, time, score, ratio, optRatio);
                    // Console.ReadLine();
                }
        }
    }

    private static void Test6(double alpha)
    {
        var fileNames = new DirectoryInfo(@"C:\Users\Rogier\Google Drive\Data\Github\").GetFiles();
        //  int totalTime = 24;
        var SW = new Stopwatch();
        double rho = 0.00001;
        var dict = GetRhoAndStuff();


        string logName = String.Format("C:\\Users\\Rogier\\Dropbox\\{0}Instances.txt", alpha);
        File.WriteAllText(logName, "Instance\tHorizon\tTime\tObjective\tGap\tOptGap\tTimeMIP\tIterations\tTimeFactor\tGapBestKnown");
        foreach (var totalTime in new List<int>() { 24 * 1, 24 * 2, 24 * 3, 24 * 4, 24 * 5, 24 * 6, 24 * 7, })
        {
            foreach (var filething in fileNames)
                for (int i = 0; i < 10; i++)
                {
                    int count = 5;// dict[filething.Name].Item2;
                    string id = string.Format("({0},{1},{2})", alpha, rho, count);
                    //Console.WriteLine(id);  
                    if (filething.Name[..3] != "RTS") continue;
                    //  if (filething.Name != "TAI38.uc") continue;
                    var filenameScore = @"C:\Users\" + Environment.UserName + @"\OneDrive - Universiteit Utrecht\UCSolutionOptimalScores\" + totalTime + @"\" + filething.Name;

                    List<(double, double, double)> MILPSnaps = File.ReadAllLines(filenameScore).Select(x =>
                    {
                        var input = x.Split('\t').Take(3).Select(double.Parse).ToList();
                        return (input[0], input[1], input[2]);
                    }).ToList();
                    SW.Restart();
                    var PSS = new PowerSystemSolution(filething.FullName, totalTime, rho, alpha, count, 1);
                    PSS.RunIterations(10000);
                    var time = (SW.Elapsed.TotalMilliseconds / 1000);
                    var score = PSS.GetScore();


                    var gTime = GetTime(MILPSnaps, time, score);
                    var ratio = GetScore(MILPSnaps, time, score);
                    var optRatio = GetObjectiveRatio(MILPSnaps, score);
                    string timeHsting = totalTime.ToString();
                    if (totalTime < 100)
                    {
                        timeHsting = "0" + timeHsting + "h";
                    }
                    else
                    {
                        timeHsting = timeHsting + "h";
                    }
                    var line = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
                        filething.Name.Replace("201409", "").Replace("01", "").Replace("01h", ""),
                        timeHsting,
                        Math.Round(time, 2),
                        score,
                        ratio,
                        optRatio,
                        gTime,
                        PSS.i,
                        gTime / time,
                        Math.Min(0, optRatio * 100) * -1);
                    Console.WriteLine(line);
                    //Console.ReadLine();
                    score = PSS.FinalScore;
                    File.AppendAllText(logName, line + "\n");
                    //
                    // ratio = GetScore(MILPSnaps, time, score);
                    //optRatio = GetObjectiveRatio(MILPSnaps, score);
                    // Console.WriteLine("{0} {1} {2} {3} {4}", filething.Name, time, score, ratio, optRatio);
                    // Console.ReadLine();
                }
        }
    }

    private static void Test7(double alpha)
    {
        var fileNames = new DirectoryInfo(@"C:\Users\Rogier\Google Drive\Data\Github\").GetFiles();
        //  int totalTime = 24;
        var SW = new Stopwatch();
        double rho = 0.00001;
        var dict = GetRhoAndStuff();


        string logName = String.Format("C:\\Users\\Rogier\\Dropbox\\{0}ExchangeInstances.txt", alpha);
        File.WriteAllText(logName, "Instance\tHorizon\tTime\tObjective\tGap\tOptGap\tTimeMIP\tIterations\tTimeFactor\tGapBestKnown");
        foreach (var totalTime in new List<int>() { 24 * 1 })
        {
            foreach (var filething in fileNames)
                for (int i = 0; i < 10; i++)
                {
                    int count = 3;// dict[filething.Name].Item2;
                    string id = string.Format("({0},{1},{2})", alpha, rho, count);
                    //Console.WriteLine(id);  
                    if (filething.Name[..3] == "RTS") continue;
                    //  if (filething.Name != "TAI38.uc") continue;
                    var filenameScore = @"C:\Users\" + Environment.UserName + @"\OneDrive - Universiteit Utrecht\UCSolutionOptimalScores\" + totalTime + @"\" + filething.Name;

                    List<(double, double, double)> MILPSnaps = File.ReadAllLines(filenameScore).Select(x =>
                    {
                        var input = x.Split('\t').Take(3).Select(double.Parse).ToList();
                        return (input[0], input[1], input[2]);
                    }).ToList();
                    SW.Restart();
                    var PSS = new RealADMM(filething.FullName, totalTime, rho, alpha, count, 1);
                    PSS.WriteAlgorithmToFile(10000);
                    var time = (SW.Elapsed.TotalMilliseconds / 1000);
                    var score = PSS.GetScore();


                    var gTime = GetTime(MILPSnaps, time, score);
                    var ratio = GetScore(MILPSnaps, time, score);
                    var optRatio = GetObjectiveRatio(MILPSnaps, score);
                    string timeHsting = totalTime.ToString();
                    if (totalTime < 100)
                    {
                        timeHsting = "0" + timeHsting + "h";
                    }
                    else
                    {
                        timeHsting = timeHsting + "h";
                    }
                    var line = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
                        filething.Name.Replace("201409", "").Replace("01", "").Replace("01h", ""),
                        timeHsting,
                        Math.Round(time, 2),
                        score,
                        ratio,
                        optRatio,
                        gTime,
                        PSS.i,
                        gTime / time,
                        Math.Min(0, optRatio * 100) * -1);
                    Console.WriteLine(line);
                    //Console.ReadLine();
                    score = PSS.FinalScore;
                    File.AppendAllText(logName, line + "\n");
                    //
                    // ratio = GetScore(MILPSnaps, time, score);
                    //optRatio = GetObjectiveRatio(MILPSnaps, score);
                    // Console.WriteLine("{0} {1} {2} {3} {4}", filething.Name, time, score, ratio, optRatio);
                    // Console.ReadLine();
                }
        }
    }
    private static void Test8(double alpha)
    {
        var fileNames = new DirectoryInfo(@"C:\Users\Rogier\Google Drive\Data\Github\").GetFiles();
        //  int totalTime = 24;
        var SW = new Stopwatch();
        double rho = 0.00001;
        var dict = GetRhoAndStuff();


        string logName = String.Format("C:\\Users\\Rogier\\Dropbox\\{0}Extra3.txt", alpha);
        File.WriteAllText(logName, "Instance\tHorizon\tTime\tObjective\tGap\tOptGap\tTimeMIP\tIterations\tTimeFactor\tGapBestKnown");
        for (int k = 1; k <= 7; k++)
        {

            int totalTime = 24 * k;
            foreach (var filething in fileNames)
                for (int i = 0; i < 10; i++)
                {
                    int count = 10;// dict[filething.Name].Item2;
                    string id = string.Format("({0},{1},{2})", alpha, rho, count);
                    //Console.WriteLine(id);  
                    if (filething.Name[..3] != "RTS") continue;
                    //  if (filething.Name != "TAI38.uc") continue;
                    var filenameScore = @"C:\Users\" + Environment.UserName + @"\OneDrive - Universiteit Utrecht\UCSolutionOptimalScores\" + totalTime + @"\" + filething.Name;

                    List<(double, double, double)> MILPSnaps = File.ReadAllLines(filenameScore).Select(x =>
                    {
                        var input = x.Split('\t').Take(3).Select(double.Parse).ToList();
                        return (input[0], input[1], input[2]);
                    }).ToList();

                    var PSS = new PowerSystemSolution(filething.FullName, totalTime, rho, alpha, count, 1);
                    SW.Restart();
                    PSS.RunIterations(15000);
                    var time = (SW.Elapsed.TotalMilliseconds / 1000);
                    var score = PSS.GetScore();


                    var gTime = GetTime(MILPSnaps, time, score);
                    var ratio = GetScore(MILPSnaps, time, score);
                    var optRatio = GetObjectiveRatio(MILPSnaps, score);
                    string timeHsting = totalTime.ToString();
                    if (totalTime < 100)
                    {
                        timeHsting = "0" + timeHsting + "h";
                    }
                    else
                    {
                        timeHsting = timeHsting + "h";
                    }
                    var line = string.Format("{0}\t{1}\t{2}\t{3}\t{4}\t{5}\t{6}\t{7}\t{8}\t{9}",
                        filething.Name.Replace("201409", "").Replace("01", "").Replace("01h", ""),
                        timeHsting,
                        Math.Round(time, 2),
                        score,
                        ratio,
                        optRatio,
                        gTime,
                        PSS.i,
                        gTime / time,
                        Math.Min(0, optRatio * 100) * -1);
                    Console.WriteLine(line);
                    //Console.ReadLine();
                    score = PSS.FinalScore;
                    File.AppendAllText(logName, line + "\n");
                    //
                    // ratio = GetScore(MILPSnaps, time, score);
                    //optRatio = GetObjectiveRatio(MILPSnaps, score);
                    // Console.WriteLine("{0} {1} {2} {3} {4}", filething.Name, time, score, ratio, optRatio);
                    // Console.ReadLine();
                }
        }
    }
    private static void TransTest()
    {
        var fileNames = new DirectoryInfo(@"C:\Users\Rogier\Google Drive\Data\Github\").GetFiles();
        //  int totalTime = 24;
        var SW = new Stopwatch();
        double rho = 0.00001;
        var dict = GetRhoAndStuff();

        foreach (var filething in fileNames)
        {
            Console.WriteLine(filething);
            foreach (var totalTime in new List<int>() { 24 * 1, 24 * 2, 24 * 3, 24 * 4, 24 * 5, 24 * 6, 24 * 7, })
                for (int i = 0; i < 10; i++)
                {

                    double alpha = 1.01;
                    int count = 5;// dict[filething.Name].Item2;
                    string id = string.Format("({0},{1},{2})", alpha, rho, count);

                    if (filething.Name[..3] != "RTS") continue;
                    var filenameScore = @"C:\Users\" + Environment.UserName + @"\OneDrive - Universiteit Utrecht\UCSolutionOptimalScores\" + totalTime + @"\" + filething.Name;

                    List<(double, double, double)> MILPSnaps = File.ReadAllLines(filenameScore).Select(x =>
                    {
                        var input = x.Split('\t').Take(3).Select(double.Parse).ToList();
                        return (input[0], input[1], input[2]);
                    }).ToList();
                    SW.Restart();
                    var PSS = new PowerSystemSolution(filething.FullName, totalTime, rho, alpha, count, 1);
                    PSS.RunIterations(10000);
                    var time = (SW.Elapsed.TotalMilliseconds / 1000);
                    var score = PSS.GetScore();


                    var gTime = GetTime(MILPSnaps, time, score);
                    var ratio = GetScore(MILPSnaps, time, score);
                    var optRatio = GetObjectiveRatio(MILPSnaps, score);
                    var line = string.Format("{0} {1} {2} {3} {4} {5} {6} {7}", filething.Name[..3], totalTime, Math.Round(time, 1), score, ratio, optRatio, gTime, PSS.i);
                    Console.WriteLine(line);
                    score = PSS.FinalScore;
                    File.AppendAllText(@"C:\Users\Rogier\Desktop\smallLog6.txt", line + "\n");
                    File.AppendAllText(@"C:\Users\Rogier\Dropbox\smallLog6.txt", line + "\n");
                    //
                    // ratio = GetScore(MILPSnaps, time, score);
                    //optRatio = GetObjectiveRatio(MILPSnaps, score);
                    // Console.WriteLine("{0} {1} {2} {3} {4}", filething.Name, time, score, ratio, optRatio);
                    // Console.ReadLine();
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

