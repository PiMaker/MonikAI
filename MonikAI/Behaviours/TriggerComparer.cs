using System.Collections.Generic;

namespace MonikAI.Behaviours
{
    /// <summary>
    /// Used for comparing response table keys since response table keys are currently string arrays.
    /// </summary>
    class TriggerComparer : IEqualityComparer<string[]>
    {
        public bool Equals(string[] x, string[] y)
        {
            if ((x == null && y != null) || (x != null && y == null))
            {
                return false;
            }

            if (x.Length != y.Length)
            {
                return false;
            }

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(string[] obj)
        {
            return base.GetHashCode();
        }
    }
}