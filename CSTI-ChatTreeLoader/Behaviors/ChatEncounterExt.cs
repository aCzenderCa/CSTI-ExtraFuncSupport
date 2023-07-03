using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ChatTreeLoader.Util;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ChatTreeLoader.Behaviors
{
    public class ChatEncounterExt : ModEncounterExtBase<ModEncounter>
    {
        private static readonly Regex ActionRecordRegex = new(
            @"__\{(?<EncounterId>.+?)\}ModEncounter\.Infos__\{EncounterPath\:(?<path>(?<pathStartNode>\d+)(\.(?<pathNodes>\d+))*)\}");

        public static readonly Dictionary<string, List<int>> CurPaths = new();
        public static readonly Dictionary<string, ModEncounterNode[]> CurPathChildrenNodes = new();
        public static readonly Dictionary<string, ModEncounterNode> CurPathNode = new();

        public override void DisplayChatModEncounter(EncounterPopup __instance)
        {
            var encounterModelUniqueID = __instance.CurrentEncounter.EncounterModel.UniqueID;
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
            __instance.CheckButtonCountAndEnable(curNodes.Length, (_, button, index) =>
            {
                button.Setup(index, curNodes[index].Title, null, false);
                button.Interactable = curNodes[index].Condition
                                          .ConditionsValid(false, GameManager.Instance.CurrentEnvironmentCard) ||
                                      curNodes[index].Condition
                                          .ConditionsValid(false, GameManager.Instance.CurrentWeatherCard);
                button.gameObject.SetActive(true);
            });
        }

        public override void DoModPlayerAction(EncounterPopup __instance, int _Action)
        {
            var instanceCurrentEnvironmentCard = GameManager.Instance.CurrentEnvironmentCard;
            var encounterModelUniqueID = __instance.CurrentEncounter.EncounterModel.UniqueID;

            __instance.AddLogSeparator();
            CurPaths[encounterModelUniqueID].Add(_Action);
            CurPathNode[encounterModelUniqueID] = CurPathChildrenNodes[encounterModelUniqueID][_Action];
            CurPathChildrenNodes[encounterModelUniqueID] = CurPathNode[encounterModelUniqueID].ChildrenEncounterNodes;
            var modEncounterNode = CurPathNode[encounterModelUniqueID];
            if (modEncounterNode.EndNode)
            {
                __instance.CurrentEncounter.EncounterResult = EncounterResult.PlayerDemoralized;
                instanceCurrentEnvironmentCard.DroppedCollections.SafeRemove(
                    instanceCurrentEnvironmentCard.DroppedCollections.Keys.FirstOrDefault(key =>
                        ActionRecordRegex.IsMatch(key)));
                if (modEncounterNode.HasNodeEffect)
                {
                    __instance.ContinueButton.interactable = false;
                    GameManager.Instance.StartCoroutine(WaitCloseWindow(__instance)
                        .OnEnd(GameManager.Instance.ActionRoutine(modEncounterNode.NodeEffect,
                            GameManager.Instance.CurrentEnvironmentCard, false)));
                }
                else
                {
                    __instance.ContinueButton.interactable = false;
                    GameManager.Instance.StartCoroutine(WaitCloseWindow(__instance));
                }

                GraphicsManager.Instance.CardsDestroyed.Setup(
                    modEncounterNode.PlayerText + "\n\n" + modEncounterNode.EnemyText, modEncounterNode.Title);
                return;
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
            instanceCurrentEnvironmentCard.DroppedCollections[
                __instance.CurrentEncounter.EncounterModel.SavePath<ModEncounter>()] = Vector2Int.one;
            __instance.CurrentEncounter.CurrentRound += 1;
            __instance.DisplayPlayerActions();
        }

        public override void ModRoundStart(EncounterPopup __instance, bool _Loaded)
        {
            __instance.ActionsPlaying.Clear();
            var onNextRound = EncounterPopup.OnNextRound;
            onNextRound?.Invoke(__instance.CurrentEncounter.CurrentRound);
            if (__instance.ContinueButtonObject.activeSelf)
            {
                __instance.ContinueButtonObject.SetActive(false);
            }

            __instance.DisplayPlayerActions();
        }

        public override void UpdateModEx(EncounterPopup __instance)
        {
            throw new NotImplementedException();
        }

        public static IEnumerator WaitCloseWindow(EncounterPopup popup)
        {
            while (popup.ActionPlaying || popup.LogIsUpdating)
            {
                yield return null;
            }

            popup.OngoingEncounter = false;
            popup.gameObject.SetActive(false);
        }
    }


    public static class SavePathExt
    {
        public static string SavePath<T>(this Encounter encounter)
        {
            if (typeof(T) == typeof(ModEncounter))
            {
                return SaveChatPath(encounter);
            }

            return "";
        }

        public static string SaveChatPath(this Encounter encounter)
        {
            var encounterUniqueID = encounter.UniqueID;

            if (!ModEncounter.ModEncounters.ContainsKey(encounterUniqueID))
            {
                return null;
            }

            var curPath = ChatEncounterExt.CurPaths[encounterUniqueID];
            return $"__{{{encounterUniqueID}}}ModEncounter.Infos__{{EncounterPath:{curPath.Join(null, ".")}}}";
        }
    }
}