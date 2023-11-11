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
        var addCard = manager.AddCard(_Data, _FromCard, _InCurrentEnv, _TransferedDurabilites, _UseDefaultInventory,
            _WithLiquid,
            _Tick, _MoveView);
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
                }

                yield return enumerator.Current;
            }
        }
    }
}