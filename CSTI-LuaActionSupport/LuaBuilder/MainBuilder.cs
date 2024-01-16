using System.Collections.Generic;
using CSTI_LuaActionSupport.LuaBuilder.CardDataModels;
using CSTI_LuaActionSupport.LuaCodeHelper;
using NLua;

namespace CSTI_LuaActionSupport.LuaBuilder;

public static class MainBuilder
{
    public static readonly Dictionary<string, CardData> Name2Card = new();
    public static string CurModId = "";

    [LuaFunc]
    public static void BeginMod(string id)
    {
        CurModId = id;
    }

    [LuaFunc]
    public static CardDataPack BuildBase(string name, string desc = "暂无", float weight = 100, string? img = null)
    {
        name = CurModId + "_" + name;
        return BaseItemModel.BuildBase(name, desc, weight, img ?? name + "_CardImage");
    }

    [LuaFunc]
    public static CardDataPack BuildLocation(string name, string desc = "暂无", float weight = 100, string? img = null)
    {
        name = CurModId + "_" + name;
        return BaseItemModel.BuildBaseLocation(name, desc, weight, img ?? name + "_CardImage");
    }

    [LuaFunc]
    public static CardDataPack BuildSimplePath(string name, string where2Go, LuaFunction lua, string desc = "暂无",
        float weight = 0, string? img = null)
    {
        name = CurModId + "_" + name;
        return BaseItemModel.BuildSimplePath(name, desc, weight, img ?? name + "_CardImage", where2Go, lua);
    }

    [LuaFunc]
    public static CardDataPack BuildMultiPath(string name, LuaTable where2Go, LuaTable goLua, string desc = "暂无",
        float weight = 0, string? img = null)
    {
        name = CurModId + "_" + name;
        return BaseItemModel.BuildMultiPath(name, desc, weight, img ?? name + "_CardImage", where2Go, goLua);
    }
}