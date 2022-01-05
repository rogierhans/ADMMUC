namespace ADMMUC._1UC
{
    class RRF
    {
        public bool Reduction = true;
        public List<F>[] Fs;
        readonly double[,] OffStateCosts;
        readonly GeneratorQuadratic UC;
        public int MaxFunctions = 0;
        public int MaxInterval = 0;

        public RRF(GeneratorQuadratic uc, bool reduction)
        {
            UC = uc;
            Reduction = reduction;
            OffStateCosts = new double[UC.LagrangeMultipliers.Count, UC.minDownTime];
            Fs = new List<F>[UC.LagrangeMultipliers.Count];
            for (int i = 0; i < UC.LagrangeMultipliers.Count; i++)
            {
                Fs[i] = new List<F>();
            }
            AddNew(0, UC.startCost);
            for (int t = 1; t < UC.totalTime; t++)
            {
                OffStateCosts[t, UC.minDownTime - 1] = Math.Min(OffStateCosts[t - 1, UC.minDownTime - 2], OffStateCosts[t - 1, UC.minDownTime - 1]);
                for (int tau = 1; tau < UC.minDownTime - 1; tau++)
                {
                    OffStateCosts[t, tau] = OffStateCosts[t - 1, tau - 1];
                }
                OffStateCosts[t, 0] = GetBestStop(t);

                Update(t);
                var bestStart = Math.Min(UC.startCost, UC.startCost + OffStateCosts[t - 1, UC.minDownTime - 1]);
                AddNew(t, bestStart);
            }
        }
        public SUCSolution GetSolution()
        {
            int t = UC.LagrangeMultipliers.Count() - 1;
            double bestValue = double.MaxValue;
            int bestTau = int.MaxValue;
            bool On = false;
            double bestP = 0;
            F bestF = Fs[UC.LagrangeMultipliers.Count - 1].First();

            for (int tau = 0; tau < UC.minDownTime; tau++)
            {
                double value = OffStateCosts[t, tau];
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
            List<DPQSolution> solution = new List<DPQSolution>();
            DPQSolution bestLastStep = new DPQSolution(t, bestTau, On, bestP, bestF, bestValue);
            solution.Add(bestLastStep);
            double prevBestValue = 0;
            var caluclatedValue = bestValue - (bestLastStep.On ? bestLastStep.F.ValueAtP(bestLastStep.T, bestLastStep.P) : 0);
            while (t-- > 0)
            {
                bestLastStep = bestLastStep.GetBestPrev(UC, Fs, OffStateCosts);
                caluclatedValue = caluclatedValue - (bestLastStep.On ? bestLastStep.F.ValueAtP(bestLastStep.T, bestLastStep.P) : 0);
                solution.Add(bestLastStep);
                prevBestValue = bestLastStep.Value - (bestLastStep.On ? bestLastStep.F.ValueAtP(bestLastStep.T, bestLastStep.P) : 0);
            }
            solution.Reverse();
            return new SUCSolution(UC, solution, GetScore());
        }


        public double GetScore()
        {
            //FillInDP();
            double bestValue = 0;
            for (int tau = 0; tau < UC.minDownTime; tau++)
            {
                bestValue = Math.Min(bestValue, OffStateCosts[UC.LagrangeMultipliers.Count - 1, tau]);
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
        internal void Update(int t)
        {
            foreach (var prevF in Fs[t - 1])
            {
                Fs[t].Add(new F(prevF));
            }
            foreach (var F in Fs[t])
            {
                F.NextPoints(t);
                F.IncreasePoints(t);
            }
            if (t > UC.minUpTime && Reduction)
            {
                RemoveNonDominatingFs(t);
            }
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
        List<F> next = new List<F>();

        static int counter = 0;
        private void RemoveNonDominatingFs(int t)
        {
        //    if(counter++ % 1000 == 0)
           // Console.WriteLine(counter);
            List<F> CurrentFs = Fs[t];
            bool[] flagged = FlagThoseWithTauLowerThanMinimumUpTime(t, CurrentFs);
            double lastValue = GetHighestDomain(CurrentFs);
            int currentIndex = GetIndexOfFminimalAtP(t, CurrentFs, UC.pMin);
            bool interSects = true;
            while (interSects)
            {
                var currentF = CurrentFs[currentIndex];
                interSects = false;
                flagged[currentIndex] = true;
                double currentPieceEnd = CurrentFs[currentIndex].Intervals.Last().To;
                for (int i = 0; i < CurrentFs.Count; i++)
                {
                    int nextIndex = currentIndex;
                    if (i != currentIndex) continue;
                    var otherF = CurrentFs[i];
                    bool IsCandidate = (t - otherF.StartIndex) >= UC.minUpTime;
                    if (!IsCandidate) continue;
                    if (currentF.DoesIntersect(otherF, currentPieceEnd))
                    {
                        double firstIntersection = currentF.FirstIntersect(otherF, currentPieceEnd);
                        interSects = true;
                        if (firstIntersection < currentPieceEnd)
                        {
                            nextIndex = i;
                            currentPieceEnd = firstIntersection;
                        }
                    }
                    currentIndex = nextIndex;
                }
                //if there is no intersection but there is a function F with a higher p in  domain
                if (currentPieceEnd < lastValue && !interSects)
                {
                    double bestValue = double.MaxValue;
                    for (int i = 0; i < CurrentFs.Count; i++)
                    {
                        var otherF = CurrentFs[i];
                        double otherValue = otherF.ValueAtP(currentPieceEnd);
                        bool IsCandidate = (t - otherF.StartIndex) >= UC.minUpTime;
                        bool HigherPInDomain = CurrentFs[i].Intervals.Last().To > currentPieceEnd;
                        if (HigherPInDomain && otherValue < bestValue && IsCandidate)
                        {
                            currentIndex = i;
                            bestValue = otherValue;
                        }
                    }
                    interSects = true;
                }

            }
            RemoveFlagged(CurrentFs, flagged);
            Fs[t] = CurrentFs;
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
                if (ActiveSetOfF[i].Intervals.Last().To > p && valuez < bbestValue && (h - Z.StartIndex) >= UC.minUpTime)
                {
                    INDEX = i;
                    bbestValue = valuez;
                }
            }

            return INDEX;
        }

        private bool[] FlagThoseWithTauLowerThanMinimumUpTime(int t, List<F> currentF)
        {
            bool[] flagged = new bool[currentF.Count];
            for (int i = 0; i < currentF.Count; i++)
            {
                if ((t - currentF[i].StartIndex) >= UC.minUpTime)
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
    }
}
