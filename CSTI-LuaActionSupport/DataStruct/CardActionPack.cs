using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CSTI_LuaActionSupport.Attr;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaCodeHelper;
using LitJson;
using UnityEngine;

namespace CSTI_LuaActionSupport.DataStruct;

public class CardActionPack : ScriptableObject, IModLoaderJsonObj
{
    private static readonly Dictionary<string, CardActionPack> AllActionPacks = new();
    private static readonly Queue<CardActionPack> ProcessQueue = new();

    [Note("ActionPack的id")] [DefaultFieldVal("CardActionPack_ModId")]
    public string uid = "";

    [Note("针对包含该action本身卡的条件")] public GeneralCondition recCondition;

    [Note("针对交互action时拖到卡上的卡的条件(与原版一样,开启双向不会改变)")]
    public GeneralCondition giveCondition;

    [Note("高级action效果组")] public List<ActionEffectPack> effectPacks = new();

    [Note("action要经过的tp(15分钟),最终经过时间为所有效果综合")]
    public int waitTime;

    [Note("action要经过的mini tp(3分钟),最终经过时间为所有效果综合")]
    public int miniWaitTime;

    [Note("action要经过的tick tp(18秒),最终经过时间为所有效果综合")]
    public int tickWaitTime;

    [Note("是否是baseAction(类似营火这样不能随身携带的物体的效果不是base,所以类似探索的action应设置为true)")]
    public bool isNotInBase;

    [Note("是否在卡牌初始化时执行引用该pack的action,初始化调用时将会忽略时间消耗")]
    public bool actOnCardInit;

    [Note("自动搜索Give卡,比如战斗action中搜索武器卡")] public AutoFindGiveEntry autoFindGive = new();

    [Serializable]
    public class AutoFindGiveEntry
    {
        [Note("是否有效")] public bool active;
        [Note("是否在存在Give卡时仍然进行搜索")] public bool force;
        [Note("要搜索的tag")] public List<CardTag> tags = new();
        [Note("要搜索的card")] public List<CardData> cards = new();
        [Note("对搜索到卡牌的条件")] public GeneralCondition condition;
        [Note("是否需要搜索到最优卡")] public bool needFindBest;
        [Note("用于评价最优卡的通用变量id")] public string bestById = "";
        [Note("是否需要卡槽符合")] public bool needSlot;
        [Note("所需的卡槽")] public SlotsTypes onSlot;

        public InGameCardBase? Find(CardActionPack actionPack, InGameCardBase recCard, InGameCardBase? giveCard)
        {
            if (!active) return giveCard;
            if (!force && giveCard != null) return giveCard;
            if (tags.Count == 0 && cards.Count == 0)
                return null;
            var inGameCardBases = GameManager.Instance.AllCards;
            var best = giveCard;
            var bestVal = float.NegativeInfinity;
            if (giveCard != null) bestVal = SimpleVarModEntry.Id2Val(giveCard, null, bestById);
            if (float.IsNaN(bestVal)) bestVal = float.NegativeInfinity;
            foreach (var card in inGameCardBases)
            {
                if (card == recCard) continue;
                if (tags.All(tag => !card.CardModel.HasTag(tag)) && cards.All(data => card.CardModel != data)) continue;
                if (needSlot && card.CurrentSlotInfo.SlotType != onSlot) continue;
                if (!condition.ConditionsValid(actionPack.isNotInBase, card)) continue;
                if (!needFindBest)
                {
                    return card;
                }

                var thisVal = SimpleVarModEntry.Id2Val(card, null, bestById);
                if (thisVal > bestVal)
                {
                    best = card;
                    bestVal = thisVal;
                }
            }

            return best;
        }
    }

    public static CardActionPack? GetActionPack(string id)
    {
        while (ProcessQueue.Count > 0)
        {
            var cardActionPack = ProcessQueue.Dequeue();
            AllActionPacks[cardActionPack.uid] = cardActionPack;
        }

        return AllActionPacks.SafeGet(id);
    }

    private void OnEnable()
    {
        ProcessQueue.Enqueue(this);
    }

    public IEnumerator ProcessAction(GameManager gameManager, InGameCardBase recCard, InGameCardBase? giveCard,
        CardAction action)
    {
        var retValues = new LuaScriptRetValues();
        Act(gameManager, recCard, giveCard, retValues, action);
        var tp = retValues["result"].TryNum<int>() ?? 0;
        var miniTp = retValues["miniTime"].TryNum<int>() ?? 0;
        var tickTp = retValues["tickTime"].TryNum<int>() ?? 0;
        var coroutineQueue = gameManager.ProcessTime(recCard, tp, miniTp, tickTp).ProcessCache();
        while (coroutineQueue.Count > 0)
        {
            var coroutineController = coroutineQueue.Dequeue();
            if (coroutineController == null) continue;
            while (coroutineController.state == CoroutineState.Running)
            {
                yield return null;
            }
        }
    }

    public void Act(GameManager gameManager, InGameCardBase recCard, InGameCardBase? giveCard,
        LuaScriptRetValues retValues, CardAction action)
    {
        giveCard = autoFindGive.Find(this, recCard, giveCard);
        if (!recCondition.ConditionsValid(isNotInBase, recCard)) return;
        if (giveCard != null && !giveCondition.ConditionsValid(isNotInBase, giveCard)) return;

        retValues["result"] = waitTime;
        retValues["miniTime"] = miniWaitTime;
        retValues["tickTime"] = tickWaitTime;

        foreach (var effectPack in effectPacks)
        {
            effectPack.Act(gameManager, recCard, giveCard, retValues, action);
        }
    }

    public void CreateByJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
        var jsonData = JsonMapper.ToObject(json);
        if (jsonData.ContainsKey(nameof(autoFindGive)) && jsonData[nameof(autoFindGive)] is
                { IsObject: true } jsonDataAutoFindGive)
        {
            JsonUtility.FromJsonOverwrite(jsonDataAutoFindGive.ToJson(), autoFindGive);
        }
    }
}