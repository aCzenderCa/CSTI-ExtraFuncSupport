using System;
using ModCommonUI.Event;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ModCommonUI
{
    [RequireComponent(typeof(RectTransform))]
    public partial class UISubManager : MonoBehaviour
    {
        public Rect ControlRange
        {
            get => _controlRange;
            set
            {
                rt.position = value.position;
                rt.sizeDelta = value.size;
                _controlRange = value;
            }
        }

        public Image Image
        {
            get
            {
                if (_image == null)
                {
                    _image = gameObject.GetComponent<Image>();
                    if (_image != null)
                    {
                        return _image;
                    }

                    _image = gameObject.AddComponent<Image>();
                }

                return _image;
            }
        }

        public TextMeshProUGUI Text
        {
            get
            {
                if (_text == null)
                {
                    _text = gameObject.GetComponent<TextMeshProUGUI>();
                    if (_text != null)
                    {
                        return _text;
                    }

                    _text = gameObject.AddComponent<TextMeshProUGUI>();
                }

                return _text;
            }
        }

        private TextMeshProUGUI? _text;
        private Image? _image;
        private Rect _controlRange;
        public RectTransform rt = null!;

        private void Awake()
        {
            rt = GetComponent<RectTransform>();
            _controlRange = rt.rect;
            gameObject.layer = 5;
        }
    }

    public static partial class UIMainManager
    {
        public static TMP_FontAsset? CommonFont;

        public static void InitRuntime(TMP_FontAsset commonFont)
        {
            CommonFont = commonFont;
        }

        public static readonly UISubManager UIBase;

        public static UISubManager CreateWith(this UISubManager? rt, string name = "|TEMP|",
            Action<UISubManager>? init = null)
        {
            var newGoRT = new GameObject(name, typeof(RectTransform), typeof(UISubManager))
                .GetComponent<UISubManager>();
            if (rt != null)
            {
                newGoRT.Parent = rt;
            }

            init?.Invoke(newGoRT);
            return newGoRT;
        }

        public static (UISubManager rt, TCom0 com0) CreateWith<TCom0>(this UISubManager? rt, string name = "|TEMP|",
            Action<UISubManager, TCom0>? init = null)
            where TCom0 : Component
        {
            var newGoRT = new GameObject(name, typeof(RectTransform), typeof(UISubManager), typeof(TCom0))
                .GetComponent<UISubManager>();
            if (rt != null)
            {
                newGoRT.Parent = rt;
            }

            var tCom0 = newGoRT.GetComponent<TCom0>();
            init?.Invoke(newGoRT, tCom0);
            return (newGoRT, tCom0);
        }

        public static (UISubManager rt, TCom0 com0, TCom1 com1) CreateWith<TCom0, TCom1>(this UISubManager? rt,
            string name = "|TEMP|", Action<UISubManager, TCom0, TCom1>? init = null)
            where TCom0 : Component where TCom1 : Component
        {
            var newGoRT = new GameObject(name, typeof(RectTransform), typeof(UISubManager), typeof(TCom0),
                typeof(TCom1)).GetComponent<UISubManager>();
            if (rt != null)
            {
                newGoRT.Parent = rt;
            }

            var tCom0 = newGoRT.GetComponent<TCom0>();
            var tCom1 = newGoRT.GetComponent<TCom1>();
            init?.Invoke(newGoRT, tCom0, tCom1);
            return (newGoRT, tCom0, tCom1);
        }

        public static (UISubManager rt, TCom0 com0, TCom1 com1, TCom2 com2) CreateWith<TCom0, TCom1, TCom2>(
            this UISubManager? rt, string name = "|TEMP|", Action<UISubManager, TCom0, TCom1, TCom2>? init = null)
            where TCom0 : Component where TCom1 : Component where TCom2 : Component
        {
            var newGoRT = new GameObject(name, typeof(RectTransform), typeof(UISubManager), typeof(TCom0),
                typeof(TCom1), typeof(TCom2)).GetComponent<UISubManager>();
            if (rt != null)
            {
                newGoRT.Parent = rt;
            }

            var tCom0 = newGoRT.GetComponent<TCom0>();
            var tCom1 = newGoRT.GetComponent<TCom1>();
            var tCom2 = newGoRT.GetComponent<TCom2>();
            init?.Invoke(newGoRT, tCom0, tCom1, tCom2);
            return (newGoRT, tCom0, tCom1, tCom2);
        }

        public static (UISubManager rt, TCom0 com0, TCom1 com1, TCom2 com2, TCom3 com3) CreateWith<TCom0, TCom1, TCom2,
            TCom3>(this UISubManager? rt, string name = "|TEMP|",
            Action<UISubManager, TCom0, TCom1, TCom2, TCom3>? init = null)
            where TCom0 : Component where TCom1 : Component where TCom2 : Component where TCom3 : Component
        {
            var newGoRT = new GameObject(name, typeof(RectTransform), typeof(UISubManager), typeof(TCom0),
                typeof(TCom1), typeof(TCom2), typeof(TCom3)).GetComponent<UISubManager>();
            if (rt != null)
            {
                newGoRT.Parent = rt;
            }

            var tCom0 = newGoRT.GetComponent<TCom0>();
            var tCom1 = newGoRT.GetComponent<TCom1>();
            var tCom2 = newGoRT.GetComponent<TCom2>();
            var tCom3 = newGoRT.GetComponent<TCom3>();
            init?.Invoke(newGoRT, tCom0, tCom1, tCom2, tCom3);
            return (newGoRT, tCom0, tCom1, tCom2, tCom3);
        }

        public static UISubManager CreateBase(string name)
        {
            return UIBase.CreateWith(name, manager => { manager.ControlRange = UIBase.ControlRange; });
        }

        static UIMainManager()
        {
            UIBase = CreateWith<Canvas, CanvasScaler, MyEventSys>(null, "[CommonUI.UIBase]",
                (manager, canvas, canvasScaler, sys) =>
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None |
                                                      AdditionalCanvasShaderChannels.TexCoord1 |
                                                      AdditionalCanvasShaderChannels.Normal |
                                                      AdditionalCanvasShaderChannels.Tangent;

                    canvasScaler.referenceResolution = new Vector2(1920, 1080);
                    canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    canvasScaler.matchWidthOrHeight = 1;
                    canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    canvasScaler.scaleFactor = 100;

                    manager.ControlRange = new Rect(new Vector2(960, 540), new Vector2(1920, 1080));
                }).rt;
            Object.DontDestroyOnLoad(UIBase);
        }
    }
}