﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CSTI_LuaActionSupport.AllPatcher;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaBuilder;
using HarmonyLib;
using NLua;
using UnityEngine;
using static CSTI_LuaActionSupport.AllPatcher.SavePatcher;
using Random = UnityEngine.Random;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public class SimpleAccessTool
{
    public static readonly Regex SUAFindCardRe = new(@"^(?<name>.+)\|Card\|(?<type>.+)$");
    public static readonly Regex SUAFindStatRe = new(@"^(?<name>.+)\|Stat$");
    public static readonly Dictionary<string, CardData> CardFindCache = new();
    public static readonly Dictionary<string, GameStat> StatFindCache = new();

    public static GameStat? FindStat(string key)
    {
        if (SUAFindStatRe.Match(key) is {Success: true} match)
        {
            if (StatFindCache.TryGetValue(key, out var findStat))
            {
                return findStat;
            }

            var name = match.Groups["name"].Value;
            if (UniqueIDScriptable.AllUniqueObjects.FirstOrDefault(pair =>
                        pair.Value is GameStat stat &&
                        (stat.GameName.DefaultText == name || stat.GameName == name ||
                         stat.name == name || stat.GameName.CnStr() == name))
                    is var (_, _uniqueIDScriptable) && _uniqueIDScriptable != null)
            {
                StatFindCache[key] = (GameStat) _uniqueIDScriptable;
                return (GameStat) _uniqueIDScriptable;
            }

            if (GameLoad.Instance.DataBase.AllData.FirstOrDefault(us =>
                    us is GameStat stat &&
                    (stat.GameName.DefaultText == name || stat.GameName == name ||
                     stat.name == name || stat.GameName.CnStr() == name))
                is { } __uniqueIDScriptable)
            {
                StatFindCache[key] = (GameStat) __uniqueIDScriptable;
                return (GameStat) __uniqueIDScriptable;
            }
        }

        return null;
    }

    public static CardData? FindCard(string key)
    {
        if (MainBuilder.Name2Card.TryGetValue(key, out var namedCard))
        {
            return namedCard;
        }

        if (SUAFindCardRe.Match(key) is {Success: true} match)
        {
            if (CardFindCache.TryGetValue(key, out var findCard))
            {
                return findCard;
            }

            var name = match.Groups["name"].Value;
            var type = match.Groups["type"].Value;
            if (UniqueIDScriptable.AllUniqueObjects.FirstOrDefault(pair =>
                        pair.Value is CardData cardData && cardData.CardType.ToString() == type &&
                        (cardData.CardName.DefaultText == name || cardData.CardName == name ||
                         cardData.name == name || cardData.CardName.CnStr() == name))
                    is var (_, _uniqueIDScriptable) && _uniqueIDScriptable != null)
            {
                CardFindCache[key] = (CardData) _uniqueIDScriptable;
                return (CardData) _uniqueIDScriptable;
            }

            if (GameLoad.Instance.DataBase.AllData.FirstOrDefault(us =>
                    us is CardData cardData && cardData.CardType.ToString() == type &&
                    (cardData.CardName.DefaultText == name || cardData.CardName == name ||
                     cardData.name == name || cardData.CardName.CnStr() == name))
                is { } __uniqueIDScriptable)
            {
                CardFindCache[key] = (CardData) __uniqueIDScriptable;
                return (CardData) __uniqueIDScriptable;
            }
        }

        return null;
    }

    public SimpleUniqueAccess? this[string key]
    {
        get
        {
            if (UniqueIDScriptable.AllUniqueObjects.TryGetValue(key, out var uniqueIDScriptable))
            {
                return new SimpleUniqueAccess(uniqueIDScriptable);
            }

            if (FindCard(key) is { } cardData)
            {
                return new SimpleUniqueAccess(cardData);
            }

            if (FindStat(key) is { } stat)
            {
                return new SimpleUniqueAccess(stat);
            }

            if (GameLoad.Instance.DataBase.AllData.FirstOrDefault(scriptable => scriptable.UniqueID == key) is
                { } idScriptable)
            {
                return new SimpleUniqueAccess(idScriptable);
            }

            Debug.LogWarning($"no unique id : {key}");
            return null;
        }
    }

    public void ClearCurrentEnv()
    {
        var gameManager = GameManager.Instance;
        var gameManagerCurrentExplorableCard = gameManager.CurrentExplorableCard;
        gameManagerCurrentExplorableCard.ExplorationData.CurrentExploration = 0;
        gameManagerCurrentExplorableCard.ExplorationData.ExplorationResults.Do(data =>
        {
            data.Triggered = false;
            data.TriggeredWithoutResults = false;
        });
        ClearStats(gameManagerCurrentExplorableCard);

        var gameManagerCurrentEnvironmentCard = gameManager.CurrentEnvironmentCard;
        ClearStats(gameManagerCurrentEnvironmentCard);

        foreach (var card in gameManager.AllVisibleCards.Where(card =>
                     card.CurrentSlotInfo.SlotType is SlotsTypes.Base or SlotsTypes.Location &&
                     gameManagerCurrentEnvironmentCard.CardModel.DefaultEnvCards.All(data =>
                         data != card.CardModel)))
        {
            gameManager.RemoveCard(card, true, false, GameManager.RemoveOption.RemoveAll)
                .Add2AllEnumerators(PriorityEnumerators.Low);
        }
    }

    public static void ClearStats(InGameCardBase cardBase)
    {
        cardBase.DroppedCollections = new Dictionary<string, Vector2Int>();
        cardBase.CurrentUsageDurability = cardBase.CardModel.UsageDurability.FloatValue;
        cardBase.CurrentProgress = cardBase.CardModel.Progress.FloatValue;
        cardBase.CurrentFuel = cardBase.CardModel.FuelCapacity.FloatValue;
        cardBase.CurrentSpoilage = cardBase.CardModel.SpoilageTime.FloatValue;
        cardBase.CurrentSpecial1 = cardBase.CardModel.SpecialDurability1.FloatValue;
        cardBase.CurrentSpecial2 = cardBase.CardModel.SpecialDurability2.FloatValue;
        cardBase.CurrentSpecial3 = cardBase.CardModel.SpecialDurability3.FloatValue;
        cardBase.CurrentSpecial4 = cardBase.CardModel.SpecialDurability4.FloatValue;
        if (cardBase.CardVisuals)
        {
            cardBase.CardVisuals.RefreshDurabilities();
        }
    }
}

public static class FuncFor1_0_5
{
    public static void GenEncounter(UniqueIDScriptable uniqueIDScriptable)
    {
        if (uniqueIDScriptable is not Encounter encounter) return;
        GameManager.Instance.GameGraphics.EncounterPopupWindow.StartEncounter(encounter,
            GameManager.Instance.CurrentSaveData.HasEncounterData);
        WaitEncounter(encounter).Add2AllEnumerators(PriorityEnumerators.Normal);
    }

    private static IEnumerator WaitEncounter(Encounter encounter)
    {
        while (GameManager.Instance.GameGraphics.EncounterPopupWindow.OngoingEncounter)
            yield return null;
        GameManager.Instance.StartCoroutineEx(GameManager.Instance.CheckAllStatsForActions(), out var controller);
        while (controller.state != CoroutineState.Finished)
            yield return null;
    }
}

public abstract class CommonSimpleAccess
{
    public abstract object? AccessObj { get; }

    public virtual object? this[string key]
    {
        get
        {
            if (AccessObj == null)
            {
                return null;
            }

            var type = AccessObj.GetType();
            var fieldInfo = AccessTools.Field(type, key);
            if (fieldInfo == null)
            {
                return null;
            }

            var value = fieldInfo.GetValue(AccessObj);
            if (value == null)
            {
                return null;
            }

            if (value is UniqueIDScriptable uniqueIDScriptable)
            {
                return new SimpleUniqueAccess(uniqueIDScriptable);
            }

            return new SimpleObjAccess(value);
        }
        set
        {
            if (AccessObj == null)
            {
                return;
            }

            var type = AccessObj.GetType();
            var fieldInfo = AccessTools.Field(type, key);
            if (fieldInfo == null)
            {
                return;
            }

            if (value is double or long)
            {
                if (fieldInfo.FieldType == typeof(float))
                {
                    fieldInfo.SetValue(AccessObj, value.TryNum<float>());
                }
                else if (fieldInfo.FieldType == typeof(int))
                {
                    fieldInfo.SetValue(AccessObj, value.TryNum<int>());
                }
                else if (fieldInfo.FieldType == typeof(long))
                {
                    fieldInfo.SetValue(AccessObj, value.TryNum<long>());
                }

                return;
            }

            if (value?.GetType() != fieldInfo.FieldType)
            {
                return;
            }


            fieldInfo.SetValue(AccessObj, value);
        }
    }
}

public class SimpleObjAccess : CommonSimpleAccess
{
    public override object AccessObj => _AccessObj;
    public readonly object _AccessObj;

    public SimpleObjAccess(object accessObj)
    {
        _AccessObj = accessObj;
    }
}

public class SimpleUniqueAccess : CommonSimpleAccess
{
    public readonly UniqueIDScriptable UniqueIDScriptable;
    private static readonly Action<UniqueIDScriptable>? GenEncounter;
    private readonly UniqueIDScriptable _uniqueIDScriptable;
    public const string SaveKey = "zender." + nameof(SimpleUniqueAccess);
    public bool IsInstanceEnv => _uniqueIDScriptable is CardData {InstancedEnvironment: true};

    public override object? this[string key]
    {
        get
        {
            if (UniqueIDScriptable is not CardData cardData) return base[key];

            if (cardData.TimeValues.FirstOrDefault(objective => objective.ObjectiveName == key) is
                { } timeObjective)
            {
                return timeObjective.Value;
            }

            if (cardData.StatValues.FirstOrDefault(objective => objective.ObjectiveName == key) is
                { } statSubObjective)
            {
                return statSubObjective.StatCondition.Stat;
            }

            if (cardData.CardsOnBoard.FirstOrDefault(objective => objective.ObjectiveName == key) is
                { } cardOnBoardSubObjective)
            {
                return cardOnBoardSubObjective.Card;
            }

            if (cardData.TagsOnBoard.FirstOrDefault(objective => objective.ObjectiveName == key) is
                { } tagOnBoardSubObjective)
            {
                return tagOnBoardSubObjective.Tag;
            }

            return base[key];
        }
    }

    static SimpleUniqueAccess()
    {
        var encounter = AccessTools.TypeByName("Encounter");
        if (encounter != null)
        {
            GenEncounter = FuncFor1_0_5.GenEncounter;
        }
    }

    public SimpleUniqueAccess(UniqueIDScriptable uniqueIDScriptable)
    {
        _uniqueIDScriptable = uniqueIDScriptable;
        UniqueIDScriptable = uniqueIDScriptable;
    }

    public string? CardDescription
    {
        get
        {
            if (UniqueIDScriptable is CardData cardData)
            {
                return cardData.CardDescription;
            }

            return null;
        }
        set
        {
            if (value == null)
            {
                return;
            }

            if (UniqueIDScriptable is CardData cardData)
            {
                if (LoadCurrentSlot(SaveKey) is DataNodeTableAccessBridge accessBridge)
                {
                    if (accessBridge[cardData.UniqueID] is DataNodeTableAccessBridge uniqueAccessBridge)
                    {
                        uniqueAccessBridge[nameof(CardData.CardDescription)] = value;
                    }
                    else
                    {
                        accessBridge[cardData.UniqueID] = new Dictionary<string, object>
                        {
                            {nameof(CardData.CardDescription), value}
                        };
                    }
                }
                else
                {
                    SaveCurrentSlot(SaveKey, new Dictionary<string, object>
                    {
                        {
                            cardData.UniqueID, new Dictionary<string, object>
                            {
                                {nameof(CardData.CardDescription), value}
                            }
                        }
                    });
                }
            }
        }
    }

    public void CompleteResearch()
    {
        if (UniqueIDScriptable is not CardData {CardType: CardTypes.Blueprint} cardData) return;
        if (!GameManager.Instance.BlueprintModelStates.TryGetValue(cardData, out var state))
            state = BlueprintModelState.Hidden;

        if (state == BlueprintModelState.Available) return;
        GameManager.Instance.BlueprintModelStates[cardData] = BlueprintModelState.Available;
        if (GameManager.Instance.PurchasableBlueprintCards.Contains(cardData))
            GameManager.Instance.PurchasableBlueprintCards.Remove(cardData);
        if (GameManager.Instance.BlueprintResearchTimes.ContainsKey(cardData))
            GameManager.Instance.BlueprintResearchTimes.Remove(cardData);
        if (GraphicsManager.Instance.BlueprintModelsPopup.CurrentResearch == cardData)
            GraphicsManager.Instance.BlueprintModelsPopup.CurrentResearch = null;
        GameManager.Instance.FinishedBlueprintResearch = cardData;
        GraphicsManager.Instance.BlueprintModelsPopup.UpdateResearchIcon();
    }

    public void UnlockBlueprint()
    {
        if (UniqueIDScriptable is not CardData {CardType: CardTypes.Blueprint} cardData) return;
        if (!GameManager.Instance.BlueprintModelStates.TryGetValue(cardData, out var state))
            state = BlueprintModelState.Hidden;
        if (state is BlueprintModelState.Available or BlueprintModelState.Purchasable) return;
        GameManager.Instance.BlueprintModelStates[cardData] = BlueprintModelState.Purchasable;
        if (!GameManager.Instance.PurchasableBlueprintCards.Contains(cardData))
            GameManager.Instance.PurchasableBlueprintCards.Add(cardData);
        GraphicsManager.Instance.BlueprintModelsPopup.UpdateResearchIcon();
    }

    public void ProcessBlueprint(int time)
    {
        if (UniqueIDScriptable is not CardData {CardType: CardTypes.Blueprint} cardData) return;
        if (!GameManager.Instance.BlueprintModelStates.TryGetValue(cardData, out var state))
            state = BlueprintModelState.Hidden;
        if (state is BlueprintModelState.Available) return;
        UnlockBlueprint();
        if (GameManager.Instance.BlueprintResearchTimes.ContainsKey(cardData))
        {
            GameManager.Instance.BlueprintResearchTimes[cardData] += time;
        }
        else
        {
            GameManager.Instance.BlueprintResearchTimes[cardData] = time;
        }

        if (GameManager.Instance.BlueprintResearchTimes[cardData] >=
            GameManager.DaysToTicks(cardData.BuildingDaytimeCost))
            CompleteResearch();
    }

    // language=Lua
    [TestCode("""
              local uid = "cee786e0869369d4597877e838f2586f" ---铜长矛
              local uid_1 = "3d2a3fb85bfa7d042b0388308b2b1fd5" ---箭矢蓝图
              local ext = { Usage = 5,SlotType="Location",CurrentBpStage=0 }
              ---SimpleAccessTool[uid_1]:CompleteResearch()
              SimpleAccessTool[uid_1]:Gen(1,ext)
              """)]
    public void Gen(int count = 1, LuaTable? ext = null)
    {
        if (count <= 0) return;
        if (UniqueIDScriptable is CardData cardData)
        {
            var GenAfterEnvChange = false;
            var tDur = new TransferedDurabilities
            {
                Usage = cardData.UsageDurability.Copy(),
                Fuel = cardData.FuelCapacity.Copy(),
                Spoilage = cardData.SpoilageTime.Copy(),
                ConsumableCharges = cardData.Progress.Copy(),
                Special1 = cardData.SpecialDurability1.Copy(),
                Special2 = cardData.SpecialDurability2.Copy(),
                Special3 = cardData.SpecialDurability3.Copy(),
                Special4 = cardData.SpecialDurability4.Copy(),
                Liquid = Random.Range(cardData.DefaultLiquidContained.Quantity[0],
                    cardData.DefaultLiquidContained.Quantity[1])
            };
            var sLiq = new SpawningLiquid
            {
                LiquidCard = cardData.DefaultLiquidContained.LiquidCard,
                StayEmpty = !cardData.DefaultLiquidContained.LiquidCard
            };
            DataNodeTableAccessBridge? initData = null;
            var forceSlotInfo = new SlotInfo(SlotsTypes.Base, -10086);
            var forceBpData = new BlueprintSaveData(null, null) {CurrentStage = -10086};
            // ReSharper disable once InconsistentNaming
            var NeedPreInit = false;
            if (ext != null)
            {
                tDur.Usage.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Usage)]);
                tDur.Fuel.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Fuel)]);
                tDur.Spoilage.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Spoilage)]);
                tDur.ConsumableCharges.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.ConsumableCharges)]);
                tDur.Liquid.TryModBy(ext[nameof(TransferedDurabilities.Liquid)]);
                tDur.Special1.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Special1)]);
                tDur.Special2.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Special2)]);
                tDur.Special3.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Special3)]);
                tDur.Special4.FloatValue.TryModBy(ext[nameof(TransferedDurabilities.Special4)]);
                NeedPreInit.TryModBy(ext[nameof(NeedPreInit)]);

                GenAfterEnvChange.TryModBy(ext[nameof(GenAfterEnvChange)]);

                var card =
                    (ext[nameof(SpawningLiquid.LiquidCard)] as SimpleUniqueAccess)?.UniqueIDScriptable as CardData;
                sLiq.LiquidCard = card;
                sLiq.StayEmpty = !card;

                count.TryModBy(ext[nameof(count)]);

                if (ext[nameof(forceSlotInfo.SlotType)] is string slotType &&
                    Enum.TryParse(slotType, out forceSlotInfo.SlotType))
                {
                    forceSlotInfo.SlotIndex = -2;
                }

                forceBpData.CurrentStage.TryModBy(ext["CurrentBpStage"]);

                if (ext[nameof(initData)] is DataNodeTableAccessBridge dataNodeTable)
                    initData = dataNodeTable;
            }

            var GenPriority = GenAfterEnvChange ? PriorityEnumerators.AfterEnvChange : PriorityEnumerators.Normal;
            var enumerators = new List<IEnumerator>();
            if (cardData.CardType != CardTypes.Liquid)
            {
                for (var i = 0; i < count; i++)
                {
                    GameManager.Instance.MoniAddCard(cardData, null,
                            tDur, forceSlotInfo, forceBpData, true, sLiq,
                            new Vector2Int(GameManager.Instance.CurrentTickInfo.z, -1),
                            initData != null ? [initData.IntoSave()] : null, NeedPreInit)
                        .Add2Li(enumerators);
                }
            }

            enumerators.Add2AllEnumerators(GenPriority);
            return;
        }

        GenEncounter?.Invoke(UniqueIDScriptable);
    }

    public static void SetInitData(InGameCardBase cardBase, DataNodeTableAccessBridge? data)
    {
        if (data?.Keys == null) return;
        var cardAccessBridge = new CardAccessBridge(cardBase);
        cardAccessBridge.InitData();
        foreach (var key in data.Keys)
        {
            cardAccessBridge[key] = data[key];
        }

        cardAccessBridge.SaveData();
    }

    public float StatValue
    {
        get
        {
            if (UniqueIDScriptable is not GameStat gameStat) return -1;
            var inGameStat = GameManager.Instance.StatsDict[gameStat];
            return inGameStat.SimpleCurrentValue;
        }
        set
        {
            if (UniqueIDScriptable is not GameStat gameStat) return;
            var inGameStat = GameManager.Instance.StatsDict[gameStat];
            GameManager.Instance.ChangeStatValueTo(inGameStat, value);
        }
    }

    public float StatValueMin
    {
        get
        {
            if (UniqueIDScriptable is not GameStat gameStat) return -1;
            return gameStat.MinMaxValue.x;
        }
        set
        {
            if (UniqueIDScriptable is not GameStat gameStat) return;
            gameStat.MinMaxValue.Set(value, gameStat.MinMaxValue.y);
        }
    }

    public float StatValueMax
    {
        get
        {
            if (UniqueIDScriptable is not GameStat gameStat) return -1;
            return gameStat.MinMaxValue.y;
        }
        set
        {
            if (UniqueIDScriptable is not GameStat gameStat) return;
            gameStat.MinMaxValue.Set(gameStat.MinMaxValue.x, value);
        }
    }

    // language=Lua
    [TestCode("""
              local sua = SimpleAccessTool["79290cafb08e48f4d871704c20e69b1c"]
              sua:CacheRawValRange(0,100)
              sua.StatValueMin = -100
              sua.StatValueMax = 200
              """)]
    public void CacheRawValRange(float x, float y)
    {
        if (UniqueIDScriptable is not GameStat gameStat) return;
        gameStat.MinMaxValue = new Vector2(x, y);
        var currentGSlotSaveData = CurrentGSlotSaveData();

        if (!currentGSlotSaveData.ContainsKey(StatCache))
            currentGSlotSaveData[StatCache] = DataNode.EmptyTable;

        if (!currentGSlotSaveData[StatCache].table!.ContainsKey(gameStat.UniqueID))
            currentGSlotSaveData[StatCache].table![gameStat.UniqueID] = DataNode.EmptyTable;

        if (!currentGSlotSaveData[StatCache].table![gameStat.UniqueID].table!.ContainsKey(
                nameof(GameStat.MinMaxValue)))
            currentGSlotSaveData[StatCache].table![gameStat.UniqueID].table![nameof(GameStat.MinMaxValue)] =
                new DataNode(gameStat.MinMaxValue);
    }

    public void CacheRawRateRange(float x, float y)
    {
        if (UniqueIDScriptable is not GameStat gameStat) return;
        gameStat.MinMaxRate = new Vector2(x, y);
        var currentGSlotSaveData = CurrentGSlotSaveData();

        if (!currentGSlotSaveData.ContainsKey(StatCache))
            currentGSlotSaveData[StatCache] = DataNode.EmptyTable;

        if (!currentGSlotSaveData[StatCache].table!.ContainsKey(gameStat.UniqueID))
            currentGSlotSaveData[StatCache].table![gameStat.UniqueID] = DataNode.EmptyTable;

        if (!currentGSlotSaveData[StatCache].table![gameStat.UniqueID].table!.ContainsKey(
                nameof(GameStat.MinMaxRate)))
            currentGSlotSaveData[StatCache].table![gameStat.UniqueID].table![nameof(GameStat.MinMaxRate)] =
                new DataNode(gameStat.MinMaxRate);
    }

    public float StatRate
    {
        get
        {
            if (UniqueIDScriptable is not GameStat gameStat) return -1;
            var inGameStat = GameManager.Instance.StatsDict[gameStat];
            return inGameStat.SimpleRatePerTick;
        }
        set
        {
            if (UniqueIDScriptable is not GameStat gameStat) return;
            var inGameStat = GameManager.Instance.StatsDict[gameStat];
            GameManager.Instance.ChangeStatRateTo(inGameStat, value);
        }
    }

    public float StatRateMin
    {
        get
        {
            if (UniqueIDScriptable is not GameStat gameStat) return -1;
            return gameStat.MinMaxRate.x;
        }
        set
        {
            if (UniqueIDScriptable is not GameStat gameStat) return;
            gameStat.MinMaxRate.Set(value, gameStat.MinMaxRate.y);
        }
    }

    public float StatRateMax
    {
        get
        {
            if (UniqueIDScriptable is not GameStat gameStat) return -1;
            return gameStat.MinMaxRate.y;
        }
        set
        {
            if (UniqueIDScriptable is not GameStat gameStat) return;
            gameStat.MinMaxRate.Set(gameStat.MinMaxRate.x, value);
        }
    }

    public EnvDataAccessBridge? GetEnvData(long? index = null)
    {
        if (UniqueIDScriptable is not CardData {CardType: CardTypes.Environment} cardData)
        {
            return null;
        }

        if (cardData is {InstancedEnvironment: false} &&
            GameManager.Instance.EnvironmentsData.TryGetValue(cardData.UniqueID, out var environmentSaveData))
        {
            return new EnvDataAccessBridge(environmentSaveData);
        }

        if (index != null && cardData is {InstancedEnvironment: true} &&
            GameManager.Instance.EnvironmentsData.FirstOrDefault(pair =>
                pair.Key.StartsWith(cardData.UniqueID) && pair.Key.EndsWith(index.ToString())) is var dataPair)
        {
            return new EnvDataAccessBridge(dataPair.Value);
        }

        return null;
    }

    public override object AccessObj => UniqueIDScriptable;
}

public class EnvDataAccessBridge : CommonSimpleAccess
{
    public readonly EnvironmentSaveData EnvironmentSaveData;

    public EnvDataAccessBridge(EnvironmentSaveData environmentSaveData)
    {
        EnvironmentSaveData = environmentSaveData;
    }

    public override object AccessObj => EnvironmentSaveData;
}