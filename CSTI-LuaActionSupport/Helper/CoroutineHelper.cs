using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CSTI_LuaActionSupport.AllPatcher;
using UnityEngine;

namespace CSTI_LuaActionSupport.Helper;

public static class CoroutineHelper
{
    public class CoroutineQueue(Queue<List<IEnumerator>> allCoroutines)
    {
        public readonly Queue<List<IEnumerator>> AllCoroutines = allCoroutines;
        public Queue<CoroutineController>? CurCoroutineControllers;

        public int Count => AllCoroutines.Sum(list => list.Count) +
                            (CurCoroutineControllers?.Count ?? 0);

        public CoroutineController? Dequeue()
        {
            if (!GameManager.Instance)
            {
                AllCoroutines.Clear();
                CurCoroutineControllers = null;
                return null;
            }

            if (Count == 0) return null;

            while (CurCoroutineControllers == null || CurCoroutineControllers.Count == 0)
            {
                CurCoroutineControllers = new Queue<CoroutineController>();
                var enumerators = AllCoroutines.Dequeue();
                foreach (var enumerator in enumerators)
                {
                    GameManager.Instance.StartCoroutineEx(enumerator, out var controller);
                    CurCoroutineControllers.Enqueue(controller);
                }
            }

            var coroutineController = CurCoroutineControllers.Dequeue();
            if (CurCoroutineControllers.Count == 0)
            {
                CurCoroutineControllers = null;
            }

            return coroutineController;
        }
    }

    public static CoroutineQueue ProcessCache(this GameManager manager)
    {
        var queue = new Queue<List<IEnumerator>>();
        foreach (var enumerators in from pEnumerators in AllEnumerators
                 orderby pEnumerators.Key descending
                 select pEnumerators.Value)
        {
            queue.Enqueue(enumerators.ThisEnumerators.ToList());
            enumerators.ThisEnumerators.Clear();
        }

        LuaGraphics.UpdatePopup();

        return new CoroutineQueue(queue);
    }

    public static void ReCommonSetup(this InspectionPopup popup)
    {
        if (popup.isActiveAndEnabled && popup.CurrentCard)
        {
            popup.CommonSetup(popup.CurrentCard.CardName(), popup.CurrentCard.CardDescription());
        }
    }

    public static IEnumerator SpendDaytimePoints(this GameManager manager, int tp, InGameCardBase _ReceivingCard)
    {
        return manager.SpendDaytimePoints(tp, true, true, false, _ReceivingCard,
            FadeToBlackTypes.None, "", false, false, null, null, null);
    }

    public static CoroutineController Start(this IEnumerator enumerator, MonoBehaviour monoBehaviour)
    {
        monoBehaviour.StartCoroutineEx(enumerator, out var controller);
        return controller;
    }
}