using Microsoft.VisualStudio.TestTools.UnitTesting;
using ADMMUC.Solutions;
using ADMMUC;
using ADMMUC._1UC;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
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
            var rho = 1;
            var count = 1;
            //for (int iter = 0; iter < 3; iter++)

            Console.WriteLine("TestGO");
            {
                var PSS = new PowerSystemSolution(filename, totalTime, rho, rhoUpdate, count, 1);
                PSS.Test1UC = true;
                PSS.RunIterations(100);
                PSS.Deltas.ForEach(x => Assert.IsTrue(x <= 0.001));

            }
        }


        [TestMethod]
        public void CA426_1UC_Long_Check()
        {
            var filename = @"C:\Users\Rogier\Google Drive\Data\Github\CA426.uc";
            int totalTime = 24;
            var rhoUpdate = 1.01;
            var rho = 1;
            var count = 1;
            //for (int iter = 0; iter < 3; iter++)
            {
                var PSS = new PowerSystemSolution(filename, totalTime, rho, rhoUpdate, count, 1);
                PSS.Test1UC = true;
                PSS.RunIterations(100);
                //PSS.Deltas.ForEach(x => Console.WriteLine(x));
                PSS.Deltas.ForEach(x => Assert.IsTrue(x <= 0.001));

            }
        }

        [TestMethod]
        public void Ferc_1UC_Long_Check()
        {
            var filename = @"C:\Users\Rogier\Google Drive\Data\Github\FERC923.uc";
            int totalTime = 24;
            var rhoUpdate = 1.01;
            var rho = 1;
            var count = 1;
            //for (int iter = 0; iter < 3; iter++)
            {
                var PSS = new PowerSystemSolution(filename, totalTime, rho, rhoUpdate, count, 1);
                PSS.Test1UC = true;
                PSS.RunIterations(100);
                // PSS.Deltas.ForEach(x => Console.WriteLine(x));
                PSS.Deltas.ForEach(x => Assert.IsTrue(x <= 0.001));

            }
        }

        [TestMethod]
        public void SuperTest()
        {
            var filename = @"C:\Users\Rogier\Google Drive\Data\Github\" + "GA10.uc";
            int totalTime = 24;
            var rhoUpdate = 1.1;
            var rho = 0.0001;
            var count = 10;
            //for (int iter = 0; iter < 3; iter++)
            {
                var PSS = new PowerSystemSolution(filename, totalTime, rho, rhoUpdate, count, 1);

                PSS.RunIterations(10000);
                Console.WriteLine(PSS.FinalScore);
                Assert.IsTrue(PSS.FinalScore < 569000);
            }
        }

        [TestMethod]
        public void RTS26_1UCCheck()
        {

            int totalTime = 24;
            var count = 1;
            {
                var PSS = new PowerSystemSolution(@"C:\Users\Rogier\Google Drive\Data\Github\" + "RTS26.uc", totalTime, 1, 1.1, count, 1);
                PSS.Test1UC = true;
                PSS.RunIterations(100);
                PSS.Deltas.ForEach(x => Assert.IsTrue(x <= 0.001));

            }
        }

    }
}