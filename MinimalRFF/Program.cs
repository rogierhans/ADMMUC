
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class QuadraticInterval
{

    public double From;
    public double To;
    public double A, B, C;

    public int ZID;

    public QuadraticInterval(double from, double to, double a, double b, double c, int zid)
    {
        From = from;
        To = to;
        A = a;
        B = b;
        C = c;
        ZID = zid;
    }
    public QuadraticInterval Copy()
    {
        return new QuadraticInterval(From, To, A, B, C, ZID);
    }
    public double IntersectPoint(QuadraticInterval other, double Point, double NextPoint)
    {
        if (Point == To)
            return To;
        double p = (A - other.A) / (other.B - B);
        if (From <= p && p <= NextPoint && other.From <= p && p <= NextPoint)
        {
            double val1 = A + B * p;
            double val2 = other.A + other.B * p;
            if (Math.Abs(val1 - val2) > 0.001)
            {
                Console.WriteLine("[{0},{1}] [{2},{3}] ,{4}", From, To, other.From, other.To, p);
                Console.WriteLine("{0} {1}", val1, val2);
                // Console.ReadLine();
            }

            return p;
        }
        if (To < NextPoint)
        {
            return To;
        }
        Console.WriteLine("{0} {1}", Point, NextPoint);
        Console.WriteLine("[{0},{1}] [{2},{3}] ,{4}", From, To, other.From, other.To, p);
        Console.WriteLine("you fucked up, Rogier");
        Console.ReadLine();
        throw new Exception("STOP LEL");
    }

    public double Minimum()
    {
        if (C == 0)
        {
            if (B > 0)
            {
                return From;
            }
            else
            {
                return To;
            }
        }
        return (-B / (2 * C));

    }
    public double MinimumHack()
    {
        if (C == 0)
        {
            if (B > 0)
            {
                return double.MinValue;
            }
            else if (B == 0)
            {
                return From;
            }
            else
            {
                return double.MaxValue;
            }
        }
        return (-B / (2 * C));

    }
    public double MinimumAtInterval()
    {
        if (C == 0)
        {
            if (B > 0)
            {
                return From;
            }
            else
            {
                return To;
            }
        }
        double minimum = (-B / (2 * C));
        if (minimum < From)
        {
            return From;
        }
        else if (minimum > To)
        {
            return To;
        }
        else
        {
            return minimum;
        }
    }

    public bool NonEmptyInterval(double from, double to)
    {
        double realFrom = Math.Max(From, from);
        double realTo = Math.Min(To, to);
        return realFrom <= realTo;
    }
    public (double, double) MinimumPointAndValue(double from, double to)
    {
        if (To < from || From > to)
            return (double.MaxValue, double.MaxValue);


        double minimum = Math.Min(to, Math.Max(from, MinimumAtInterval())); ;
        return (minimum, GetValueInt(minimum));
    }
    public double MinimumAtIntervalRestricted(double max)
    {
        double minimum = MinimumAtInterval();
        if (max < From)
        {
            Console.WriteLine("{0}-{1} , {2}", From, To, max);
            Console.ReadLine();
            return double.MaxValue;
        }
        if (minimum > max)
            return max;
        else
            return minimum;
    }
    public double ValueMinimum()
    {
        double minimum = MinimumAtInterval();
        return GetValue(minimum);
    }
    public double ValueMinimumRestriced(double max)
    {
        double minimum = MinimumAtIntervalRestricted(max);
        return GetValue(minimum);
    }

    public double GetValue(double p)
    {

        return A + p * B + (p * p * C);
    }

    public double GetValueInt(double p)
    {
        if (p <= To && From <= To)
            return A + p * B + (p * p * C);
        else
            return Double.MaxValue;
    }
}

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
}

public class SUC
{
    public int RampUp;
    public int RampDown;
    public int pMax;
    public int pMin;

    public int minDownTime;
    public int minUpTime;

    public int SU;
    public int SD;

    public double startCost;

    public double A;
    public double B;
    public double C;


    public List<double> LagrangeMultipliers = new List<double>();
    public int totalTime;
    public double[] BM;
    public double[] CM;

    internal void PrintStats()
    {
        Console.WriteLine("[{0},{1}] +{2}  -{3}   {4}  {5}  {6} {7}", pMin, pMax, RampUp, RampDown, SU, SD, minUpTime, minDownTime);
    }


    public SUC(double a, double b, double c, double start, int pMax, int pMin, int rampUp, int rampDown, int minUpTime, int minDownTime, int su, int sd, int totalTime)
    {

        this.pMax = pMax;
        this.pMin = pMin;
        SU = su;
        SD = sd;
        RampUp = Math.Max(rampUp, 1);
        RampDown = Math.Max(rampDown, 1);
        this.minDownTime = minDownTime;
        this.minUpTime = minUpTime;
        startCost = start;
        A = a;
        B = b;
        C = c;
        this.totalTime = totalTime;
        BM = new double[totalTime];
        CM = new double[totalTime];

    }

    public void SetLM(List<double> LM, double[] bm, double[] cm)
    {
        LagrangeMultipliers = LM; BM = bm; CM = cm;

    }
}

public class RRF
{
    public bool Reduction = true;
    public List<F>[] Fs;
    double[,] stop;
    SUC UC;
    public int maxFunctions = 0;
    public int maxInterval = 0;
    public RRF(SUC uc, bool reduction)
    {
        UC = uc;
        Reduction = reduction;

    }
    public void FillInDP()
    {
        stop = new double[UC.LagrangeMultipliers.Count, UC.minDownTime];
        for (int tau = 0; tau < UC.minDownTime; tau++)
        {
            stop[0, tau] = 0;
        }
        Fs = new List<F>[UC.LagrangeMultipliers.Count];
        for (int z = 0; z < UC.LagrangeMultipliers.Count; z++)
        {
            Fs[z] = new List<F>();
        }
        AddNew(0, UC.startCost);
        for (int h = 1; h < UC.totalTime; h++)
        {
            stop[h, UC.minDownTime - 1] = Math.Min(stop[h - 1, UC.minDownTime - 2], stop[h - 1, UC.minDownTime - 1]);
            for (int t = 1; t < UC.minDownTime - 1; t++)
            {
                stop[h, t] = stop[h - 1, t - 1];
            }
            stop[h, 0] = GetBestStop(h);
            Update(h);
            var bestStart = Math.Min(UC.startCost, UC.startCost + stop[h - 1, UC.minDownTime - 1]);
            AddNew(h, bestStart);


        }
    }
    public double GetScore()
    {
        //FillInDP();
        double bestValue = 0;
        for (int tau = 0; tau < UC.minDownTime; tau++)
        {
            bestValue = Math.Min(bestValue, stop[UC.LagrangeMultipliers.Count - 1, tau]);
        }
        bestValue = Math.Min(bestValue, BestValue(UC.LagrangeMultipliers.Count - 1));
        return bestValue;
    }
    internal double GetBestStop(int h)
    {
        double bestStop = double.MaxValue;
        //h-1???
        foreach (var Z in Fs[h - 1])
        {
            if (h - Z.StartIndex >= UC.minUpTime || Z.StartIndex == 0)
            {
                bestStop = Math.Min(Z.BestEnd(), bestStop);
            }
        }
        return bestStop;
    }

    internal void Update(int h)
    {
        foreach (var z in Fs[h - 1])
        {
            Fs[h].Add(new F(z));
        }
        foreach (var Z in Fs[h])
        {
            Z.NextPoints(h);
            Z.IncreasePoints(h);
        };
    }

    internal void AddNew(int h, double startCost)
    {
        Fs[h].Add(new F(UC, h, startCost));
    }

    internal double BestValue(int h)
    {
        double bestValue = double.MaxValue;
        foreach (var Z in Fs[h])
        {
            bestValue = Math.Min(bestValue, Z.BestValue());
        }
        return bestValue;
    }
}