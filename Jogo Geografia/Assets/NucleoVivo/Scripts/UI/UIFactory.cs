using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NucleoVivo
{
    /// <summary>Atalhos para montar a interface por código, com as molduras e ícones pintados.</summary>
    public static class UIFactory
    {
        static Font font;

        /// <summary>Fonte serifada do sistema (Georgia no Windows), combinando com a arte pintada.</summary>
        public static Font DefaultFont
        {
            get
            {
                if (font == null)
                {
                    try { font = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Palatino Linotype", "Book Antiqua", "Times New Roman" }, 32); } catch { }
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return font;
            }
        }

        public class ButtonView
        {
            public Button button;
            public Image bg;
            public Text label;
            public Text sub;
            public RectTransform rt;
            public LayoutElement layout;
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
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

        static float Lum(Color c) => 0.3f * c.r + 0.59f * c.g + 0.11f * c.b;
        static bool Same(Color a, Color b) => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.02f;

        /// <summary>
        /// Aplica a cor pedida usando as molduras pintadas: painéis escuros, cartões claros, destaques
        /// e botões (verde, marrom ou apagado).
        /// </summary>
        public static void Tint(Image img, Color c)
        {
            if (img == null) return;
            string n = img.sprite != null ? img.sprite.name : "";
            if (n.StartsWith("ui_button"))
            {
                string s = Same(c, Palette.UiButton) ? "ui_button" : Same(c, Palette.UiButtonDisabled) ? "ui_button_off" : "ui_button_alt";
                img.sprite = Pinturas.Get(s);
                img.color = Same(c, Palette.UiCardHighlight) ? new Color(1f, 0.93f, 0.8f, 1f) : Color.white;
                return;
            }
            if (n.StartsWith("ui_panel"))
            {
                img.sprite = Pinturas.Get(Lum(c) < 0.16f ? "ui_panel" : "ui_panel_light");
                Color t = Same(c, Palette.UiCardHighlight) ? new Color(1f, 0.9f, 0.74f) : Color.white;
                t.a = c.a;
                img.color = t;
                return;
            }
            img.color = c;
        }

        public static Image Img(RectTransform rt, Color color, Sprite sprite = null, bool raycast = false)
        {
            var img = rt.gameObject.GetComponent<Image>();
            if (img == null) img = rt.gameObject.AddComponent<Image>();
            bool panel = sprite == null || sprite == Art.RoundRect || sprite == Art.RoundRectOutline;
            img.sprite = panel ? Pinturas.Get("ui_panel") : sprite;
            img.type = panel ? Image.Type.Sliced : Image.Type.Simple;
            img.pixelsPerUnitMultiplier = 1.2f;
            img.raycastTarget = raycast;
            if (panel) Tint(img, color); else img.color = color;
            return img;
        }

        public static Image Panel(string name, Transform parent, Color color) => Img(Rect(name, parent), color, null, true);

        public static Image Icon(Transform parent, IconKind kind, float size, bool badge = true)
        {
            size = Mathf.Round(size * 1.2f);
            var holder = Rect("ícone", parent);
            LE(holder.gameObject, size, size, size, size);
            if (badge)
            {
                var bg = holder.gameObject.AddComponent<Image>();
                bg.sprite = Pinturas.Get("ui_badge");
                bg.raycastTarget = false;
            }
            var ic = Rect("desenho", holder);
            ic.anchorMin = ic.anchorMax = new Vector2(0.5f, 0.5f);
            float inner = size * (badge ? 0.62f : 1f);
            ic.sizeDelta = new Vector2(inner, inner);
            var img = ic.gameObject.AddComponent<Image>();
            img.sprite = Pinturas.Icon(kind);
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false)
        {
            var rt = Rect("texto", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = bold || size >= 44 ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.lineSpacing = 1.05f;
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
            var bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = Pinturas.Get("ui_button");
            bg.type = Image.Type.Sliced;
            bg.pixelsPerUnitMultiplier = 1.2f;
            Tint(bg, color);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = bg;
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.92f, 1f);
            colors.pressedColor = new Color(0.8f, 0.78f, 0.74f, 1f);
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.08f;
            b.colors = colors;
            if (onClick != null) b.onClick.AddListener(onClick);

            var v = VLayout(rt.gameObject, 14, 2, TextAnchor.MiddleCenter);
            v.padding = new RectOffset(18, 18, 8, 14);
            var lbl = Label(rt, label, fontSize, Palette.UiText, TextAnchor.MiddleCenter, true);
            var sh = lbl.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.45f);
            sh.effectDistance = new Vector2(1.5f, -1.5f);
            Text subT = null;
            if (sub != null) subT = Label(rt, sub, Mathf.RoundToInt(fontSize * 0.75f), Palette.UiText.WithAlpha(0.85f), TextAnchor.MiddleCenter);
            var le = LE(rt.gameObject, -1, -1, 60, -1);
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
            hit.color = new Color(0, 0, 0, 0);
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
            Tint(bv.bg, on ? onColor : Palette.UiButtonDisabled);
            bv.label.color = on ? Palette.UiText : Palette.UiMuted;
            if (bv.sub != null) bv.sub.color = on ? Palette.UiText.WithAlpha(0.85f) : Palette.UiMuted;
        }
    }

    /// <summary>Ajusta o retângulo à área segura da tela.</summary>
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
