using System.Collections.Generic;
using System.Linq;
using CSTI_LuaActionSupport.AllPatcher;
using CSTI_LuaActionSupport.Attr;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaCodeHelper;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CSTI_LuaActionSupport.DataStruct;

public class ActionEffectPack : ScriptableObject
{
    [Note("效果是否包含条件")]
    public bool hasCondition;
    [Note("针对包含该action本身卡的条件")]
    public GeneralCondition recCondition;
    [Note("针对交互action时拖到卡上的卡的条件(与原版一样,开启双向不会改变)")]
    public GeneralCondition giveCondition;
    [Note("action要经过的tp(15分钟),最终经过时间为所有效果综合")]
    public int waitTime;
    [Note("action要经过的mini tp(3分钟),最终经过时间为所有效果综合")]
    public int miniWaitTime;
    [Note("action要经过的tick tp(18秒),最终经过时间为所有效果综合")]
    public int tickWaitTime;
    [Note("是否是baseAction(类似营火这样不能随身携带的物体的效果不是base,所以类似探索的action应设置为true)")]
    public bool isNotInBase;
    [Note("生成卡组,与CardAction.ProducedCards类似")]
    public List<CardsDropCollection> dropCollections = new();
    [Note("对状态的修改,注意:rate修改有效")]
    public List<StatModifier> statModifications = new();
    [Note("总是会生成的一些卡(dropCollections则是按权重随机选一个)")]
    public List<CardDrop> alwaysDrops = new();
    [Note("为相应的蓝图研究增加进度,Quantity是要增加的进度,DroppedCard是要增加的蓝图卡")]
    public List<CardDrop> blueprintProgress = new();
    [Note("状态加状态功能,statModByStatBy每一项的值加到对应statModByStatTo项上")]
    public List<GameStat> statModByStatTo = new();
    [Note("状态加状态功能,statModByStatBy每一项的值加到对应statModByStatTo项上")]
    public List<GameStat> statModByStatBy = new();
    [Note("生成状态值张卡,cardDropByStatTo每一项生成对应cardDropByStatBy值向下取整次")]
    public List<CardData> cardDropByStatTo = new();
    [Note("生成状态值张卡,cardDropByStatTo每一项生成对应cardDropByStatBy值向下取整次")]
    public List<GameStat> cardDropByStatBy = new();
    [Note("专门生成蓝图卡,不会解锁蓝图而是生成一个可以建造的卡到location,没有解锁,没有研究的蓝图也能生成")]
    public List<CardDrop> blueprintDrops = new();
    [Note("针对包含该action本身卡的修改(CardAction.ReceivingCardChanges)")]
    public CardStateChange recCardStateChange;
    [Note("针对交互action时拖到卡上的卡的修改(CardOnCardAction.GivenCardChanges)")]
    public CardStateChange giveCardStateChange;
    [Note("如果receive卡本身是exp卡,为其探索进度增加该值,注意:满进度=1.0")]
    public float expProgress;

    public void Act(GameManager gameManager, InGameCardBase recCard, InGameCardBase? giveCard,
        LuaScriptRetValues retValues, CardAction action)
    {
        if (hasCondition)
        {
            if (!recCondition.ConditionsValid(isNotInBase, recCard)) return;
            if (giveCard != null && !giveCondition.ConditionsValid(isNotInBase, giveCard)) return;
        }

        retValues["result"] = waitTime + retValues["result"].TryNum<int>();
        retValues["miniTime"] = miniWaitTime + retValues["miniTime"].TryNum<int>();
        retValues["tickTime"] = tickWaitTime + retValues["tickTime"].TryNum<int>();
        if (dropCollections.Count > 0)
        {
            if (dropCollections.Sum(collection => collection.CollectionWeight) is var sumWeight && sumWeight == 0)
            {
                var rand = Random.Range(0, dropCollections.Count);
                var drop = dropCollections[rand];
                drop.FillStatModsList();
                drop.FillDropList(true, 1);
                gameManager.ProduceCards(drop, recCard, false,
                    recCard.CardModel.CardType == CardTypes.Explorable,
                    false).Add2AllEnumerators(PriorityEnumerators.Normal);
            }
            else
            {
                var rand = Random.Range(0, sumWeight);
                var cur = 0;
                var i = 0;
                for (; i < dropCollections.Count; i++)
                {
                    cur += dropCollections[i].CollectionWeight;
                    if (rand > cur)
                    {
                        break;
                    }
                }

                var drop = dropCollections[i];
                drop.FillStatModsList();
                drop.FillDropList(true, 1);
                gameManager.ProduceCards(drop, recCard, false,
                    recCard.CardModel.CardType == CardTypes.Explorable,
                    false).Add2AllEnumerators(PriorityEnumerators.Normal);
            }
        }

        if (statModifications.Count > 0)
        {
            foreach (var modifier in statModifications)
            {
                gameManager.ChangeStat(modifier, StatModification.Permanent,
                        StatModifierReport.SourceFromAction(action, recCard), action.NoveltyID(recCard),
                        -1, null,
                        null, false)
                    .Add2AllEnumerators(PriorityEnumerators.High);
            }
        }

        if (alwaysDrops.Count > 0)
        {
            foreach (var drop in alwaysDrops)
            {
                if (drop.IsEmpty || !drop.CanDrop) continue;
                var dropAccess = new SimpleUniqueAccess(drop.DroppedCard);
                var rand = Random.Range(drop.Quantity.x, drop.Quantity.y + 1);
                dropAccess.Gen(rand);
            }
        }

        if (blueprintProgress.Count > 0)
        {
            foreach (var bpProgress in blueprintProgress)
            {
                if (bpProgress.DroppedCard.CardType != CardTypes.Blueprint) continue;
                var rand = Random.Range(bpProgress.Quantity.x, bpProgress.Quantity.y + 1);
                new SimpleUniqueAccess(bpProgress.DroppedCard).ProcessBlueprint(rand);
            }
        }

        if (statModByStatTo.Count > 0)
        {
            for (var i = 0; i < statModByStatTo.Count; i++)
            {
                if (statModByStatBy.Count <= i) break;
                new SimpleUniqueAccess(statModByStatTo[i]).StatValue +=
                    new SimpleUniqueAccess(statModByStatBy[i]).StatValue;
            }
        }

        if (cardDropByStatTo.Count > 0)
        {
            for (var i = 0; i < cardDropByStatTo.Count; i++)
            {
                if (cardDropByStatBy.Count <= i) break;
                var by = new SimpleUniqueAccess(cardDropByStatBy[i]);
                var to = new SimpleUniqueAccess(cardDropByStatTo[i]);
                to.Gen((int)by.StatValue);
            }
        }

        if (blueprintDrops.Count > 0)
        {
            var temp = GetTempTable();
            temp["SlotType"] = "Location";
            temp["NeedPreInit"] = true;
            foreach (var bpDrop in blueprintDrops)
            {
                if (bpDrop.DroppedCard.CardType != CardTypes.Blueprint) continue;
                var bpAccess = new SimpleUniqueAccess(bpDrop.DroppedCard);
                var rand = Random.Range(bpDrop.Quantity.x, bpDrop.Quantity.y + 1);
                bpAccess.Gen(rand, temp);
            }
        }

        if (expProgress > 0 && recCard.CardModel.CardType == CardTypes.Explorable)
        {
            new CardAccessBridge(recCard).AddExpProgress(expProgress);
        }

        gameManager.ActionRoutine(
            new CardAction
            {
                ActionName = new LocalizedString { DefaultText = "ByGen" },
                ReceivingCardChanges = recCardStateChange
            }, recCard, false).Add2AllEnumerators(PriorityEnumerators.Normal);
        if (giveCard != null)
        {
            gameManager.ActionRoutine(
                new CardAction
                {
                    ActionName = new LocalizedString { DefaultText = "ByGen" },
                    ReceivingCardChanges = giveCardStateChange
                }, giveCard, false).Add2AllEnumerators(PriorityEnumerators.Normal);
        }
    }
}