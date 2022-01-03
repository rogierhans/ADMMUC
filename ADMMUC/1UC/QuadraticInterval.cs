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
        //public double MinimumAtInterval()
        //{
        //    double minimum = (-B / (2 * C));
        //    if ((C == 0 && B > 0) || minimum < From)
        //    {
        //        return From;
        //    }
        //    else if (C == 0 || minimum > To)
        //    {
        //        return To;
        //    }
        //    else
        //    {
        //        return minimum;
        //    }
        //}
        //public double idiomaticCSharp()
        //{
        //    double minimum = (-B / (2 * C));
        //    return ((C == 0 && B > 0) || minimum < From) ? From : (C == 0 || minimum > To) ? To : minimum;
        //}
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

        public Tuple<double, double> Intersects(QuadraticInterval otherInterval)
        {
            double a = otherInterval.A - A;
            double b = otherInterval.B - B;
            double c = otherInterval.C - C;
            if (c == 0)
            {
                if (b == 0)
                {
                    return new Tuple<double, double>(double.MinValue, double.MinValue);
                }
                else
                {
                    var test = -a / b;
                    return new Tuple<double, double>(test, test);
                }

            }
            else
            {
                double discriminant = (b * b - a * c * 4);
                if (discriminant < 0)
                {
                    return new Tuple<double, double>(double.MinValue, double.MinValue);
                }
                else
                {
                    double left = -b / (2 * c);
                    double right = (Math.Sqrt(discriminant) / (2 * c));
                    return new Tuple<double, double>(left -right, left+right);
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

        public  Tuple<bool,double> IntersectInIntervalCombined(double iFrom, double iTo, QuadraticInterval interval, double p)
        {
            var interstion = Intersects(interval);
            double firstIntersect = interstion.Item1;

            if (iFrom <= firstIntersect && firstIntersect <= iTo && p < firstIntersect)
            {
                return new Tuple<bool, double>(true, firstIntersect);
            }
            double secondIntersect = interstion.Item2;
            if (iFrom <= secondIntersect && secondIntersect <= iTo && p < secondIntersect)
            {
                return new Tuple<bool, double>(true, secondIntersect);
            }
            return new Tuple<bool,double> (false,0);
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
