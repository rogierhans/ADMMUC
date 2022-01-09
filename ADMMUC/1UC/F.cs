

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace ADMMUC._1UC
{
    public class F
    {
        SUC UC;
        public List<QuadraticInterval> Intervals = new List<QuadraticInterval>();
        public int StartIndex = 0;
        //public LinkedListNode<QuadraticInterval> OldOptimum;

        public F(F other)
        {
            StartIndex = other.StartIndex;
            UC = other.UC;

            Intervals.Add(other.Intervals[0].Copy());
            for (int i = 1; i < other.Intervals.Count; i++)
            {
                var interval = other.Intervals[i];
                if (interval.From != interval.To)
                {
                    Intervals.Add(interval.Copy());
                }
            }
        }
        public F(SUC uc, int startIndex, double startCost)
        {
            UC = uc;
            StartIndex = startIndex;
            if (startIndex == 0)
            {
                Intervals.Add(new QuadraticInterval(UC.pMin, UC.pMax, 0, 0, 0, StartIndex));
            }
            else
            {
                Intervals.Add(new QuadraticInterval(UC.pMin, UC.SU, startCost, 0, 0, StartIndex));
            }
            IncreasePoints(startIndex);
        }


        internal double BestValue()
        {
            return Intervals.Min(i => i.ValueMinimum());
        }

        public void NextPoints(int h)
        {
            int Index = GetOptimalNode();
            var bestInterval = Intervals[Index];
            double pStar = bestInterval.MinimumAtInterval();
            double To = bestInterval.To;
            bestInterval.To = pStar;
            var midInterval = new QuadraticInterval(Math.Max(pStar - UC.RampDown, UC.pMin), Math.Min(pStar + UC.RampUp, UC.pMax), bestInterval.ValueMinimum(), 0, 0, StartIndex);
            Intervals.Insert(Index + 1, midInterval);
            var rightInterval = bestInterval.Copy();
            rightInterval.From = pStar;
            rightInterval.To = To;
            Intervals.Insert(Index + 2, rightInterval);
            ShiftLeft(Index);
            ShiftRight(Index + 2);
            Trim();
        }


        private int GetOptimalNode()

        {
            int INDEX = 0;
            var min = Intervals[INDEX].MinimumHack();
            while (min > Intervals[INDEX].To && INDEX < Intervals.Count - 1)
            {
                INDEX++;
                min = Intervals[INDEX].MinimumHack();
            }

            return INDEX;
        }



        public void ShiftLeft(int index)
        {
            for (int i = 0; i <= index; i++)
            {

                var interval = Intervals[i];
                interval.From = Math.Max(UC.pMin, interval.From - UC.RampDown);
                interval.To = Math.Max(UC.pMin, interval.To - UC.RampDown);
                interval.A = interval.A + UC.RampDown * interval.B + UC.RampDown * UC.RampDown * interval.C;
                interval.B = interval.B + UC.RampDown * interval.C * 2;
            }
        }



        public void ShiftRight(int index)
        {
            for (int i = index; i < Intervals.Count; i++)
            {
                var interval = Intervals[i];
                interval.From = Math.Min(UC.pMax, interval.From + UC.RampUp);
                interval.To = Math.Min(UC.pMax, interval.To + UC.RampUp);
                interval.A = interval.A - UC.RampUp * interval.B + UC.RampUp * UC.RampUp * interval.C;
                interval.B = interval.B - (UC.RampUp * 2 * interval.C);
            }
        }
        public void Trim()
        {
            var first = Intervals.First();
            while (first.From == first.To)
            {
                Intervals.RemoveAt(0);
                first = Intervals.First();
            }
            var last = Intervals.Last();
            while (last.From == last.To)
            {
                Intervals.RemoveAt(Intervals.Count - 1);
                last = Intervals.Last();
            }
        }


        public void IncreasePoints(int t)
        {
            foreach (var interval in Intervals)
            {
                interval.A += UC.A;
                interval.B += -UC.LagrangeMultipliers[t] + UC.B + UC.BM[t];
                interval.C += UC.C + UC.CM[t];
            }
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
                else return bestValue;
            }
            return bestValue;
        }

        public double ValueAtP(int h, double p)
        {
            double value =
            UC.A +
            p * (-UC.LagrangeMultipliers[h] + UC.B) +
            p * p * UC.C;
            return value;
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


        public List<QuadraticInterval> GetIntervals()
        {
            return Intervals.Select(i => i.Copy()).ToList();
        }

        public double ValueAtP(double p)
        {

            foreach (var interval in Intervals)
            {
                if (interval.From <= p && p <= interval.To)
                    return interval.GetValue(p);
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
            int counter = 0;
            int otherCoutner = 0;   
            while (counter< Intervals.Count  && otherCoutner < otherZP.Intervals.Count)
            {
                var interval = Intervals[counter];
                var otherInterval = otherZP.Intervals[otherCoutner];
                double iFrom = Math.Max(interval.From, otherInterval.From);
                double iTo = Math.Min(interval.To, otherInterval.To);

                var tuple = interval.IntersectInIntervalCombined(iFrom, iTo, otherInterval, p);
                if (tuple.Item1)
                {
                    return tuple.Item2;
                }

                if (interval.To < otherInterval.To)
                {
                    counter++;
                }
                else
                {
                    otherCoutner++;
                }
            }
            return double.MaxValue;
        }
        public bool DoesIntersect(F otherZP, double p)
        {
            int counter = 0;
            int otherCoutner = 0;
            while (counter < Intervals.Count && otherCoutner < otherZP.Intervals.Count)
            {
                var interval = Intervals[counter];
                var otherInterval = otherZP.Intervals[otherCoutner];
                double iFrom = Math.Max(interval.From, otherInterval.From);
                double iTo = Math.Min(interval.To, otherInterval.To);
                if (interval.IntersectInIntervalBool(iFrom, iTo, otherInterval, p))
                {
                    return true;
                }

                if (interval.To < otherInterval.To)
                {
                    counter++;
                }
                else
                {
                    otherCoutner++;
                }
            }
            return false;
        }
    }
}
