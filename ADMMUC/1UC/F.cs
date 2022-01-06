

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADMMUC._1UC
{
    public class F
    {
        SUC UC;
        public LinkedList<QuadraticInterval> Intervals = new LinkedList<QuadraticInterval>();
        public int StartIndex = 0;
        //public LinkedListNode<QuadraticInterval> OldOptimum;

        public F(F ZPQuadratic)
        {
            StartIndex = ZPQuadratic.StartIndex;
            UC = ZPQuadratic.UC;
            var node = ZPQuadratic.Intervals.First;
            while (node != null)
            {
                Intervals.AddLast(node.Value.Copy());
                node = node.Next;
            }
            //OldOptimum = ZPQuadratic.OldOptimum;
        }
        public F(SUC uc, int startIndex, double startCost)
        {
            UC = uc;
            StartIndex = startIndex;
            if (startIndex == 0)
            {
                Intervals.AddFirst(new QuadraticInterval(UC.pMin, UC.pMax, 0, 0, 0, StartIndex));
            }
            else
            {
                Intervals.AddFirst(new QuadraticInterval(UC.pMin, UC.SU, startCost, 0, 0, StartIndex));
            }
            IncreasePoints(startIndex);
        }


        internal double BestValue()
        {
            return Intervals.Min(i => i.ValueMinimum());
        }

        public QuadraticInterval GetFirst()
        {
            return Intervals.First.Value;
        }

        public void NextPoints(int h)
        {
            var OptimalNode = GetOptimalNode();
            var bestInterval = OptimalNode.Value;

            double From = bestInterval.From;
            double pStar = bestInterval.MinimumAtInterval();
            double To = bestInterval.To;
            //Console.WriteLine("{0} {1} {2}", From, pStar, To);
            LinkedListNode<QuadraticInterval> LeftNode = OptimalNode;
            OptimalNode.Value.To = pStar;
            var midInterval = new QuadraticInterval(Math.Max(pStar - UC.RampDown, UC.pMin), Math.Min(pStar + UC.RampUp, UC.pMax), bestInterval.ValueMinimum(), 0, 0, StartIndex);
            LinkedListNode<QuadraticInterval> Middle = Intervals.AddAfter(OptimalNode, midInterval);
            var rightInterval = bestInterval.Copy();
            rightInterval.From = pStar;
            rightInterval.To = To;
            LinkedListNode<QuadraticInterval> RightNode = Intervals.AddAfter(Middle, rightInterval);
            //Print();
            if (From == pStar)
            {
                Intervals.Remove(LeftNode);
                ShiftLeft(Middle.Previous);
                ShiftRight(RightNode);
            }
            else if (pStar == To)
            {
                Intervals.Remove(RightNode);
                ShiftLeft(LeftNode);
                ShiftRight(Middle.Next);
            }
            else
            {
                ShiftLeft(LeftNode);
                ShiftRight(RightNode);
            }
            //Print();
            // OldOptimum = Middle;
            Trim();
        }


        private LinkedListNode<QuadraticInterval> GetOptimalNode()

        {
            var currentbest = Intervals.First;
            var min = currentbest.Value.MinimumHack();
            if (min < currentbest.Value.From)
            {
                while (min < currentbest.Value.From && currentbest.Previous != null)
                {
                    currentbest = currentbest.Previous;
                    min = currentbest.Value.MinimumHack();
                }
                return currentbest;
            }
            else if (min > currentbest.Value.To)
            {

                while (min > currentbest.Value.To && currentbest.Next != null)
                {
                    currentbest = currentbest.Next;
                    min = currentbest.Value.MinimumHack();
                }
                return currentbest;
            }
            else
            {
                return currentbest;
            }

        }


        public void ShiftLeft(LinkedListNode<QuadraticInterval> node)
        {
            while (node != null)
            {
                var interval = node.Value;
                interval.From = Math.Max(UC.pMin, interval.From - UC.RampDown);
                interval.To = Math.Max(UC.pMin, interval.To - UC.RampDown);
                interval.A = interval.A + UC.RampDown * interval.B + UC.RampDown * UC.RampDown * interval.C;
                interval.B = interval.B + UC.RampDown * interval.C * 2;
                node = node.Previous;
            }
        }



        public void ShiftRight(LinkedListNode<QuadraticInterval> node)
        {
            while (node != null)
            {
                var interval = node.Value;
                interval.From = Math.Min(UC.pMax, interval.From + UC.RampUp);
                interval.To = Math.Min(UC.pMax, interval.To + UC.RampUp);
                interval.A = interval.A - UC.RampUp * interval.B + UC.RampUp * UC.RampUp * interval.C;
                interval.B = interval.B - (UC.RampUp * 2 * interval.C);
                node = node.Next;
            }

        }
        public void Trim()
        {
            // Console.WriteLine(UC);
            var first = Intervals.First;
            while (first.Value.To == first.Value.From)
            {
                first = first.Next;
                Intervals.RemoveFirst();
            }
            var last = Intervals.Last;
            while (last.Value.To == last.Value.From)
            {
                last = last.Previous;
                Intervals.RemoveLast();
            }

        }

        public void IncreasePoints(int h)
        {
            LinkedListNode<QuadraticInterval> node = Intervals.First;
            while (node != null)
            {
                node.Value.A += UC.A;
                node.Value.B += -UC.LagrangeMultipliers[h] + UC.B + UC.BM[h];
                node.Value.C += UC.C + UC.CM[h];
                node = node.Next;
            }
        }

        public double ValueAtP(int h, double p)
        {
            double value =
            UC.A +
            p * (-UC.LagrangeMultipliers[h] + UC.B) +
            p * p * UC.C;
            return value;
        }

        internal double BestEnd()
        {
            double bestValue = double.MaxValue;
            foreach (var interval in Intervals)
            {
                if (interval.From <= UC.SD)
                {
                    bestValue = Math.Min(bestValue, interval.ValueMinimumRestriced(UC.SD));
                }
            }
            return bestValue;
        }

        public void Print()
        {
            string line = "";// + BestEnd();
            foreach (var interval in Intervals)
            {
                line += "ID:" + interval.ZID + "\t" + (interval.GetValue(interval.From)) + "[" + interval.From + "," + interval.To + "]" + (interval.GetValue(interval.To)) + " ";
                //line += ( interval.B ) + "[" + interval.From + "," + interval.To + "]" +  interval.B  + " ";

            }
            //{
            //    var interval = Intervals.Last.Value;
            //    line += (interval.A + interval.B * interval.To);
            //}
            Console.WriteLine(line);
        }


        public void FPrint()
        {
            string line = "ID:" + Intervals.First.Value.ZID;
            foreach (var interval in Intervals)
            {
                line += "\t(" + Math.Round(interval.A) + "," + Math.Round(interval.B) + "," + interval.C + ")[" + Math.Round(interval.From) + "," + Math.Round(interval.To) + "]";
            }
            Console.WriteLine(line);
        }


        public List<QuadraticInterval> GetIntervals()
        {
            return Intervals.Select(i => i.Copy()).ToList();
        }

        public double ValueAtP(double p)
        {
            LinkedListNode<QuadraticInterval> node = Intervals.First;
            while (node != null)
            {
                var interval = node.Value;
                if (interval.From <= p && p <= interval.To)
                    return interval.GetValue(p);
                node = node.Next;
            }
            return double.MaxValue;
        }

        public void Test()
        {
            var testinters = GetIntervals();
            for (int i = 0; i < testinters.Count - 1; i++)
            {
                var interval1 = testinters[i];
                var interval2 = testinters[i + 1];
                double val1 = interval1.GetValue(interval1.To);
                double val2 = interval2.GetValue(interval2.From);
                if (val1 != val2)
                {
                    throw new Exception(val1 + " " + val2);
                }
            }
        }

        public double FirstIntersect(F otherZP, double p)
        {
            LinkedListNode<QuadraticInterval> node = Intervals.First;
            LinkedListNode<QuadraticInterval> otherNode = otherZP.Intervals.First;
            while (node != null && otherNode != null)
            {
                var interval = node.Value;
                var otherInterval = otherNode.Value;
                double iFrom = Math.Max(interval.From, otherInterval.From);
                double iTo = Math.Min(interval.To, otherInterval.To);

                var tuple = interval.IntersectInIntervalCombined(iFrom, iTo, otherInterval, p);
                if (tuple.Item1)
                {
                    return tuple.Item2;
                }

                if (interval.To < otherInterval.To)
                {
                    node = node.Next;
                }
                else
                {
                    otherNode = otherNode.Next;
                }
            }
            return double.MaxValue;
        }
        public bool DoesIntersect(F otherZP, double p)
        {
            LinkedListNode<QuadraticInterval> node = Intervals.First;
            LinkedListNode<QuadraticInterval> otherNode = otherZP.Intervals.First;
            while (node != null && otherNode != null)
            {
                var interval = node.Value;
                var otherInterval = otherNode.Value;
                double iFrom = Math.Max(interval.From, otherInterval.From);
                double iTo = Math.Min(interval.To, otherInterval.To);
                if (interval.IntersectInIntervalBool(iFrom, iTo, otherInterval, p))
                {
                    return true;
                }

                if (interval.To < otherInterval.To)
                {
                    node = node.Next;
                }
                else
                {
                    otherNode = otherNode.Next;
                }
            }
            return false;
        }
    }
}
