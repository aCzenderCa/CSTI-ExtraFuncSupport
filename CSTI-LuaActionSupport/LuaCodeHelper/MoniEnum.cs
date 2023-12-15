using System;
using System.Collections;
using UnityEngine;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public static class MoniEnum
{
    public static bool OnMoniAddCard;
    public static Func<IEnumerator, IEnumerator> MoniFunc = null!;

    public static IEnumerator MoniAddCard<TArg>(this GameManager manager, CardData _Data,
        InGameCardBase? _FromCard, bool _InCurrentEnv, TransferedDurabilities _TransferedDurabilites,
        bool _UseDefaultInventory, SpawningLiquid _WithLiquid, Vector2Int _Tick, bool _MoveView,
        Action<InGameCardBase, TArg> action, TArg arg)
    {
        var addCard = AddCard();
        var b = true;
        while (b)
        {
            OnMoniAddCard = true;
            MoniFunc = _MoniFunc;
            b = addCard.MoveNext();
            OnMoniAddCard = false;
            yield return addCard.Current;
        }

        yield break;

        IEnumerator AddCard()
        {
            if (_FromCard != null)
            {
                switch (_Data.CardType)
                {
                    case CardTypes.Liquid when _FromCard.IsLiquidContainer:
                        yield return manager.StartCoroutine(manager.AddCard(_Data, _FromCard.CurrentSlotInfo,
                            _FromCard.Environment, _FromCard, _InCurrentEnv, _TransferedDurabilites, null, null, null,
                            null, _FromCard.ValidPosition, _UseDefaultInventory, _WithLiquid, _MoveView, false, _Tick,
                            _FromCard.PrevEnvironment, _FromCard.PrevEnvTravelIndex));
                        break;
                    case CardTypes.Liquid:
                    {
                        if (_FromCard.IsLiquid)
                        {
                            yield return manager.StartCoroutine(manager.AddCard(_Data, _FromCard.CurrentSlotInfo,
                                _FromCard.Environment, _FromCard.CurrentContainer, _InCurrentEnv, _TransferedDurabilites,
                                null, null, null, null, _FromCard.ValidPosition, _UseDefaultInventory, _WithLiquid,
                                _MoveView, false, _Tick, _FromCard.PrevEnvironment, _FromCard.PrevEnvTravelIndex));
                        }

                        break;
                    }
                    case CardTypes.Blueprint when _FromCard.CurrentSlotInfo.SlotType != SlotsTypes.Blueprint:
                        yield return manager.StartCoroutine(manager.AddCard(_Data,
                            new SlotInfo(SlotsTypes.Blueprint, -2),
                            _FromCard.Environment, _FromCard.CurrentContainer, _InCurrentEnv, _TransferedDurabilites,
                            null, null, null, null, _FromCard.ValidPosition, _UseDefaultInventory, _WithLiquid,
                            _MoveView, false, _Tick, _FromCard.PrevEnvironment, _FromCard.PrevEnvTravelIndex));
                        break;
                    case CardTypes.Blueprint:
                        yield return manager.StartCoroutine(manager.AddCard(_Data,
                            new SlotInfo(
                                manager.GameGraphics.BlueprintInstanceGoToLocations
                                    ? SlotsTypes.Location
                                    : SlotsTypes.Base, -2), _FromCard.Environment, _FromCard.CurrentContainer,
                            _InCurrentEnv, _TransferedDurabilites, null, null, null, null, _FromCard.ValidPosition,
                            _UseDefaultInventory, _WithLiquid, _MoveView, false, _Tick, _FromCard.PrevEnvironment,
                            _FromCard.PrevEnvTravelIndex));
                        break;
                    default:
                    {
                        if (_FromCard.CardModel == null || _FromCard.Destroyed || !_FromCard.IsInventoryCard)
                        {
                            yield return manager.StartCoroutine(manager.AddCard(_Data, _FromCard.CurrentSlotInfo,
                                _FromCard.Environment, _FromCard.CurrentContainer, _InCurrentEnv, _TransferedDurabilites,
                                null, null, null, null, _FromCard.ValidPosition, _UseDefaultInventory, _WithLiquid,
                                _MoveView, false, _Tick, _FromCard.PrevEnvironment, _FromCard.PrevEnvTravelIndex));
                        }
                        else
                        {
                            yield return manager.StartCoroutine(manager.AddCard(_Data,
                                new SlotInfo(SlotsTypes.Inventory, -2),
                                _FromCard.Environment, _FromCard, _InCurrentEnv, _TransferedDurabilites, null, null, null,
                                null, _FromCard.ValidPosition, _UseDefaultInventory, _WithLiquid, _MoveView, false, _Tick,
                                _FromCard.PrevEnvironment, _FromCard.PrevEnvTravelIndex));
                        }

                        break;
                    }
                }
            }
            else
            {
                yield return manager.StartCoroutine(manager.AddCard(_Data, null, manager.CurrentEnvironment, null,
                    _InCurrentEnv,
                    _TransferedDurabilites, null, null, null, null,
                    manager.IsInitializing ? Vector3.zero : manager.GameGraphics.FadeToBlack.TimeSpentPos,
                    _UseDefaultInventory, _WithLiquid, _MoveView, false, _Tick, null, 0));
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