using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CSTI_LuaActionSupport.UIStruct;

public static class UIStructs
{
    [RequireComponent(typeof(RectTransform))]
    public class MyCardSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler,
        IPointerUpHandler, IEventSystemHandler
    {
        public class MyCardSlotUITag : MonoBehaviour
        {
            public MyCardSlot Slot = null!;
        }

        public float DragStackTimer;
        public SlotsTypes LastSlotsTypes;
        public bool PointOn;
        public int NextDrag;
        public bool CanBeDrag => IsRealMove;
        public bool IsRealMove;
        public bool CanPile = true;
        public static readonly GameObject CardSlotPrefab;
        public Vector3 RawPos = Vector3.zero;
        public InGameCardBase? CardBase;
        public List<InGameCardBase> PileCard = new();
        public RectTransform Transform = null!;
        public DynamicFontText CardPileNumber = null!;
        public RectTransform CardPileNumberBGTr = null!;
        public RectTransform ContentRectTransform = null!;
        public Image ContentImg = null!;
        public Image BgImg = null!;
        public Image FgImg = null!;

        public Vector3 RawScale
        {
            get => ContentRectTransform.localScale;
            set => ContentRectTransform.localScale = value;
        }

        public void OnDrop(PointerEventData _Pointer)
        {
            var draggedCard = GameManager.DraggedCard;
            if (draggedCard == null) return;
            if (CardBase == null)
            {
                var myCardSlotUITag = draggedCard.GetComponentInParent<MyCardSlotUITag>();
                if (myCardSlotUITag != null && myCardSlotUITag.Slot == this) return;
                SetCard(draggedCard, true);
                var cards = GameManager.DraggedCardStack.ToArray();
                foreach (var card in cards)
                {
                    if (card != draggedCard)
                    {
                        AddCard(card, true);
                    }
                }
            }
            else
            {
                var model = CardBase.CardModel;
                if (CanPile && model.CanPile && model == draggedCard.CardModel)
                {
                    var cards = GameManager.DraggedCardStack.ToArray();
                    foreach (var card in cards)
                    {
                        if (PileCard.Contains(card) || card == CardBase) continue;

                        AddCard(card, true);
                    }
                }
            }
        }

        public void AddCard(InGameCardBase card, bool realRemove)
        {
            RemoveFromRaw(card, realRemove);
            PileCard.Add(card);
            card.SetParent(ContentRectTransform, true);
            card.transform.localScale = Vector3.zero;
            UpdatePile();
            CardPileNumber.gameObject.SafeSetActive(true);
        }

        public void LeaveCtrlTo(DynamicLayoutSlot slot)
        {
            if (CardBase == null) return;
            if (CardBase.InspectionDuplicate && CardBase.CardVisuals)
            {
                CardBase.CardVisuals.DestroyDuplicate();
                CardBase.InspectionDuplicate = null;
            }

            CardBase.SetParent(slot.GetParent);
            if (CardBase.CurrentSlot != slot)
            {
                CardBase.CurrentSlot?.RemoveSpecificCard(CardBase, true);
                slot.AddCard(CardBase, CardBase.CurrentSlot, true);
            }

            slot.SortCardPile();
        }

        public void LeaveCtrlTo(InGameCardBase container, InGameCardBase forCard)
        {
            if (forCard.InspectionDuplicate && forCard.CardVisuals)
            {
                forCard.CardVisuals.DestroyDuplicate();
                forCard.InspectionDuplicate = null;
            }

            forCard.SetParent(container.CurrentParentObject);
            forCard.CurrentSlot?.RemoveSpecificCard(forCard, true);
            if (GraphicsManager.Instance.InspectedCard != container)
            {
                forCard.gameObject.SafeSetActive(false);
            }
            else
            {
                GraphicsManager.Instance.MoveCardToSlot(forCard, new SlotInfo(SlotsTypes.Inventory, -2), false, true);
            }
        }

        public static void DropCard(InGameCardBase card, Vector3 rawScale)
        {
            card.gameObject.SafeSetActive(true);
            var myCardSlotUITag = card.GetComponentInParent<MyCardSlotUITag>();
            var cardTransform = card.transform;
            cardTransform.localScale = rawScale;
            if (card.InspectionDuplicate && card.CardVisuals)
            {
                card.CardVisuals.DestroyDuplicate();
                card.InspectionDuplicate = null;
            }

            if (card.CurrentSlotInfo.SlotType == SlotsTypes.Event)
            {
                if (myCardSlotUITag != null)
                {
                    card.CurrentSlotInfo = new SlotInfo(myCardSlotUITag.Slot.LastSlotsTypes, -2);
                    if (card.CardModel.CanPile)
                    {
                        card.CurrentSlot = FindSlotForCard(card);
                        if (card.CurrentSlot != null) card.CurrentSlotInfo = card.CurrentSlot.ToInfo();
                    }
                }
                else
                {
                    card.CurrentSlotInfo = new SlotInfo(SlotsTypes.Base, -2);
                    if (card.CardModel.CardType == CardTypes.Location)
                        card.CurrentSlotInfo.SlotType = SlotsTypes.Location;
                }
            }

            if (card.CurrentSlot != null)
            {
                card.CurrentSlot.AddCard(card, null, true);
                card.CurrentSlot.SortCardPile();
            }

            card.CurrentSlot ??= GraphicsManager.Instance.GetSlotForCard(card.CardModel, card.ContainedLiquidModel,
                card.CurrentSlotInfo);

            if (card.CurrentSlot != null)
            {
                card.SetParent(card.CurrentSlot.GetParent);
                card.CurrentSlot.AssignCard(card, true);
            }

            cardTransform.localScale = rawScale;
            cardTransform.localPosition = Vector3.zero;
            GraphicsManager.Instance.RefreshSlots(true);
        }

        public void DropCard()
        {
            if (CardBase == null) return;
            InGameCardBase? card;
            if (PileCard.Count > 0)
            {
                card = PileCard[0];
                PileCard.RemoveAt(0);
            }
            else
            {
                card = CardBase;
            }

            DropCard(card, RawScale);
            UpdatePile();
        }

        public void UpdatePile()
        {
            if (PileCard.Count == 0)
            {
                CardPileNumber.gameObject.SafeSetActive(false);
                CardPileNumberBGTr.gameObject.SafeSetActive(false);
            }
            else
            {
                CardPileNumber.TargetText.text = $"x{1 + PileCard.Count}";
                CardPileNumber.gameObject.SafeSetActive(true);
                CardPileNumberBGTr.gameObject.SafeSetActive(true);
            }
        }

        public static void RemoveFromRaw(InGameCardBase card, bool removeFromContainer)
        {
            if (card.CardVisuals) card.CardVisuals.CardStackObject.SafeSetActive(false);

            if (card.CurrentSlot != null)
            {
                card.CurrentSlot.RemoveSpecificCard(card, true);
                card.CurrentSlot.SortCardPile();
                if (card.CurrentSlot.CardPileCount() == 0)
                {
                    switch (card.CurrentSlotInfo.SlotType)
                    {
                        case SlotsTypes.Base:
                            GraphicsManager.Instance.BaseSlotsLine.RemoveSlot(card.CurrentSlotInfo.SlotIndex);
                            break;
                        case SlotsTypes.Location:
                            GraphicsManager.Instance.LocationSlotsLine.RemoveSlot(card.CurrentSlotInfo.SlotIndex);
                            break;
                        case SlotsTypes.Inventory
                            when card.CurrentContainer == GraphicsManager.Instance.InspectedCard:
                            GraphicsManager.Instance.InventoryInspectionPopup.InventorySlotsLine.RemoveSlot(
                                card.CurrentSlotInfo.SlotIndex);
                            break;
                    }
                }
            }

            card.CurrentSlot = null;
            if (removeFromContainer || card.CurrentSlotInfo.SlotType != SlotsTypes.Inventory)
            {
                card.CurrentSlotInfo = new SlotInfo(SlotsTypes.Event);
            }

            if (removeFromContainer && card.CurrentContainer)
            {
                card.CurrentContainer.RemoveCardFromInventory(card);
                card.CurrentContainer = null;
            }
        }

        public void SetCard(InGameCardBase card, bool isRealMove)
        {
            gameObject.SafeSetActive(true);
            if (card.CardModel.CardType == CardTypes.Event) return;
            if (card.CardModel.CardType == CardTypes.Explorable) return;
            if (card.CardModel.CardType == CardTypes.Environment) return;
            CardBase = card;
            LastSlotsTypes = CardBase.CurrentSlotInfo.SlotType;
            if (!isRealMove)
            {
                if (CardBase.CardVisuals.gameObject.activeInHierarchy)
                {
                    CardBase.DuplicateParentObject = CardBase.CurrentParentObject;
                    CardBase.InspectionDuplicate = CardBase.CardVisuals.CreateDuplicate(CardBase.DuplicateParentObject);
                }

                CardBase.SetParent(ContentRectTransform, true);
                RemoveFromRaw(card, false);
            }
            else
            {
                CardBase.SetParent(ContentRectTransform, true);
                RemoveFromRaw(card, true);
            }

            var cardBaseTransform = CardBase.transform;
            cardBaseTransform.localScale = RawScale;
            cardBaseTransform.localPosition = Vector3.zero;
            IsRealMove = isRealMove;
            CardBase.BlocksRaycasts = true;

            BgImg.color = new Color(1, 1, 1, 0.6f);
            FgImg.color = new Color(1, 1, 1, 0.6f);
        }

        public static MyCardSlot CreateOn(Vector3 pos, Vector3 rawScale, Transform? parent = null)
        {
            var instantiate = Instantiate(CardSlotPrefab,
                parent == null ? UITools.BaseCanvasContentTransform : parent);
            instantiate.SetActive(true);
            var instantiateTransform = instantiate.transform;
            instantiateTransform.localPosition = pos;
            var myCardSlot = instantiate.GetComponent<MyCardSlot>();
            myCardSlot.RawPos = pos;
            myCardSlot.RawScale = rawScale;
            return myCardSlot;
        }

        public static DynamicLayoutSlot? FindSlotForCard(InGameCardBase card)
        {
            if (card.CurrentSlotInfo == null) return null;
            switch (card.CurrentSlotInfo.SlotType)
            {
                case SlotsTypes.Base:
                    return GraphicsManager.Instance.BaseSlotsLine.Slots.FirstOrDefault(
                        slot => slot.AssignedCard != null && slot.AssignedCard.CardModel == card.CardModel);
                case SlotsTypes.Location:
                    return GraphicsManager.Instance.LocationSlotsLine.Slots.FirstOrDefault(
                        slot => slot.AssignedCard != null && slot.AssignedCard.CardModel == card.CardModel);
                case SlotsTypes.Hand:
                    return GraphicsManager.Instance.ItemSlotsLine.Slots.FirstOrDefault(
                        slot => slot.AssignedCard != null && slot.AssignedCard.CardModel == card.CardModel);
                case SlotsTypes.Inventory when GraphicsManager.Instance.InventoryInspectionPopup.gameObject
                    .activeInHierarchy:
                    return GraphicsManager.Instance.InventoryInspectionPopup
                        .InventorySlotsLine.Slots.FirstOrDefault(slot =>
                            slot.AssignedCard != null && slot.AssignedCard.CardModel == card.CardModel);
                case SlotsTypes.Equipment:
                    return GraphicsManager.Instance.CharacterWindow.EquipmentSlotsLine.Slots
                        .FirstOrDefault(slot =>
                            slot.AssignedCard != null && slot.AssignedCard.CardModel == card.CardModel);
            }

            return null;
        }

        private void LateUpdate()
        {
            if (CardBase == null) return;
            var toRemove = new List<InGameCardBase?>();
            foreach (var card in PileCard)
            {
                if (card == null)
                {
                    toRemove.Add(card);
                    continue;
                }

                if (card.CurrentParentObject != ContentRectTransform)
                {
                    toRemove.Add(card);
                }
            }

            foreach (var card in toRemove)
            {
                PileCard.Remove(card);
                if (card == null) continue;
                card.gameObject.SetActive(true);
                var cardTransform = card.transform;
                cardTransform.localScale = Vector3.zero;
                cardTransform.localPosition = Vector3.zero;
            }

            var cardBaseTransform = CardBase.transform;
            if (cardBaseTransform.parent != ContentRectTransform)
            {
                cardBaseTransform.localScale = RawScale;
                cardBaseTransform.localPosition = Vector3.zero;
                if (PileCard.Count > 0)
                {
                    cardBaseTransform.localScale = RawScale;
                    CardBase = PileCard[0];
                    PileCard.RemoveAt(0);
                    CardBase.gameObject.SafeSetActive(true);
                }
                else
                {
                    BgImg.color = new Color(1, 1, 1, 0.1f);
                    FgImg.color = new Color(1, 1, 1, 0.1f);
                    CardBase = null;
                }

                UpdatePile();
            }
        }

        static MyCardSlot()
        {
            CardSlotPrefab = new GameObject("{MyCardSlot}", typeof(RectTransform), typeof(MyCardSlot));
            CardSlotPrefab.SafeSetActive(false);
            var myCardSlot = CardSlotPrefab.GetComponent<MyCardSlot>();
            var prefabTransform = (RectTransform) CardSlotPrefab.transform;
            prefabTransform.sizeDelta = new Vector2(200, 300);
            prefabTransform.SetParent(UITools.BaseCanvasContentTransform);
            myCardSlot.Transform = prefabTransform;

            var fg = new GameObject("{MyCardSlotFg}", typeof(RectTransform), typeof(Image));
            var fgTransform = (RectTransform) fg.transform;
            fgTransform.SetParent(prefabTransform);
            myCardSlot.FgImg = fg.GetComponent<Image>();
            fgTransform.sizeDelta = new Vector2(200, 300);
            myCardSlot.FgImg.sprite = Resources.FindObjectsOfTypeAll<Sprite>()
                .FirstOrDefault(sprite => sprite.name == "SmallRoundedRect");
            myCardSlot.FgImg.type = Image.Type.Sliced;
            myCardSlot.FgImg.material = Resources.FindObjectsOfTypeAll<Material>()
                .FirstOrDefault(material => material.name == "Default UI Material");
            myCardSlot.FgImg.color = new Color(1, 1, 1, 0.1f);

            var bg = new GameObject("{MyCardSlotBg}", typeof(RectTransform), typeof(Image));
            var bgTransform = (RectTransform) bg.transform;
            bgTransform.SetParent(prefabTransform);
            myCardSlot.BgImg = bg.GetComponent<Image>();
            bgTransform.sizeDelta = new Vector2(200, 300);
            myCardSlot.BgImg.sprite = Resources.FindObjectsOfTypeAll<Sprite>()
                .FirstOrDefault(sprite => sprite.name == "SmallRoundedRect_Line");
            myCardSlot.BgImg.type = Image.Type.Sliced;
            myCardSlot.BgImg.material = myCardSlot.FgImg.material;
            myCardSlot.BgImg.color = new Color(1, 1, 1, 0.1f);

            var content = new GameObject("{MyCardSlotContent}", typeof(RectTransform), typeof(MyCardSlotUITag));
            content.transform.SetParent(prefabTransform);
            myCardSlot.ContentRectTransform = content.GetComponent<RectTransform>();
            content.GetComponent<MyCardSlotUITag>().Slot = myCardSlot;

            var contentImg = new GameObject("{MyCardSlotContentImg}", typeof(RectTransform), typeof(Image));
            var contentImgTransform = (RectTransform) contentImg.transform;
            contentImgTransform.SetParent(prefabTransform);
            contentImgTransform.sizeDelta = new Vector2(200, 300);
            contentImg.SetActive(false);
            myCardSlot.ContentImg = contentImg.GetComponent<Image>();
            myCardSlot.ContentImg.color = new Color(1, 1, 1, 0);

            var pileCountBg = new GameObject("{MyCardSlotPileCountBg}", typeof(RectTransform), typeof(Image));
            var pileCountBgTr = (RectTransform) pileCountBg.transform;
            pileCountBgTr.SetParent(prefabTransform);
            pileCountBgTr.localPosition = new Vector3(100, 150);
            pileCountBgTr.sizeDelta = new Vector2(60, 60);
            var pileCountBgImage = pileCountBg.GetComponent<Image>();
            pileCountBgImage.sprite = Resources.FindObjectsOfTypeAll<Sprite>()
                .FirstOrDefault(sprite => sprite.name == "StackKnob");
            pileCountBgImage.material = myCardSlot.FgImg.material;
            var canvasRenderer = pileCountBg.GetComponent<CanvasRenderer>();
            canvasRenderer.SetMesh(Resources.FindObjectsOfTypeAll<Mesh>()
                .FirstOrDefault(mesh => mesh.name == "Shared UI Mesh"));
            myCardSlot.CardPileNumberBGTr = pileCountBgTr;

            var pileCount = new GameObject("{MyCardSlotPileCount}", typeof(RectTransform), typeof(TextMeshProUGUI),
                typeof(DynamicFontText));
            var pileCountTr = (RectTransform) pileCount.transform;
            pileCountTr.SetParent(prefabTransform);
            pileCountTr.localPosition = new Vector3(108, 140);
            pileCountTr.sizeDelta = new Vector2(60, 60);
            var textMeshProUGUI = pileCount.GetComponent<TextMeshProUGUI>();
            textMeshProUGUI.color = Color.black;
            var dynamicFontText = pileCount.GetComponent<DynamicFontText>();
            dynamicFontText.TargetText = textMeshProUGUI;
            dynamicFontText.FontSize = 36;
            dynamicFontText.FontID = "Standard";
            myCardSlot.CardPileNumber = dynamicFontText;
            pileCount.SafeSetActive(false);
            pileCountBg.SafeSetActive(false);
        }

        private void Start()
        {
            CardPileNumber.TargetText = CardPileNumber.GetComponent<TextMeshProUGUI>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PointOn = true;
            UITools.GMUpdateBlock.Add(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PointOn = false;
            UITools.GMUpdateBlock.Remove(this);
        }

        private void Update()
        {
            if (GameManager.DraggedCard != null)
            {
                if (CardBase != null &&
                    (GameManager.DraggedCard == CardBase || PileCard.Contains(GameManager.DraggedCard))
                    && !ContentImg.gameObject.activeSelf)
                {
                    ContentImg.sprite = CardBase.CardModel.CardImage;
                    ContentImg.color = new Color(1, 1, 1, 0.6f);
                    ContentImg.gameObject.SetActive(true);
                }
            }
            else
            {
                ContentImg.color = new Color(1, 1, 1, 0);
                ContentImg.gameObject.SetActive(false);
            }

            if (PointOn)
            {
                if (GameManager.DraggedCard == null ||
                    (GameManager.DraggedCard.GetComponentInParent<MyCardSlotUITag>() is var uiTag &&
                     (uiTag == null || uiTag.Slot != this)))
                {
                    DragStackTimer += Time.deltaTime;
                    if (DragStackTimer >= GameManager.DragStackDuration)
                    {
                        DragStackTimer -= GameManager.DragStackDuration;
                        GameManager.AddCardToDraggedStack();
                    }
                }
            }

            if (!GameManager.DraggedCard)
            {
                NextDrag = 0;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            GameManager.DraggedCardStack.Clear();
        }
    }
}