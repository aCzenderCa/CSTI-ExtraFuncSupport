using System;
using System.Collections;
using System.Collections.Generic;
using NLua;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public static class EnumHelper
{
    public static List<TVal>? ToList<TVal>(this LuaTable? table, Func<object, TVal>? func = null)
    {
        if (table == null) return null;
        List<TVal> list = [];
        for (var i = 1;; i++)
        {
            var o = table[i];
            if (o == null)
            {
                return list;
            }

            if (func == null)
            {
                list.Add((TVal) o);
            }
            else
            {
                list.Add(func(o));
            }
        }
    }

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