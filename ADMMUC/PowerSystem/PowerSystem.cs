using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using ADMMUC.PWS;
namespace ADMMUC
{
    class PowerSystem
    {

        public string Name;
        public List<Unit> Units;
        public List<Node> Nodes;
        public List<TransmissionLine> Lines;
        public List<ResGeneration> Res;
        public List<StorageUnit> Storages;
        //   public List<TransmissionLine> Lines;
        public ConstraintConfiguration ConstraintConfiguration;
        public double VOLL = 10000;
        
        public double[] Demands;

        public double[,] PTDF;
        public PowerSystem(string name, List<Unit> units, List<Node> nodes, List<TransmissionLine> lines ,List<ResGeneration> res, List<StorageUnit> storages, ConstraintConfiguration constraintConfiguration)
        {
            Name = name;
            Units = units;
            Nodes = nodes;
            int timesteps = nodes.Max(node => node.Demands.Count());
            Demands = new double[timesteps];
            for (int t = 0; t < timesteps; t++)
            {
                Demands[t] += Nodes.Sum(node => node.NodalDemand(t));
            }
            //Demands.ToList().ForEach(x => Console.WriteLine(x));
            Res = res;
            Lines = lines;
            Storages = storages;
            ConstraintConfiguration = constraintConfiguration;
           // PTDF = new PTDF(lines, nodes).GetPTDF();
        }
    }
    class ConstraintConfiguration
    {

        public bool RampingLimits;
        public bool MinUpMinDown;
        public string TransmissionMode;

        public bool Storage;
        public bool TimeDependantStartUpCost;
        public bool Relax;

        public int skipTime;
        public int maxTime;
        public int maxUnits;
        public int maxStorage;

        public int PiecewiseSegments;

        public ConstraintConfiguration(bool rampingLimits, bool minUpMinDown, string transmissionMode, bool storage, bool timeDependantStartUpCost, bool relax, int pwsegments)
        {
            RampingLimits = rampingLimits;
            MinUpMinDown = minUpMinDown;
            TransmissionMode = transmissionMode;
            Storage = storage;
            TimeDependantStartUpCost = timeDependantStartUpCost;
            Relax = relax;
            PiecewiseSegments = pwsegments;
        }

        public void SetLimits(int skipTime, int maxTime, int maxUnits, int maxStorage)
        {
            this.skipTime = skipTime;
            this.maxTime = maxTime;
            this.maxUnits = maxUnits < 0 ? int.MaxValue : maxUnits;
            this.maxStorage = maxStorage < 0 ? int.MaxValue : maxStorage;
        }



        private string Str(bool b)
        {
            if (b) return "1"; else return "0";
        }
    }
}
