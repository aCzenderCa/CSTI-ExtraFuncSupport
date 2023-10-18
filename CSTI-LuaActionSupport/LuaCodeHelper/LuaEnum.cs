using System.Collections;
using System.Collections.Generic;
using NLua;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public class LuaEnum
{
    public static readonly LuaEnum Enum = new();

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
            if (call.Length > 0 && call[0] is double d)
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