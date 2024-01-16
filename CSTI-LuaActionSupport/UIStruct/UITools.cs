using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CSTI_LuaActionSupport.UIStruct;

[HarmonyPatch]
public static class UITools
{
    public static void SafeSetActive(this GameObject o, bool active)
    {
        if (active != o.activeSelf) o.SetActive(active);
    }

    public static readonly GameObject BaseGameObject;
    public static readonly Canvas BaseCanvas;
    public static readonly RectTransform BaseCanvasTransform;
    public static readonly RectTransform BaseCanvasContentTransform;
    public static readonly CanvasScaler BaseCanvasScale;
    public static readonly List<UIStructs.MyCardSlot> GMUpdateBlock = new();

    [HarmonyPrefix, HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    public static bool GMUpdate()
    {
        return GMUpdateBlock.Count == 0 || !UnityInput.Current.GetMouseButton(0);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GameManager), nameof(GameManager.AddCardToDraggedStack))]
    public static bool GameManager_AddCardToDraggedStack()
    {
        if (GameManager.DraggedCard == null) return true;
        var myCardSlotUITag = GameManager.DraggedCard.GetComponentInParent<UIStructs.MyCardSlot.MyCardSlotUITag>();
        if (myCardSlotUITag == null) return true;
        var slot = myCardSlotUITag.Slot;
        if (slot.PileCard.Count > slot.NextDrag)
        {
            var card = (InGameDraggableCard) slot.PileCard[slot.NextDrag];
            if (card)
            {
                card.OnBeginDrag(null);
            }

            slot.NextDrag += 1;
        }

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GameManager), nameof(GameManager.ClearDraggedStack))]
    public static bool GameManager_ClearDraggedStack()
    {
        if (GameManager.DraggedCard == null) return true;
        var myCardSlotUITag = GameManager.DraggedCard.GetComponentInParent<UIStructs.MyCardSlot.MyCardSlotUITag>();
        if (myCardSlotUITag == null) return true;
        myCardSlotUITag.Slot.NextDrag = 0;
        var cards = GameManager.DraggedCardStack.ToArray();
        GameManager.DraggedCardStack.Clear();
        foreach (var card in cards)
        {
            if (card == myCardSlotUITag.Slot.CardBase) continue;
            var cardTransform = card.transform;
            cardTransform.localScale = myCardSlotUITag.Slot.RawScale;
            cardTransform.localPosition = Vector3.zero;
            card.OnEndDrag(null);
        }

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CardSlot), nameof(CardSlot.OnDrop))]
    public static bool CardSlot_OnDrop(CardSlot __instance, PointerEventData _Pointer)
    {
        if (GameManager.DraggedCard == null) return true;
        var myCardSlotUITag = GameManager.DraggedCard.GetComponentInParent<UIStructs.MyCardSlot.MyCardSlotUITag>();
        if (myCardSlotUITag == null) return true;
        if (__instance.ParentSlotData.CardPileCount() > 0 && __instance.CanHostPile &&
            GameManager.DraggedCard.CardModel.CanPile && GameManager.DraggedCard.CardModel ==
            __instance.ParentSlotData.AssignedCard.CardModel)
        {
            GameManager.DraggedCard.CurrentSlot = __instance.ParentSlotData;
            GameManager.DraggedCard.CurrentSlotInfo = __instance.ParentSlotData.ToInfo();
            UIStructs.MyCardSlot.DropCard(GameManager.DraggedCard, myCardSlotUITag.Slot.RawScale);
            myCardSlotUITag.Slot.UpdatePile();
            return false;
        }

        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.OnPointerClick))]
    public static bool InGameCardBase_OnPointerClick(InGameCardBase __instance, PointerEventData _Pointer)
    {
        var myCardSlotUITag = __instance.GetComponentInParent<UIStructs.MyCardSlot.MyCardSlotUITag>();
        if (myCardSlotUITag == null) return true;
        myCardSlotUITag.Slot.DropCard();
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.UpdateVisibility)),
     HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.UpdateActiveState))]
    public static bool UpdateVisibility(InGameCardBase __instance)
    {
        var myCardSlotUITag = __instance.GetComponentInParent<UIStructs.MyCardSlot.MyCardSlotUITag>();
        if (myCardSlotUITag == null) return true;
        if (GameManager.DraggedCardStack.Contains(__instance))
        {
            __instance.gameObject.SafeSetActive(true);
            if (__instance.CardVisuals)
            {
                __instance.CardVisuals.gameObject.SafeSetActive(true);
            }

            return false;
        }

        if (GameManager.DraggedCard == myCardSlotUITag.Slot.CardBase && myCardSlotUITag.Slot.PileCard.Count > 0 &&
            __instance == myCardSlotUITag.Slot.PileCard[0])
        {
            __instance.gameObject.SafeSetActive(true);
            if (__instance.CardVisuals)
            {
                __instance.CardVisuals.gameObject.SafeSetActive(true);
            }

            return false;
        }

        __instance.gameObject.SafeSetActive(__instance == myCardSlotUITag.Slot.CardBase);
        if (__instance.CardVisuals)
        {
            __instance.CardVisuals.gameObject.SafeSetActive(__instance == myCardSlotUITag.Slot.CardBase);
        }

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(DynamicLayoutSlot), nameof(DynamicLayoutSlot.AssignCard))]
    public static void AssignCard(DynamicLayoutSlot __instance, InGameCardBase _Card)
    {
        var myCardSlotUITag = _Card.GetComponentInParent<UIStructs.MyCardSlot.MyCardSlotUITag>();
        if (myCardSlotUITag == null) return;
        myCardSlotUITag.Slot.LeaveCtrlTo(__instance);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(InGameCardBase), nameof(InGameCardBase.DropInInventory))]
    public static void InGameCardBase_DropInInventory(InGameCardBase __instance, InGameCardBase _Card)
    {
        var myCardSlotUITag = _Card.GetComponentInParent<UIStructs.MyCardSlot.MyCardSlotUITag>();
        if (myCardSlotUITag == null) return;
        myCardSlotUITag.Slot.LeaveCtrlTo(__instance, _Card);
    }

    [HarmonyPrefix,
     HarmonyPatch(typeof(InGameDraggableCard), nameof(InGameDraggableCard.CanBeDragged), MethodType.Getter)]
    public static bool CanBeDragged(InGameDraggableCard __instance, out bool __result)
    {
        var myCardSlotUITag = __instance.GetComponentInParent<UIStructs.MyCardSlot.MyCardSlotUITag>();
        if (myCardSlotUITag != null)
        {
            __result = myCardSlotUITag.Slot.CanBeDrag;
            return false;
        }

        __result = false;
        return true;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(InGameDraggableCard), nameof(InGameDraggableCard.OnBeginDrag))]
    public static bool OnBeginDrag(InGameDraggableCard __instance, PointerEventData _Pointer)
    {
        var myCardSlotUITag = __instance.GetComponentInParent<UIStructs.MyCardSlot.MyCardSlotUITag>();
        if (myCardSlotUITag == null) return true;
        __instance.BlocksRaycasts = false;
        __instance.Dragged = true;
        __instance.ForceActive = true;
        __instance.UpdateActiveState();
        GameManager.BeginDragItem(__instance);

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(InGameDraggableCard), nameof(InGameDraggableCard.OnDrag))]
    public static bool OnDrag(InGameDraggableCard __instance, PointerEventData _Pointer)
    {
        var myCardSlotUITag = __instance.GetComponentInParent<UIStructs.MyCardSlot.MyCardSlotUITag>();
        if (myCardSlotUITag == null) return true;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(myCardSlotUITag.Slot.ContentRectTransform,
            _Pointer.position, _Pointer.pressEventCamera, out var vector);
        myCardSlotUITag.Slot.ContentRectTransform.position = vector;

        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(InGameDraggableCard), nameof(InGameDraggableCard.OnEndDrag))]
    public static bool OnEndDrag(InGameDraggableCard __instance, PointerEventData _Pointer)
    {
        var myCardSlotUITag = __instance.GetComponentInParent<UIStructs.MyCardSlot.MyCardSlotUITag>();
        if (myCardSlotUITag == null) return true;
        __instance.Dragged = false;
        __instance.ForceActive = false;
        __instance.UpdateActiveState();
        __instance.BlocksRaycasts = true;
        myCardSlotUITag.Slot.ContentRectTransform.localPosition = Vector3.zero;
        EndDragItem(__instance);
        return false;
    }

    public static void EndDragItem(InGameDraggableCard _Card)
    {
        GameManager.DraggedCardStack ??= new List<InGameDraggableCard>();
        Action<InGameDraggableCard> onEndDragItem = GameManager.OnEndDragItem;
        onEndDragItem?.Invoke(_Card);
        MBSingleton<GameManager>.Instance.GameGraphics.RefreshSlots(false);
    }

    public static void Init()
    {
        // HarmonyInstance.PatchAll(typeof(UITools));
        // HarmonyInstance.PatchAll(typeof(UIManagers));
    }

    static UITools()
    {
        BaseGameObject = new GameObject("[CSTI_LuaActionSupport_UICanvas]", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        Object.DontDestroyOnLoad(BaseGameObject);
        BaseCanvas = BaseGameObject.GetComponent<Canvas>();
        BaseCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        BaseCanvasTransform = BaseGameObject.GetComponent<RectTransform>();
        BaseCanvasScale = BaseGameObject.GetComponent<CanvasScaler>();
        BaseCanvasScale.referenceResolution = new Vector2(1920, 1080);
        BaseCanvasScale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        BaseCanvasScale.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        BaseCanvasScale.matchWidthOrHeight = 0.5f;

        var contentTr = new GameObject("[UICanvas_Content]", typeof(RectTransform));
        BaseCanvasContentTransform = (RectTransform) contentTr.transform;
        BaseCanvasContentTransform.SetParent(BaseCanvasTransform);
    }

    private static UIStructs.MyCardSlot? TestInventorySlotModel(InGameCardBase? CardBase = null, bool realMove = true,
        float x = 200, float y = 200)
    {
        if (CardBase == null)
            CardBase = GameManager.Instance.AllCards.FirstOrDefault(card => card.name.Contains("DogFriend"));
        if (CardBase == null) return null;
        var myCardSlot = UIStructs.MyCardSlot.CreateOn(new Vector3(x, y), CardBase.transform.localScale);
        myCardSlot.SetCard(CardBase, realMove);
        return myCardSlot;
    }

    public static RectTransform GetParent(this RectTransform rectTransform)
    {
        return (rectTransform.parent as RectTransform)!;
    }
}