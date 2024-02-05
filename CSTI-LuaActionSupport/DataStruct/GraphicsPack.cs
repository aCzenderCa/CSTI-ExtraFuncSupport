using CSTI_LuaActionSupport.AllPatcher;
using CSTI_LuaActionSupport.Attr;
using CSTI_LuaActionSupport.LuaCodeHelper;
using UnityEngine;

namespace CSTI_LuaActionSupport.DataStruct;

public class GraphicsPack : ScriptableObject
{
    [Note("要使用的特效组的id(暂时只有CFXR)")] [DefaultFieldVal("CFXR")]
    public string fxPack = "";

    [Note("特效本身的id,详见示例中的测试特效卡mod")] [DefaultFieldVal("CFXR _BOOM_")]
    public string fxId = "";

    [Note("将特效生成到触发action的卡牌上")] [DefaultFieldVal(true)]
    public bool genOnReceiveCard;

    [Note("将特效生成到提供给receive卡的卡上(交互过程中被拖上去的卡)")]
    public bool genOnGiveCard;

    [Note("将特效生成到鼠标位置")] public bool genOnMouse;
    [Note("使得特效跟随生成目标(如跟随卡牌,跟随鼠标)")] public bool moveWithGenObj;
    [Note("生成到特殊的指定位置(specialScreenPos)")] public bool genOnSpecial;

    [Note("指定位置(0,0=左下角;0,1=左上角;1,0=右下角;1,1=右上角)")]
    public Vector2 specialScreenPos;

    public void Act(GameManager gameManager, InGameCardBase recCard, InGameCardBase? giveCard)
    {
        switch (fxPack)
        {
            case "CFXR":
                if (genOnReceiveCard)
                {
                    LuaAnim.GenCFXR(new CardAccessBridge(recCard), fxId, moveWithGenObj);
                }
                else if (genOnGiveCard)
                {
                    LuaAnim.GenCFXR(new CardAccessBridge(giveCard), fxId, moveWithGenObj);
                }
                else if (genOnMouse)
                {
                    if (moveWithGenObj)
                    {
                        var tempTable = GetTempTable();
                        tempTable["FollowMouse"] = true;
                        LuaAnim.GenCFXR(new CardAccessBridge(recCard), fxId, false,
                            null, tempTable);
                    }
                    else if (LuaAnim.CurMouse() is { } mouse)
                    {
                        LuaAnim.GenCFXR(mouse, fxId);
                    }
                }
                else if (genOnSpecial)
                {
                    if (LuaAnim.SpecialScreenPos(specialScreenPos) is { } specialScreenPosProvider)
                    {
                        LuaAnim.GenCFXR(specialScreenPosProvider, fxId);
                    }
                }

                break;
        }
    }
}