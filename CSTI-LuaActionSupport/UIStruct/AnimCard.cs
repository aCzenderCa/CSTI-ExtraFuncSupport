using System.Collections.Generic;
using CSTI_LuaActionSupport.LuaCodeHelper;
using UnityEngine;

namespace CSTI_LuaActionSupport.UIStruct;

[RequireComponent(typeof(InGameCardBase))]
public class AnimCard : MonoBehaviour
{
    public InGameCardBase CardBase = null!;
    public List<string>? AnimList;
    public List<float> AnimTimeList;

    private int _CurAnimIndex;

    public int CurAnimIndex
    {
        get => _CurAnimIndex;
        set
        {
            if (_CurAnimIndex != value && AnimList != null && AnimTimeList != null &&
                SpriteDict.TryGetValue(AnimList[value], out var sprite))
            {
                CardBase.CardVisuals.CardImage.overrideSprite = sprite;
            }

            _CurAnimIndex = value;
        }
    }

    public float CurTime;
    private readonly LuaTimer.SimpleTimer SimpleTimer = new(0.02f, 0);

    public void Init(List<string>? animList, List<float> animTimeList)
    {
        CurTime = 0;
        _CurAnimIndex = 0;
        SimpleTimer.CurTime = 0;
        AnimList = animList;
        AnimTimeList = animTimeList;
    }

    private void Awake()
    {
        CardBase = gameObject.GetComponent<InGameCardBase>();
    }

    private void Update()
    {
        if (CardBase.Destroyed) Destroy(this);
    }

    private void LateUpdate()
    {
        if (!CardBase.VisibleOnScreen) return;
        if (AnimList == null || AnimTimeList == null)
        {
            if (SimpleTimer.Step()) CardBase.CardVisuals.Setup(CardBase);
            return;
        }

        CurTime += Time.deltaTime;
        if (CurTime < AnimTimeList[CurAnimIndex]) return;
        CurTime -= AnimTimeList[CurAnimIndex];
        CurAnimIndex = (CurAnimIndex + 1) % AnimTimeList.Count;
    }
}