using System;
using ModCommonUI.Util;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModCommonUI.Impl
{
}

namespace ModCommonUI
{
    public static partial class UIMainManager
    {
    }

    public partial class UISubManager : IPointerClickHandler
    {
        public Action<PointerEventData>? OnClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            OnClick?.Invoke(eventData);
        }

        public UISubManager TextAutoSize(bool enable = true)
        {
            if (_text != null)
            {
                _text.autoSizeTextContainer = enable;
            }

            return this;
        }

        public string NormalText
        {
            get => _normalText ?? "";
            set
            {
                _normalText = value;
                Text.text = value;
            }
        }

        private string? _normalText;

        public UISubManager CreatePanel(MyRect rect, Color? panelColor = null, string cname = "|PANEL|")
        {
            panelColor ??= new Color(0.375f, 0.75f, 1f, 0.25f);
            return this.CreateWith<Image>(cname, (manager, image) =>
            {
                image.color = panelColor.Value;

                manager.ControlRange = ControlRange * rect;
                manager._image = image;
            }).rt;
        }

        public UISubManager CreateIcon(MyRect rect, Sprite icon, string cname = "|ICON|")
        {
            return this.CreateWith<Image>(cname, (manager, image) =>
            {
                image.sprite = icon;
                image.useSpriteMesh = true;

                manager.ControlRange = ControlRange * rect;
                manager._image = image;
            }).rt;
        }

        public UISubManager CreateText(MyRect rect, string text, Action<TextMeshProUGUI>? init,
            float fontSize = 16,
            TMP_FontAsset? font = null, bool autoSize = false, string cname = "|TXT|")
        {
            if (font == null) font = UIMainManager.CommonFont;
            if (font == null)
            {
                Debug.LogWarning("ModCommonUI 默认字体未初始化");
                return null!;
            }


            return this.CreateWith<TextMeshProUGUI>(cname, (manager, tmp) =>
            {
                tmp.text = text;
                tmp.autoSizeTextContainer = autoSize;
                tmp.fontSize = fontSize;

                manager.ControlRange = ControlRange * rect;
                manager._text = tmp;
                manager._normalText = text;

                init?.Invoke(tmp);
            }).rt;
        }

        public UISubManager Clickable(Action<PointerEventData> clickedAct)
        {
            OnClick = (Action<PointerEventData>) Delegate.Combine(OnClick, clickedAct);

            return this;
        }

        public UISubManager WithImg(Sprite? sprite)
        {
            Image.sprite = sprite;
            Image.useSpriteMesh = sprite;

            return this;
        }
    }
}