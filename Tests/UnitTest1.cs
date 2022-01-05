using Microsoft.VisualStudio.TestTools.UnitTesting;
using ADMMUC.Solutions;
using ADMMUC;
using System.Linq;
using System;
using System.Collections.Generic;
namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void GA10_1UCCheck()
        {
            var filename = @"C:\Users\Rogier\Google Drive\Data\Github\" + "GA10.uc";
            int totalTime = 24;
            var rhoUpdate = 1.01;
            var rho = 0.00001;
            var count = 1;
            //for (int iter = 0; iter < 3; iter++)
            {
                var PSS = new PowerSystemSolutionTest(filename, totalTime, rho, rhoUpdate, count, 1);
                PSS.Test1UC = true;
                PSS.RunIterations(100);
                 PSS.Deltas.ForEach(x => Assert.IsTrue( x <= 0.001));

            }
        }
        [TestMethod]
        public void SuperTest()
        {
            var filename = @"C:\Users\Rogier\Google Drive\Data\Github\" + "GA10.uc";
            int totalTime = 24;
            var rhoUpdate = 1.01;
            var rho = 0.00001;
            var count = 1;
            //for (int iter = 0; iter < 3; iter++)
            {
                var PSS = new PowerSystemSolution(filename, totalTime, rho, rhoUpdate, count, 1);

                PSS.RunIterations(10000);
                Console.WriteLine(PSS.FinalScore);
                Assert.IsTrue(PSS.FinalScore < 568000);
            }
        }

        [TestMethod]
        public void RTS26_1UCCheck()
        {

            int totalTime = 24;
            var count = 1;
            {
                var PSS = new PowerSystemSolutionTest(@"C:\Users\Rogier\Google Drive\Data\Github\" + "RTS26.uc", totalTime, 1, 1.1, count, 1);
                //PSS.Test1UC = true;
                PSS.RunIterations(30);
                 PSS.Deltas.ForEach(x => Assert.IsTrue( x <= 0.001));

            }
        }


        public class PowerSystemSolutionTest : PowerSystemSolution
        {
            public PowerSystemSolutionTest(string fileName, int totalTime, double rho, double rhoMultiplier, int rhoUpdateCounter, double multiplierMultiplier)
                : base(fileName, totalTime, rho, rhoMultiplier, rhoUpdateCounter, multiplierMultiplier)
            {

            }
            public override void Go(int rhoUpdateCounter)
            {
                var CurrentDemand = GetDemand();
                foreach (var g in Enumerable.Range(0, RSolutions.Length).OrderBy(i => RNG.NextDouble()).ToList())
                {
                    RSolutions[g].Reevaluate(NodeMultipliers, CurrentDemand, Rho, totalTime);
                }
                foreach (var g in Enumerable.Range(0, GSolutions.Length).OrderBy(i => RNG.NextDouble()).ToList())
                {
                    if (GLOBAL.UseGurobi)
                    {
                        GSolutions[g].ReGurobi(NodeMultipliers, CurrentDemand, Rho, totalTime);
                    }
                    else
                    {
                        var delta = GSolutions[g].Reevaluate(NodeMultipliers, CurrentDemand, Rho, totalTime, true);
                        Deltas.Add(delta);
                    }
                }
                if (PowerSystem.Nodes.Count > 1)
                {
                    TSolution.Reevaluate(NodeMultipliers, CurrentDemand, Rho);
                }
                UpdateMultiplers(Rho);
                if (counter++ % rhoUpdateCounter == 0 && GLOBAL.IncreaseRho)
                {
                    Rho *= RhoMultiplier;
                    if (ConvergedObjective() && GLOBAL.ForceEnding)
                    {
                        Rho *= 10;
                        rhoUpdateCounter = 1;
                    }
                }
                Values.Add(GSolutions.Sum(g => g.ReevalCost));
            }

            public List<double> Deltas = new List<double>();
            //    public override void Go(int rhoUpdateCounter)
            //    {
            //        var CurrentDemand = GetDemand();
            //        foreach (var g in Enumerable.Range(0, RSolutions.Length).OrderBy(i => RNG.NextDouble()).ToList())
            //        {
            //            RSolutions[g].Reevaluate(NodeMultipliers, CurrentDemand, Rho, totalTime);
            //        }
            //        foreach (var g in Enumerable.Range(0, GSolutions.Length).OrderBy(i => RNG.NextDouble()).ToList())
            //        {
            //            if (GLOBAL.UseGurobi)
            //                GSolutions[g].ReGurobi(NodeMultipliers, CurrentDemand, Rho, totalTime);
            //            else
            //            {
            //                var delta = GSolutions[g].Reevaluate(NodeMultipliers, CurrentDemand, Rho, totalTime, true);
            //                Deltas.Add(delta);

            //            }
            //            if (PowerSystem.Nodes.Count > 1)
            //            {
            //                TSolution.Reevaluate(NodeMultipliers, CurrentDemand, Rho);
            //            }
            //            UpdateMultiplers(Rho);
            //            if (counter++ % rhoUpdateCounter == 0 && GLOBAL.IncreaseRho)
            //            {
            //                Rho *= RhoMultiplier;
            //                if (ConvergedObjective() && GLOBAL.ForceEnding)
            //                {
            //                    Rho *= 10;
            //                    rhoUpdateCounter = 1;
            //                }
            //            }
            //            Values.Add(GSolutions.Sum(g => g.ReevalCost));
            //        }


            //    }
        }
    }
}