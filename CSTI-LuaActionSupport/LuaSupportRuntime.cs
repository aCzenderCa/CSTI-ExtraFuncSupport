using System.Collections.Generic;
using System.IO;
using BepInEx;
using CSTI_LuaActionSupport.AllPatcher;
using HarmonyLib;
using static CSTI_LuaActionSupport.AllPatcher.CardActionPatcher;
using static CSTI_LuaActionSupport.AllPatcher.SavePatcher;
using static CSTI_LuaActionSupport.AllPatcher.SavePatcher.LoadEnv;

namespace CSTI_LuaActionSupport;

[BepInPlugin("zender.LuaActionSupport.LuaSupportRuntime", "LuaActionSupport", "1.0.0.34")]
public class LuaSupportRuntime : BaseUnityPlugin
{
    public static readonly Harmony HarmonyInstance = new("zender.LuaActionSupport.LuaSupportRuntime");
    public static readonly string ModInfo = "ModInfo.json";
    public static readonly string LuaInit = "LuaInit";
    public static readonly string LuaOnGameLoad = "LuaOnGameLoad";
    public static readonly string LuaOnGameSave = "LuaOnGameSave";
    public static readonly List<string> LuaFilesOnGameLoad = new();
    public static readonly List<string> LuaFilesOnGameSave = new();

    static LuaSupportRuntime()
    {
        HarmonyInstance.PatchAll(typeof(CardActionPatcher));
        HarmonyInstance.PatchAll(typeof(OnGameLoad));
        HarmonyInstance.PatchAll(typeof(SavePatcher));
        HarmonyInstance.PatchAll(typeof(ObjModifyPatcher));
        HarmonyInstance.PatchAll(typeof(LuaRegister));
    }

    private static void LoadLuaSave()
    {
        if (!File.Exists(SavePath)) return;
        using var saveFile = new BufferedStream(File.OpenRead(SavePath));
        using var saveFileReader = new BinaryReader(saveFile);
        using (BeginLoadEnv(saveFileReader, out _, 0))
        {
            var gCount = saveFileReader.ReadInt32();
            for (var i = 0; i < gCount; i++)
            {
                var key = saveFileReader.ReadString();
                GSaveData[key] = DataNode.Load(saveFileReader);
            }
        }
    }

    private void Awake()
    {
        LoadLuaSave();
    }
}