using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NucleoVivo
{
    /// <summary>Atalhos para montar uGUI por código com layouts automáticos.</summary>
    public static class UIFactory
    {
        static Font font;
        public static Font DefaultFont
        {
            get
            {
                if (font == null)
                {
                    try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                    if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                }
                return font;
            }
        }

        public class ButtonView
        {
            public Button button;
            public Image bg;
            public PixelText label;
            public PixelText sub;
            public RectTransform rt;
            public LayoutElement layout;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static RectTransform Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
            return rt;
        }

        public static Image Img(RectTransform rt, Color color, Sprite sprite = null, bool raycast = false)
        {
            var img = rt.gameObject.GetComponent<Image>();
            if (img == null) img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite ?? Art.RoundRect;
            img.type = img.sprite == Art.RoundRect || img.sprite == Art.RoundRectOutline ? Image.Type.Sliced : Image.Type.Simple;
            img.pixelsPerUnitMultiplier = 1f / PixelText.UiPixel; // 1 texel da moldura = 1 pixel da interface
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Panel(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            return Img(rt, color, Art.RoundRect, true);
        }

        public static Image Icon(Transform parent, IconKind kind, float size, bool badge = true)
        {
            // Ícone 18x18 ampliado por múltiplo inteiro do pixel da interface.
            int scale = size >= 100 ? 2 : 1;
            float icon = 18 * PixelText.UiPixel * scale;
            float holderSize = badge ? icon + 6 * PixelText.UiPixel : icon;
            var holder = Rect("ícone", parent);
            LE(holder.gameObject, holderSize, holderSize, holderSize, holderSize);
            if (badge) Img(holder, Palette.UiCardHighlight);
            var ic = Rect("desenho", holder);
            ic.anchorMin = ic.anchorMax = new Vector2(0.5f, 0.5f);
            ic.sizeDelta = new Vector2(icon, icon);
            var img = ic.gameObject.AddComponent<Image>();
            img.sprite = Art.Icon(kind);
            img.raycastTarget = false;
            return img;
        }

        public static PixelText Label(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false)
        {
            var rt = Rect("texto", parent);
            if (rt.GetComponent<CanvasRenderer>() == null) rt.gameObject.AddComponent<CanvasRenderer>();
            var t = rt.gameObject.AddComponent<PixelText>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            return t;
        }

        public static LayoutElement LE(GameObject go, float minW = -1, float prefW = -1, float minH = -1, float prefH = -1, float flexW = -1, float flexH = -1)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minWidth = minW; le.preferredWidth = prefW;
            le.minHeight = minH; le.preferredHeight = prefH;
            le.flexibleWidth = flexW; le.flexibleHeight = flexH;
            return le;
        }

        public static VerticalLayoutGroup VLayout(GameObject go, int padding, float spacing, TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.spacing = spacing;
            v.childAlignment = align;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HLayout(GameObject go, int padding, float spacing, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = go.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(padding, padding, padding, padding);
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            return h;
        }

        public static ButtonView Button(Transform parent, string label, Color color, UnityAction onClick, int fontSize = 36, string sub = null)
        {
            var rt = Rect("botão " + label, parent);
            var bg = Img(rt, color, Art.RoundRect, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1, 1, 1, 0.92f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(1, 1, 1, 1f);
            colors.fadeDuration = 0.06f;
            b.colors = colors;
            if (onClick != null) b.onClick.AddListener(onClick);

            var v = VLayout(rt.gameObject, 14, 2, TextAnchor.MiddleCenter);
            v.padding = new RectOffset(18, 18, 12, 12);
            var lbl = Label(rt, label, fontSize, Palette.UiText, TextAnchor.MiddleCenter, true);
            PixelText subT = null;
            if (sub != null) subT = Label(rt, sub, Mathf.RoundToInt(fontSize * 0.75f), Palette.UiText.WithAlpha(0.85f), TextAnchor.MiddleCenter);
            var le = LE(rt.gameObject, -1, -1, 96, -1);
            return new ButtonView { button = b, bg = bg, label = lbl, sub = subT, rt = rt, layout = le };
        }

        /// <summary>Área rolável vertical; devolve o conteúdo (com VerticalLayoutGroup).</summary>
        public static RectTransform Scroll(Transform parent, out ScrollRect scroll)
        {
            var root = Rect("rolagem", parent);
            Stretch(root);
            scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;
            scroll.decelerationRate = 0.12f;
            var hit = root.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0); // recebe arrastos
            var viewport = Rect("viewport", root);
            Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("conteúdo", viewport);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
            var v = VLayout(content.gameObject, 20, 18);
            v.padding = new RectOffset(24, 24, 20, 40);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        public static void SetInteractable(ButtonView bv, bool on, Color onColor)
        {
            bv.button.interactable = on;
            bv.bg.color = on ? onColor : Palette.UiButtonDisabled;
            bv.label.color = on ? Palette.UiText : Palette.UiMuted;
            if (bv.sub != null) bv.sub.color = on ? Palette.UiText.WithAlpha(0.85f) : Palette.UiMuted;
        }
    }

    /// <summary>Área invisível sobre o reator que recebe toques (inclusive multitoque).</summary>
    public class TapArea : MonoBehaviour, IPointerDownHandler
    {
        public Action<PointerEventData> onDown;
        public void OnPointerDown(PointerEventData e) => onDown?.Invoke(e);
    }

    /// <summary>Ajusta o retângulo à área segura do aparelho (notch, barra de gestos).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        Rect last;
        Vector2Int lastScreen;

        void Update()
        {
            Rect sa = Screen.safeArea;
            var scr = new Vector2Int(Screen.width, Screen.height);
            if (sa == last && scr == lastScreen) return;
            last = sa; lastScreen = scr;
            var rt = (RectTransform)transform;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }
}
