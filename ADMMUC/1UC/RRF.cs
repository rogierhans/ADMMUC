using System;
using System.Collections.Generic;
using System.Linq;

namespace ADMMUC._1UC
{

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
        public SUCSolution GetSolution()
        {
            FillInDP();
            int t = UC.LagrangeMultipliers.Count() - 1;
            double bestValue = double.MaxValue;
            int bestTau = int.MaxValue;
            bool On = false;
            double bestP = 0;
            F bestF = null;

            for (int tau = 0; tau < UC.minDownTime; tau++)
            {
                double value = stop[t, tau];
                if (value <= bestValue)
                {
                    bestValue = value;
                    bestTau = tau;
                }
            }
            foreach (var F in Fs[UC.LagrangeMultipliers.Count - 1])
            {
                foreach (var interval in F.Intervals.Where(interval => interval.NonEmptyInterval(UC.pMin, UC.pMax)))
                {
                    var MinimumPointAndValue = interval.MinimumPointAndValue(UC.pMin, UC.pMax);
                    var minimum = MinimumPointAndValue.Item1;
                    var valueAtMinimum = MinimumPointAndValue.Item2;
                    // Console.WriteLine("{0} {1} {2} {3} {4}", F.StartIndex,  valueAtMinimum, minimum, interval.From, interval.To);
                }
            }

            foreach (var F in Fs[UC.LagrangeMultipliers.Count - 1])
            {
                foreach (var interval in F.Intervals)
                {
                    var MinimumPointAndValue = interval.MinimumPointAndValue(UC.pMin, UC.pMax);
                    var minimum = MinimumPointAndValue.Item1;
                    var valueAtMinimum = MinimumPointAndValue.Item2;
                    if (bestValue > valueAtMinimum)
                    {
                        On = true;
                        bestP = minimum;
                        bestValue = valueAtMinimum;
                        bestTau = UC.LagrangeMultipliers.Count - 1 - F.StartIndex;
                        bestF = F;
                    }

                }
            }

            //Console.WriteLine("Best {0} {1} {2} ", On ? bestF.StartIndex.ToString() : "xD", bestValue,bestP);

            List<DPQSolution> solution = new List<DPQSolution>();
            DPQSolution bestLastStep = new DPQSolution(t, bestTau, On, bestP, bestF, bestValue);
            // Console.WriteLine("Value: " + bestValue);
            //bestLastStep.Print();
            solution.Add(bestLastStep);
            double prevBestValue = 0;
            var caluclatedValue = bestValue - (bestLastStep.On ? bestLastStep.F.ValueAtP(bestLastStep.T, bestLastStep.P) : 0);
            while (t-- > 0)
            {
                bestLastStep = bestLastStep.GetBestPrev(UC, Fs, stop);
                caluclatedValue = caluclatedValue - (bestLastStep.On ? bestLastStep.F.ValueAtP(bestLastStep.T, bestLastStep.P) : 0);
                solution.Add(bestLastStep);
                prevBestValue = bestLastStep.Value - (bestLastStep.On ? bestLastStep.F.ValueAtP(bestLastStep.T, bestLastStep.P) : 0);
            }
            solution.Reverse();
            //Fill in Dp twee keer!!!!

            //Console.WriteLine(GetScore());
            //UC.CreateEnv();
            //UC.CalcOptimum();
            //Console.ReadLine();
            return new SUCSolution(UC, solution, GetScore());
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

        // public List<int> tempCount = new List<int>();
        internal void Update(int h)
        {
            // Print(h);

            foreach (var z in Fs[h - 1])
            {
                Fs[h].Add(new F(z));
            }
            //ListOfZ[h] = ListOfZ[h - 1].Select(z => new ZP(z)).ToList();
            foreach (var Z in Fs[h])
            {
                Z.NextPoints(h);
                Z.IncreasePoints(h);
            }
            if (h > UC.minUpTime && Reduction)
            {
                OGremoveWeaklings(h);
                //AltRemoveWeaklings(h);
            }
            //Print(h);

            //Picture();
            //Console.ReadLine();
        }

        //public void Picture()
        //{
        //    new Print(UC).Draw(ListOfZ.Select(Z => Z.Intervals.ToList()).ToList(), Reduction);
        //}
        internal void AddNew(int h, double startCost)
        {
            Fs[h].Add(new F(UC, h, startCost));
            //Console.WriteLine(h);
            //Print(h);
            //Console.WriteLine("******");
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

        private void AltRemoveWeaklings(int h)
        {
            HashSet<F> keepers = new HashSet<F>();
            double maxValue = Fs[h].Max(Z => Z.Intervals.Last.Value.To);
            foreach (var Z in Fs[h].Where(Z => (h - Z.StartIndex) < UC.minUpTime))
            {
                //Console.WriteLine("{0} {1} {2}<{3}", h, Z.StartIndex, h - Z.StartIndex, UC.minDownTime);
                //Console.ReadLine();
                keepers.Add(Z);
            }
            double p = UC.pMin;
            F currentMinimum = GetMinimalFunction(p);
            bool interSects = true;
            while (interSects)
            {
                F nextMinimum = null;
                keepers.Add(currentMinimum);
                interSects = false;
                double pIntersect = currentMinimum.Intervals.Last.Value.To;
                foreach (var otherZ in Fs[h].Where(otherZ => currentMinimum != otherZ && (h - otherZ.StartIndex) >= UC.minUpTime))
                {
                    if (currentMinimum.DoesIntersect(otherZ, p))
                    {
                        interSects = true;
                        double intersectPoint = currentMinimum.FirstIntersect(otherZ, p);
                        if (intersectPoint < pIntersect)
                        {

                            nextMinimum = otherZ;
                            pIntersect = intersectPoint;
                        }
                    }
                }
                if (pIntersect < maxValue && !interSects)
                {
                    nextMinimum = GetMinimalFunction(pIntersect);
                    interSects = true;
                }
                p = pIntersect;
                currentMinimum = nextMinimum;
            }
            Console.WriteLine("{0}   {1}", h, String.Join(" ", keepers.Select(F => F.StartIndex)));
            Fs[h] = keepers.ToList();
            F GetMinimalFunction(double point)
            {
                F minimalFunction = null;
                double bestValue = double.MaxValue;
                foreach (var Z in Fs[h])
                {
                    double valuez = Z.ValueAtP(p);
                    if (Z.Intervals.Last.Value.To > point && valuez < bestValue && (h - Z.StartIndex) >= UC.minUpTime)
                    {
                        minimalFunction = Z;
                        bestValue = valuez;
                    }
                }
                return minimalFunction;
            }
        }

        private void OGremoveWeaklings(int h)
        {
            List<F> ActiveSetOfF = Fs[h];
            bool[] flagged = FlagThoseWithTauLowerThanMinimumUpTime(h, ActiveSetOfF);
            double lastValue = GetHighestDomain(ActiveSetOfF);
            double p = UC.pMin;
            int INDEX = GetIndexOfFminimalAtP(h, ActiveSetOfF, p);
            bool interSects = true;
            while (interSects)
            {
                int nextIndex = INDEX;
                flagged[INDEX] = true;
                interSects = false;
                double currentPieceEnd = ActiveSetOfF[INDEX].Intervals.Last.Value.To;

                for (int i = 0; i < ActiveSetOfF.Count; i++)
                {
                    var currentF = ActiveSetOfF[INDEX];
                    var otherF = ActiveSetOfF[i];
                    bool IsCandidate = (h - otherF.StartIndex) >= UC.minUpTime;
                    if (i != INDEX && IsCandidate && currentF.DoesIntersect(otherF, p))
                    {
                        double firstIntersection = currentF.FirstIntersect(otherF, p);
                        interSects = true;
                        if (firstIntersection < currentPieceEnd)
                        {
                            nextIndex = i;
                            currentPieceEnd = firstIntersection;
                        }
                    }
                }
                p = currentPieceEnd;
                //if there is no intersection but there is a function F with a higher p in  domain
                if (p < lastValue && !interSects)
                {
                    nextIndex = -1;
                    double bestValue = double.MaxValue;
                    for (int i = 0; i < ActiveSetOfF.Count; i++)
                    {
                        var otherF = ActiveSetOfF[i];
                        double otherValue = otherF.ValueAtP(p);
                        bool IsCandidate = (h - otherF.StartIndex) >= UC.minUpTime;
                        bool HigherPInDomain = ActiveSetOfF[i].Intervals.Last.Value.To > p;
                        if (HigherPInDomain && otherValue < bestValue && IsCandidate)
                        {
                            nextIndex = i;
                            bestValue = otherValue;
                        }
                    }
                    interSects = true;
                }
                INDEX = nextIndex;
            }
            RemoveFlagged(ActiveSetOfF, flagged);
            Fs[h] = ActiveSetOfF;
        }

        private static double GetHighestDomain(List<F> ActiveSetOfF)
        {
            double lastValue = double.MinValue;
            for (int i = 0; i < ActiveSetOfF.Count; i++)
            {
                lastValue = Math.Max(lastValue, ActiveSetOfF[i].Intervals.Last.Value.To);
            }

            return lastValue;
        }

        private static void RemoveFlagged(List<F> ActiveSetOfF, bool[] flagged)
        {
            for (int i = ActiveSetOfF.Count - 1; i >= 0; i--)
            {
                if (!flagged[i])
                {
                    ActiveSetOfF.RemoveAt(i);
                }
            }
        }

        private int GetIndexOfFminimalAtP(int h, List<F> ActiveSetOfF, double p)
        {
            int INDEX = -1;
            double bbestValue = double.MaxValue;
            for (int i = 0; i < ActiveSetOfF.Count; i++)
            {
                var Z = ActiveSetOfF[i];
                double valuez = Z.ValueAtP(p);
                if (ActiveSetOfF[i].Intervals.Last.Value.To > p && valuez < bbestValue && (h - Z.StartIndex) >= UC.minUpTime)
                {
                    INDEX = i;
                    bbestValue = valuez;
                }
            }

            return INDEX;
        }

        private bool[] FlagThoseWithTauLowerThanMinimumUpTime(int h, List<F> ListOfZ)
        {
            bool[] flagged = new bool[ListOfZ.Count];
            for (int i = 0; i < ListOfZ.Count; i++)
            {
                if ((h - ListOfZ[i].StartIndex) >= UC.minUpTime)
                {
                    flagged[i] = false;
                }
                else
                {
                    flagged[i] = true;
                }
            }

            return flagged;
        }

        public void Print(int h)
        {
            Fs[h].ForEach(z => { z.FPrint(); });
        }
        private void NewRemoveWeak(int h)
        {
            List<F> CurrentFs = Fs[h];
            double lastValue = double.MinValue;
            bool[] flagged = new bool[CurrentFs.Count];
            for (int i = 0; i < CurrentFs.Count; i++)
            {
                flagged[i] = (h - CurrentFs[i].StartIndex) < UC.minUpTime;
                lastValue = Math.Max(lastValue, CurrentFs[i].Intervals.Last.Value.To);
            }
            double p = UC.pMin;
            int INDEX = -1;
            double bestValue = double.MaxValue;
            for (int i = 0; i < CurrentFs.Count; i++)
            {
                var Z = CurrentFs[i];
                double valuez = Z.ValueAtP(p);
                if (CurrentFs[i].Intervals.Last.Value.To > p && valuez < bestValue && (h - Z.StartIndex) >= UC.minUpTime)
                {
                    INDEX = i;
                    bestValue = valuez;
                }
            }
            bool interSects = true;
            while (interSects)
            {
                int nextIndex = INDEX;
                flagged[INDEX] = true;
                interSects = false;
                double pIntersect = CurrentFs[INDEX].Intervals.Last.Value.To;

                for (int i = 0; i < CurrentFs.Count; i++)
                {
                    var Z = CurrentFs[INDEX];
                    var otherZ = CurrentFs[i];
                    if (i != INDEX && (h - otherZ.StartIndex) >= UC.minUpTime && Z.DoesIntersect(otherZ, p))
                    {
                        double intersectPoint = Z.FirstIntersect(otherZ, p);
                        interSects = true;
                        if (intersectPoint < pIntersect)
                        {
                            nextIndex = i;
                            pIntersect = intersectPoint;
                        }
                    }
                }
                p = pIntersect;
                if (p < lastValue && !interSects)
                {
                    nextIndex = -1;
                    bestValue = double.MaxValue;
                    for (int i = 0; i < CurrentFs.Count; i++)
                    {
                        var Z = CurrentFs[i];
                        double valuez = Z.ValueAtP(p);
                        if (CurrentFs[i].Intervals.Last.Value.To > p && valuez < bestValue && (h - Z.StartIndex) >= UC.minUpTime)
                        {
                            nextIndex = i;
                            bestValue = valuez;
                        }
                    }
                    interSects = true;
                }
                INDEX = nextIndex;
            }
            for (int i = CurrentFs.Count - 1; i >= 0; i--)
            {
                if (!flagged[i])
                {
                    CurrentFs.RemoveAt(i);
                };
            }
            Fs[h] = CurrentFs;
        }

        // its super ugly but it works
        private void OldRemoveWeaklings(int h)
        {
            List<F> ListOfZ = Fs[h];
            double lastValue = double.MinValue;
            bool[] flagged = new bool[ListOfZ.Count];
            for (int i = 0; i < ListOfZ.Count; i++)
            {
                if ((h - ListOfZ[i].StartIndex) >= UC.minUpTime)
                {
                    flagged[i] = false;
                }
                else
                {
                    flagged[i] = true;
                }
                lastValue = Math.Max(lastValue, ListOfZ[i].Intervals.Last.Value.To);
            }
            double p = UC.pMin;
            int INDEX = -1;
            double bestValue = double.MaxValue;
            for (int i = 0; i < ListOfZ.Count; i++)
            {
                var Z = ListOfZ[i];
                double valuez = Z.ValueAtP(p);
                if (ListOfZ[i].Intervals.Last.Value.To > p && valuez < bestValue && (h - Z.StartIndex) >= UC.minUpTime)
                {
                    INDEX = i;
                    bestValue = valuez;
                }
            }
            bool interSects = true;
            while (interSects)
            {
                int nextIndex = INDEX;
                flagged[INDEX] = true;
                interSects = false;
                double pIntersect = ListOfZ[INDEX].Intervals.Last.Value.To;

                for (int i = 0; i < ListOfZ.Count; i++)
                {
                    var Z = ListOfZ[INDEX];
                    var otherZ = ListOfZ[i];
                    if (i != INDEX && (h - otherZ.StartIndex) >= UC.minUpTime)
                    {
                        if (Z.DoesIntersect(otherZ, p))
                        {
                            double intersectPoint = Z.FirstIntersect(otherZ, p);
                            interSects = true;
                            if (intersectPoint < pIntersect)
                            {
                                nextIndex = i;
                                pIntersect = intersectPoint;
                            }
                        }

                    }
                }
                p = pIntersect;
                if (p < lastValue && !interSects)
                {
                    nextIndex = -1;
                    bestValue = double.MaxValue;
                    for (int i = 0; i < ListOfZ.Count; i++)
                    {
                        var Z = ListOfZ[i];
                        double valuez = Z.ValueAtP(p);
                        if (ListOfZ[i].Intervals.Last.Value.To > p && valuez < bestValue && (h - Z.StartIndex) >= UC.minUpTime)
                        {
                            nextIndex = i;
                            bestValue = valuez;
                        }
                    }
                    interSects = true;
                }
                INDEX = nextIndex;
            }
            for (int i = ListOfZ.Count - 1; i >= 0; i--)
            {
                if (!flagged[i])
                {
                    ListOfZ.RemoveAt(i);
                };
            }
            Fs[h] = ListOfZ;
        }
    }
}
