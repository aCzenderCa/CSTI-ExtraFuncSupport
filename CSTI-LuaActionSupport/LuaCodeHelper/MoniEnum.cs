using System;
using System.Collections;
using UnityEngine;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public static class MoniEnum
{
    public static Func<IEnumerator, IEnumerator> MoniFunc = null!;

    private static Coroutine StartCoroutine_(this GameManager gameManager, IEnumerator enumerator)
    {
        return gameManager.StartCoroutine(MoniFunc(enumerator));
    }

    public static IEnumerator MoniAddCard<TArg>(this GameManager manager, CardData _Data,
        InGameCardBase? _FromCard, TransferedDurabilities _TransferedDurabilites,
        bool _UseDefaultInventory, SpawningLiquid _WithLiquid, Vector2Int _Tick,
        Action<InGameCardBase, TArg> action, TArg arg)
    {
        var addCard = AddCard();
        var b = true;
        while (b)
        {
            MoniFunc = _MoniFunc;
            b = addCard.MoveNext();
            yield return addCard.Current;
        }

        yield break;

        IEnumerator FastAddCard(SlotInfo slotInfo, InGameCardBase container)
        {
            return manager.AddCard(_Data, slotInfo,
                _FromCard != null ? _FromCard.Environment : manager.CurrentEnvironment, container, true,
                _TransferedDurabilites, null, null, null, null,
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
                        yield return manager.StartCoroutine_(FastAddCard(_FromCard.CurrentSlotInfo, _FromCard));
                        break;
                    case CardTypes.Liquid when _FromCard.IsLiquid:
                        yield return manager.StartCoroutine_(FastAddCard(_FromCard.CurrentSlotInfo,
                            _FromCard.CurrentContainer));
                        break;
                    case CardTypes.Blueprint when _FromCard.CurrentSlotInfo.SlotType != SlotsTypes.Blueprint:
                        yield return manager.StartCoroutine_(FastAddCard(new SlotInfo(SlotsTypes.Blueprint, -2),
                            _FromCard.CurrentContainer));
                        break;
                    case CardTypes.Blueprint:
                        yield return manager.StartCoroutine_(FastAddCard(new SlotInfo(
                                manager.GameGraphics.BlueprintInstanceGoToLocations
                                    ? SlotsTypes.Location
                                    : SlotsTypes.Base, -2),
                            _FromCard.CurrentContainer));
                        break;
                    default:
                        if (_FromCard.CardModel == null || _FromCard.Destroyed || !_FromCard.IsInventoryCard)
                        {
                            yield return manager.StartCoroutine_(FastAddCard(_FromCard.CurrentSlotInfo,
                                _FromCard.CurrentContainer));
                        }
                        else
                        {
                            yield return manager.StartCoroutine_(FastAddCard(new SlotInfo(SlotsTypes.Inventory, -2),
                                _FromCard));
                        }

                        break;
                }
            }
            else
            {
                yield return manager.StartCoroutine_(manager.AddCard(_Data, null, manager.CurrentEnvironment, null,
                    true,
                    _TransferedDurabilites, null, null, null, null,
                    manager.IsInitializing ? Vector3.zero : manager.GameGraphics.FadeToBlack.TimeSpentPos,
                    _UseDefaultInventory, _WithLiquid, false, false, _Tick, null, 0));
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
                    action(card, arg);
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