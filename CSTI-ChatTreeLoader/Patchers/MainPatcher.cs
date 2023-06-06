using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ChatTreeLoader.Util;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ChatTreeLoader.Patchers
{
    public static class MainPatcher
    {
        public static bool DoPatch(Harmony harmony)
        {
            try
            {
                harmony.PatchAll(typeof(MainPatcher));
                harmony.PatchAll(typeof(TestCardAdd));
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }

            return true;
        }

        public static readonly Regex ActionRecordRegex =
            new(
                @"__\{(?<EncounterId>.+?)\}ModEncounter\.Infos__\{EncounterPath\:(?<path>(?<pathStartNode>\d+)(\.(?<pathNodes>\d+))*)\}");

        public static readonly Dictionary<string, List<int>> CurPaths = new();
        public static readonly Dictionary<string, ModEncounterNode[]> CurPathChildrenNodes = new();
        public static readonly Dictionary<string, ModEncounterNode> CurPathNode = new();

        [HarmonyPrefix, HarmonyPatch(typeof(EncounterPopup), nameof(EncounterPopup.DisplayPlayerActions))]
        public static bool DisplayModEncounter(EncounterPopup __instance)
        {
            var findObjectsOfTypeAll = Resources.FindObjectsOfTypeAll<ModEncounter>();
            foreach (var encounter in findObjectsOfTypeAll)
            {
                encounter.Init();
            }

            var encounterModelUniqueID = __instance.CurrentEncounter.EncounterModel.UniqueID;
            if (!ModEncounter.ModEncounters.ContainsKey(encounterModelUniqueID))
            {
                return true;
            }

            __instance.EncounterPlayerActions.Clear();
            var modEncounter = ModEncounter.ModEncounters[encounterModelUniqueID];
            var collectionsKeys = GameManager.Instance.CurrentEnvironmentCard.DroppedCollections.Keys;
            var nodePath = new List<int>();
            foreach (var match in from key in collectionsKeys
                     select ActionRecordRegex.Match(key)
                     into match
                     where match.Success
                     where match.Groups["EncounterId"].Value == encounterModelUniqueID
                     select match)
            {
                if (int.TryParse(match.Groups["pathStartNode"].Value, out var i))
                {
                    nodePath.Add(i);
                }
                else
                {
                    continue;
                }

                foreach (Capture capture in match.Groups["pathNodes"].Captures)
                {
                    if (int.TryParse(capture.Value, out i))
                    {
                        nodePath.Add(i);
                    }
                    else
                    {
                        goto outContinue;
                    }
                }

                break;
                outContinue: ;
            }

            CurPaths[encounterModelUniqueID] = nodePath;
            var curNode = nodePath.Aggregate((ModEncounterNode) null,
                (current, i) =>
                    current == null ? modEncounter.ModEncounterNodes[i] : current.ChildrenEncounterNodes[i]);

            CurPathNode[encounterModelUniqueID] = curNode;
            var curNodes = nodePath.Aggregate(modEncounter.ModEncounterNodes,
                (current, i) => current[i].ChildrenEncounterNodes);
            CurPathChildrenNodes[encounterModelUniqueID] = curNodes;
            while (__instance.ActionButtons.Count < curNodes.Length)
            {
                var encounterOptionButton =
                    Object.Instantiate(__instance.ActionButtonPrefab, __instance.ActionButtonsParent);
                __instance.ActionButtons.Add(encounterOptionButton);
                encounterOptionButton.OnClicked = (Action<int>) Delegate.Combine(encounterOptionButton.OnClicked,
                    new Action<int>(__instance.DoPlayerAction));
            }

            for (var i = 0; i < __instance.ActionButtons.Count; i++)
            {
                if (i >= curNodes.Length)
                {
                    continue;
                }

                __instance.ActionButtons[i].Setup(i, curNodes[i].Title, null, false);
                __instance.ActionButtons[i].Interactable = curNodes[i].Condition
                    .ConditionsValid(false, GameManager.Instance.CurrentEnvironmentCard) || curNodes[i].Condition
                    .ConditionsValid(false, GameManager.Instance.CurrentWeatherCard);
                __instance.ActionButtons[i].gameObject.SetActive(true);
            }

            return false;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(EncounterPopup), nameof(EncounterPopup.DoPlayerAction))]
        public static bool DoModPlayerAction(EncounterPopup __instance, int _Action)
        {
            var encounterModelUniqueID = __instance.CurrentEncounter.EncounterModel.UniqueID;
            if (!ModEncounter.ModEncounters.ContainsKey(encounterModelUniqueID))
            {
                return true;
            }

            var instanceCurrentEnvironmentCard = GameManager.Instance.CurrentEnvironmentCard;

            __instance.AddLogSeparator();
            CurPaths[encounterModelUniqueID].Add(_Action);
            CurPathNode[encounterModelUniqueID] = CurPathChildrenNodes[encounterModelUniqueID][_Action];
            CurPathChildrenNodes[encounterModelUniqueID] = CurPathNode[encounterModelUniqueID].ChildrenEncounterNodes;
            var modEncounterNode = CurPathNode[encounterModelUniqueID];
            if (modEncounterNode.EndNode)
            {
                __instance.CurrentEncounter.EncounterResult = EncounterResult.PlayerWin;
                instanceCurrentEnvironmentCard.DroppedCollections.SafeRemove(
                    instanceCurrentEnvironmentCard.DroppedCollections.Keys.FirstOrDefault(key =>
                        ActionRecordRegex.IsMatch(key)));
                if (modEncounterNode.HasNodeEffect)
                {
                    __instance.StartCoroutine(__instance
                        .WaitForAction(modEncounterNode.NodeEffect, instanceCurrentEnvironmentCard)
                        .OnEnd(() =>
                        {
                            __instance.ContinueButton.interactable = false;
                            GameManager.Instance.StartCoroutine(WaitCloseWindow(__instance));
                        }));
                }
                else
                {
                    __instance.ContinueButton.interactable = false;
                    GameManager.Instance.StartCoroutine(WaitCloseWindow(__instance));
                }

                GraphicsManager.Instance.CardsDestroyed.Setup(
                    modEncounterNode.PlayerText + "\n\n" + modEncounterNode.EnemyText, modEncounterNode.Title);
                return false;
            }

            __instance.AddToLog(new EncounterLogMessage(modEncounterNode.PlayerText,
                Mathf.Abs(modEncounterNode.PlayerTextDuration) < 0.01 ? 1 : modEncounterNode.PlayerTextDuration)
            {
                SoundEffects = new[]
                {
                    modEncounterNode.PlayerAudio
                        ? modEncounterNode.PlayerAudio
                        : ModEncounter.ModEncounters[encounterModelUniqueID].DefaultPlayerAudio
                }
            });
            __instance.AddLogSeparator();
            __instance.AddToLog(new EncounterLogMessage(modEncounterNode.EnemyText,
                Mathf.Abs(modEncounterNode.EnemyTextDuration) < 0.01 ? 1 : modEncounterNode.EnemyTextDuration)
            {
                SoundEffects = new[]
                {
                    modEncounterNode.EnemyAudio
                        ? modEncounterNode.EnemyAudio
                        : ModEncounter.ModEncounters[encounterModelUniqueID].DefaultEnemyAudio
                }
            });
            if (modEncounterNode.HasNodeEffect)
            {
                __instance.StartCoroutine(__instance.WaitForAction(modEncounterNode.NodeEffect,
                    GameManager.Instance.CurrentEnvironmentCard));
            }

            var toRemove =
                instanceCurrentEnvironmentCard.DroppedCollections.Keys.FirstOrDefault(key =>
                    ActionRecordRegex.IsMatch(key));
            instanceCurrentEnvironmentCard.DroppedCollections.SafeRemove(toRemove);
            instanceCurrentEnvironmentCard.DroppedCollections[__instance.CurrentEncounter.EncounterModel.SavePath()] =
                Vector2Int.one;
            __instance.CurrentEncounter.CurrentRound += 1;
            __instance.DisplayPlayerActions();
            return false;
        }

        private static IEnumerator WaitCloseWindow(EncounterPopup popup)
        {
            while (popup.ActionPlaying || popup.LogIsUpdating)
            {
                yield return null;
            }

            popup.OngoingEncounter = false;
            popup.gameObject.SetActive(false);
        }
    }
}