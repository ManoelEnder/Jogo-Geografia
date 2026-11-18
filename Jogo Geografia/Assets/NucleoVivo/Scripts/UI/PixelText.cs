using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NucleoVivo
{
    /// <summary>
    /// Texto de interface desenhado com a <see cref="PixelFont"/>: cada pixel da fonte vira um bloco nítido.
    /// Aceita &lt;color=#RRGGBB&gt;; ignora &lt;b&gt;, &lt;i&gt; e &lt;size&gt;.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class PixelText : MaskableGraphic, ILayoutElement
    {
        /// <summary>Tamanho, em unidades do Canvas, de um pixel da interface.</summary>
        public const int UiPixel = 3;

        [TextArea(2, 6)] [SerializeField] string m_Text = "";
        [SerializeField] int m_PixelSize = UiPixel;
        [SerializeField] TextAnchor m_Alignment = TextAnchor.UpperLeft;
        [SerializeField] bool m_Shadow = true;
        [SerializeField] Color m_ShadowColor = new Color(0.02f, 0.05f, 0.07f, 0.9f);

        public string text
        {
            get => m_Text;
            set { if (m_Text == value) return; m_Text = value ?? ""; Dirty(); }
        }

        public int pixelSize
        {
            get => m_PixelSize;
            set { value = Mathf.Max(1, value); if (m_PixelSize == value) return; m_PixelSize = value; Dirty(); }
        }

        /// <summary>Compatibilidade: tamanhos grandes viram o dobro de pixel.</summary>
        public int fontSize
        {
            get => m_PixelSize * 12;
            set => pixelSize = value >= 70 ? UiPixel * 3 : value >= 44 ? UiPixel * 2 : UiPixel;
        }

        public TextAnchor alignment
        {
            get => m_Alignment;
            set { if (m_Alignment == value) return; m_Alignment = value; SetVerticesDirty(); }
        }

        public bool shadow { get => m_Shadow; set { m_Shadow = value; SetVerticesDirty(); } }
        public Color shadowColor { get => m_ShadowColor; set { m_ShadowColor = value; SetVerticesDirty(); } }

        void Dirty() { SetVerticesDirty(); SetLayoutDirty(); }

        // ---------- texto rico ----------
        struct Ch { public char c; public Color? col; }

        List<Ch> Parse()
        {
            var list = new List<Ch>(m_Text.Length);
            var stack = new Stack<Color?>();
            Color? cur = null;
            string s = m_Text;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '<')
                {
                    int end = s.IndexOf('>', i);
                    if (end > i)
                    {
                        string tag = s.Substring(i + 1, end - i - 1);
                        if (tag.StartsWith("color="))
                        {
                            stack.Push(cur);
                            if (ColorUtility.TryParseHtmlString(tag.Substring(6).Trim('"'), out var c)) cur = c;
                            i = end; continue;
                        }
                        if (tag == "/color") { cur = stack.Count > 0 ? stack.Pop() : null; i = end; continue; }
                        if (tag == "b" || tag == "/b" || tag == "i" || tag == "/i" || tag.StartsWith("size=") || tag == "/size") { i = end; continue; }
                    }
                }
                list.Add(new Ch { c = s[i], col = cur });
            }
            return list;
        }

        /// <summary>Quebra em linhas que cabem em <paramref name="maxWidth"/> pixels da fonte (≤0 = sem limite).</summary>
        List<List<Ch>> Lines(List<Ch> chars, int maxWidth)
        {
            var lines = new List<List<Ch>>();
            var line = new List<Ch>();
            int lineW = 0;
            int i = 0;
            while (i < chars.Count)
            {
                if (chars[i].c == '\n') { lines.Add(line); line = new List<Ch>(); lineW = 0; i++; continue; }
                // próxima palavra (inclui o espaço seguinte)
                int j = i, wordW = 0;
                while (j < chars.Count && chars[j].c != ' ' && chars[j].c != '\n') { wordW += PixelFont.Advance(chars[j].c); j++; }
                bool hasSpace = j < chars.Count && chars[j].c == ' ';
                if (maxWidth > 0 && lineW > 0 && lineW + wordW - PixelFont.Spacing > maxWidth)
                {
                    TrimEnd(line);
                    lines.Add(line); line = new List<Ch>(); lineW = 0;
                }
                for (int k = i; k < j; k++) line.Add(chars[k]);
                lineW += wordW;
                if (hasSpace) { line.Add(chars[j]); lineW += PixelFont.Advance(' '); j++; }
                i = j;
            }
            TrimEnd(line);
            lines.Add(line);
            return lines;
        }

        static void TrimEnd(List<Ch> line) { while (line.Count > 0 && line[line.Count - 1].c == ' ') line.RemoveAt(line.Count - 1); }

        static int Width(List<Ch> line)
        {
            int w = 0;
            foreach (var c in line) w += PixelFont.Advance(c.c);
            return Mathf.Max(0, w - PixelFont.Spacing);
        }

        int MaxFontWidth(float w) => Mathf.Max(1, Mathf.FloorToInt(w / m_PixelSize));

        // ---------- malha ----------
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (string.IsNullOrEmpty(m_Text)) return;
            Rect r = rectTransform.rect;
            var lines = Lines(Parse(), MaxFontWidth(r.width));
            int ps = m_PixelSize;
            float totalH = (lines.Count * PixelFont.LineHeight - (PixelFont.LineHeight - PixelFont.CellHeight)) * ps;

            float y0; // topo do bloco de texto
            switch (m_Alignment)
            {
                case TextAnchor.MiddleLeft: case TextAnchor.MiddleCenter: case TextAnchor.MiddleRight:
                    y0 = r.center.y + totalH * 0.5f; break;
                case TextAnchor.LowerLeft: case TextAnchor.LowerCenter: case TextAnchor.LowerRight:
                    y0 = r.yMin + totalH; break;
                default: y0 = r.yMax; break;
            }
            y0 = Mathf.Round(y0 / ps) * ps;

            for (int pass = m_Shadow ? 0 : 1; pass < 2; pass++)
            {
                for (int li = 0; li < lines.Count; li++)
                {
                    var line = lines[li];
                    float lw = Width(line) * ps;
                    float x0;
                    switch (m_Alignment)
                    {
                        case TextAnchor.UpperCenter: case TextAnchor.MiddleCenter: case TextAnchor.LowerCenter:
                            x0 = r.center.x - lw * 0.5f; break;
                        case TextAnchor.UpperRight: case TextAnchor.MiddleRight: case TextAnchor.LowerRight:
                            x0 = r.xMax - lw; break;
                        default: x0 = r.xMin; break;
                    }
                    x0 = Mathf.Round(x0 / ps) * ps;
                    float top = y0 - li * PixelFont.LineHeight * ps;
                    int pen = 0;
                    foreach (var ch in line)
                    {
                        var g = PixelFont.Get(ch.c);
                        Color c = pass == 0 ? m_ShadowColor : (ch.col ?? color);
                        if (pass == 1 && ch.col.HasValue) c.a *= color.a;
                        if (pass == 0) c.a *= color.a;
                        for (int gy = 0; gy < PixelFont.CellHeight; gy++)
                        {
                            int gx = 0;
                            while (gx < g.width)
                            {
                                if (!g.px[gy, gx]) { gx++; continue; }
                                int start = gx;
                                while (gx < g.width && g.px[gy, gx]) gx++;
                                float qx = x0 + (pen + start) * ps + (pass == 0 ? ps : 0);
                                float qy = top - gy * ps - (pass == 0 ? ps : 0);
                                Quad(vh, qx, qy - ps, qx + (gx - start) * ps, qy, c);
                            }
                        }
                        pen += PixelFont.Advance(ch.c);
                    }
                }
            }
        }

        static void Quad(VertexHelper vh, float x0, float y0, float x1, float y1, Color32 c)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(x0, y0), c, Vector2.zero);
            vh.AddVert(new Vector3(x0, y1), c, Vector2.zero);
            vh.AddVert(new Vector3(x1, y1), c, Vector2.zero);
            vh.AddVert(new Vector3(x1, y0), c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i + 2, i + 3, i);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            SetVerticesDirty();
        }

        // ---------- layout ----------
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public float minWidth => 0;
        public float preferredWidth
        {
            get
            {
                if (string.IsNullOrEmpty(m_Text)) return 0;
                int w = 0;
                foreach (var l in Lines(Parse(), 0)) w = Mathf.Max(w, Width(l));
                return (w + (m_Shadow ? 1 : 0)) * m_PixelSize;
            }
        }
        public float flexibleWidth => -1;
        public float minHeight => 0;
        public float preferredHeight
        {
            get
            {
                if (string.IsNullOrEmpty(m_Text)) return 0;
                float w = rectTransform.rect.width;
                var lines = Lines(Parse(), w > 1 ? MaxFontWidth(w) : 0);
                return (lines.Count * PixelFont.LineHeight - (PixelFont.LineHeight - PixelFont.CellHeight) + (m_Shadow ? 1 : 0)) * m_PixelSize;
            }
        }
        public float flexibleHeight => -1;
        public int layoutPriority => 0;
    }
}
