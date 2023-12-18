using System.Collections;
using System.Collections.Generic;
using CSTI_LuaActionSupport.Helper;
using NLua;

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

    public static void SafeDAdd<TKey1, TKey2, TVal>(this Dictionary<TKey1, Dictionary<TKey2, List<TVal>>> dictionary,
        TKey1 key1, TKey2 key2, TVal val)
    {
        if (dictionary.TryGetValue(key1, out var dictionary2))
        {
            if (dictionary2.TryGetValue(key2, out var list))
            {
                list.Add(val);
            }
            else
            {
                dictionary2[key2] = [val];
            }
        }
        else
        {
            dictionary[key1] = new Dictionary<TKey2, List<TVal>> {{key2, [val]}};
        }
    }

    public static void SafeDAdd<TKey1, TKey2, TKey3, TVal>(
        this Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, List<TVal>>>> dictionary,
        TKey1 key1, TKey2 key2, TKey3 key3, TVal val)
    {
        if (dictionary.TryGetValue(key1, out var dictionary2))
        {
            if (dictionary2.TryGetValue(key2, out var dictionary3))
            {
                if (dictionary3.TryGetValue(key3,out var list))
                {
                    list.Add(val);
                }
                else
                {
                    dictionary3[key3] = [val];
                }
            }
            else
            {
                dictionary2[key2] = new Dictionary<TKey3, List<TVal>> {{key3, [val]}};
            }
        }
        else
        {
            dictionary[key1] = new Dictionary<TKey2, Dictionary<TKey3, List<TVal>>>
                {{key2, new Dictionary<TKey3, List<TVal>> {{key3, [val]}}}};
        }
    }

    public static TVal? SafeDGet<TKey1, TKey2, TKey3, TVal>(this Dictionary<TKey1, Dictionary<TKey2, Dictionary<TKey3, TVal>>> dictionary,
        TKey1 key1, TKey2 key2, TKey3 key3)
    {
        if (dictionary.TryGetValue(key1, out var dictionary2) && dictionary2.TryGetValue(key2, out var dictionary3)
            && dictionary3.TryGetValue(key3,out var val))
        {
            return val;
        }

        return default;
    }
}