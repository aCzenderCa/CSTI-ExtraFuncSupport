using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using CSTI_LuaActionSupport.AllPatcher;
using CSTI_LuaActionSupport.LuaCodeHelper;
using HarmonyLib;
using UnityEngine;
using static CSTI_LuaActionSupport.AllPatcher.SavePatcher;
using static CSTI_LuaActionSupport.AllPatcher.SavePatcher.LoadEnv;

namespace CSTI_LuaActionSupport;

[BepInPlugin("zender.LuaActionSupport.LuaSupportRuntime", "LuaActionSupport", ModVersion)]
public class LuaSupportRuntime : BaseUnityPlugin
{
    public const string ModVersion = "1.0.3.28";
    public static readonly Harmony HarmonyInstance = new("zender.LuaActionSupport.LuaSupportRuntime");
    public static readonly string ModInfo = "ModInfo.json";
    public static readonly string LuaInit = "LuaInit";
    public static readonly string LuaOnGameLoad = "LuaOnGameLoad";
    public static readonly string LuaOnGameSave = "LuaOnGameSave";
    public static readonly List<string> LuaFilesOnGameLoad = new();
    public static readonly List<string> LuaFilesOnGameSave = new();
    public static LuaSupportRuntime Runtime = null!;
    public static Dictionary<string, Sprite> SpriteDict = null!;
    public static Dictionary<string, Dictionary<string, string>> AllLuaFiles = null!;

    public static void Init(Dictionary<string, Sprite> spriteDict,Dictionary<string, Dictionary<string, string>> allLuaFiles)
    {
        SpriteDict = spriteDict;
        AllLuaFiles = allLuaFiles;
    }

    static LuaSupportRuntime()
    {
        HarmonyInstance.PatchAll(typeof(SafeAttrPatcher));
        HarmonyInstance.PatchAll(typeof(CardActionPatcher));
        HarmonyInstance.PatchAll(typeof(OnGameLoad));
        HarmonyInstance.PatchAll(typeof(SavePatcher));
        HarmonyInstance.PatchAll(typeof(ObjModifyPatcher));
        HarmonyInstance.PatchAll(typeof(LuaRegister));
        HarmonyInstance.PatchAll(typeof(LuaTimer));
        HarmonyInstance.PatchAll(typeof(LuaGraphics));
        LuaSystem.SetupPatch(HarmonyInstance);
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
        Runtime = this;
        LoadLuaSave();
    }

    private void Update()
    {
        var remove = (from function in LuaTimer.FrameFunctions
            let objects = function.Call()
            where objects.Length > 0 && objects[0] is false
            select function).ToList();

        foreach (var function in remove)
        {
            LuaTimer.FrameFunctions.Remove(function);
        }

        remove.Clear();
        foreach (var (function, timer) in LuaTimer.EveryTimeFunctions)
        {
            if (!timer.Step()) continue;
            var objects = function.Call();
            if (objects.Length > 0 && objects[0] is false)
            {
                remove.Add(function);
            }

            timer.CurTime -= timer.Time;
        }

        foreach (var function in remove)
        {
            LuaTimer.EveryTimeFunctions.Remove(function);
        }
    }

    private void FixedUpdate()
    {
        var remove = (from function in LuaTimer.FixFrameFunctions
            let objects = function.Call()
            where objects.Length > 0 && objects[0] is false
            select function).ToList();

        foreach (var function in remove)
        {
            LuaTimer.FixFrameFunctions.Remove(function);
        }
    }
}