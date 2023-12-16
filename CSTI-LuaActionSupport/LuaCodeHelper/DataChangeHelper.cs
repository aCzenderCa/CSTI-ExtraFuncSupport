using System;
using CSTI_LuaActionSupport.AllPatcher;
using UnityEngine;
using static CSTI_LuaActionSupport.AllPatcher.LuaRegister;

namespace CSTI_LuaActionSupport.LuaCodeHelper;

public static class DataChangeHelper
{
    public static void ChangeStatValueTo(this GameManager gameManager, InGameStat inGameStat, float _Value)
    {
        if (inGameStat == null) return;

        var preVal = inGameStat.SimpleCurrentValue;

        if (Register.TryGet(nameof(GameManager), nameof(GameManager.ChangeStatValue),
                inGameStat.StatModel.UniqueID, out var luaFunctions))
        {
            var mod = _Value - preVal;
            foreach (var luaFunction in luaFunctions)
            {
                try
                {
                    luaFunction.Call(gameManager, inGameStat, mod, StatModification.Permanent);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }

        inGameStat.CurrentBaseValue = _Value - inGameStat.GlobalModifiedValue -
                                      (gameManager.NotInBase ? 0 : inGameStat.AtBaseModifiedValue);
        if (inGameStat.StatModel.MinMaxValue != Vector2.zero)
        {
            inGameStat.CurrentBaseValue = Mathf.Clamp(inGameStat.CurrentBaseValue,
                inGameStat.StatModel.MinMaxValue.x, inGameStat.StatModel.MinMaxValue.y);
        }

        CardActionPatcher.Enumerators.Add(gameManager.UpdateStatStatuses(inGameStat, preVal, null));
    }

    public static void ChangeStatRateTo(this GameManager gameManager, InGameStat inGameStat, float _Rate)
    {
        if (inGameStat == null) return;

        if (Register.TryGet(nameof(GameManager), nameof(GameManager.ChangeStatRate),
                inGameStat.StatModel.UniqueID, out var luaFunctions))
        {
            var mod = _Rate - inGameStat.SimpleRatePerTick;
            foreach (var luaFunction in luaFunctions)
            {
                try
                {
                    luaFunction.Call(gameManager, inGameStat, mod, StatModification.Permanent);
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }

        inGameStat.CurrentBaseRate = _Rate - inGameStat.GlobalModifiedRate -
                                     (gameManager.NotInBase ? 0 : inGameStat.AtBaseModifiedRate);
        if (inGameStat.StatModel.MinMaxRate != Vector2.zero)
        {
            inGameStat.CurrentBaseRate = Mathf.Clamp(inGameStat.CurrentBaseRate,
                inGameStat.StatModel.MinMaxRate.x, inGameStat.StatModel.MinMaxRate.y);
        }
    }

    public static OptionalFloatValue Copy(this OptionalFloatValue optionalFloatValue)
    {
        return new OptionalFloatValue(optionalFloatValue.Active, optionalFloatValue.FloatValue);
    }
}