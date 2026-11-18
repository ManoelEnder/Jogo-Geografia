using System.Collections.Generic;
using UnityEngine;

namespace NucleoVivo
{
    /// <summary>
    /// Sprites em pixel art gerados por código, sem antisserrilhado, com contorno de 1 pixel
    /// e sombreamento em faixas. Escala do mundo: 24 pixels por unidade.
    /// </summary>
    public static class Art
    {
        public const float WorldPPU = 24f;

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly Color Ink = new Color32(0x08, 0x10, 0x14, 255);

        static Sprite Get(string key, System.Func<Sprite> build)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = build();
            cache[key] = s;
            return s;
        }

        static Vector2 V(float x, float y) => new Vector2(x, y);
        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        static readonly Vector2 BottomCenter = new Vector2(0.5f, 0f);

        /// <summary>Sprite em que cada texel ocupa 1/24 de unidade.</summary>
        static Sprite WorldSprite(Painter p, Vector2 pivot, string name) => p.ToSprite(p.Height / WorldPPU, pivot, name);

        /// <summary>Três tons em faixas (luz da esquerda), como num sprite feito à mão.</summary>
        static Color Band(float x, float left, float right, Color light, Color mid, Color dark)
        {
            float t = Mathf.InverseLerp(left, right, x);
            return t < 0.3f ? light : t < 0.78f ? mid : dark;
        }

        // ---------- primitivas ----------
        public static Sprite Pixel => Get("pixel", () =>
        {
            var p = new Painter(1, 1, true); p.Fill(Color.white);
            return p.ToSprite(1f, Center, "pixel");
        });

        public static Sprite PixelBottom => Get("pixelBottom", () =>
        {
            var p = new Painter(1, 1, true); p.Fill(Color.white);
            return p.ToSprite(1f, BottomCenter, "pixelBottom");
        });

        public static Sprite Circle => Get("circle", () =>
        {
            var p = new Painter(16, 16, true); p.Circle(0.5f, 0.5f, 0.47f, Color.white);
            return p.ToSprite(1f, Center, "circle");
        });

        /// <summary>Halo pontilhado (substitui brilhos difusos).</summary>
        public static Sprite Glow => Get("glow", () =>
        {
            var p = new Painter(128, 128, true); p.Glow(0.5f, 0.5f, 0.5f, Color.white, 1.6f);
            return p.ToSprite(1f, Center, "glow");
        });

        public static Sprite SkyGradient => Get("sky", () =>
        {
            var p = new Painter(320, 560, true);
            Color[] bands = { Palette.SkyHorizon, Color.Lerp(Palette.SkyHorizon, Palette.SkyMid, 0.5f), Palette.SkyMid, Color.Lerp(Palette.SkyMid, Palette.SkyTop, 0.5f), Palette.SkyTop };
            for (int y = 0; y < p.Height; y++)
            {
                float t = Mathf.Clamp01((y / (float)p.Height - 0.05f) / 0.6f) * (bands.Length - 1);
                int i = Mathf.Min(bands.Length - 2, (int)t);
                float f = t - i;
                for (int x = 0; x < p.Width; x++)
                    p.SetPixel(x, y, f > Painter.Bayer(x, y) ? bands[i + 1] : bands[i]);
            }
            return WorldSprite(p, BottomCenter, "sky");
        });

        // ---------- interface ----------
        static readonly string[] FrameMap =
        {
            ".kkkkkkk.",
            "kwwwwwwwk",
            "kwfffffsk",
            "kwfffffsk",
            "kwfffffsk",
            "kwfffffsk",
            "kwfffffsk",
            "ksssssssk",
            ".kkkkkkk.",
        };

        static Sprite Frame(string[] map, string name, float w, float f, float sh)
        {
            int n = map.Length;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = name };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    char c = map[y][x];
                    Color col = c == 'k' ? new Color(0.03f, 0.06f, 0.08f, 1f) : c == 'w' ? new Color(w, w, w, 1) : c == 'f' ? new Color(f, f, f, 1) : c == 's' ? new Color(sh, sh, sh, 1) : new Color(0, 0, 0, 0);
                    tex.SetPixel(x, n - 1 - y, col);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), Center, 100f, 0, SpriteMeshType.FullRect, new Vector4(3, 3, 3, 3));
        }

        /// <summary>Moldura de pixel com relevo (9 fatias). Usar Image.pixelsPerUnitMultiplier = 1/3.</summary>
        public static Sprite RoundRect => Get("frame", () => Frame(FrameMap, "frame", 1f, 0.82f, 0.6f));

        /// <summary>Moldura apenas com borda clara (destaques).</summary>
        public static Sprite RoundRectOutline => Get("frameOutline", () =>
        {
            string[] m =
            {
                ".wwwwwww.",
                "wwwwwwwww",
                "ww.....ww",
                "ww.....ww",
                "ww.....ww",
                "ww.....ww",
                "ww.....ww",
                "wwwwwwwww",
                ".wwwwwww.",
            };
            return Frame(m, "frameOutline", 1f, 1f, 1f);
        });

        // ---------- cenário ----------
        public static Sprite Hills(int seed, Color body, Color rim) => Get("hills" + seed + body, () =>
        {
            var p = new Painter(336, 60, true);
            var pts = new List<Vector2> { V(0, 0) };
            var rnd = new System.Random(seed);
            float ph1 = (float)rnd.NextDouble() * 6f, ph2 = (float)rnd.NextDouble() * 6f;
            for (int i = 0; i <= 42; i++)
            {
                float x = i / 42f;
                float y = 0.6f + 0.2f * Mathf.Sin(x * 6.2f + ph1) + 0.1f * Mathf.Sin(x * 13f + ph2);
                pts.Add(V(x, Mathf.Clamp(y, 0.2f, 0.97f)));
            }
            pts.Add(V(1, 0));
            p.Poly(body, pts.ToArray());
            // crista iluminada com pontilhado
            for (int x = 0; x < p.Width; x++)
            {
                int top = -1;
                for (int y = p.Height - 1; y >= 0; y--) if (p.GetPixel(x, y).a > 0.5f) { top = y; break; }
                for (int k = 0; k < 4 && top - k >= 0; k++)
                    if (k == 0 || (1f - k / 4f) > Painter.Bayer(x, top - k)) p.SetPixel(x, top - k, rim);
            }
            return WorldSprite(p, BottomCenter, "hills" + seed);
        });

        public static Sprite Tree => Get("tree", () =>
        {
            var p = new Painter(14, 20, true);
            p.Rect01(0.43f, 0.0f, 0.57f, 0.32f, Palette.ConcreteShadow);
            p.Circle(0.5f, 0.42f, 0.2f, Palette.GreenDark);
            p.Circle(0.33f, 0.5f, 0.17f, Palette.GreenDark);
            p.Circle(0.66f, 0.52f, 0.16f, Palette.Green);
            p.Circle(0.5f, 0.68f, 0.18f, Palette.Green);
            p.Circle(0.44f, 0.74f, 0.08f, Palette.GreenLight);
            p.Outline(Ink);
            return WorldSprite(p, BottomCenter, "tree");
        });

        public static Sprite Reactor => Get("reactor", () =>
        {
            var p = new Painter(72, 72, true);
            Color L = Palette.ConcreteLight, M = Palette.Concrete, D = Palette.ConcreteDark;
            p.Rect01(0.1f, 0.0f, 0.9f, 0.07f, Palette.ConcreteShadow);
            p.Box(0.5f, 0.31f, 0.33f, 0.25f, 0f, q => Band(q.x, 0.17f, 0.83f, L, M, D));
            Vector2 c = V(0.5f, 0.56f);
            p.Shape(q => Mathf.Max((q - c).magnitude - 0.33f, c.y - q.y), q =>
            {
                Color b = Band(q.x, 0.17f, 0.83f, L, M, D);
                return q.y > 0.82f && q.x < 0.62f ? Color.white * 0.95f + Color.black * 0.05f : b;
            }, Rect.MinMaxRect(0.15f, 0.55f, 0.85f, 0.92f));
            p.Rect01(0.17f, 0.40f, 0.83f, 0.47f, Palette.PetrolDark);
            for (int i = 1; i < 6; i++)
            {
                float x = 0.17f + i * 0.11f;
                p.Rect01(x, 0.07f, x + 1f / 72f, 0.40f, D);
            }
            p.Rect01(0.46f, 0.88f, 0.54f, 0.92f, D);
            p.Rect01(0.43f, 0.07f, 0.57f, 0.21f, Palette.PetrolDark);
            p.Rect01(0.46f, 0.08f, 0.54f, 0.19f, Palette.Petrol);
            p.Outline(Ink);
            return WorldSprite(p, BottomCenter, "reactor");
        });

        public static Sprite CoolingTower => Get("coolingTower", () =>
        {
            var p = new Painter(48, 64, true);
            var pts = new List<Vector2>();
            int n = 16;
            for (int i = 0; i <= n; i++) { float y = 0.02f + 0.93f * i / n; pts.Add(V(0.5f - TowerHalf(y), y)); }
            for (int i = n; i >= 0; i--) { float y = 0.02f + 0.93f * i / n; pts.Add(V(0.5f + TowerHalf(y), y)); }
            float aspect = 48f / 64f;
            p.Poly(q =>
            {
                float nx = q.x / aspect;
                float hw = TowerHalf(q.y);
                Color c = Band(nx, 0.5f - hw, 0.5f + hw, Palette.ConcreteLight, Palette.Concrete, Palette.ConcreteDark);
                if (q.y > 0.84f && q.y < 0.89f) c = Palette.PetrolDark;
                return c;
            }, pts.ToArray());
            for (int i = 0; i < 6; i++)
            {
                float x = 0.14f + i * 0.145f;
                p.Rect01(x, 0.0f, x + 0.04f, 0.05f, Palette.ConcreteShadow);
            }
            p.Outline(Ink);
            return WorldSprite(p, BottomCenter, "coolingTower");
        });

        static float TowerHalf(float y) => 0.2f + 0.5f * (y - 0.68f) * (y - 0.68f);

        public static Sprite Rotor => Get("rotor", () =>
        {
            var p = new Painter(16, 16, true);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2f + 0.4f;
                p.Line(0.5f, 0.5f, 0.5f + Mathf.Cos(a) * 0.42f, 0.5f + Mathf.Sin(a) * 0.42f, 0.16f, Color.white);
            }
            p.Circle(0.5f, 0.5f, 0.13f, Color.white);
            return p.ToSprite(1f, Center, "rotor");
        });

        public static Sprite Pylon => Get("pylon", () =>
        {
            var p = new Painter(22, 58, true);
            Color c = Color.white;
            float t = 1.3f / 58f;
            p.Line(0.1f, 0f, 0.4f, 0.95f, t, c);
            p.Line(0.9f, 0f, 0.6f, 0.95f, t, c);
            p.Line(0.0f, 0.78f, 1.0f, 0.78f, t, c);
            p.Line(0.12f, 0.62f, 0.88f, 0.62f, t, c);
            for (int i = 0; i < 4; i++)
            {
                float y0 = i * 0.2f, y1 = y0 + 0.2f;
                float l0 = Mathf.Lerp(0.1f, 0.4f, y0 / 0.95f), r0 = Mathf.Lerp(0.9f, 0.6f, y0 / 0.95f);
                float l1 = Mathf.Lerp(0.1f, 0.4f, y1 / 0.95f), r1 = Mathf.Lerp(0.9f, 0.6f, y1 / 0.95f);
                p.Line(l0, y0, r1, y1, t, c);
                p.Line(r0, y0, l1, y1, t, c);
            }
            return WorldSprite(p, BottomCenter, "pylon");
        });

        /// <summary>Texto em pixel no mundo (1 pixel da fonte = 1 pixel do cenário).</summary>
        public static Sprite WorldText(string text, Color color)
        {
            var tex = PixelFont.Render(text, color, Ink);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Center, WorldPPU, 0, SpriteMeshType.FullRect);
        }

        // ---------- ícones ----------
        public static Sprite Icon(IconKind kind) => PixelIcons.Get(kind);
    }
}
