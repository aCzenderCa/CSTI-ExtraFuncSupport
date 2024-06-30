using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using CSTI_LuaActionSupport.Attr;
using CSTI_LuaActionSupport.LuaCodeHelper;

// ReSharper disable InconsistentNaming

namespace CSTI_LuaActionSupport.DataStruct;

[Serializable]
public class CommonCardFinder
{
    [Note("为true时有效")] public bool Active;
    [Note("为true则仅卡槽约束有效")] public bool OnlySlot;
    [Note("卡牌所需要在的槽位类型")] public string SlotsType = "";
    [Note("为true则接受任意card，且忽视CardToFind")] public bool AllowAnyCard;
    [Note("为true则接受任意tag，且忽视TagToFind")] public bool AllowAnyTag;

    [Note("是否需要card与tag同时满足，true为需要")] [DefaultFieldVal(true)]
    public bool NeedBothCardAndTag;

    [Note("所需的卡牌")] public CardData[] CardToFind = [];
    [Note("所需的卡牌tag")] public CardTag[] TagToFind = [];
    [Note("所需满足的通用变量条件")] public SimpleVarCond[] CardConds = [];


    public InGameCardBase? FindFirst()
    {
        foreach (var cardBase in GameManager.Instance.AllCards)
        {
            var card = new CardAccessBridge(cardBase);
            if (!SlotsType.IsNullOrWhiteSpace() && card.SlotType != SlotsType)
            {
                continue;
            }

            if (OnlySlot)
            {
                return cardBase;
            }

            var ctNoPass = 0;
            if (!AllowAnyCard && CardToFind.Length > 0)
            {
                if (!CardToFind.Contains(cardBase.CardModel))
                {
                    ctNoPass += 1;
                }
            }

            if (NeedBothCardAndTag && ctNoPass > 0) continue;

            if (!AllowAnyTag && TagToFind.Length > 0)
            {
                if (TagToFind.All(tag => !cardBase.CardModel.HasTag(tag)))
                {
                    ctNoPass += 1;
                }
            }

            if (NeedBothCardAndTag && ctNoPass > 0) continue;
            if (ctNoPass > 1) continue;
            foreach (var cond in CardConds)
            {
                if (!cond.Check(GameManager.Instance, cardBase, null))
                {
                    goto end;
                }
            }

            return cardBase;
            end: ;
        }

        return null;
    }

    public List<CardAccessBridge> FindAll()
    {
        var cardAccessBridges = new List<CardAccessBridge>();
        foreach (var cardBase in GameManager.Instance.AllCards)
        {
            var card = new CardAccessBridge(cardBase);
            if (!SlotsType.IsNullOrWhiteSpace() && card.SlotType != SlotsType)
            {
                continue;
            }

            if (OnlySlot)
            {
                cardAccessBridges.Add(card);
                continue;
            }

            var ctNoPass = 0;
            if (!AllowAnyCard && CardToFind.Length > 0)
            {
                if (!CardToFind.Contains(cardBase.CardModel))
                {
                    ctNoPass += 1;
                }
            }

            if (NeedBothCardAndTag && ctNoPass > 0) continue;

            if (!AllowAnyTag && TagToFind.Length > 0)
            {
                if (TagToFind.All(tag => !cardBase.CardModel.HasTag(tag)))
                {
                    ctNoPass += 1;
                }
            }

            if (NeedBothCardAndTag && ctNoPass > 0) continue;
            if (ctNoPass > 1) continue;
            foreach (var cond in CardConds)
            {
                if (!cond.Check(GameManager.Instance, cardBase, null))
                {
                    goto end;
                }
            }

            cardAccessBridges.Add(card);
            end: ;
        }

        return cardAccessBridges;
    }
}