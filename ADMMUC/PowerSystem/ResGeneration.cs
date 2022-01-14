using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADMMUC.PWS
{
    public class ResGeneration
    {
        public string Name;
        public int ID;
        public List<double> ResValues;
        public ResGeneration(int id, List<double> resValues, string name)
        {
            ID = id;
            ResValues = resValues;
            Name = name;
        }

        public double GetResValue(int t) {
            return ResValues[t % ResValues.Count];
        } 
    }
}
