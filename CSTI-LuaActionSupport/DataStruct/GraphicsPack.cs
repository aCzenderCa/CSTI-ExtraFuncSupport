using System;
using System.Collections.Generic;
using BepInEx;
using CSTI_LuaActionSupport.AllPatcher;
using CSTI_LuaActionSupport.Attr;
using CSTI_LuaActionSupport.LuaCodeHelper;
using LitJson;
using UnityEngine;

namespace CSTI_LuaActionSupport.DataStruct;

public class GraphicsPack : ScriptableObject, IModLoaderJsonObj
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

    [Note("子特效表,可以复用一些字段")] public List<SubGraphics> subGraphicsList = new();

    public void Act(GameManager gameManager, InGameCardBase recCard, InGameCardBase? giveCard)
    {
        new GenGraphicsData(fxPack, fxId, genOnReceiveCard, genOnGiveCard, genOnMouse, moveWithGenObj, genOnSpecial,
            specialScreenPos).Gen(recCard, giveCard);
        foreach (var subGraphics in subGraphicsList)
        {
            subGraphics.Act(this, gameManager, recCard, giveCard);
        }
    }

    public void CreateByJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);
        var jsonData = JsonMapper.ToObject(json);
        if (jsonData.ContainsKey(nameof(subGraphicsList)))
        {
            var data = jsonData[nameof(subGraphicsList)];
            if (data.IsArray)
                for (var i = 0; i < data.Count; i++)
                {
                    subGraphicsList.Add(JsonUtility.FromJson<SubGraphics>(data[i].ToJson()));
                }
        }
    }
}

public readonly struct GenGraphicsData
{
    public readonly string FXPack;
    public readonly string FXId;
    public readonly bool GenOnReceiveCard;
    public readonly bool GenOnGiveCard;
    public readonly bool GenOnMouse;
    public readonly bool MoveWithGenObj;
    public readonly bool GenOnSpecial;
    public readonly Vector2 SpecialScreenPos;

    public GenGraphicsData(string fxPack, string fxId, bool genOnReceiveCard, bool genOnGiveCard, bool genOnMouse,
        bool moveWithGenObj, bool genOnSpecial, Vector2 specialScreenPos)
    {
        FXPack = fxPack;
        FXId = fxId;
        GenOnReceiveCard = genOnReceiveCard;
        GenOnGiveCard = genOnGiveCard;
        GenOnMouse = genOnMouse;
        MoveWithGenObj = moveWithGenObj;
        GenOnSpecial = genOnSpecial;
        SpecialScreenPos = specialScreenPos;
    }

    public void Gen(InGameCardBase recCard, InGameCardBase? giveCard)
    {
        switch (FXPack)
        {
            case "CFXR":
                if (GenOnReceiveCard)
                {
                    LuaAnim.GenCFXR(new CardAccessBridge(recCard), FXId, MoveWithGenObj);
                }
                else if (GenOnGiveCard)
                {
                    LuaAnim.GenCFXR(new CardAccessBridge(giveCard), FXId, MoveWithGenObj);
                }
                else if (GenOnMouse)
                {
                    if (MoveWithGenObj)
                    {
                        var tempTable = GetTempTable();
                        tempTable["FollowMouse"] = true;
                        LuaAnim.GenCFXR(new CardAccessBridge(recCard), FXId, false,
                            null, tempTable);
                    }
                    else if (LuaAnim.CurMouse() is { } mouse)
                    {
                        LuaAnim.GenCFXR(mouse, FXId);
                    }
                }
                else if (GenOnSpecial)
                {
                    if (LuaAnim.SpecialScreenPos(SpecialScreenPos) is { } specialScreenPosProvider)
                    {
                        LuaAnim.GenCFXR(specialScreenPosProvider, FXId);
                    }
                }

                break;
        }
    }
}

[Serializable]
public class SubGraphics
{
    [Note("要使用的特效组的id,注:该字段为空则重用GraphicsPack中的值")]
    public string fxPack = "";

    [Note("特效本身的id,注:该字段为空则重用GraphicsPack中的值")]
    public string fxId = "";

    [Note("将特效生成到触发action的卡牌上")] public bool genOnReceiveCard;

    [Note("将特效生成到提供给receive卡的卡上(交互过程中被拖上去的卡)")]
    public bool genOnGiveCard;

    [Note("将特效生成到鼠标位置")] public bool genOnMouse;
    [Note("使得特效跟随生成目标(如跟随卡牌,跟随鼠标)")] public bool moveWithGenObj;
    [Note("生成到特殊的指定位置(specialScreenPos)")] public bool genOnSpecial;

    [Note("指定位置(0,0=左下角;0,1=左上角;1,0=右下角;1,1=右上角)")]
    public Vector2 specialScreenPos;

    public void Act(GraphicsPack mainGraphics, GameManager gameManager, InGameCardBase recCard,
        InGameCardBase? giveCard)
    {
        new GenGraphicsData(fxPack.IsNullOrWhiteSpace() ? mainGraphics.fxPack : fxPack,
            fxId.IsNullOrWhiteSpace() ? mainGraphics.fxId : fxId,
            genOnReceiveCard, genOnGiveCard, genOnMouse, moveWithGenObj,
            genOnSpecial, specialScreenPos
        ).Gen(recCard, giveCard);
    }
}