using System;
using System.Collections.Generic;
using System.Linq;
using CSTI_LuaActionSupport.Attr;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaCodeHelper;
using LitJson;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CSTI_LuaActionSupport.DataStruct;

public class ActionEffectPack : ScriptableObject, IModLoaderJsonObj
{
    [Note("效果是否包含条件")] public bool hasCondition;
    [Note("简易通用变量条件表")] public List<SimpleVarCond> simpleVarConditions = new();
    [Note("针对包含该action本身卡的条件")] public GeneralCondition recCondition;

    [Note("针对交互action时拖到卡上的卡的条件(与原版一样,开启双向不会改变)")]
    public GeneralCondition giveCondition;

    [Note("对场上其他牌的修改(根据tag或cardData搜索)")] public List<FindAndActEntry> extActEntries = new();

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

    [Note("对状态的修改,注意:rate修改有效")] public List<StatModifier> statModifications = new();

    [Note("总是会生成的一些卡(dropCollections则是按权重随机选一个)")]
    public List<CardDrop> alwaysDrops = new();

    [Note("为相应的蓝图研究增加进度,Quantity是要增加的进度,DroppedCard是要增加的蓝图卡")]
    public List<CardDrop> blueprintProgress = new();

    [Note("状态加状态功能,statModByStatBy每一项的值加到对应statModByStatTo项上")]
    public List<GameStat> statModByStatTo = new();

    [Note("状态加状态功能,statModByStatBy每一项的值加到对应statModByStatTo项上")]
    public List<GameStat> statModByStatBy = new();

    [Note("状态加状态功能,statModByStatFunc中如果存在对应的项,to=func.x*to+func.y*by+func.z")]
    public List<Vector3> statModByStatFunc = new();

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

    [Note("效果有效时播放的特效(条件满足)")] public GraphicsPack? graphicsPack;

    [Note("通用变量修改,支持卡牌耐久,状态,卡牌变量三者间任意加减互操作")]
    public List<SimpleVarModEntry> simpleVarModEntries = new();

    [Note("如果receive卡本身是exp卡,为其探索进度增加该值,注意:满进度=1.0")]
    public float expProgress;

    public void Act(GameManager gameManager, InGameCardBase recCard, InGameCardBase? giveCard,
        LuaScriptRetValues retValues, CardAction action)
    {
        if (hasCondition)
        {
            if (!recCondition.ConditionsValid(isNotInBase, recCard)) return;
            if (giveCard != null && !giveCondition.ConditionsValid(isNotInBase, giveCard)) return;
            if (!simpleVarConditions.TrueForAll(cond => cond.Check(gameManager, recCard, giveCard)))
                return;
        }

        retValues["result"] = waitTime + retValues["result"].TryNum<int>();
        retValues["miniTime"] = miniWaitTime + retValues["miniTime"].TryNum<int>();
        retValues["tickTime"] = tickWaitTime + retValues["tickTime"].TryNum<int>();

        if (graphicsPack != null)
        {
            graphicsPack.Act(gameManager, recCard, giveCard);
        }

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
                if (statModByStatFunc.Count > i)
                {
                    var to = new SimpleUniqueAccess(statModByStatTo[i]);
                    var by = new SimpleUniqueAccess(statModByStatBy[i]);
                    to.StatValue = statModByStatFunc[i].x * to.StatValue + statModByStatFunc[i].y * by.StatValue +
                                   statModByStatFunc[i].z;
                }
                else
                {
                    new SimpleUniqueAccess(statModByStatTo[i]).StatValue +=
                        new SimpleUniqueAccess(statModByStatBy[i]).StatValue;
                }
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

        if (simpleVarModEntries.Count > 0)
        {
            foreach (var modEntry in simpleVarModEntries)
            {
                modEntry.Act(gameManager, recCard, giveCard);
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

    public void CreateByJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
        var jsonData = JsonMapper.ToObject(json);
        if (jsonData.ContainsKey(nameof(simpleVarModEntries)))
        {
            var data = jsonData[nameof(simpleVarModEntries)];
            if (data.IsArray)
                for (var i = 0; i < data.Count; i++)
                {
                    var simpleVarModEntry = new SimpleVarModEntry();
                    simpleVarModEntry.CreateByJson(data[i].ToJson());
                    simpleVarModEntries.Add(simpleVarModEntry);
                }
        }

        if (jsonData.ContainsKey(nameof(simpleVarConditions)))
        {
            var data = jsonData[nameof(simpleVarConditions)];
            if (data.IsArray)
                for (var i = 0; i < data.Count; i++)
                {
                    simpleVarConditions.Add(JsonUtility.FromJson<SimpleVarCond>(data[i].ToJson()));
                }
        }

        if (jsonData.ContainsKey(nameof(extActEntries)))
        {
            var data = jsonData[nameof(extActEntries)];
            if (data.IsArray)
                for (var i = 0; i < data.Count; i++)
                {
                    extActEntries.Add(JsonUtility.FromJson<FindAndActEntry>(data[i].ToJson()));
                }
        }
    }
}

[Serializable]
public class FindAndActEntry
{
    [Note("是否应用到搜索到的所有卡上")] public bool actAll;
    [Note("是否仅应用到卡槽正确的卡上")] public bool actAllInSlot;

    [Note("是否应用到搜索到的在rec卡里的卡(一般容器内/液体容器里的液体)")]
    public bool actAllInRec;

    [Note("是否应用到搜索到的在give卡里的卡(一般容器内/液体容器里的液体)")]
    public bool actAllInGive;

    [Note("所需的卡槽类型")] public SlotsTypes needSlot;
    [Note("所要搜索的tag")] public CardTag? targetTag;

    [Note("所要搜索的cardData,仅在无targetTag时使用")]
    public CardData? targetCard;

    [Note("要执行的action")] public CardActionPack? actionPack;

    private void Act2Li(List<CardAccessBridge>? cardAccessBridges, GameManager gameManager,
        LuaScriptRetValues retValues, InGameCardBase recCard, InGameCardBase? giveCard,
        CardAction action)
    {
        if (cardAccessBridges == null) return;
        if (actionPack == null) return;
        if (actAll)
        {
            foreach (var card in cardAccessBridges)
            {
                if (card == null || card.CardBase == null) continue;
                if (actAllInSlot && card.CardBase.CurrentSlotInfo.SlotType != needSlot) continue;
                actionPack.Act(gameManager, card.CardBase, null, retValues, action);
            }
        }
        else if (actAllInRec)
        {
            foreach (var card in cardAccessBridges)
            {
                if (card == null || card.CardBase == null) continue;
                if (card.CardBase.CurrentContainer != recCard) continue;
                actionPack.Act(gameManager, card.CardBase, null, retValues, action);
            }
        }
        else if (actAllInGive)
        {
            foreach (var card in cardAccessBridges)
            {
                if (card == null || card.CardBase == null) continue;
                if (card.CardBase.CurrentContainer != giveCard) continue;
                actionPack.Act(gameManager, card.CardBase, null, retValues, action);
            }
        }
        else
        {
            foreach (var card in cardAccessBridges)
            {
                if (card == null || card.CardBase == null) continue;
                if (actAllInSlot && card.CardBase.CurrentSlotInfo.SlotType != needSlot) continue;
                actionPack.Act(gameManager, card.CardBase, null, retValues, action);
                break;
            }
        }
    }

    public void Act(GameManager gameManager, InGameCardBase recCard, InGameCardBase? giveCard,
        LuaScriptRetValues retValues, CardAction action)
    {
        if (actionPack == null) return;
        if (targetTag != null)
        {
            var gameCardsByTag = DataAccessTool.GetGameCardsByTag(targetTag.name);
            Act2Li(gameCardsByTag, gameManager, retValues, recCard, giveCard, action);
        }
        else if (targetCard != null)
        {
            var gameCards = DataAccessTool.GetGameCards(targetCard.UniqueID);
            Act2Li(gameCards, gameManager, retValues, recCard, giveCard, action);
        }
    }
}

[Serializable]
public class SimpleVarCond
{
    [Note("检测范围的变量的id")] [DefaultFieldVal("Special1")]
    public string id = "";

    [Note("需求变量值在的范围,如果x>y则该条件固定有效")] [DefaultFieldValStr("""{"x":1.0,"y":-1.0}""")]
    public Vector2 needRange;

    public bool Check(GameManager gameManager, InGameCardBase recCard, InGameCardBase? giveCard)
    {
        if (needRange.x > needRange.y) return true;
        var id2Val = SimpleVarModEntry.Id2Val(recCard, giveCard, id);
        if (float.IsNaN(id2Val)) return false;
        if (id2Val < needRange.x) return false;
        if (id2Val > needRange.y) return false;
        return true;
    }
}

[Serializable]
public class SimpleVarModEntry : IModLoaderJsonObj
{
    [Note("要修改的变量,默认情况下实现了Sp1=Sp1+Sp1*0")] [DefaultFieldVal("Special1")]
    public string toId = "";

    [Note("to=to*funcTo.x+funcTo.y+...")] [DefaultFieldValStr("""{"x":1.0,"y":0.0}""")]
    public Vector2 modFuncTo;

    [Note("用于修改的变量实体,to+=byEntity.by*byEntity.modFunc")] [DefaultFieldValStr("""[{"byId":"Special1","modFunc":0.0}]""")]
    public List<ByModEntity> byEntities = new();

    [Serializable]
    public struct ByModEntity
    {
        [Note("使用的变量的id")] public string byId;
        [Note("修改系数")] public float modFunc;
        [Note("可变修改系数")] public string modFuncById;
    }

    public static bool SubStrC(string s, string start, out string sub)
    {
        if (!s.StartsWith(start))
        {
            sub = "";
            return false;
        }

        sub = s.Substring(start.Length);
        return true;
    }

    public static float DurType2Val(CardAccessBridge accessBridge, DurabilitiesTypes types)
    {
        return types switch
        {
            DurabilitiesTypes.Spoilage => accessBridge.Spoilage,
            DurabilitiesTypes.Usage => accessBridge.Usage,
            DurabilitiesTypes.Fuel => accessBridge.Fuel,
            DurabilitiesTypes.Progress => accessBridge.Progress,
            DurabilitiesTypes.Liquid => accessBridge.LiquidQuantity,
            DurabilitiesTypes.Special1 => accessBridge.Special1,
            DurabilitiesTypes.Special2 => accessBridge.Special2,
            DurabilitiesTypes.Special3 => accessBridge.Special3,
            DurabilitiesTypes.Special4 => accessBridge.Special4,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public static float Id2Val(InGameCardBase recCard, InGameCardBase? giveCard, string id)
    {
        if (Enum.TryParse<DurabilitiesTypes>(id, out var durType))
        {
            var rec = new CardAccessBridge(recCard);
            return DurType2Val(rec, durType);
        }

        if (giveCard != null && SubStrC(id, "Give|", out var gId) &&
            Enum.TryParse<DurabilitiesTypes>(gId, out var gDurType))
        {
            var give = new CardAccessBridge(giveCard);
            return DurType2Val(give, gDurType);
        }

        if (SubStrC(id, "Stat|", out var sId) &&
            UniqueIDScriptable.GetFromID<GameStat>(sId) is { } stat)
        {
            return new SimpleUniqueAccess(stat).StatValue;
        }

        if (SubStrC(id, "Val|", out var vId))
        {
            var rec = new CardAccessBridge(recCard);
            rec.InitData();
            return rec.Data![vId].TryNum<float>() ?? 0;
        }

        if (giveCard != null && SubStrC(id, "ValGive|", out var vgId))
        {
            var give = new CardAccessBridge(giveCard);
            give.InitData();
            return give.Data![vgId].TryNum<float>() ?? 0;
        }

        if (SubStrC(id, "ConstVal|", out var cvId))
        {
            var timeObjective =
                recCard.CardModel.TimeValues.FirstOrDefault(FindObjective(cvId));
            if (timeObjective != null)
            {
                return timeObjective.Value / 1000.0f;
            }

            return 0;
        }

        if (giveCard != null && SubStrC(id, "ConstValGive|", out var cvgId))
        {
            var timeObjective = giveCard.CardModel.TimeValues.FirstOrDefault(FindObjective(cvgId));
            if (timeObjective != null)
            {
                return timeObjective.Value / 1000.0f;
            }

            return 0;
        }

        if (id == "Random")
        {
            return Random.value;
        }

        return float.NaN;

        Func<TimeObjective, bool> FindObjective(string objId)
        {
            return objective => objective.ObjectiveName == objId;
        }
    }

    public void Act(GameManager gameManager, InGameCardBase recCard, InGameCardBase? giveCard)
    {
        var rawVal = Id2Val(recCard, giveCard, toId);
        if (float.IsNaN(rawVal)) return;
        if (Enum.TryParse<DurabilitiesTypes>(toId, out var durType))
        {
            var rec = new CardAccessBridge(recCard);
            var val = CalcModVal(recCard, giveCard, rawVal);

            if (Math.Abs(val - rawVal) > 0.001)
                rec.ModifyDurability(val, durType).Add2AllEnumerators(PriorityEnumerators.High);
            return;
        }


        if (SubStrC(toId, "Give|", out var gId) &&
            Enum.TryParse<DurabilitiesTypes>(gId, out var gDurType))
        {
            var give = new CardAccessBridge(giveCard);
            var val = CalcModVal(recCard, giveCard, rawVal);

            if (Math.Abs(val - rawVal) > 0.001)
                give.ModifyDurability(val, gDurType).Add2AllEnumerators(PriorityEnumerators.High);
            return;
        }

        if (SubStrC(toId, "Stat|", out var sId) &&
            UniqueIDScriptable.GetFromID<GameStat>(sId) is { } stat)
        {
            var statAccess = new SimpleUniqueAccess(stat);
            var val = CalcModVal(recCard, giveCard, rawVal);

            if (Math.Abs(val - rawVal) > 0.001) statAccess.StatValue = val;
            return;
        }

        if (SubStrC(toId, "Val|", out var vId))
        {
            var rec = new CardAccessBridge(recCard);
            var val = CalcModVal(recCard, giveCard, rawVal);

            rec.InitData();
            rec.Data![vId] = (double)val;
            rec.SaveData();

            return;
        }

        if (SubStrC(toId, "ValGive|", out var vgId))
        {
            var give = new CardAccessBridge(giveCard);
            var val = CalcModVal(recCard, giveCard, rawVal);

            give.InitData();
            give.Data![vgId] = (double)val;
            give.SaveData();

            return;
        }
    }

    private float CalcModVal(InGameCardBase recCard, InGameCardBase? giveCard, float rawVal)
    {
        var val = rawVal * modFuncTo.x + modFuncTo.y;
        for (var index = 0; index < byEntities.Count; index++)
        {
            var byId = byEntities[index].byId;
            var modFunc = byEntities[index].modFunc;
            var modFuncById = byEntities[index].modFuncById;
            var id2Val = Id2Val(recCard, giveCard, byId);
            var modFuncByIdVal = Id2Val(recCard, giveCard, modFuncById);
            if (float.IsNaN(id2Val)) continue;
            if (!float.IsNaN(modFuncByIdVal))
            {
                val += id2Val * modFuncByIdVal;
            }
            else
            {
                val += id2Val * modFunc;
            }
        }

        return val;
    }

    public void CreateByJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
        var jsonData = JsonMapper.ToObject(json);
        if (jsonData.ContainsKey(nameof(byEntities)))
        {
            var eData = jsonData[nameof(byEntities)];
            if (eData.IsArray)
            {
                for (int i = 0; i < eData.Count; i++)
                {
                    byEntities.Add(JsonUtility.FromJson<ByModEntity>(eData[i].ToJson()));
                }
            }
        }
    }
}