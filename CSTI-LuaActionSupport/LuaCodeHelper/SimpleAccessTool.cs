using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CSTI_LuaActionSupport.AllPatcher;
using CSTI_LuaActionSupport.Helper;
using HarmonyLib;
using NLua;
using UnityEngine;
using static CSTI_LuaActionSupport.AllPatcher.SavePatcher;
using Random = UnityEngine.Random;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public class SimpleAccessTool
{
    public SimpleUniqueAccess? this[string key]
    {
        get
        {
            if (UniqueIDScriptable.AllUniqueObjects.TryGetValue(key, out var uniqueIDScriptable))
            {
                return new SimpleUniqueAccess(uniqueIDScriptable);
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

public class SimpleUniqueAccess(UniqueIDScriptable uniqueIDScriptable) : CommonSimpleAccess
{
    public readonly UniqueIDScriptable UniqueIDScriptable = uniqueIDScriptable;
    private static readonly Action<UniqueIDScriptable>? GenEncounter;
    public const string SaveKey = "zender." + nameof(SimpleUniqueAccess);
    public bool IsInstanceEnv => uniqueIDScriptable is CardData {InstancedEnvironment: true};

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

    // language=Lua
    [TestCode("""
              local uid = "cee786e0869369d4597877e838f2586f"
              local ext = { Usage = 5 }
              SimpleAccessTool[uid]:Gen(1,ext)
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
                
                GenAfterEnvChange.TryModBy(ext[nameof(GenAfterEnvChange)]);

                var card =
                    (ext[nameof(SpawningLiquid.LiquidCard)] as SimpleUniqueAccess)?.UniqueIDScriptable as CardData;
                sLiq.LiquidCard = card;
                sLiq.StayEmpty = !card;

                count.TryModBy(ext[nameof(count)]);

                if (ext[nameof(initData)] is DataNodeTableAccessBridge dataNodeTable)
                    initData = dataNodeTable;
            }

            var GenPriority = GenAfterEnvChange?PriorityEnumerators.AfterEnvChange:PriorityEnumerators.Normal;
            if (cardData.CardType != CardTypes.Liquid)
            {
                for (var i = 0; i < count; i++)
                {
                    if (initData != null)
                    {
                        GameManager.Instance.MoniAddCard(cardData, null,
                            tDur, true, sLiq, new Vector2Int(GameManager.Instance.CurrentTickInfo.z, -1),
                            SetInitData, initData).Add2AllEnumerators(GenPriority);
                    }
                    else
                    {
                        GameManager.Instance.AddCard(cardData, null, true,
                                tDur, true, sLiq, new Vector2Int(GameManager.Instance.CurrentTickInfo.z, -1), false)
                            .Add2AllEnumerators(GenPriority);
                    }
                }
            }

            return;
        }

        GenEncounter?.Invoke(UniqueIDScriptable);
    }

    public static void SetInitData(InGameCardBase cardBase, DataNodeTableAccessBridge? data)
    {
        if (data == null) return;
        var cardAccessBridge = new CardAccessBridge(cardBase);
        cardAccessBridge.InitData(data);
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