using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using ChatTreeLoader.LocalText;
using UnityEngine;

namespace ChatTreeLoader.ScriptObjects
{
    [Serializable]
    public class SimpleTraderEncounter : ModEncounterTypedBase<SimpleTraderEncounter>
    {
        public static readonly Dictionary<string, SimpleTraderEncounter> SimpleTraderEncounters = new();
        public string ThisId;
        public CoinSet CoinSet;
        public float CoinCostBase = 1;
        public GameStat CoinCostBaseByStat;
        public List<BuySet> BuySets;
        public LocalizedString TradeEndMessage;

        public override Dictionary<string, SimpleTraderEncounter> GetValidEncounterTable()
        {
            return SimpleTraderEncounters;
        }

        public override void OnEnable()
        {
            if (AllModEncounters.TryGetValue(typeof(SimpleTraderEncounter), out var modEncounterBases))
            {
                modEncounterBases.Add(this);
            }
            else
            {
                AllModEncounters[typeof(SimpleTraderEncounter)] = new List<ModEncounterBase> {this};
            }
        }

        public override void Init()
        {
            if (HadInit)
            {
                return;
            }

            if (ThisId.IsNullOrWhiteSpace()) return;
            HadInit = true;
            SimpleTraderEncounters[ThisId] = this;
        }
    }

    [Serializable]
    public class BuySet : ScriptableObject
    {
        public string Name;
        public LocalizedString NameLocal;
        public float CoinCostFactor = 1;
        public CardsDropCollection BuyResult;
        public CardAction BuyEffect;
        public bool UseBuyEffect;
        public GeneralCondition BuyCondition;
        public GeneralCondition BuyShowCondition;
        public EncounterLogMessage BuySetMessage;
        public bool UseBuySetMessage;

        public string GetShowName()
        {
            if (!NameLocal.ToString().IsNullOrWhiteSpace())
            {
                return NameLocal.ToString();
            }

            if (!Name.IsNullOrWhiteSpace())
            {
                return Name;
            }

            var showNameBuilder = new StringBuilder();
            foreach (var cardDrop in BuyResult.DroppedCards)
            {
                showNameBuilder.Append($"{cardDrop.DroppedCard.CardName}*{cardDrop.Quantity.x};");
            }

            return showNameBuilder.ToString();
        }

        public int CalCost(SimpleTraderEncounter traderEncounter)
        {
            return (int) (traderEncounter.CoinCostBaseByStat
                ? GameManager.Instance.StatsDict[traderEncounter.CoinCostBaseByStat]
                    .SimpleCurrentValue * CoinCostFactor
                : traderEncounter.CoinCostBase *
                  CoinCostFactor);
        }
    }

    [Serializable]
    public class CoinSet : ScriptableObject
    {
        public List<CardDrop> CoinData = new();
        public List<GameStat> CoinStat = new();

        public bool ValidCoins(int count)
        {
            return GameManager.Instance.AllCards.Select(card =>
                        (card, coin: CoinData.FirstOrDefault(drop => drop.DroppedCard == card.CardModel).Quantity.x))
                    .Where(tuple => tuple.coin != 0).Sum(tuple => tuple.coin) +
                CoinStat.Sum(stat => GameManager.Instance.StatsDict[stat].SimpleCurrentValue) >= count;
        }

        public IEnumerator CostCoin(int count)
        {
            var valueTuples = GameManager.Instance.AllCards.Select(card =>
                    (card, coin: CoinData.FirstOrDefault(drop => drop.DroppedCard == card.CardModel).Quantity.x))
                .Where(tuple => tuple.coin != 0).OrderByDescending(tuple => tuple.coin).ToList();
            var toRemove = new List<InGameCardBase>();
            var hadPay = 0f;
            foreach (var tuple in valueTuples.Where(tuple => hadPay + tuple.coin <= count))
            {
                hadPay += tuple.coin;
                toRemove.Add(tuple.card);
            }

            var waitQueue = new Queue<CoroutineController>();
            foreach (var cardBase in toRemove)
            {
                GameManager.Instance.StartCoroutineEx(GameManager.Instance.RemoveCard(cardBase, false, true),
                    out var controller);
                waitQueue.Enqueue(controller);
            }

            if (hadPay < count)
            {
                foreach (var inGameStat in CoinStat.Select(stat => GameManager.Instance.StatsDict[stat]))
                {
                    if (inGameStat.SimpleCurrentValue + hadPay <= count)
                    {
                        GameManager.Instance.StartCoroutineEx(
                            GameManager.Instance.ChangeStatValue(inGameStat, -inGameStat.SimpleCurrentValue,
                                StatModification.Permanent), out var controller);
                        hadPay += inGameStat.SimpleCurrentValue;
                        waitQueue.Enqueue(controller);
                    }
                    else
                    {
                        GameManager.Instance.StartCoroutineEx(
                            GameManager.Instance.ChangeStatValue(inGameStat, -(count - hadPay),
                                StatModification.Permanent), out var controller);
                        hadPay = count;
                        waitQueue.Enqueue(controller);
                        break;
                    }
                }
            }

            foreach (var controller in waitQueue)
            {
                while (controller.state != CoroutineState.Finished)
                {
                    yield return null;
                }
            }
        }

        public string Show()
        {
            var coinSetOut = new StringBuilder();
            foreach (var cardDrop in CoinData)
            {
                coinSetOut.AppendFormat("一个{0}算{1}元钱,".Local(), cardDrop.DroppedCard.CardName, cardDrop.Quantity.x);
            }

            if (CoinStat.Count > 0)
            {
                coinSetOut.Append("\n");
                coinSetOut.Append("以下状态每一点算一元钱\n".Local());

                foreach (var gameStat in CoinStat)
                {
                    coinSetOut.AppendFormat("{0} ", gameStat.GameName);
                }
            }

            coinSetOut.Append("\n按下键盘上方向键再次显示".Local());
            return coinSetOut.ToString();
        }
    }
}