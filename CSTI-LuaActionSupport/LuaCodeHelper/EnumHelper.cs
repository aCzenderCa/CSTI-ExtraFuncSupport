﻿using System.Collections;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public static class EnumHelper
{
    public static IEnumerator Prepend(this IEnumerator enumerator, IEnumerator other)
    {
        while (other.MoveNext())
        {
            yield return other.Current;
        }

        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }
    }

    public static IEnumerator Concat(this IEnumerator enumerator, IEnumerator other)
    {
        while (enumerator.MoveNext())
        {
            yield return enumerator.Current;
        }

        while (other.MoveNext())
        {
            yield return other.Current;
        }
    }
}