using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADMMUC
{

    public class AlgorithmCaller {


        public (double, List<DPStep>) SolveWithSolution(SUC suc)
        {
            XPieceSolution H = new XPieceSolution(suc)
            {
                OPTIMIZE = true
            };
            double bestValue = H.GetSink();
            var temphex = H.GetSolution();
            return (bestValue, temphex);
        }

    }

    public class DPStep
    {
        public int T;
        public int Tau;
        public bool On;
        public double P;

        public DPStep(int t, int tau, bool on, double p)
        {
            T = t;
            Tau = tau;
            On = on;
            P = p;
        }
        public DPStep GetBestPrev(SUC UC, double[,,] H, double[,] Off, double[] pPoints)
        {
            if (T == 0)
                throw new Exception("?????");
            if (!On)
            {
                return OffPrevStep(UC, H, Off, pPoints);
            }
            else
                return OnPrevStep(UC, H, Off, pPoints);
        }
        public DPStep OffPrevStep(SUC UC, double[,,] H, double[,] Off, double[] pPoints)
        {
            if (Tau == 0)
            {
                double bestP = double.MaxValue;
                double bestValue = double.MaxValue;
                for (int i = 0; i < pPoints.Length; i++)
                {
                    double prevP = pPoints[i];
                    double value = H[T - 1, UC.MinUpTime - 1, i];
                    if (value < bestValue &&
                        prevP <= UC.SD)
                    {
                        bestP = prevP;
                        bestValue = value;
                    }
                }
                return new DPStep(T - 1, UC.MinUpTime - 1, true, bestP);
            }
            else if (0 < Tau && Tau < UC.MinDownTime - 1)
            {
                return new DPStep(T - 1, Tau - 1, false, 0);
            }
            else if (Tau == UC.MinDownTime - 1)
            {
                if (Off[Tau, T - 1] < Off[Tau - 1, T - 1])
                {
                    return new DPStep(T - 1, Tau, false, 0);
                }
                else
                {
                    return new DPStep(T - 1, Tau - 1, false, 0);
                }
            }
            else
            {
                throw new Exception("HMMM");
            }

        }
        public DPStep OnPrevStep(SUC UC, double[,,] H, double[,] Off, double[] pPoints)
        {
            if (Tau == 0)
            {
                return new DPStep(T - 1, UC.MinDownTime - 1, false, 0);
            }
            else if (0 < Tau && Tau < UC.MinUpTime - 1)
            {
                double bestP = double.MaxValue;
                double bestValue = double.MaxValue;
                for (int i = 0; i < pPoints.Length; i++)
                {
                    double prevP = pPoints[i];
                    double value = H[T - 1, Tau - 1, i];
                    if (value < bestValue &&
                        (prevP - P) <= UC.RampDown &&
                        (P - prevP) <= UC.RampUp)
                    {
                        bestP = prevP;
                        bestValue = value;
                    }
                }
                return new DPStep(T - 1, Tau - 1, true, bestP);
            }
            else if (Tau == UC.MinUpTime - 1)
            {
                double bestP = double.MaxValue;
                double bestValue = double.MaxValue;
                int bestTau = int.MaxValue;
                for (int i = 0; i < pPoints.Length; i++)
                {
                    double prevP = pPoints[i];
                    double value = H[T - 1, Tau - 1, i];
                    if (value < bestValue &&
                        (prevP - P) <= UC.RampDown &&
                        (P - prevP) <= UC.RampUp)
                    {
                        bestP = prevP;
                        bestValue = value;
                        bestTau = Tau - 1;
                    }
                    //Console.WriteLine(value);
                }

                for (int i = 0; i < pPoints.Length; i++)
                {
                    double prevP = pPoints[i];
                    double value = H[T - 1, Tau, i];
                    if (value < bestValue &&
                        (prevP - P) <= UC.RampDown &&
                        (P - prevP) <= UC.RampUp)
                    {
                        bestP = prevP;
                        bestValue = value;
                        bestTau = Tau;
                    }
                    //Console.WriteLine(value + " ");
                }
                //Console.ReadLine();
                return new DPStep(T - 1, bestTau, true, bestP);
            }
            else
            {
                throw new Exception("HMMM");
            }
        }
        public void Print()
        {
            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine("{0} {1} {2} {3}", On ? "On" : "Off", T, Tau, P);
        }
    }
    class XPieceSolution
    {
        //public List<Interval>[] SetOfIntervals;// = new List<Interval>[24];

        public double RampUp, RampDown, SU;

        public int totalTime;
        SUC UC;
        public double[,,] points;
        public double[,] off;

        public double[] pPoint;
        public double StartCost;
        public int[] UpRange;
        public int[] DownRange;

        public XPieceSolution(SUC uc)
        {

            UC = uc;
            RampUp = UC.RampUp;
            RampDown = UC.RampDown;
            SU = UC.SU;
            totalTime = UC.LagrangeMultipliers.Count;

            StartCost = uc.StartCost;
            pPoint = AltCreatedPoints();
            copy = new double[pPoint.Length];
            array = new int[pPoint.Length];
            points = new double[totalTime, uc.MinUpTime, pPoint.Length];

            UpRange = StepUpSizeAll(pPoint);
            DownRange = StepDownSizeAll(pPoint);
            off = new double[uc.MinDownTime, totalTime];
            SetPieceWiseCost();

        }

        public List<DPStep> GetSolution()
        {

            int t = totalTime - 1;
            double bestValue = double.MaxValue;
            int bestTau = int.MaxValue;
            bool On = false;
            double bestP = 0;

            for (int tau = 0; tau < UC.MinDownTime; tau++)
            {
                double value = off[tau, t];
                if (value < bestValue)
                {
                    bestValue = value;
                    bestTau = tau;
                }
            }
            for (int tau = 0; tau < UC.MinUpTime; tau++)
            {
                for (int i = 0; i < pPoint.Length; i++)
                {
                    double p = pPoint[i];
                    double value = points[t, tau, i];
                    if (value < bestValue)
                    {
                        bestValue = value;
                        bestTau = tau;
                        On = true;
                        bestP = p;
                    }
                }
            }


            List<DPStep> solution = new List<DPStep>();
            DPStep bestLastStep = new DPStep(t, bestTau, On, bestP);
            solution.Add(bestLastStep);
            while (t-- > 0)
            {
                //bestLastStep.Print();
                bestLastStep = bestLastStep.GetBestPrev(UC, points, off, pPoint);
                solution.Add(bestLastStep);
            }
            solution.Reverse();
            return solution;
            //
            //solution.ForEach(step => Console.Write(step.On? "1" : "0"));
            //Console.WriteLine(solution.Count);
            // Console.WriteLine(solution.Count);
        }


        public bool OPTIMIZE;
        public void Update(int t)
        {
            //start = Math.Min(startCost, startCost + off[minDownTime - 1, t - 1]);
            off[UC.MinDownTime - 1, t] = Math.Min(off[UC.MinDownTime - 2, t - 1], off[UC.MinDownTime - 1, t - 1]);
            for (int tau = UC.MinDownTime - 2; tau >= 1; tau--)
            {
                off[tau, t] = off[tau - 1, t - 1];
            }
            off[0, t] = BestEnd(t - 1);

            if (OPTIMIZE)
            {
                NextPoints(t);
                NextConvexPoints(t);
            }
            else
            {
                NextBad(t);
            }


            New(t, Math.Min(UC.StartCost, UC.StartCost + off[UC.MinDownTime - 1, t - 1]));
            IncreasePoints(t);
        }
        public double BestValue()
        {
            double best = double.MaxValue;
            for (int tau = 0; tau < UC.MinUpTime; tau++)
            {
                for (int p = 0; p < pPoint.Length; p++)
                {
                    // Console.WriteLine(points[totalTime - 1, tau, p]);
                    best = Math.Min(best, points[totalTime - 1, tau, p]);
                }
            }
            return best;
        }



        private void SetBeginPoints()
        {
            for (int tau = 0; tau < UC.MinUpTime; tau++)
            {

                for (int p = 0; p < pPoint.Length; p++)
                {
                    if (pPoint[p] <= UC.SU && tau == 0)
                    {
                        // Console.WriteLine("{0} {1} {2}", 0, tau, p);
                        points[0, tau, p] = 0;// StartCost;
                    }
                    else
                    {
                        points[0, tau, p] = 0;// StartCost;// double.MaxValue;
                    }
                }
            }
        }

        private double[] PieceWiseCost;
        private void SetPieceWiseCost()
        {

            throw new NotImplementedException();
            //PieceWiseCost = new double[pPoint.Length];

            //for (int p = 0; p < pPoint.Length; p++)
            //{
            //    PieceWiseCost[p] = UC.PWC.GetPieceWiseCost(pPoint[p]);
            //    var point = pPoint[p];
            //    var value = UC.A + UC.B * point + point * point * UC.C;
            //}
        }

        public void IncreasePoints(int t)
        {
            var multiplier = UC.LagrangeMultipliers[t];
            for (int tau = 0; tau < UC.MinUpTime; tau++)
            {

                for (int p = 0; p < pPoint.Length; p++)
                {
                    points[t, tau, p] += (-pPoint[p] * multiplier) + PieceWiseCost[p];
                }

            }
        }

        public double[] copy;
        private readonly int[] array;
        public void NextPoints(int t)
        {

            for (int p = 0; p < pPoint.Length; p++)
            {
                points[t, UC.MinUpTime - 1, p] = Math.Min(points[t - 1, UC.MinUpTime - 1, p], points[t - 1, UC.MinUpTime - 2, p]);
                //?points[t - 1, UC.minUpTime - 1, p] : points[t - 1, UC.minUpTime - 2, p];
            }
            int tau = UC.MinUpTime - 1;
            int rangeMin = 0;
            int rangeMax = -1;
            int indexFront = 0;
            int indexBack = 0;
            for (int i = 0; i < pPoint.Length; i++)
            {
                int minIndex = i - UpRange[i];
                int maxIndex = i + DownRange[i];
                while (rangeMax < maxIndex)
                {
                    rangeMax++;
                    double value = points[t, tau, rangeMax];
                    while ((indexFront != indexBack) && value <= points[t, tau, array[indexBack - 1]])
                    {
                        //popback
                        indexBack--;
                    }
                    //pushback
                    array[indexBack++] = rangeMax;
                }
                while (rangeMin < minIndex)
                {
                    if (array[indexFront] <= rangeMin)
                    {
                        //popfront
                        indexFront++;
                    }
                    rangeMin++;
                }
                copy[i] = points[t, tau, array[indexFront]];
            }
            for (int p = 0; p < pPoint.Length; p++)
            {
                points[t, tau, p] = copy[p];

            }
        }
        public void NextConvexPoints(int t)
        {
            //for (int tau = UC.minUpTime - 2; tau >= 1; tau--)
            //{
            //    int bestIndex = BestIndex(t - 1, tau - 1);
            //    int max = bestIndex - UpRange[bestIndex];
            //    for (int p = 0; p < max; p++)
            //    {
            //        points[t, tau, p] = points[t - 1, tau - 1, p + DownRange[p]];
            //    }
            //    int min = bestIndex + DownRange[bestIndex];
            //    for (int p = pPoint.Length - 1; p > min; p--)
            //    {

            //        points[t, tau, p] = points[t - 1, tau - 1, p - UpRange[p]];

            //    }
            //    double best = points[t - 1, tau - 1, bestIndex];
            //    max = bestIndex + DownRange[bestIndex];
            //    for (int p = bestIndex - UpRange[bestIndex]; p <= max; p++)
            //    {
            //        points[t, tau, p] = best;
            //    }
            //}
            //for (int tau = UC.minUpTime - 2; tau >= 1; tau--)
            //{
            //    int bestIndex = BestIndex(t - 1, tau - 1);
            //    for (int p = 0; p < pPoint.Length; p++)
            //    {
            //        if (p < bestIndex - DownRange[p])
            //        {
            //            points[t, tau, p] = points[t - 1, tau - 1, p + DownRange[p]];
            //        }
            //    }
            //    for (int p = pPoint.Length - 1; p >= 0; p--)
            //    {
            //        if (p > bestIndex + UpRange[p])
            //        {
            //            points[t, tau, p] = points[t - 1, tau - 1, p - UpRange[p]];
            //        }
            //    }
            //    double best = points[t - 1, tau - 1, bestIndex];
            //    int max = bestIndex + DownRange[bestIndex];
            //    for (int p = bestIndex - UpRange[bestIndex]; p <= max; p++)
            //    {
            //        points[t, tau, p] = best;
            //    }
            //}
            for (int tau = UC.MinUpTime - 2; tau >= 1; tau--)
            {
                int bestIndex = BestIndex(t - 1, tau - 1);
                for (int p = 0; p < pPoint.Length; p++)
                {
                    if (p < bestIndex - DownRange[p])
                    {
                        points[t, tau, p] = points[t - 1, tau - 1, p + DownRange[p]];
                    }
                }
                for (int p = pPoint.Length - 1; p >= 0; p--)
                {
                    if (p > bestIndex + UpRange[p])
                    {
                        points[t, tau, p] = points[t - 1, tau - 1, p - UpRange[p]];
                    }
                }
                for (int p = 0; p < pPoint.Length; p++)
                {
                    if (p + DownRange[p] >= bestIndex && p - UpRange[p] <= bestIndex)
                    {
                        points[t, tau, p] = points[t - 1, tau - 1, bestIndex];
                    }
                }
            }
        }
        public void NextBad(int t)
        {
            for (int tau = 1; tau < UC.MinUpTime - 1; tau++)
                for (int p = 0; p < pPoint.Length; p++)
                {
                    double bestValue = double.MaxValue;
                    for (int pp = 0; pp < pPoint.Length; pp++)
                    {
                        double rampdown = pPoint[pp] - pPoint[p];
                        double rampup = -rampdown;
                        if (rampdown <= UC.RampDown && rampup <= UC.RampUp && bestValue > points[t - 1, tau - 1, pp])
                        {
                            bestValue = points[t - 1, tau - 1, pp];
                        }
                    }
                    points[t, tau, p] = bestValue;
                }
            for (int p = 0; p < pPoint.Length; p++)
            {
                double bestValue = double.MaxValue;
                for (int pp = 0; pp < pPoint.Length; pp++)
                {
                    double rampdown = pPoint[pp] - pPoint[p];
                    double rampup = -rampdown;
                    var minimunAtP = Math.Min(points[t - 1, UC.MinUpTime - 1, pp], points[t - 1, UC.MinUpTime - 2, pp]);
                    if (rampdown <= UC.RampDown && rampup <= UC.RampUp && bestValue > minimunAtP)
                    {
                        bestValue = minimunAtP;
                    }
                }
                points[t, UC.MinUpTime - 1, p] = bestValue;
            }
        }

        private int BestIndex(int t, int tau)
        {
            //Console.WriteLine(t +" "+ tau + " " +UC.minUpTime);
            double bestValue = double.MaxValue;
            int bestIndex = 0;
            for (int p = 0; p < pPoint.Length; p++)
            {
                if (bestValue > points[t, tau, p])
                {
                    bestIndex = p;
                    bestValue = points[t, tau, p];
                }
            }
            return bestIndex;
        }

        public double BestEnd(int t)
        {
            double bestValue = double.MaxValue;
            int p = 0;
            while (p < pPoint.Length && pPoint[p] <= UC.SD)
            {
                if (bestValue > points[t, UC.MinUpTime - 1, p])
                {
                    bestValue = points[t, UC.MinUpTime - 1, p];
                }
                p++;
            }
            return bestValue;
        }


        public void New(int t, double start)
        {

            for (int p = 0; p < pPoint.Length; p++)
            {
                if (pPoint[p] <= UC.SU)
                {
                    points[t, 0, p] = start;
                }
                else
                {
                    points[t, 0, p] = double.MaxValue;
                }
            }

        }

        internal double GetSink()
        {
            for (int tau = 0; tau < UC.MinDownTime; tau++)
            {
                off[tau, 0] = double.MaxValue;
            }
            off[UC.MinDownTime - 1, 0] = 0;
            SetBeginPoints();
            IncreasePoints(0);
            //  Print(0);
            for (int t = 1; t < totalTime; t++)
            {
                Update(t);
                //Print(t);
            }
            double bestValue = BestValue();
            for (int tau = 0; tau < UC.MinDownTime; tau++)
            {
                bestValue = Math.Min(bestValue, off[tau, totalTime - 1]);
            }
            return bestValue;
        }



        HashSet<double> uniquePoints = new HashSet<double>();

        
        private double[] AltCreatedPoints()
        {
            uniquePoints = new HashSet<double>();
            First(UC.pMin);
            First(UC.pMax);
            First(SU);
            foreach (double breakpoint in new List<double>())
            {
                throw new NotImplementedException();
                First(breakpoint);
            }
            uniquePoints.ToList().ForEach(x => { Second(UC.SD); Second(x + RampDown); Second(x - RampUp); });
            var array = uniquePoints.ToList().OrderBy(x => x).ToArray();
            return array;
        }


        private void First(double p)
        {
            if (!uniquePoints.Contains(p) && UC.pMin <= p && p <= UC.pMax)
            {
                uniquePoints.Add(p);
                First(p - RampDown);
                First(p + RampUp);
            }
        }

        private void Second(double p)
        {
            if (!uniquePoints.Contains(p) && UC.pMin <= p && p <= UC.pMax)
            {
                uniquePoints.Add(p);
                Second(p + RampDown);
                Second(p - RampUp);
            }
        }

        private int[] StepUpSizeAll(double[] pPoints)
        {
            int[] sizes = new int[pPoints.Length];
            for (int i = 0; i < pPoints.Length; i++)
            {
                sizes[i] = StepUpSize(pPoints, i);
            }
            return sizes;
        }

        private int[] StepDownSizeAll(double[] pPoints)
        {
            int[] sizes = new int[pPoints.Length];
            for (int i = 0; i < pPoints.Length; i++)
            {
                sizes[i] = StepDownSize(pPoints, i);
            }
            return sizes;
        }

        private int StepUpSize(double[] pPoint, int index)
        {
            int maxStepSize = 0;
            for (int stepSize = 0; stepSize < pPoint.Length; stepSize++)
            {
                if (index - stepSize >= 0 && pPoint[index] - pPoint[index - stepSize] <= RampUp)
                {
                    maxStepSize = stepSize;
                }
            }
            return maxStepSize;
        }

        private int StepDownSize(double[] pPoint, int index)
        {
            int maxStepSize = 0;
            for (int stepSize = 0; stepSize < pPoint.Length; stepSize++)
            {
                if (index + stepSize < pPoint.Length && pPoint[index + stepSize] - pPoint[index] <= RampDown)
                {
                    maxStepSize = stepSize;
                }
            }
            return maxStepSize;
        }
        public void Print(int t)
        {
            List<string> cells = new List<string>();
            for (int tau = 0; tau < UC.MinDownTime; tau++)
            {
                cells.Add(off[tau, t].ToString());
            }
            Console.WriteLine(t + " off" + String.Join("\t", cells));
            for (int tau = 0; tau < UC.MinUpTime; tau++)
            {
                cells = new List<string>();

                for (int p = 0; p < pPoint.Length; p++)
                {
                    cells.Add(points[t, tau, p].ToString());
                }
                Console.WriteLine(t + "on" + tau + " " + String.Join("\t", cells));
            }
        }
    }
}
