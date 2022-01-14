using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADMMUC._1UC
{


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




        public double MinimumAtInterval()
        {
            if (-B <= From * (2 * C))
            {
                return From;
            }
            else if (-B  >= To * (2 * C))
            {
                return To;
            }
            else
            {
                return (-B / (2 * C));
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

        public (double, double) Intersects(QuadraticInterval otherInterval)
        {
            double a = otherInterval.A - A;
            double b = otherInterval.B - B;
            double c = otherInterval.C - C;
            if (c == 0)
            {
                if (b == 0)
                {
                    return (double.MinValue, double.MinValue);
                }
                else
                {
                    var test = -a / b;
                    return (test, test);
                }

            }
            else
            {
                double discriminant = (b * b - a * c * 4);
                if (discriminant < 0)
                {
                    return (double.MinValue, double.MinValue);
                }
                else
                {
                    double left = -b / (2 * c);
                    double right = (Math.Sqrt(discriminant) / (2 * c));
                    return (left - right, left + right);
                }
            }
        }

        public double FirstIntersect(QuadraticInterval otherInterval)
        {
            double a = otherInterval.A - A;
            double b = otherInterval.B - B;
            double c = otherInterval.C - C;
            // Console.WriteLine("{0} {1} {2}", a, b, c);
            if (c == 0 && b != 0)
            {
                return -a / b;
            }
            else if (c == 0)
            {
                return double.MinValue;
            }
            double discriminant = (b * b - a * c * 4);
            if (discriminant < 0)
            {
                return double.MinValue;
            }
            double solution = ((-b - (Math.Sqrt(discriminant))) / (2 * c));
            return solution;
        }

        public double SecondIntersect(QuadraticInterval otherInterval)
        {
            double a = otherInterval.A - A;
            double b = otherInterval.B - B;
            double c = otherInterval.C - C;
            //Console.WriteLine("{0} {1} {2}", a, b, c);
            //Console.ReadLine();
            if (c == 0 && b != 0)
            {
                return -a / b;
            }
            else if (c == 0)
            {
                return double.MinValue;
            }
            double discriminant = (b * b - a * c * 4);
            if (discriminant < 0)
            {
                return double.MinValue;
            }
            double solution = ((-b + (Math.Sqrt(discriminant))) / (2 * c));
            return solution;
        }

        public (bool, double) IntersectInIntervalCombined(double iFrom, double iTo, QuadraticInterval interval, double p)
        {
            var (firstIntersect, secondIntersect) = Intersects(interval);

            if (iFrom <= firstIntersect && firstIntersect <= iTo && p < firstIntersect)
            {
                return (true, firstIntersect);
            }
            if (iFrom <= secondIntersect && secondIntersect <= iTo && p < secondIntersect)
            {
                return (true, secondIntersect);
            }
            return (false, 0);
        }
        public bool IntersectInIntervalBool(double iFrom, double iTo, QuadraticInterval interval, double p)
        {
            var interstion = Intersects(interval);
            double firstIntersect = interstion.Item1;

            if (iFrom <= firstIntersect && firstIntersect <= iTo && p < firstIntersect)
            {
                return true;
            }
            double secondIntersect = interstion.Item2;
            if (iFrom <= secondIntersect && secondIntersect <= iTo && p < secondIntersect)
            {
                return true;
            }
            return false;
        }
        public double IntersectInIntervalDouble(double iFrom, double iTo, QuadraticInterval interval, double p)
        {
            var interstion = Intersects(interval);
            double firstIntersect = interstion.Item1;
            double secondIntersect = interstion.Item2;
            if (iFrom <= firstIntersect && firstIntersect <= iTo && p < firstIntersect) return firstIntersect;
            if (iFrom <= secondIntersect && secondIntersect <= iTo && p < secondIntersect) return secondIntersect;
            throw new Exception("????????");
        }
    }
}
