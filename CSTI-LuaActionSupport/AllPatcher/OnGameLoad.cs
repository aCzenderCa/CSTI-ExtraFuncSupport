using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using CSTI_LuaActionSupport.LuaCodeHelper;
using gfoidl.Base64;
using HarmonyLib;
using UnityEngine;
using static CSTI_LuaActionSupport.AllPatcher.SavePatcher;
using static CSTI_LuaActionSupport.AllPatcher.SavePatcher.LoadEnv;

namespace CSTI_LuaActionSupport.AllPatcher;

public static class OnGameLoad
{
    private static byte[] Decode(string data)
    {
        return Base64.Default.Decode(data.AsSpan());
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GameSaveData), nameof(GameSaveData.CreateDicts))]
    public static void LoadLuaLongTimeData(GameSaveData __instance)
    {
        if (__instance.AllEndgameLogs.Count > 0 && __instance.AllEndgameLogs[0].CategoryID == LuaLongTimeSaveId)
        {
            GSlotSaveData[GameLoad.Instance.CurrentGameDataIndex] = new Dictionary<string, DataNode>();
            var logText = __instance.AllEndgameLogs[0].LogText;
            var data = Base64.Default.Decode(logText.AsSpan());
            var memoryStream = new MemoryStream(data);
            var saveFileReader = new BinaryReader(memoryStream);

            try
            {
                using (BeginLoadEnv(saveFileReader, out _, 0))
                {
                    var gCount = saveFileReader.ReadInt32();
                    for (var i = 0; i < gCount; i++)
                    {
                        var key = saveFileReader.ReadString();
                        GSlotSaveData[GameLoad.Instance.CurrentGameDataIndex][key] = DataNode.Load(saveFileReader);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.Awake))]
    public static void DoOnGameLoad(GameManager __instance)
    {
        var initRuntime = InitRuntime(__instance);
        LuaFilesOnGameLoad.Do(pat =>
        {
            try
            {
                initRuntime.DoString(File.ReadAllText(pat), pat);
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        });
        if (AllLuaFiles.TryGetValue(LuaOnGameLoad, out var luaFiles))
        {
            foreach (var (filePat, lua) in luaFiles)
            {
                try
                {
                    initRuntime.DoString(lua, filePat);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }

        if (CurrentGSlotSaveData().TryGetValue(StatCache, out var statCache))
        {
            foreach (var (key, value) in statCache.table!)
            {
                if (UniqueIDScriptable.GetFromID<GameStat>(key) is not { } stat) continue;
                foreach (var (field, fVal) in value.table!)
                {
                    switch (field)
                    {
                        case nameof(GameStat.MinMaxValue):
                            stat.MinMaxValue = fVal.vector2;
                            break;
                        case nameof(GameStat.MinMaxRate):
                            stat.MinMaxRate = fVal.vector2;
                            break;
                    }
                }
            }
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GameLoad), nameof(GameLoad.SaveGame))]
    public static void DoOnGameSave()
    {
        LuaFilesOnGameSave.Do(pat =>
        {
            try
            {
                LuaRuntime.DoString(File.ReadAllText(pat));
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        });
        if (AllLuaFiles.TryGetValue(LuaOnGameSave, out var luaFiles))
        {
            foreach (var (filePat, lua) in luaFiles)
            {
                try
                {
                    LuaRuntime.DoString(lua, filePat);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(UniqueIDScriptable), nameof(UniqueIDScriptable.ClearDict))]
    public static void DoOnAfterModLoader()
    {
        foreach (var directory in Directory.EnumerateDirectories(Paths.PluginPath))
        {
            if (!File.Exists(Path.Combine(directory, ModInfo))) continue;
            if (Directory.Exists(Path.Combine(directory, LuaInit)))
            {
                foreach (var luaInitFile in Directory.EnumerateFiles(Path.Combine(directory, LuaInit), "*.lua"))
                {
                    try
                    {
                        LuaRuntime.DoString(File.ReadAllText(luaInitFile), luaInitFile);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                    }
                }
            }

            if (Directory.Exists(Path.Combine(directory, LuaOnGameLoad)))
            {
                LuaFilesOnGameLoad.AddRange(Directory.EnumerateFiles(Path.Combine(directory, LuaOnGameLoad),
                    "*.lua"));
            }

            if (Directory.Exists(Path.Combine(directory, LuaOnGameSave)))
            {
                LuaFilesOnGameSave.AddRange(Directory.EnumerateFiles(Path.Combine(directory, LuaOnGameSave),
                    "*.lua"));
            }
        }

        if (AllLuaFiles.TryGetValue(LuaInit, out var luaFiles))
        {
            foreach (var (filePat, lua) in luaFiles)
            {
                try
                {
                    LuaRuntime.DoString(lua, filePat);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }
    }
}