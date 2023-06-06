using System;
using System.Collections;

namespace ChatTreeLoader.Util
{
    public static class EnumHelper
    {
        public static IEnumerator OnEnd(this IEnumerator iEnumerator, Action action)
        {
            while (iEnumerator.MoveNext())
            {
                yield return iEnumerator.Current;
            }

            action();
        }
    }
}