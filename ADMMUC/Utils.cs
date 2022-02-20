using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADMMUC
{
    public static class Utils
    {

        public static List<T> Flat<T>(this T[,] list2d)
        {
            List<T> list = new List<T>();
            for (int i = 0; i < list2d.GetLength(0); i++)
            {
                for (int j = 0; j < list2d.GetLength(1); j++)
                {
                    list.Add(list2d[i, j]); 
                }
            }
            return list;
        }
    }
}
