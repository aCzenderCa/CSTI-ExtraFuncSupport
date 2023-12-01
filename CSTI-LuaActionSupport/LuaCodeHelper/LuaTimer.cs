using System.Collections.Generic;
using BepInEx;
using CSTI_LuaActionSupport.Helper;
using NLua;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public static class LuaTimer
{
    public static readonly List<LuaFunction> FrameFunctions = new();
    public static readonly List<LuaFunction> FixFrameFunctions = new();

    [LuaFunc]
    public static void ProcessCacheEnum()
    {
        GameManager.Instance.ProcessCache();
    }

    [LuaFunc]
    public static void Frame(LuaFunction function)
    {
        FrameFunctions.Add(function);
    }

    [LuaFunc]
    public static void FixFrame(LuaFunction function)
    {
        FixFrameFunctions.Add(function);
    }
}

public static class LuaInput
{
    [LuaFunc]
    public static float GetScroll()
    {
        return UnityInput.Current.mouseScrollDelta.y;
    }
    
    [LuaFunc]
    public static bool GetKey(string key)
    {
        return UnityInput.Current.GetKey(key);
    }

    [LuaFunc]
    public static bool GetKeyDown(string key)
    {
        return UnityInput.Current.GetKeyDown(key);
    }

    [LuaFunc]
    public static bool GetKeyUp(string key)
    {
        return UnityInput.Current.GetKeyUp(key);
    }
}