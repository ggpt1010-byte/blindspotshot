using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlindGunfight
{
    /// <summary>
    /// 코드로 uGUI 화면을 만드는 도우미. 위치는 화면 비율(0~1) 앵커로 정한다.
    /// </summary>
    public static class UIFactory
    {
        private static Font font;

        public static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>부모 안에서 anchorMin~anchorMax 비율 영역을 채우는 사각형</summary>
        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = Rect(parent, name, anchorMin, anchorMax);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align,
                                 Vector2 anchorMin, Vector2 anchorMax, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(parent, "Text", anchorMin, anchorMax);
            var label = rt.gameObject.AddComponent<Text>();
            label.font = DefaultFont;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.alignment = align;
            label.fontStyle = style;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        public static Button CreateButton(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Action onClick)
        {
            var image = Panel(parent, name, color, anchorMin, anchorMax);
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick?.Invoke());
            return button;
        }
    }
}
