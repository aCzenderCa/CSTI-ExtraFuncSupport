using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChatTreeLoader.ScriptObjects;
using ChatTreeLoader.Util;
using HarmonyLib;
using UnityEngine;

namespace ChatTreeLoader.Patchers
{
    public static class ExtraStatImpl
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.ChangeEnvironment))]
        public static void OnEnvChange(GameManager __instance, ref IEnumerator __result)
        {
            __result = __result.OnStart(() =>
            {
                foreach (var gameStat in ExtraStatTable.AllTables.SelectMany(table => table.EnvBindStats)
                             .Where(stat => stat))
                {
                    var key = $"{gameStat.UniqueID}_{__instance.CurrentEnvironment.UniqueID}__EnvBind_Stat";
                    var inGameStat = __instance.StatsDict[gameStat];
                    __instance.CurrentEnvironmentCard.DroppedCollections[key] =
                        new Vector2Int(Mathf.RoundToInt(inGameStat.CurrentBaseValue), 1000);
                }
            }).OnEnd(() =>
            {
                var queue = new Queue<CoroutineController>();
                foreach (var gameStat in ExtraStatTable.AllTables.SelectMany(table => table.EnvBindStats)
                             .Where(stat => stat))
                {
                    if (__instance.CurrentEnvironmentCard.DroppedCollections.TryGetValue(
                            $"{gameStat.UniqueID}_{__instance.CurrentEnvironment.UniqueID}__EnvBind_Stat",
                            out var value))
                    {
                        var inGameStat = __instance.StatsDict[gameStat];
                        __instance.StartCoroutineEx(__instance.ChangeStatValue(inGameStat,
                            gameStat.BaseValue - inGameStat.CurrentBaseValue,
                            StatModification.Permanent).OnEnd(__instance.ChangeStatValue(inGameStat,
                            value.x - gameStat.BaseValue, StatModification.Permanent)), out var controller);
                        queue.Enqueue(controller);
                    }
                }

                __instance.WaitAll(queue);
            });
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GameManager), nameof(GameManager.ActionRoutine))]
        public static void OnCardAction(GameManager __instance, InGameCardBase _ReceivingCard, ref IEnumerator __result)
        {
            __result = __result.OnStart(() =>
            {
                foreach (var extraStatTable in ExtraStatTable.AllTables)
                {
                    if (extraStatTable.CardBindCards.All(data => data != _ReceivingCard.CardModel))
                    {
                        continue;
                    }

                    foreach (var bindStat in extraStatTable.CardBindStats)
                    {
                        var inGameStat = __instance.StatsDict[bindStat];
                        var key = $"{bindStat.UniqueID}_{_ReceivingCard.CardModel.UniqueID}__CardBind_Stat";
                        if (_ReceivingCard.DroppedCollections.TryGetValue(key, out var collection))
                        {
                            inGameStat.CurrentBaseValue = collection.x;
                        }
                        else
                        {
                            inGameStat.CurrentBaseValue = Mathf.RoundToInt(bindStat.BaseValue);
                            _ReceivingCard.DroppedCollections[key] =
                                new Vector2Int(Mathf.RoundToInt(bindStat.BaseValue), 0);
                        }
                    }
                }
            }).OnEnd(() =>
            {
                foreach (var extraStatTable in ExtraStatTable.AllTables)
                {
                    if (extraStatTable.CardBindCards.All(data => data != _ReceivingCard.CardModel))
                    {
                        continue;
                    }

                    foreach (var bindStat in extraStatTable.CardBindStats)
                    {
                        var inGameStat = __instance.StatsDict[bindStat];
                        var key = $"{bindStat.UniqueID}_{_ReceivingCard.CardModel.UniqueID}__CardBind_Stat";
                        _ReceivingCard.DroppedCollections[key] =
                            new Vector2Int(Mathf.RoundToInt(inGameStat.CurrentBaseValue), 0);
                    }
                }
            });
        }
    }
}