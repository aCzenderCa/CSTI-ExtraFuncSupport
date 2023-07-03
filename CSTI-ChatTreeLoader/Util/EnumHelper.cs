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
        public static IEnumerator OnEnd(this IEnumerator iEnumerator, IEnumerator enumerator)
        {
            while (iEnumerator.MoveNext())
            {
                yield return iEnumerator.Current;
            }

            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }
}