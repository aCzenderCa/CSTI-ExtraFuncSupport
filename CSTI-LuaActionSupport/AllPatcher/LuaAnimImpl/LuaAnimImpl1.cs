using NLua;
using UnityEngine;

namespace CSTI_LuaActionSupport.AllPatcher.LuaAnimImpl;

public static class LuaAnimImpl1
{
    public static void MovePath(Object o, LuaTable args)
    {
        if (o is not Transform transform) return;
        
    }
}