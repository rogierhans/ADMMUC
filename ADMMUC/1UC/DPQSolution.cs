using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADMMUC._1UC
{
    public class DPQSolution
    {
        public int T;
        public int Tau;
        public bool On;
        public double P;
        public F F;
        public double Value;

        public DPQSolution(int t, int tau, bool on, double p, F f, double value)
        {
            T = t;
            Tau = tau;
            On = on;
            P = p;
            F = f;
            Value = value;
        }
        public DPQSolution GetBestPrev(GeneratorQuadratic UC, List<F>[] Fs, double[,] Off)
        {
            if (T == 0)
                throw new Exception("?????");
            if (!On)
            {
                return OffPrevStep(UC, Fs, Off);
            }
            else
                return OnPrevStep(UC, Fs, Off);
        }
        public DPQSolution OffPrevStep(GeneratorQuadratic UC, List<F>[] Fs, double[,] Off)
        {
            if (Tau == 0)
            {
                double bestP = double.MaxValue;
                double bestValue = double.MaxValue;
                F bestF = null;
                //Console.WriteLine("T:{0}", T);
                foreach (var F in Fs[T - 1].Where(F => ((T - 1) - F.StartIndex) >= UC.minUpTime - 1 || F.StartIndex == 0))
                {
                    foreach (var interval in F.Intervals.Where(interval => interval.NonEmptyInterval(UC.pMin, UC.SD)))
                    {
                        var MinimumPointAndValue = interval.MinimumPointAndValue(UC.pMin, UC.SD);
                        var minimum = MinimumPointAndValue.Item1;
                        var valueAtMinimum = MinimumPointAndValue.Item2;
                        if (bestValue > valueAtMinimum)
                        {
                            bestP = minimum;
                            bestValue = valueAtMinimum;
                            bestF = F;
                        }

                    }
                }
                // Console.WriteLine("deze:{0} {1} {2}", bestF.StartIndex, bestValue, bestP);
                return new DPQSolution(T - 1, (T - 1) - bestF.StartIndex, true, bestP, bestF, bestValue);
            }
            else if (0 < Tau && Tau < UC.minDownTime - 1)
            {
                return new DPQSolution(T - 1, Tau - 1, false, 0, null, Value);
            }
            else if (Tau == UC.minDownTime - 1)
            {
                if (Off[T - 1, Tau] < Off[T - 1, Tau - 1])
                {
                    return new DPQSolution(T - 1, Tau, false, 0, null, Value);
                }
                else
                {
                    return new DPQSolution(T - 1, Tau - 1, false, 0, null, Value);
                }
            }
            else
            {
                throw new Exception("HMMM");
            }

        }
        public DPQSolution OnPrevStep(GeneratorQuadratic UC, List<F>[] Fs, double[,] Off)
        {
            if (Tau == 0)
            {
                return new DPQSolution(T - 1, UC.minDownTime - 1, false, 0, null, Value);
            }
            else if (0 < Tau)
            {
                double bestP = double.MaxValue;
                double bestValue = double.MaxValue;
                F bestF = Fs[T - 1].Where(F => ((T - 1) - F.StartIndex) == Tau - 1).First();

                foreach (var interval in bestF.Intervals.Where(interval => interval.NonEmptyInterval(P - UC.RampUp, P + UC.RampDown)))
                {
                    var MinimumPointAndValue = interval.MinimumPointAndValue(P - UC.RampUp, P + UC.RampDown);
                    var minimum = MinimumPointAndValue.Item1;
                    var valueAtMinimum = MinimumPointAndValue.Item2;
                    if (bestValue > valueAtMinimum)
                    {
                        bestP = minimum;
                        bestValue = valueAtMinimum;
                    }

                }
                return new DPQSolution(T - 1, (T - 1) - bestF.StartIndex, true, bestP, bestF, bestValue);

            }
            else
            {
                throw new Exception("HMMM");
            }
        }
        public void Short()
        {
            if (T == 0) Console.WriteLine("");
            Console.Write("({0} {1} {2} {3})", On ? "On" : "Off", T, Tau, P);
        }
        public void Print()
        {
            Console.WriteLine("{0} {1} {2} {3}", On ? "On" : "Off", T, Tau, P);
        }
    }
}
