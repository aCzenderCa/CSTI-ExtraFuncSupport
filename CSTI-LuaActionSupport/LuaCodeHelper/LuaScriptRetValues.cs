using System.Collections.Generic;
using NLua;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public class LuaScriptRetValues
{
    private readonly Dictionary<string, object?> Values = new();

    public bool CheckKey<TVal>(string key, out TVal? value)
    {
        if (Values.TryGetValue(key,out var oValue)&&oValue is TVal tValue)
        {
            value = tValue;
            return true;
        }

        value = default;
        return false;
    }

    public object? this[string key]
    {
        get
        {
            _ = Values.TryGetValue(key, out var value);
            return value;
        }
        set => Values[key] = value;
    }
}