﻿using System;
using System.Linq;
using HarmonyLib;
using NLua;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public static class LuaRegistrable
{
    public static void Register<T>(this Lua lua, string? basename = null)
        where T : struct, Enum
    {
        var type = typeof(T);
        basename ??= type.Name;
        if (lua.GetObjectFromPath(basename) != null)
        {
            return;
        }

        lua.NewTable(basename);
        var luaTable = lua.GetTable(basename);
        foreach (var value in Enum.GetValues(type))
        {
            luaTable[Enum.GetName(type, value)] = value;
        }
    }

    public static void Register(this Lua luaRuntime, Type typeInfo, string? basePath = null)
    {
        if (basePath != null && luaRuntime.GetTable(basePath) == null)
        {
            luaRuntime.NewTable(basePath);
        }

        if (typeInfo.CustomAttributes.FirstOrDefault(data => data.AttributeType == typeof(LuaFuncTodo)) is not null)
        {
            return;
        }

        var declaredMethods = AccessTools.GetDeclaredMethods(typeInfo);

        foreach (var methodInfo in declaredMethods)
        {
            if (!methodInfo.IsStatic) continue;
            var customAttributeData =
                methodInfo.CustomAttributes.FirstOrDefault(data => data.AttributeType == typeof(LuaFuncAttribute));
            if (customAttributeData == null) continue;
            luaRuntime.RegisterFunction(
                (basePath == null ? "" : basePath + '.') +
                (customAttributeData.NamedArguments?.FirstOrDefault(namedArgument =>
                        namedArgument.MemberName == nameof(LuaFuncAttribute.FuncName)).TypedValue
                    .Value is not null and var argument
                    ? argument
                    : methodInfo.Name), null,
                methodInfo);
        }
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class LuaFuncTodo : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public class LuaFuncAttribute : Attribute
{
    public string? FuncName { get; set; }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Method)]
public class TestCodeAttribute : Attribute
{
    public TestCodeAttribute(string code)
    {
    }
}