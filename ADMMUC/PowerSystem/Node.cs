using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADMMUC
{
   public class Node
    {
        public int ID;
        public string Name;


        public List<int> UnitsIndex;
        public List<int> StorageUnitsIndex;
        public List<int> RESindex;
        public List<double> Demands = new List<double>();


        public Node(int iD, string name, List<int> unitsIndex, List<int> storageUnitsIndex, List<int> resindex)
        {
            ID = iD;
            Name = name;
            UnitsIndex = unitsIndex;
            StorageUnitsIndex = storageUnitsIndex;
            RESindex = resindex;
        }


        private double stored = 0;
        public double PotentialExport(PowerSystem PS) {
            if (stored == 0) {
                stored = PS.Units.Where(unit => UnitsIndex.Contains(unit.ID)).Sum(x => x.PMax) + PS.Res.Where(res => RESindex.Contains(res.ID)).Sum(x => x.ResValues.Max());
            }
            return stored;
        }

        public void PeturbDemand(Random RNG, double factor)
        {
            if (Demands != null)
                for (int t = 0; t < Demands.Count; t++)
                {
                    double demand = Demands[t];
                    double range = demand / factor;
                    double delta = RNG.NextDouble() * range * 2 - range;
                    Demands[t] = demand + delta;
                }
        }

        public void SetDemand(List<double> values)
        {
            Demands = values;
        }

        public double NodalDemand(int time)
        {
            if (Demands != null && Demands.Count>0)
            {
                return Demands[time % Demands.Count];
            }
            return 0;
        }

        public void PrintInfo()
        {
            Console.WriteLine("Name:{0}", Name);
            Console.WriteLine("Units:");
            foreach (var unit in UnitsIndex)
            {
                Console.WriteLine(unit);
            }
            Console.WriteLine("Demand:");
            if (Demands != null)
                Demands.Take(10).ToList().ForEach(demand => Console.WriteLine(demand));

            Console.WriteLine("ResGeneration:");
        }


        public void PrintStorage()
        {
            Console.WriteLine("StorageUnits {0}:", Name);
            foreach (var storageUnit in StorageUnitsIndex)
            {
                Console.WriteLine(storageUnit);
            }
        }
    }
}
