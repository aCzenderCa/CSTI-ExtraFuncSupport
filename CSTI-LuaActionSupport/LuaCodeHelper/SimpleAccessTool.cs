using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CSTI_LuaActionSupport.AllPatcher;
using HarmonyLib;
using UnityEngine;
using static CSTI_LuaActionSupport.AllPatcher.CardActionPatcher;

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

        public object? this[string key]
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
                        fieldInfo.SetValue(AccessObj, (float) value);
                    }
                    else if (fieldInfo.FieldType == typeof(int))
                    {
                        fieldInfo.SetValue(AccessObj, (int) value);
                    }
                    else if (fieldInfo.FieldType == typeof(long))
                    {
                        fieldInfo.SetValue(AccessObj, (long) value);
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
        private readonly UniqueIDScriptable UniqueIDScriptable;
        private static readonly Action<UniqueIDScriptable>? GenEncounter;
        public const string SaveKey = "zender." + nameof(SimpleUniqueAccess);

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
                        SaveCurrentSlot(SaveKey,new Dictionary<string,object>
                        {
                            {cardData.UniqueID,new Dictionary<string,object>
                            {
                                {nameof(CardData.CardDescription), value}
                            }}
                        });
                    }
                }
            }
        }

        public void Gen()
        {
            Gen(1);
        }

        public void Gen(int count)
        {
            if (UniqueIDScriptable is CardData cardData)
            {
                if (cardData.CardType != CardTypes.Liquid)
                {
                    for (int i = 0; i < count; i++)
                    {
                        Enumerators.Add(GameManager.Instance.AddCard(cardData, null, true,
                            null, true, SpawningLiquid.Empty, Vector2Int.zero, false));
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