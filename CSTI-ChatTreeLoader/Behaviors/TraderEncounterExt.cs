using System;
using System.Text.RegularExpressions;
using BepInEx;
using ChatTreeLoader.LocalText;
using ChatTreeLoader.ScriptObjects;
using ChatTreeLoader.Util;
using UnityEngine;
using static CSTI_LuaActionSupport.AllPatcher.CardActionPatcher;

namespace ChatTreeLoader.Behaviors
{
    public class TraderEncounterExt : ModEncounterExtBase<SimpleTraderEncounter>
    {
        private static readonly Regex ActionRecordRegex = new(
            @"__\{(?<EncounterId>.+?)\}TraderEncounter\.Infos__\{Stage:(?<stage>.+?),StageInfo:(?<stageInfo>\d+?)\}");

        public override void DisplayChatModEncounter(EncounterPopup __instance)
        {
            var traderEncounter =
                SimpleTraderEncounter.SimpleTraderEncounters[__instance.CurrentEncounter.EncounterModel.UniqueID];
            var instanceCurrentEnvironmentCard = GameManager.Instance.CurrentEnvironmentCard;
            var stage = TraderStage.empty;
            var stageInfo = 0;
            string lastRecord = "";
            foreach (var (key, val) in instanceCurrentEnvironmentCard.DroppedCollections)
            {
                if (val != Vector2Int.zero || ActionRecordRegex.Match(key) is not {Success: true} match) continue;
                Enum.TryParse(match.Groups["stage"].Value, true, out stage);
                int.TryParse(match.Groups["stageInfo"].Value, out stageInfo);
                lastRecord = key;
                break;
            }

            if (stage == TraderStage.empty)
            {
                var _stage = LoadCurrentSlot("TraderEncounterExt_stage") as string;
                var _stageInfo = LoadCurrentSlot("TraderEncounterExt_stageInfo");
                if (_stage != null && _stageInfo is double d)
                {
                    Enum.TryParse(_stage, true, out stage);
                    stageInfo = (int) d;
                }
            }

            switch (stage)
            {
                case TraderStage.empty:
                case TraderStage.ShowItem:
                    __instance.CheckButtonCountAndEnable(Math.Min(5, 1 + traderEncounter.BuySets.Count - stageInfo * 4),
                        (_, button, index) =>
                        {
                            if (index > 0)
                            {
                                var buySet = traderEncounter.BuySets[stageInfo * 4 + index - 1];
                                button.Setup(index, buySet.GetShowName(), null, false);
                                button.Interactable =
                                    buySet.BuyCondition.ConditionsValid(false,
                                        GameManager.Instance.CurrentEnvironmentCard);
                                button.gameObject.SetActive(buySet.BuyShowCondition.ConditionsValid(false,
                                    GameManager.Instance.CurrentEnvironmentCard));
                            }
                            else
                            {
                                button.Setup(index, "我该走了".Local(), null, false);
                                button.Interactable = true;
                                button.gameObject.SetActive(true);
                            }
                        });
                    break;
                case TraderStage.ShowCost:
                    __instance.CheckButtonCountAndEnable(4,
                        (_, button, index) =>
                        {
                            button.Setup(index, index switch
                            {
                                0 => "确认购买".Local(),
                                1 => "购买十份".Local(),
                                2 => "再看看别的".Local(),
                                _ => "我该走了".Local(),
                            }, null, false);
                            button.Interactable = index switch
                            {
                                0 => traderEncounter.CoinSet.ValidCoins(traderEncounter.BuySets[stageInfo]
                                    .CalCost(traderEncounter)),
                                1 => traderEncounter.CoinSet.ValidCoins(traderEncounter.BuySets[stageInfo]
                                    .CalCost(traderEncounter) * 10),
                                _ => true
                            };

                            button.gameObject.SetActive(true);
                        });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (stage == TraderStage.empty)
            {
                stage = TraderStage.ShowItem;
                instanceCurrentEnvironmentCard.DroppedCollections.Remove(lastRecord);
                SaveCurrentSlot("TraderEncounterExt_stage", stage.ToString());
                SaveCurrentSlot("TraderEncounterExt_stageInfo", stageInfo);
            }
        }

        public override void DoModPlayerAction(EncounterPopup __instance, int _Action)
        {
            var instanceCurrentEnvironmentCard = GameManager.Instance.CurrentEnvironmentCard;
            var encounterModelUniqueID = __instance.CurrentEncounter.EncounterModel.UniqueID;
            var traderEncounter = SimpleTraderEncounter.SimpleTraderEncounters[encounterModelUniqueID];

            var stage = TraderStage.empty;
            var stageInfo = 0;
            string lastRecord = "";
            foreach (var (key, val) in instanceCurrentEnvironmentCard.DroppedCollections)
            {
                if (val != Vector2Int.zero || ActionRecordRegex.Match(key) is not {Success: true} match) continue;
                Enum.TryParse(match.Groups["stage"].Value, true, out stage);
                int.TryParse(match.Groups["stageInfo"].Value, out stageInfo);
                lastRecord = key;
                break;
            }

            if (stage == TraderStage.empty)
            {
                var _stage = LoadCurrentSlot("TraderEncounterExt_stage") as string;
                var _stageInfo = LoadCurrentSlot("TraderEncounterExt_stageInfo");
                if (_stage != null && _stageInfo is double d)
                {
                    Enum.TryParse(_stage, true, out stage);
                    stageInfo = (int) d;
                }
            }

            switch (stage)
            {
                case TraderStage.ShowItem:
                    if (_Action == 0)
                    {
                        instanceCurrentEnvironmentCard.DroppedCollections.SafeRemove(lastRecord);
                        __instance.ContinueButton.interactable = false;
                        GameManager.Instance.StartCoroutine(ChatEncounterExt.WaitCloseWindow(__instance));
                        GraphicsManager.Instance.CardsDestroyed.Setup(traderEncounter.TradeEndMessage, "你离开了".Local());
                        instanceCurrentEnvironmentCard.DroppedCollections.SafeRemove(lastRecord);
                        IsRunning = false;
                        return;
                    }

                    stage = TraderStage.ShowCost;
                    stageInfo = stageInfo * 4 + _Action - 1;
                    var buySet = traderEncounter.BuySets[stageInfo];
                    __instance.AddLogSeparator();
                    if (buySet.UseBuySetMessage)
                    {
                        __instance.AddToLog(buySet.BuySetMessage);
                    }
                    else
                    {
                        __instance.AddToLog(
                            new EncounterLogMessage(string.Format("要买这个，需要{0}元钱。".Local(),
                                buySet.CalCost(traderEncounter))));
                    }

                    break;
                case TraderStage.ShowCost:
                    switch (_Action)
                    {
                        case 3:
                            instanceCurrentEnvironmentCard.DroppedCollections.SafeRemove(lastRecord);
                            __instance.ContinueButton.interactable = false;
                            GameManager.Instance.StartCoroutine(ChatEncounterExt.WaitCloseWindow(__instance));
                            GraphicsManager.Instance.CardsDestroyed.Setup(traderEncounter.TradeEndMessage,
                                "你离开了".Local());
                            IsRunning = false;
                            instanceCurrentEnvironmentCard.DroppedCollections.SafeRemove(lastRecord);
                            return;
                        case 0:
                            var traderEncounterBuySet = traderEncounter.BuySets[stageInfo];
                            traderEncounterBuySet.BuyResult.FillDropList(false, 1);
                            traderEncounterBuySet.BuyResult.FillStatModsList();
                            GameManager.Instance.StartCoroutine(traderEncounter.CoinSet.CostCoin(
                                    traderEncounterBuySet.CalCost(traderEncounter))
                                .OnEnd(GameManager.Instance.ProduceCards(traderEncounterBuySet.BuyResult, null,
                                    false, false, false)).OnEnd(() =>
                                {
                                    if (traderEncounterBuySet.UseBuyEffect)
                                    {
                                        GameManager.Instance.StartCoroutine(
                                            GameManager.Instance.ActionRoutine(traderEncounterBuySet.BuyEffect,
                                                instanceCurrentEnvironmentCard, false));
                                    }
                                }));
                            __instance.AddLogSeparator();
                            __instance.AddToLog(new EncounterLogMessage(string.Format("你购买了{0}, 花费{1}元钱".Local(),
                                traderEncounterBuySet.GetShowName(), traderEncounterBuySet.CalCost(traderEncounter))));
                            break;
                        case 1:
                            var _buySet = traderEncounter.BuySets[stageInfo];
                            _buySet.BuyResult.FillDropList(false, 10);
                            _buySet.BuyResult.FillStatModsList();
                            GameManager.Instance.StartCoroutine(traderEncounter.CoinSet.CostCoin(
                                    _buySet.CalCost(traderEncounter) * 10)
                                .OnEnd(GameManager.Instance.ProduceCards(_buySet.BuyResult, null,
                                    false, false, false)).OnEnd(() =>
                                {
                                    if (_buySet.UseBuyEffect)
                                    {
                                        GameManager.Instance.StartCoroutine(
                                            GameManager.Instance.ActionRoutine(_buySet.BuyEffect,
                                                instanceCurrentEnvironmentCard, false));
                                    }
                                }));
                            __instance.AddLogSeparator();
                            __instance.AddToLog(new EncounterLogMessage(string.Format("你购买了{0}, 花费{1}元钱".Local(),
                                _buySet.GetShowName() + "*10", _buySet.CalCost(traderEncounter) * 10)));
                            break;
                    }

                    __instance.AddLogSeparator();
                    __instance.AddToLog(new EncounterLogMessage("按下键盘左右方向键换页(AD不行)".Local()));

                    stage = TraderStage.ShowItem;
                    stageInfo = 0;

                    break;
                case TraderStage.empty:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            instanceCurrentEnvironmentCard.DroppedCollections.Remove(lastRecord);
            SaveCurrentSlot("TraderEncounterExt_stage", stage.ToString());
            SaveCurrentSlot("TraderEncounterExt_stageInfo", stageInfo);
            __instance.DisplayPlayerActions();
        }

        public override void ModRoundStart(EncounterPopup __instance, bool _Loaded)
        {
            var encounterModelUniqueID = __instance.CurrentEncounter.EncounterModel.UniqueID;
            var traderEncounter = SimpleTraderEncounter.SimpleTraderEncounters[encounterModelUniqueID];
            IsRunning = true;
            __instance.ActionsPlaying.Clear();
            var onNextRound = EncounterPopup.OnNextRound;
            onNextRound?.Invoke(__instance.CurrentEncounter.CurrentRound);
            if (__instance.ContinueButtonObject.activeSelf)
            {
                __instance.ContinueButtonObject.SetActive(false);
            }

            __instance.AddLogSeparator();
            __instance.AddToLog(new EncounterLogMessage(traderEncounter.CoinSet.Show()));
            __instance.AddLogSeparator();
            __instance.AddToLog(new EncounterLogMessage("按下键盘左右方向键换页(AD不行)".Local()));

            __instance.DisplayPlayerActions();
        }

        public override void UpdateModEx(EncounterPopup __instance)
        {
            var traderEncounter =
                SimpleTraderEncounter.SimpleTraderEncounters[__instance.CurrentEncounter.EncounterModel.UniqueID];
            var instanceCurrentEnvironmentCard = GameManager.Instance.CurrentEnvironmentCard;
            var stage = TraderStage.empty;
            var stageInfo = 0;
            string lastRecord = "";
            var toChange = false;
            foreach (var (key, val) in instanceCurrentEnvironmentCard.DroppedCollections)
            {
                if (val == Vector2Int.zero && ActionRecordRegex.Match(key) is {Success: true} match)
                {
                    Enum.TryParse(match.Groups["stage"].Value, true, out stage);
                    int.TryParse(match.Groups["stageInfo"].Value, out stageInfo);
                    lastRecord = key;
                    break;
                }
            }

            if (stage == TraderStage.empty)
            {
                var _stage = LoadCurrentSlot("TraderEncounterExt_stage") as string;
                var _stageInfo = LoadCurrentSlot("TraderEncounterExt_stageInfo");
                if (_stage != null && _stageInfo is double d)
                {
                    Enum.TryParse(_stage, true, out stage);
                    stageInfo = (int) d;
                }
            }

            if (UnityInput.Current.GetKeyDown(KeyCode.LeftArrow))
            {
                if (stage == TraderStage.ShowItem && stageInfo > 0)
                {
                    toChange = true;
                    stageInfo -= 1;
                }
            }
            else if (UnityInput.Current.GetKeyDown(KeyCode.RightArrow))
            {
                if (stage == TraderStage.ShowItem &&
                    (stageInfo + 1) * 4 < traderEncounter.BuySets.Count)
                {
                    toChange = true;
                    stageInfo += 1;
                }
            }
            else if (UnityInput.Current.GetKeyDown(KeyCode.UpArrow))
            {
                __instance.AddLogSeparator();
                __instance.AddToLog(new EncounterLogMessage(traderEncounter.CoinSet.Show()));
            }

            if (!toChange) return;
            instanceCurrentEnvironmentCard.DroppedCollections.Remove(lastRecord);
            SaveCurrentSlot("TraderEncounterExt_stage", stage.ToString());
            SaveCurrentSlot("TraderEncounterExt_stageInfo", stageInfo);
            __instance.DisplayPlayerActions();
        }

        public enum TraderStage
        {
            ShowItem,
            ShowCost,
            empty
        }
    }
}