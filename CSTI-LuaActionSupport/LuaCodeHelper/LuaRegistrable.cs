using System;
using System.Linq;
using HarmonyLib;
using NLua;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public static class LuaRegistrable
{
    public static void Register(this Type typeInfo, Lua LuaRuntime, string? basePath = null)
    {
        var declaredMethods = AccessTools.GetDeclaredMethods(typeInfo);
        if (basePath != null && LuaRuntime.GetTable(basePath) == null)
        {
            LuaRuntime.NewTable(basePath);
        }

        foreach (var methodInfo in declaredMethods)
        {
            if (!methodInfo.IsStatic) continue;
            var customAttributeData =
                methodInfo.CustomAttributes.FirstOrDefault(data => data.AttributeType == typeof(LuaFuncAttribute));
            if (customAttributeData == null) continue;
            LuaRuntime.RegisterFunction(
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

[AttributeUsage(AttributeTargets.Method)]
public class LuaFuncAttribute : Attribute
{
    public string? FuncName { get; set; }
}

[AttributeUsage(AttributeTargets.Method)]
public class TestCodeAttribute : Attribute
{
    private string Code;

    public TestCodeAttribute(string code)
    {
        Code = code;
    }
}