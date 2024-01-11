using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using NLua;
using UnityEngine;

namespace CSTI_LuaActionSupport.Helper;

public static class TryModifyPack
{
    private delegate T _TryNum<out T>(object? o) where T : struct;

    private static readonly Dictionary<Type, Dictionary<Type, MulticastDelegate>> tryNumCache = new();

    private static MulticastDelegate GenTryNum(Type self, Type tIn)
    {
        var dynamicMethod = new DynamicMethod($"TryModifyPack_TryNum_{self.Name}_{tIn.Name}", self,
            new[] {typeof(object)}, typeof(TryModifyPack));
        var ilGenerator = dynamicMethod.GetILGenerator();
        ilGenerator.Emit(OpCodes.Ldarg_0);
        ilGenerator.Emit(OpCodes.Unbox_Any, tIn);
        ilGenerator.Emit(FloatLike[self]);
        ilGenerator.Emit(OpCodes.Ret);
        return (MulticastDelegate) dynamicMethod.CreateDelegate(
            typeof(_TryNum<int>).GetGenericTypeDefinition().MakeGenericType(self));
    }

    public static T? TryNum<T>(this object? o) where T : struct
    {
        if (o == null) return default;
        var type = o.GetType();
        if (FloatLike.ContainsKey(typeof(T)) && FloatLike.ContainsKey(type))
        {
            if (!tryNumCache.ContainsKey(typeof(T)))
                tryNumCache[typeof(T)] = new Dictionary<Type, MulticastDelegate>();
            if (!tryNumCache[typeof(T)].TryGetValue(type, out var genTryNum))
            {
                genTryNum = GenTryNum(typeof(T), type);
                tryNumCache[typeof(T)][type] = genTryNum;
            }

            return ((_TryNum<T>) genTryNum)(o);
        }

        return default;
    }

    private static readonly Dictionary<Type, OpCode> FloatLike = new()
    {
        {typeof(double), OpCodes.Conv_R8}, {typeof(float), OpCodes.Conv_R4}, {typeof(long), OpCodes.Conv_I8},
        {typeof(ulong), OpCodes.Conv_U8}, {typeof(int), OpCodes.Conv_I4}, {typeof(uint), OpCodes.Conv_U4},
        {typeof(nint), OpCodes.Conv_I}, {typeof(nuint), OpCodes.Conv_U}
    };

    public static void TryModBy(this ref bool self, object? o)
    {
        if (o is bool b)
        {
            self = b;
        }
    }

    public static void TryModBy(this Gradient self, object? o)
    {
        if (o is LuaTable table)
        {
            bool GradientMode = UnityEngine.GradientMode.Blend == 0;
            GradientMode.TryModBy(table[nameof(GradientMode)]);
            self.mode = GradientMode ? UnityEngine.GradientMode.Blend : UnityEngine.GradientMode.Fixed;
            var alphaKeys = new List<GradientAlphaKey>();
            var colorKeys = new List<GradientColorKey>();
            if (table[nameof(self.alphaKeys)] is LuaTable table_alphaKeys)
            {
                for (int i = 1;; i++)
                {
                    if (table_alphaKeys[i] is not LuaTable table_alphaKey) continue;
                    var alphaKey = new GradientAlphaKey();
                    alphaKey.alpha.TryModBy(table_alphaKey[nameof(alphaKey.alpha)]);
                    alphaKey.time.TryModBy(table_alphaKey[nameof(alphaKey.time)]);
                    alphaKeys.Add(alphaKey);
                }
            }

            if (table[nameof(self.colorKeys)] is LuaTable table_colorKeys)
            {
                for (int i = 1;; i++)
                {
                    if (table_colorKeys[i] is not LuaTable table_colorKey) continue;
                    var colorKey = new GradientColorKey();
                    colorKey.time.TryModBy(table_colorKey[nameof(colorKey.time)]);
                    if (table_colorKey[nameof(colorKey.color)] is LuaTable table_colorKey_color)
                    {
                        var color = new Color();
                        color.r.TryModBy(table_colorKey_color[nameof(color.r)]);
                        color.g.TryModBy(table_colorKey_color[nameof(color.g)]);
                        color.b.TryModBy(table_colorKey_color[nameof(color.b)]);
                        colorKey.color = color;
                    }
                    else
                    {
                        colorKey.color = Color.white;
                    }

                    colorKeys.Add(colorKey);
                }
            }

            self.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
        }
    }

    public static void TryModBy(this ref float self, object? o)
    {
        if (o == null) return;
        var type = o.GetType();
        if (FloatLike.ContainsKey(type) && o.TryNum<float>() is { } f)
        {
            self = f;
        }
    }

    public static void TryModBy(this ref int self, object? o)
    {
        if (o == null) return;
        var type = o.GetType();
        if (FloatLike.ContainsKey(type) && o.TryNum<int>() is { } i)
        {
            self = i;
        }
    }
}