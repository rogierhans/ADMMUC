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
    public class UCFileTest
    {


        [TestMethod]
        public void GA10_1UCFileCheck()
        {
            foreach (var file in new DirectoryInfo(@"C:\Users\Rogier\OneDrive - Universiteit Utrecht\1UCTest\GA10").GetFiles())
            {
                var filename = file.FullName;
                var suc = SUC.ReadFromFile(filename);
                var solution = new RRF(suc, true).GetSolution();
                Console.WriteLine(suc.Objective);
                Assert.IsTrue(Math.Abs(solution.CostADMM - suc.Objective) <= 0.001);
            }

            //var rff = new Rff
        }
        [TestMethod]
        public void FERC_1UCFileCheck()
        {
            Console.WriteLine("hello");
            foreach (var file in new DirectoryInfo(@"C:\Users\Rogier\OneDrive - Universiteit Utrecht\1UCTest\FERC923").GetFiles())
            {
                var filename = file.FullName;
                var suc = SUC.ReadFromFile(filename);
                var solution = new RRF(suc, true).GetSolution();
                Console.WriteLine(suc.Objective + " " + suc.Objective);
                Assert.IsTrue(Math.Abs(solution.CostADMM - suc.Objective) <= 0.001);
            }

            //var rff = new Rff
        }


    }
}