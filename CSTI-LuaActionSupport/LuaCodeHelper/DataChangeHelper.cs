using CSTI_LuaActionSupport.AllPatcher;
using UnityEngine;

namespace CSTI_LuaActionSupport.LuaCodeHelper
{
    public static class DataChangeHelper
    {
        public static void ChangeStatValueTo(this GameManager gameManager, InGameStat inGameStat, float val)
        {
            if (val == 0 || inGameStat == null)
            {
                return;
            }

            var preVal = inGameStat.SimpleCurrentValue;
            inGameStat.CurrentBaseValue = val - inGameStat.GlobalModifiedValue -
                                          (gameManager.NotInBase ? 0 : inGameStat.AtBaseModifiedValue);
            if (inGameStat.StatModel.MinMaxValue != Vector2.zero)
            {
                inGameStat.CurrentBaseValue = Mathf.Clamp(inGameStat.CurrentBaseValue,
                    inGameStat.StatModel.MinMaxValue.x, inGameStat.StatModel.MinMaxValue.y);
            }

            CardActionPatcher.Enumerators.Add(gameManager.UpdateStatStatuses(inGameStat, preVal, null));
        }

        public static void ChangeStatRateTo(this GameManager gameManager, InGameStat inGameStat, float val)
        {
            if (val == 0 || inGameStat == null)
            {
                return;
            }

            inGameStat.CurrentBaseRate = val - inGameStat.GlobalModifiedRate -
                                         (gameManager.NotInBase ? 0 : inGameStat.AtBaseModifiedRate);
            if (inGameStat.StatModel.MinMaxRate != Vector2.zero)
            {
                inGameStat.CurrentBaseRate = Mathf.Clamp(inGameStat.CurrentBaseRate,
                    inGameStat.StatModel.MinMaxRate.x, inGameStat.StatModel.MinMaxRate.y);
            }
        }
    }
}