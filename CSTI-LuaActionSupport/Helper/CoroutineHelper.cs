using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CSTI_LuaActionSupport.AllPatcher;
using HarmonyLib;
using UnityEngine;

namespace CSTI_LuaActionSupport.Helper;

public static class CoroutineHelper
{
    public class CoroutineQueue
    {
        public Queue<List<List<IEnumerator>>> AllCoroutines;
        public Queue<CoroutineController> CurCoroutineControllers = new();

        public CoroutineQueue(Queue<List<List<IEnumerator>>> allCoroutines)
        {
            AllCoroutines = allCoroutines;
        }

        public int Count => AllCoroutines.Sum(list => list.Count) +
                            (CurCoroutineControllers?.Count ?? 0);

        public void ProcessAll()
        {
            if (!GameManager.Instance)
            {
                AllCoroutines.Clear();
                CurCoroutineControllers.Clear();
                return;
            }

            if (Count == 0) return;
            var cache = new CoroutineQueue(AllCoroutines)
            {
                CurCoroutineControllers = CurCoroutineControllers
            };
            CurCoroutineControllers.Clear();
            AllCoroutines = new Queue<List<List<IEnumerator>>>();
            Runtime.StartCoroutine(process(cache));
            return;

            IEnumerator process(CoroutineQueue coroutineQueue)
            {
                while (coroutineQueue.Count > 0)
                {
                    var coroutineController = cache.Dequeue();
                    if (coroutineController == null) break;
                    while (coroutineController.state == CoroutineState.Running)
                    {
                        yield return null;
                    }
                }
            }
        }

        public CoroutineController? Dequeue()
        {
            if (!GameManager.Instance)
            {
                AllCoroutines.Clear();
                CurCoroutineControllers.Clear();
                return null;
            }

            if (Count == 0) return null;

            while (CurCoroutineControllers.Count == 0)
            {
                CurCoroutineControllers.Clear();
                var enumerators = AllCoroutines.Peek();
                if (enumerators.Count > 0)
                {
                    foreach (var enumerator in enumerators[0])
                    {
                        GameManager.Instance.StartCoroutineEx(enumerator, out var controller);
                        CurCoroutineControllers.Enqueue(controller);
                    }

                    enumerators.RemoveAt(0);
                    if (enumerators.Count == 0) AllCoroutines.Dequeue();
                }
                else
                {
                    AllCoroutines.Dequeue();
                }
            }

            var coroutineController = CurCoroutineControllers.Dequeue();
            return coroutineController;
        }
    }

    public static CoroutineQueue ProcessCache(this GameManager manager)
    {
        var queue = new Queue<List<List<IEnumerator>>>();
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
        if (popup == null) return;
        if (!popup.isActiveAndEnabled) return;
        if (popup.CurrentCard == null) return;
        if (popup.DescriptionText) popup.DescriptionText.text = popup.CurrentCard.CardDescription();
        if (popup.PopupTitle) popup.PopupTitle.text = popup.CurrentCard.CardName();
        if (popup.CurrentCard == null) return;
        if (popup.CurrentCard.DismantleActions == null) return;
        popup.SetupActions(popup.CurrentCard.DismantleActions, true);
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