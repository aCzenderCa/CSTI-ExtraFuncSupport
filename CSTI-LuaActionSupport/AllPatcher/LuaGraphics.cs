using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaCodeHelper;
using UnityEngine;

namespace CSTI_LuaActionSupport.AllPatcher;

public static class LuaGraphics
{
    [LuaFunc]
    public static void UpdateCards()
    {
        foreach (var g in Object.FindObjectsOfType<CardGraphics>())
        {
            if (g.CardLogic) g.Setup(g.CardLogic);
        }
    }

    [LuaFunc]
    public static void UpdatePopup()
    {
        foreach (var popup in Object.FindObjectsOfType<InspectionPopup>())
        {
            popup.ReCommonSetup();
        }
    }
}