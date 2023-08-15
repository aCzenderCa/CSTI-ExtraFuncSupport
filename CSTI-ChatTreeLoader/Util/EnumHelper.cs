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

        public static IEnumerator OnStart(this IEnumerator e1, Action action)
        {
            action();
            while (e1.MoveNext())
            {
                yield return e1.Current;
            }
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

        public static IEnumerator OnStart(this IEnumerator e1, IEnumerator e2)
        {
            while (e2.MoveNext())
            {
                yield return e2.Current;
            }

            while (e1.MoveNext())
            {
                yield return e1.Current;
            }
        }
    }
}