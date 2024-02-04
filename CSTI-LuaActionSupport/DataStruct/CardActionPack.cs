using System.Collections;
using System.Collections.Generic;
using CSTI_LuaActionSupport.Attr;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaCodeHelper;
using UnityEngine;

namespace CSTI_LuaActionSupport.DataStruct;

public class CardActionPack : ScriptableObject
{
    private static readonly Dictionary<string, CardActionPack> AllActionPacks = new();
    private static readonly Queue<CardActionPack> ProcessQueue = new();

    [Note("ActionPack的id")]
    public string uid = "";
    [Note("针对包含该action本身卡的条件")]
    public GeneralCondition recCondition;
    [Note("针对交互action时拖到卡上的卡的条件(与原版一样,开启双向不会改变)")]
    public GeneralCondition giveCondition;
    [Note("高级action效果组")]
    public List<ActionEffectPack> effectPacks = new();
    [Note("action要经过的tp(15分钟),最终经过时间为所有效果综合")]
    public int waitTime;
    [Note("action要经过的mini tp(3分钟),最终经过时间为所有效果综合")]
    public int miniWaitTime;
    [Note("action要经过的tick tp(18秒),最终经过时间为所有效果综合")]
    public int tickWaitTime;
    [Note("是否是baseAction(类似营火这样不能随身携带的物体的效果不是base,所以类似探索的action应设置为true)")]
    public bool isNotInBase;

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
}