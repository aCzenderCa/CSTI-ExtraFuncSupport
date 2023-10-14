using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CSTI_LuaActionSupport.Helper;
using HarmonyLib;
using NLua;
using UnityEngine;
using static CSTI_LuaActionSupport.AllPatcher.CardActionPatcher;
using Random = UnityEngine.Random;

namespace CSTI_LuaActionSupport.LuaCodeHelper
{
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
    }

    public static class FuncFor1_0_5
    {
        public static void GenEncounter(UniqueIDScriptable uniqueIDScriptable)
        {
            if (uniqueIDScriptable is not Encounter encounter) return;
            GameManager.Instance.GameGraphics.EncounterPopupWindow.StartEncounter(encounter,
                GameManager.Instance.CurrentSaveData.HasEncounterData);
            Enumerators.Add(WaitEncounter(encounter));
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
                        fieldInfo.SetValue(AccessObj, value.TryFloat());
                    }
                    else if (fieldInfo.FieldType == typeof(int))
                    {
                        fieldInfo.SetValue(AccessObj, value.TryInt());
                    }
                    else if (fieldInfo.FieldType == typeof(long))
                    {
                        fieldInfo.SetValue(AccessObj, value.TryLong());
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
        public const string SaveKey = "zender." + nameof(SimpleUniqueAccess);

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

        public void Gen(int count = 1, LuaTable? ext = null)
        {
            if (UniqueIDScriptable is CardData cardData)
            {
                var tDur = new TransferedDurabilities
                {
                    Usage = cardData.UsageDurability,
                    Fuel = cardData.FuelCapacity,
                    Spoilage = cardData.SpoilageTime,
                    ConsumableCharges = cardData.Progress,
                    Special1 = cardData.SpecialDurability1,
                    Special2 = cardData.SpecialDurability2,
                    Special3 = cardData.SpecialDurability3,
                    Special4 = cardData.SpecialDurability4,
                    Liquid = Random.Range(cardData.DefaultLiquidContained.Quantity[0],
                        cardData.DefaultLiquidContained.Quantity[1])
                };
                var sLiq = new SpawningLiquid
                {
                    LiquidCard = cardData.DefaultLiquidContained.LiquidCard,
                    StayEmpty = !cardData.DefaultLiquidContained.LiquidCard
                };
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

                    var card =
                        (ext[nameof(SpawningLiquid.LiquidCard)] as SimpleUniqueAccess)?.UniqueIDScriptable as CardData;
                    sLiq.LiquidCard = card;
                    sLiq.StayEmpty = !card;

                    count.TryModBy(ext[nameof(count)]);
                }

                if (cardData.CardType != CardTypes.Liquid)
                {
                    for (var i = 0; i < count; i++)
                    {
                        Enumerators.Add(GameManager.Instance.AddCard(cardData, null, true,
                            tDur, true, sLiq, new Vector2Int(GameManager.Instance.CurrentTickInfo.z, -1), false));
                    }
                }

                return;
            }

            GenEncounter?.Invoke(UniqueIDScriptable);
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

        public override object? AccessObj => UniqueIDScriptable;
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
}