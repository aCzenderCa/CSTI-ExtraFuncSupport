using System;
using System.Collections;
using System.Collections.Generic;
using CSTI_LuaActionSupport.Helper;
using static CSTI_LuaActionSupport.AllPatcher.CardActionPatcher;
using NLua;
using UnityEngine;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public class LuaEnum
{
    public class MyListEnumerator : IEnumerator
    {
        private readonly IList _list;

        public MyListEnumerator(IList list)
        {
            _list = list;
            Index = -1;
        }

        public bool MoveNext()
        {
            Index += 1;
            return Index >= 0 && Index < _list.Count;
        }

        public void Reset()
        {
            Index = -1;
        }

        public int Index { get; private set; }

        public object? Current
        {
            get
            {
                if (Index >= 0 && Index < _list.Count)
                {
                    return _list[Index];
                }

                return null;
            }
        }
    }

    public delegate TStat LuaIter<in TConst, TStat, TItem>(TConst constStat, TStat stat, out TItem item);

    public static readonly LuaEnum Enum = new();

    private static LuaFunction? _iter_L;

    private static LuaFunction iter_L
    {
        get
        {
            return _iter_L ??= LuaRuntime.RegisterFunction("__temp",
                ((LuaIter<MyListEnumerator, object?, object?>) _iter).Method);


            object? _iter(MyListEnumerator l, object? index, out object? item)
            {
                if (l.MoveNext())
                {
                    item = l.Current;
                    return l.Index;
                }

                item = null;
                return null;
            }
        }
    }

    private static LuaFunction? _iter_D;

    private static LuaFunction iter_D
    {
        get
        {
            return _iter_D ??= LuaRuntime.RegisterFunction("__temp",
                ((LuaIter<IDictionaryEnumerator, object?, object?>) _iter).Method);


            object? _iter(IDictionaryEnumerator dictionaryEnumerator, object? key, out object? value)
            {
                if (dictionaryEnumerator.MoveNext())
                {
                    value = dictionaryEnumerator.Value;
                    return dictionaryEnumerator.Key;
                }

                value = null;
                return null;
            }
        }
    }

    // language=Lua
    [TestCode("""
              for i,v in Enum:Pairs(GetGameCards("6b87970979841684bb6d6a7471430798")) do
                debug.debug = v.Id
              end
              """)]
    public void Pairs(IList list, out LuaFunction func, out MyListEnumerator pack,
        out object? stat)
    {
        func = iter_L;
        pack = new MyListEnumerator(list);
        stat = null;
    }

    public void Pairs(IDictionary dictionary, out LuaFunction iter,
        out IDictionaryEnumerator dict, out object? stat)
    {
        iter = iter_D;
        dict = dictionary.GetEnumerator();
        stat = null;
    }

    public void Foreach(IEnumerable enumerable, LuaFunction func)
    {
        foreach (var o in enumerable)
        {
            func.Call(o);
        }
    }

    public double Sum(IEnumerable enumerable, double init, LuaFunction func)
    {
        foreach (var o in enumerable)
        {
            var call = func.Call(init, o);
            if (call.Length > 0 && call[0].TryNum<double>() is { } d)
            {
                init = d;
            }
        }

        return init;
    }

    public IList Map(IEnumerable enumerable, LuaFunction func)
    {
        var objects = new List<object>();
        foreach (var o in enumerable)
        {
            var call = func.Call(o);
            if (call.Length > 0)
            {
                objects.Add(call[0]);
            }
        }

        return objects;
    }

    private LuaEnum()
    {
    }
}

public static class TableHelper
{
    public static LuaTable TempTable(this Lua lua)
    {
        lua.NewTable("__temp");
        return lua.GetTable("__temp");
    }

    // ReSharper disable once MethodOverloadWithOptionalParameter
    public static TVal? SafeGet<TKey, TVal>(this IDictionary<TKey, TVal> dictionary, TKey key, object? __ = null)
        where TVal : class
    {
        return dictionary.TryGetValue(key, out var value) ? value : null;
    }

    public static TVal? SafeGet<TKey, TVal>(this IDictionary<TKey, TVal> dictionary, TKey key)
        where TVal : struct
    {
        return dictionary.TryGetValue(key, out var value) ? value : null;
    }
}