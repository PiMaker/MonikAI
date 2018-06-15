using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace MonikAI
{
    public static class ExtensionMethods
    {
        private static readonly Random sampler = new Random();

        public static T Sample<T>(this IList<T> list)
        {
            return !list.Any() ? default(T) : list[ExtensionMethods.sampler.Next(0, list.Count)];
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = ExtensionMethods.sampler.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}