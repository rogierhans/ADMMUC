using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADMMUC
{
    public class TransmissionLine
    {
        public Node From;
        public Node To;

        public double MinCapacity;
        public double MaxCapacity;

        public double Susceptance;

        public TransmissionLine(Node from, Node to, double minCapacity, double maxCapacity, double susceptance)
        {
            From = from;
            To = to;

            MinCapacity = minCapacity;
            MaxCapacity = maxCapacity;
            Susceptance = susceptance;
        }
    }
}
