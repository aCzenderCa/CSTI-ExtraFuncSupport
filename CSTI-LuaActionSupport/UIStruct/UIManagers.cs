using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CSTI_LuaActionSupport.Helper;
using CSTI_LuaActionSupport.LuaCodeHelper;
using HarmonyLib;
using NLua;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CSTI_LuaActionSupport.UIStruct;

[HarmonyPatch]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[LuaFuncTodo]
public static class UIManagers
{
    public static readonly Dictionary<string, UIModel> AllUIModel = new();

    [LuaFunc]
    public static UIModel? GetByID(string uid)
    {
        if (AllUIModel.TryGetValue(uid, out var uiModel))
        {
            return uiModel;
        }

        return null;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(GraphicsManager), nameof(GraphicsManager.InspectCard))]
    public static bool GraphicsManager_InspectCard(InGameCardBase _Card)
    {
        if (_Card.CurrentContainer) return true;
        if (AllUIModel.TryGetValue(_Card.CardModel.UniqueID, out var uiModel))
        {
            uiModel.BuildSelf();
            uiModel.GO!.SetActive(true);
            uiModel.Init(_Card);
            if (uiModel.AllSlots.Count > 0)
            {
                var mainSlot = uiModel.AllSlots[0];
            }

            for (var i = 1; i < uiModel.AllSlots.Count && i < _Card.CardsInInventory.Count; i++)
            {
                var inventorySlot = _Card.CardsInInventory[i - 1];
                var list = inventorySlot.AllCards.ToList();
                list.Remove(inventorySlot.MainCard);
            }

            return false;
        }

        return true;
    }

    [TestCode("""
              local model = UIManagers.CreateModel(1000,600,800,600,"SmallRoundedRect_Line","SmallRoundedRect",function (card,slots)
              end,
              {
              {x=-240,y=40},
              {x=240,y=40}
              }
              ,{
              {
              PlacePos={x=0,y=300},
              ButtonSize={x=120,y=30},
              Text="测试",
              ---@param card CardAccessBridge
              Function=function (card)
              card[0][1]:Remove(false)
              end
              }
              })
              model:RegForCard("0269179428204eaca0955e49090b680e")
              """)]
    [LuaFunc]
    public static UIModel CreateModel(float x, float y, float w, float h, string bg, LuaFunction init,
        LuaTable slots, LuaTable buttons)
    {
        var slotsLi = new List<Vector2>();
        var buttonsLi = new List<UIModel.UILuaButton>();
        for (var i = 1;; i++)
        {
            if (slots[i] is LuaTable table)
            {
                slotsLi.Add(UIModel.UILuaButton.Table2Vector2(table));
            }
            else
            {
                break;
            }
        }

        for (var i = 1;; i++)
        {
            if (buttons[i] is LuaTable table)
            {
                buttonsLi.Add(UIModel.UILuaButton.CreateByLua(table));
            }
            else
            {
                break;
            }
        }

        var uiModel = new UIModel(x, y, w, h, bg, init, slotsLi, buttonsLi);
        return uiModel;
    }

    public class UIModel
    {
        public GameObject? GO;
        public readonly List<UIStructs.MyTrPin> AllSlots = new();
        public readonly List<GameObject> AllLuaButtons = new();
        public InGameCardBase? Card;

        public void RegForCard(string uid)
        {
            AllUIModel[uid] = this;
        }

        public void BuildSelf()
        {
            if (GO) return;
            GO = new GameObject("SimpleUIModel", typeof(RectTransform), typeof(Image));
            GO.SetActive(false);
            var goTransform = (RectTransform)GO.transform;
            goTransform.SetParent(UITools.BaseCanvasContentTransform);
            goTransform.position = new Vector2(X, Y);
            goTransform.sizeDelta = new Vector2(Width, Height);

            var bg = goTransform.GetComponent<Image>();
            if (SpriteDict.TryGetValue(BgImg, out var bgSprite)) bg.sprite = bgSprite;
            bg.material = Resources.FindObjectsOfTypeAll<Material>()
                .FirstOrDefault(material => material.name == "Default UI Material");
            bg.type = Image.Type.Sliced;
            bg.color = new Color(1, 1, 1, 0.5f);

            var CardSlotParent = new GameObject("CardSlotParent", typeof(RectTransform));
            var CardSlotParentTransform = CardSlotParent.transform;
            CardSlotParentTransform.SetParent(goTransform);
            CardSlotParentTransform.localPosition = Vector3.zero;

            foreach (var myCardSlot in SlotSet.Select(slotPos =>
                         UIStructs.MyTrPin.CreateOn(slotPos, null, null, "",CardSlotParentTransform)))
            {
                AllSlots.Add(myCardSlot);
            }

            var LuaButtonParent = new GameObject("LuaButtonParent", typeof(RectTransform));
            var luaButtonParentTransform = LuaButtonParent.transform;
            luaButtonParentTransform.SetParent(goTransform);
            luaButtonParentTransform.localPosition = Vector3.zero;

            var ui_bg_img = Resources.FindObjectsOfTypeAll<Sprite>()
                .FirstOrDefault(sprite => sprite.name == "SmallRoundedRect_Line");
            foreach (var uiLuaButton in Buttons)
            {
                var luaButtonImg = new GameObject("LuaButtonImg", typeof(RectTransform), typeof(Image));
                var luaButton = new GameObject("LuaButton", typeof(RectTransform), typeof(Button),
                    typeof(TextMeshProUGUI), typeof(DynamicFontText));
                AllLuaButtons.Add(luaButton);
                var luaButtonImgTransform = (RectTransform)luaButtonImg.transform;
                var luaButtonTransform = (RectTransform)luaButton.transform;
                luaButtonImgTransform.SetParent(luaButtonParentTransform);
                luaButtonTransform.SetParent(luaButtonImgTransform);
                var image = luaButtonImg.GetComponent<Image>();
                image.sprite = ui_bg_img;
                image.material = bg.material;
                image.color = new Color(1, 1, 1, 0.8f);
                image.type = Image.Type.Sliced;
                luaButtonImgTransform.localPosition = uiLuaButton.PlacePos;
                luaButtonImgTransform.sizeDelta = uiLuaButton.ButtonSize;
                luaButtonTransform.localPosition = Vector3.zero;
                luaButtonTransform.sizeDelta = uiLuaButton.ButtonSize;
                var button = luaButton.GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    InitRuntime(GameManager.Instance);
                    uiLuaButton.Function?.Call(new CardAccessBridge(Card));
                });
                var textMeshProUGUI = luaButton.GetComponent<TextMeshProUGUI>();
                textMeshProUGUI.text = uiLuaButton.Text;
                textMeshProUGUI.color = Color.black;
                var dynamicFontText = luaButton.GetComponent<DynamicFontText>();
                dynamicFontText.TargetText = textMeshProUGUI;
                dynamicFontText.FontID = "Standard";
                dynamicFontText.FontSize = uiLuaButton.TextSize;
            }
        }

        public class UILuaButton
        {
            public readonly Vector2 PlacePos;
            public readonly Vector2 ButtonSize;
            public readonly LuaFunction? Function;
            public readonly LuaFunction? TextFunction;
            public readonly string Text;
            public readonly string ButtonId;
            public readonly string ButtonText;
            public readonly float TextSize;

            public static Vector2 Table2Vector2(LuaTable table)
            {
                float _x = 0;
                float _y = 0;
                _x.TryModBy(table[nameof(Vector2.x)]);
                _y.TryModBy(table[nameof(Vector2.y)]);
                return new Vector2(_x, _y);
            }

            public static UILuaButton CreateByLua(LuaTable table)
            {
                var _PlacePos = Vector2.zero;
                var _ButtonSize = Vector2.zero;
                if (table[nameof(PlacePos)] is LuaTable PlacePosTab)
                    _PlacePos = Table2Vector2(PlacePosTab);

                if (table[nameof(ButtonSize)] is LuaTable ButtonSizeTab)
                    _ButtonSize = Table2Vector2(ButtonSizeTab);

                var _Function = table[nameof(Function)] as LuaFunction;
                var _TextFunction = table[nameof(TextFunction)] as LuaFunction;
                var _Text = table[nameof(Text)] as string ?? "";
                var _ButtonId = table[nameof(ButtonId)] as string ?? "";
                var _ButtonText = table[nameof(ButtonText)] as string ?? "";
                var _TextSize = table[nameof(TextSize)].TryNum<float>() ?? 24;
                return new UILuaButton(_PlacePos, _ButtonSize, _Function, _TextFunction, _Text, _TextSize, _ButtonId,
                    _ButtonText);
            }

            public UILuaButton(Vector2 placePos, Vector2 buttonSize, LuaFunction? function, LuaFunction? textFunction,
                string text, float textSize, string buttonId, string buttonText)
            {
                Function = function;
                TextFunction = textFunction;
                Text = text;
                TextSize = textSize;
                ButtonId = buttonId;
                ButtonText = buttonText;
                PlacePos = placePos;
                ButtonSize = buttonSize;
            }
        }

        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;
        public readonly string BgImg;
        public readonly LuaFunction InitCode;
        public readonly List<Vector2> SlotSet;
        public readonly List<UILuaButton> Buttons;

        public UIModel(float x, float y, float w, float h, string bgImg, LuaFunction initCode,
            List<Vector2> slotSet, List<UILuaButton> buttons)
        {
            X = x;
            Y = y;
            Width = w;
            Height = h;
            InitCode = initCode;
            SlotSet = slotSet;
            Buttons = buttons;
            BgImg = bgImg;
        }

        public void Init(InGameCardBase card)
        {
            try
            {
                if (GameManager.Instance)
                {
                    InitRuntime(GameManager.Instance);
                }

                Card = card;
                InitCode.Call(new CardAccessBridge(card), AllSlots);
                for (var i = 0; i < AllLuaButtons.Count; i++)
                {
                    var textMeshProUGUI = AllLuaButtons[i].GetComponent<TextMeshProUGUI>();
                    var txt = Buttons[i].TextFunction?.Call(new CardAccessBridge(card)).ToString();
                    if (txt != null)
                    {
                        textMeshProUGUI.text = txt;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }

        public void Close()
        {
            if (GO != null)
            {
                GO.SafeSetActive(false);
            }
        }
    }
}