//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ADMMUC.Solutions
//{
//    internal class PrintPowerSystemSolution
//    {
//        private void PrinteGenerators()
//        {
//            Console.WriteLine();
//            for (int g = 0; g < GSolutions.Length; g++)
//            {
//                string line = "";
//                for (int t = 0; t < totalTime; t++)
//                {
//                    line += Math.Round(GSolutions[g].CurrentDispatchAtTime[t]) + "\t";
//                }
//                Console.WriteLine(line);
//                GSolutions[g].PrintStats();
//            }
//        }

//        public void PrintMultiplier()
//        {
//            Console.WriteLine("Multipliers:");
//            for (int n = 0; n < totalNodes; n++)
//            {
//                string line = "";
//                for (int t = 0; t < totalTime; t++)
//                {
//                    line += Math.Round(NodeMultipliers[n, t], 10) + "\t";
//                }
//                Console.WriteLine(line);
//            }
//        }
//        private void PrintGeneration()
//        {
//            string line = "units:";
//            for (int t = 0; t < totalTime; t++)
//            {
//                double total = 0;
//                foreach (var gsol in GSolutions)
//                {
//                    total += gsol.CurrentDispatchAtTime[t];
//                }
//                line += "\t" + Math.Round(total, 1);
//            }
//            Console.WriteLine(line);
//            line = "RES:";
//            for (int t = 0; t < totalTime; t++)
//            {
//                double total = 0;
//                foreach (var gsol in RSolutions)
//                {
//                    total += gsol.Dispatch[t];
//                }
//                line += "\t" + Math.Round(total, 1);
//            }
//            Console.WriteLine(line);

//            line = "Both:";
//            for (int t = 0; t < totalTime; t++)
//            {
//                double total = 0;
//                foreach (var gsol in RSolutions)
//                {
//                    total += gsol.Dispatch[t];
//                }
//                foreach (var gsol in GSolutions)
//                {
//                    total += gsol.CurrentDispatchAtTime[t];
//                }
//                line += "\t" + Math.Round(total, 1);
//            }
//            Console.WriteLine(line);


//            line = "Demand:";
//            for (int t = 0; t < totalTime; t++)
//            {
//                double total = 0;
//                foreach (var gsol in PowerSystem.Nodes)
//                {
//                    total += gsol.NodalDemand(t);
//                }
//                line += "\t" + Math.Round(total, 1);
//            }
//            Console.WriteLine(line);
//        }

//        private void PrintDemand()
//        {

//            var demand = GetDemand();
//            Console.WriteLine("Demand:");
//            for (int t = 0; t < totalTime; t++)
//            {
//                string line = "";
//                string line2 = "";
//                for (int n = 0; n < totalNodes; n++)
//                {

//                    var genSolutions = PowerSystem.Nodes[n].UnitsIndex.Select(g => GSolutions[g]);

//                    var resSolutions = PowerSystem.Nodes[n].RESindex.Select(g => RSolutions[g]);
//                    line += Math.Round(demand[n, t]) + "\t";
//                    line2 += PowerSystem.Nodes[n].NodalDemand(t) + "\t";
//                }
//                Console.WriteLine(line);
//                // Console.WriteLine(line2);
//            }
//            Console.WriteLine("%%%%%%%%%%%%%%%%%%%%%%%%%%%%Demand");
//        }
//    }
//}
