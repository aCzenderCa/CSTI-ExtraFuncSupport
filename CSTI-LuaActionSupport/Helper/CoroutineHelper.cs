using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CSTI_LuaActionSupport.Helper;

public static class CoroutineHelper
{
    public static Queue<CoroutineController> ProcessCache(this GameManager manager)
    {
        var queue = new Queue<CoroutineController>();
        foreach (var enumerator in Enumerators)
        {
            manager.StartCoroutineEx(enumerator, out var controller);
            queue.Enqueue(controller);
        }

        Enumerators.Clear();

        foreach (var popup in Object.FindObjectsOfType<InspectionPopup>())
        {
            popup.ReCommonSetup();
        }

        return queue;
    }

    private static void ReCommonSetup(this InspectionPopup popup)
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