using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public static class MoniEnum
{
    public static IEnumerator MoniAddCard<TArg>(this GameManager manager, CardData _Data,
        InGameCardBase? _FromCard, TransferedDurabilities _TransferedDurabilites, SlotInfo forceSlotInfo,
        BlueprintSaveData forceBpData,
        bool _UseDefaultInventory, SpawningLiquid _WithLiquid, Vector2Int _Tick,
        Action<InGameCardBase, TArg>? action, TArg arg,bool pre_init = false)
    {
        var addCard = AddCard();
        var b = true;
        while (b)
        {
            b = addCard.MoveNext();
            yield return addCard.Current;
        }

        yield break;

        IEnumerator FastAddCard(SlotInfo slotInfo, InGameCardBase? container)
        {
            return manager.AddCard(_Data, slotInfo,
                _FromCard != null ? _FromCard.Environment : manager.CurrentEnvironment, container, true,
                _TransferedDurabilites, null, pre_init?new List<StatTriggeredActionStatus>():null, null,
                forceBpData.CurrentStage != -10086 ? forceBpData : null,
                _FromCard != null
                    ? _FromCard.ValidPosition
                    : manager.IsInitializing
                        ? Vector3.zero
                        : manager.GameGraphics.FadeToBlack.TimeSpentPos,
                _UseDefaultInventory, _WithLiquid, false, false, _Tick,
                _FromCard != null ? _FromCard.PrevEnvironment : null,
                _FromCard != null ? _FromCard.PrevEnvTravelIndex : 0);
        }

        IEnumerator AddCard()
        {
            if (_FromCard != null)
            {
                switch (_Data.CardType)
                {
                    case CardTypes.Liquid when _FromCard.IsLiquidContainer:
                        yield return _MoniFunc(FastAddCard(_FromCard.CurrentSlotInfo, _FromCard));
                        break;
                    case CardTypes.Liquid when _FromCard.IsLiquid:
                        yield return _MoniFunc(FastAddCard(_FromCard.CurrentSlotInfo,
                            _FromCard.CurrentContainer));
                        break;
                    case CardTypes.Blueprint when forceSlotInfo.SlotIndex != -10086:
                        yield return _MoniFunc(FastAddCard(forceSlotInfo, _FromCard.CurrentContainer));
                        break;
                    case CardTypes.Blueprint when _FromCard.CurrentSlotInfo.SlotType != SlotsTypes.Blueprint:
                        yield return _MoniFunc(FastAddCard(new SlotInfo(SlotsTypes.Blueprint, -2),
                            _FromCard.CurrentContainer));
                        break;
                    case CardTypes.Blueprint:
                        yield return _MoniFunc(FastAddCard(new SlotInfo(
                                manager.GameGraphics.BlueprintInstanceGoToLocations
                                    ? SlotsTypes.Location
                                    : SlotsTypes.Base, -2),
                            _FromCard.CurrentContainer));
                        break;
                    default:
                        if (_FromCard.CardModel == null || _FromCard.Destroyed || !_FromCard.IsInventoryCard)
                        {
                            yield return _MoniFunc(FastAddCard(_FromCard.CurrentSlotInfo,
                                _FromCard.CurrentContainer));
                        }
                        else if (forceSlotInfo.SlotIndex != -10086)
                        {
                            yield return _MoniFunc(FastAddCard(forceSlotInfo, _FromCard));
                        }
                        else
                        {
                            yield return _MoniFunc(FastAddCard(new SlotInfo(SlotsTypes.Inventory, -2),
                                _FromCard));
                        }

                        break;
                }
            }
            else
            {
                yield return _MoniFunc(FastAddCard(forceSlotInfo, null));
            }
        }

        IEnumerator _MoniFunc(IEnumerator enumerator)
        {
            var next = true;
            while (next)
            {
                var managerLatestCreatedCard = manager.LatestCreatedCards[manager.LatestCreatedCards.Count - 1];
                next = enumerator.MoveNext();
                if (manager.LatestCreatedCards[manager.LatestCreatedCards.Count - 1] is var card &&
                    managerLatestCreatedCard != card && card.CardModel == _Data)
                {
                    action?.Invoke(card, arg);

                    yield return enumerator.Current;
                    break;
                }

                yield return enumerator.Current;
            }

            while (next)
            {
                next = enumerator.MoveNext();
                yield return enumerator.Current;
            }
        }
    }
}