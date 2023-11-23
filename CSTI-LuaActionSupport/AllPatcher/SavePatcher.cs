using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CSTI_LuaActionSupport.LuaCodeHelper;
using static CSTI_LuaActionSupport.AllPatcher.SavePatcher.SaveEnv;
using HarmonyLib;
using UnityEngine;
using Base64 = gfoidl.Base64.Base64;

namespace CSTI_LuaActionSupport.AllPatcher;

public static class SavePatcher
{
    public class SaveEnv : IDisposable
    {
        private readonly int EnvKey;
        private readonly BinaryWriter BinaryWriter;

        private SaveEnv(int envKey, BinaryWriter binaryWriter)
        {
            EnvKey = envKey;
            BinaryWriter = binaryWriter;
        }

        public static SaveEnv BeginSaveEnv(BinaryWriter writer, int envKey)
        {
            writer.Write(envKey);
            return new SaveEnv(envKey, writer);
        }

        public void Dispose()
        {
            BinaryWriter.Write(EnvKey);
        }
    }

    public class LoadEnv : IDisposable
    {
        private readonly int EnvKey;
        private readonly BinaryReader BinaryReader;

        private LoadEnv(int envKey, BinaryReader binaryReader)
        {
            EnvKey = envKey;
            BinaryReader = binaryReader;
        }

        public static LoadEnv BeginLoadEnv(BinaryReader reader, out int envKey, int? req = null)
        {
            envKey = reader.ReadInt32();
            if (req != null && envKey != req)
            {
                throw new Exception("Load Error");
            }

            return new LoadEnv(envKey, reader);
        }

        public void Dispose()
        {
            try
            {
                if (BinaryReader.ReadInt32() != EnvKey)
                {
                    throw new Exception("Load Error");
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }

    public static string SavePath => Path.Combine(Application.persistentDataPath, "LuaModData", "LuaSave.bin");

    public static string SaveBackupPath =>
        Path.Combine(Application.persistentDataPath, "LuaModData", "LuaSave.bin.backup");

    [HarmonyPrefix, HarmonyPatch(typeof(GameLoad), nameof(GameLoad.DeleteGameData))]
    public static void OnDeleteGameData(GameLoad __instance, int _Index)
    {
        try
        {
            if (GSlotSaveData.ContainsKey(_Index))
            {
                GSlotSaveData.Remove(_Index);
            }

            OnSave(__instance, -1, false);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public const string StatCache = "__StatCache";

    [HarmonyPrefix, HarmonyPatch(typeof(GameLoad), nameof(GameLoad.SaveGame))]
    public static void OnSave(GameLoad __instance, int _GameIndex, bool _Checkpoint)
    {
        try
        {
            if (CurrentGSlotSaveData().TryGetValue(StatCache, out var statCache))
            {
                foreach (var (key, value) in statCache.table!)
                {
                    if (UniqueIDScriptable.GetFromID<GameStat>(key) is not { } stat) continue;
                    Vector2? statMinMaxValue = null;
                    Vector2? statMinMaxRate = null;
                    foreach (var (field, _) in value.table!)
                    {
                        switch (field)
                        {
                            case nameof(GameStat.MinMaxValue):
                                statMinMaxValue = stat.MinMaxValue;
                                break;
                            case nameof(GameStat.MinMaxRate):
                                statMinMaxRate = stat.MinMaxRate;
                                break;
                        }
                    }

                    if (statMinMaxValue != null)
                        value.table[nameof(GameStat.MinMaxValue)] = new DataNode(statMinMaxValue.Value);
                    if (statMinMaxRate != null)
                        value.table[nameof(GameStat.MinMaxRate)] = new DataNode(statMinMaxRate.Value);
                }
            }

            var buf = new MemoryStream();
            var binaryWriter = new BinaryWriter(buf);
            var buf1 = new MemoryStream();
            var binaryWriter1 = new BinaryWriter(buf1);

            using (BeginSaveEnv(binaryWriter, 0))
            {
                binaryWriter.Write(GSaveData.Count(pair => pair.Value.NodeType != DataNode.DataNodeType.Nil));
                foreach (var (key, node) in GSaveData)
                {
                    if (node.NodeType != DataNode.DataNodeType.Nil)
                    {
                        binaryWriter.Write(key);
                        node.Save(binaryWriter);
                    }
                }
            }

            if (File.Exists(SavePath))
            {
                if (File.Exists(SaveBackupPath))
                {
                    File.Delete(SaveBackupPath);
                }

                File.Move(SavePath, SaveBackupPath);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SavePath) ?? string.Empty);
            using var saveDataFile = File.Create(SavePath);
            buf.WriteTo(saveDataFile);
            saveDataFile.Flush();

            if (GSlotSaveData.TryGetValue(_GameIndex, out var save))
            {
                using (BeginSaveEnv(binaryWriter1, 0))
                {
                    binaryWriter1.Write(save.Count(pair => pair.Value.NodeType != DataNode.DataNodeType.Nil));
                    foreach (var (key, node) in save)
                    {
                        if (node.NodeType != DataNode.DataNodeType.Nil)
                        {
                            binaryWriter1.Write(key);
                            node.Save(binaryWriter1);
                        }
                    }
                }

                SaveTo(__instance.Games[_GameIndex].MainData, buf1);
                if (_Checkpoint)
                {
                    SaveTo(__instance.Games[_GameIndex].CheckpointData, buf1);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    public const string LuaLongTimeSaveId = "zender.modLoaderUse.luaLongTimeSave";

    private static void SaveTo(GameSaveData? saveData, MemoryStream data)
    {
        if (saveData == null)
        {
            return;
        }

        saveData.AllEndgameLogs ??= new List<LogSaveData>();
        if (saveData.AllEndgameLogs.Count > 0 && saveData.AllEndgameLogs[0].CategoryID == LuaLongTimeSaveId)
        {
            var saveDataAllEndgameLog = saveData.AllEndgameLogs[0];
            saveDataAllEndgameLog.LogText = Base64.Default.Encode(data.ToArray());
            saveData.AllEndgameLogs[0] = saveDataAllEndgameLog;
        }
        else
        {
            var saveDataAllEndgameLog = new LogSaveData
            {
                CategoryID = LuaLongTimeSaveId, LoggedOnTick = -1000,
                LogText = Base64.Default.Encode(data.ToArray())
            };
            saveData.AllEndgameLogs.Insert(0, saveDataAllEndgameLog);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(EndgameMenu), nameof(EndgameMenu.Setup))]
    public static void FixEndgameMenu_Setup(ref GameSaveData _SaveData, out List<LogSaveData> __state)
    {
        __state = _SaveData.AllEndgameLogs;
        _SaveData.AllEndgameLogs = __state.Where(data => data.CategoryID != LuaLongTimeSaveId).ToList();
    }

    [HarmonyPostfix, HarmonyPatch(typeof(EndgameMenu), nameof(EndgameMenu.Setup))]
    public static void FixEndgameMenu_Setup_Post(ref GameSaveData _SaveData, List<LogSaveData> __state)
    {
        _SaveData.AllEndgameLogs = __state;
    }
        
}