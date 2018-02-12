using System;
using System.Collections.Generic;

namespace MonikAI
{
    public static class ExtensionMethods
    {
        private static readonly Random sampler = new Random();

        public static T Sample<T>(this IList<T> list)
        {
            return list[ExtensionMethods.sampler.Next(0, list.Count)];
        }
    }
}