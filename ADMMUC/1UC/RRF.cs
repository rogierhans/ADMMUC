using System;
using System.Collections.Generic;
using System.Linq;

namespace ADMMUC._1UC
{

    public class RRF
    {
        public bool Reduction = true;
        public List<F>[] Fs = new List<F>[1];
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

            for (int tau = 0; tau < UC.MinDownTime; tau++)
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
            F bestF = Fs[UC.LagrangeMultipliers.Count - 1].First();
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
            stop = new double[UC.LagrangeMultipliers.Count, UC.MinDownTime];
            for (int tau = 0; tau < UC.MinDownTime; tau++)
            {
                stop[0, tau] = 0;
            }
            Fs = new List<F>[UC.LagrangeMultipliers.Count];
            for (int z = 0; z < UC.LagrangeMultipliers.Count; z++)
            {
                Fs[z] = new List<F>();
            }
            AddNew(0, UC.StartCost);
            for (int h = 1; h < UC.TotalTime; h++)
            {
                stop[h, UC.MinDownTime - 1] = Math.Min(stop[h - 1, UC.MinDownTime - 2], stop[h - 1, UC.MinDownTime - 1]);
                for (int t = 1; t < UC.MinDownTime - 1; t++)
                {
                    stop[h, t] = stop[h - 1, t - 1];
                }
                stop[h, 0] = GetBestStop(h);
                Update(h);
                var bestStart = Math.Min(UC.StartCost, UC.StartCost + stop[h - 1, UC.MinDownTime - 1]);
                AddNew(h, bestStart);


            }

        }
        public double GetScore()
        {
            //FillInDP();
            double bestValue = 0;
            for (int tau = 0; tau < UC.MinDownTime; tau++)
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
                if (h - Z.StartIndex >= UC.MinUpTime || Z.StartIndex == 0)
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
            if (h%4 == 0 &&  h > UC.MinUpTime && Reduction)
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

        private void OGremoveWeaklings(int t)
        {
            List<F> ActiveSetOfF = Fs[t];
            bool[] flagged = FlagThoseWithTauLowerThanMinimumUpTime(t, Fs[t]);
            double lastValue = GetHighestDomain(ActiveSetOfF);
            int INDEX = GetIndexOfFminimalAtP(t, ActiveSetOfF, UC.pMin);
            bool interSects = true;
            double currentEndPiece = UC.pMin;
            while (interSects)
            {
                interSects = false;
                int nextIndex = INDEX;
                flagged[INDEX] = true;
                double nextEndPiece = ActiveSetOfF[INDEX].Intervals.Last().To;
                for (int i = 0; i < ActiveSetOfF.Count; i++)
                {
                    var otherF = ActiveSetOfF[i];
                    if (i == INDEX || (t - otherF.StartIndex) < UC.MinUpTime) continue;

                    var (suc, firstIntersection) = ActiveSetOfF[INDEX].FirstIntersect(otherF, currentEndPiece);
                    if (suc && firstIntersection < nextEndPiece)
                    {
                        interSects = true;
                        nextIndex = i;
                        nextEndPiece = firstIntersection;
                    }

                }
                if (nextEndPiece < lastValue && !interSects)
                {
                    interSects = true;
                    nextIndex = FindHighestFunction(t, ActiveSetOfF, nextIndex, nextEndPiece);
                }
                currentEndPiece = nextEndPiece;
                INDEX = nextIndex;
            }
            RemoveFlagged(ActiveSetOfF, flagged);
            Fs[t] = ActiveSetOfF;
        }

        private int FindHighestFunction(int h, List<F> ActiveSetOfF, int nextIndex, double nextEndPiece)
        {
            double bestValue = double.MaxValue;
            for (int i = 0; i < ActiveSetOfF.Count; i++)
            {
                var otherF = ActiveSetOfF[i];
                double otherValue = otherF.ValueAtP(nextEndPiece);
                bool IsCandidate = (h - otherF.StartIndex) >= UC.MinUpTime;
                bool HigherPInDomain = ActiveSetOfF[i].Intervals.Last().To > nextEndPiece;
                if (HigherPInDomain && otherValue < bestValue && IsCandidate)
                {
                    nextIndex = i;
                    bestValue = otherValue;
                }
            }
            return nextIndex;
        }

        private static double GetHighestDomain(List<F> ActiveSetOfF)
        {
            double lastValue = double.MinValue;
            for (int i = 0; i < ActiveSetOfF.Count; i++)
            {
                lastValue = Math.Max(lastValue, ActiveSetOfF[i].Intervals.Last().To);
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
                if (ActiveSetOfF[i].Intervals.Last().To > p && valuez < bbestValue)
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
                if ((h - ListOfZ[i].StartIndex) >= UC.MinUpTime)
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

    }
}
