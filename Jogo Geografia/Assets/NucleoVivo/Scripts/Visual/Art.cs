using System.Collections.Generic;
using UnityEngine;

namespace NucleoVivo
{
    /// <summary>
    /// Biblioteca de sprites gerados por código (ilustração própria, sem assets externos).
    /// Tudo é criado uma vez e mantido em cache.
    /// </summary>
    public static class Art
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

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

        // ---------- primitivas ----------
        public static Sprite Pixel => Get("pixel", () =>
        {
            var p = new Painter(4, 4); p.Fill(Color.white);
            return p.ToSprite(1f, Center, "pixel");
        });

        public static Sprite PixelBottom => Get("pixelBottom", () =>
        {
            var p = new Painter(4, 4); p.Fill(Color.white);
            return p.ToSprite(1f, BottomCenter, "pixelBottom");
        });

        public static Sprite Circle => Get("circle", () =>
        {
            var p = new Painter(128, 128); p.Circle(0.5f, 0.5f, 0.48f, Color.white);
            return p.ToSprite(1f, Center, "circle");
        });

        public static Sprite Glow => Get("glow", () =>
        {
            var p = new Painter(128, 128); p.Glow(0.5f, 0.5f, 0.5f, Color.white, 2.2f);
            return p.ToSprite(1f, Center, "glow");
        });

        public static Sprite SkyGradient => Get("sky", BuildSky);

        static Sprite BuildSky()
        {
            var p = new Painter(8, 256);
            p.Shape(_ => -1f, q =>
            {
                float t = q.y; // 0 = horizonte
                Color c = t < 0.35f ? Color.Lerp(Palette.SkyHorizon, Palette.SkyMid, t / 0.35f)
                                    : Color.Lerp(Palette.SkyMid, Palette.SkyTop, (t - 0.35f) / 0.65f);
                return c;
            }, new Rect(0, 0, 1, 1));
            return p.ToSprite(1f, BottomCenter, "sky");
        }

        /// <summary>Retângulo arredondado fatiado (9-slice) para a interface.</summary>
        public static Sprite RoundRect => Get("roundRect", () =>
        {
            var p = new Painter(64, 64);
            p.Box(0.5f, 0.5f, 0.5f, 0.5f, 0.32f, Color.white);
            return p.ToSprite(1f, Center, "roundRect", new Vector4(24, 24, 24, 24));
        });

        public static Sprite RoundRectOutline => Get("roundRectOutline", () =>
        {
            var p = new Painter(64, 64);
            Vector2 half = new Vector2(0.5f, 0.5f);
            p.Shape(q => Mathf.Abs(Painter.SdBox(q - Center, half, 0.32f)) - 0.03f, Color.white, new Rect(0, 0, 1, 1));
            return p.ToSprite(1f, Center, "roundRectOutline", new Vector4(24, 24, 24, 24));
        });

        // ---------- cenário ----------
        public static Sprite Hills(int seed) => Get("hills" + seed, () =>
        {
            var p = new Painter(512, 128);
            var pts = new List<Vector2> { V(0, 0) };
            var rnd = new System.Random(seed);
            float ph1 = (float)rnd.NextDouble() * 6f, ph2 = (float)rnd.NextDouble() * 6f;
            for (int i = 0; i <= 48; i++)
            {
                float x = i / 48f;
                float y = 0.62f + 0.18f * Mathf.Sin(x * 6.2f + ph1) + 0.1f * Mathf.Sin(x * 13f + ph2);
                pts.Add(V(x, Mathf.Clamp(y, 0.2f, 0.98f)));
            }
            pts.Add(V(1, 0));
            p.Poly(Color.white, pts.ToArray());
            return p.ToSprite(1f, BottomCenter, "hills" + seed);
        });

        public static Sprite Tree => Get("tree", () =>
        {
            var p = new Painter(64, 96);
            p.Rect01(0.44f, 0.0f, 0.56f, 0.3f, Palette.ConcreteShadow);
            p.Circle(0.5f, 0.42f, 0.2f, Palette.GreenDark);
            p.Circle(0.36f, 0.5f, 0.16f, Palette.GreenDark);
            p.Circle(0.64f, 0.52f, 0.15f, Palette.Green);
            p.Circle(0.5f, 0.66f, 0.17f, Palette.Green);
            p.Circle(0.44f, 0.72f, 0.09f, Palette.GreenLight);
            return p.ToSprite(1f, BottomCenter, "tree");
        });

        public static Sprite Reactor => Get("reactor", () =>
        {
            var p = new Painter(256, 256);
            // base
            p.Rect01(0.1f, 0.0f, 0.9f, 0.07f, Palette.ConcreteShadow, 0.01f);
            // corpo cilíndrico com sombreamento lateral
            p.Box(0.5f, 0.31f, 0.33f, 0.25f, 0.0f, q => Shade(q.x, 0.17f, 0.83f, Palette.ConcreteLight, Palette.ConcreteDark));
            // cúpula
            Vector2 c = V(0.5f, 0.56f);
            p.Shape(q => Mathf.Max((q - c).magnitude - 0.33f, c.y - q.y), q =>
            {
                Color baseC = Shade(q.x, 0.17f, 0.83f, Palette.ConcreteLight, Palette.ConcreteDark);
                float top = Mathf.Clamp01((q.y - 0.56f) / 0.33f);
                return Color.Lerp(baseC, Color.white * 0.92f + Color.black * 0.08f, top * 0.25f);
            }, Rect.MinMaxRect(0.15f, 0.55f, 0.85f, 0.92f));
            // faixa da geração (petróleo)
            p.Rect01(0.17f, 0.40f, 0.83f, 0.47f, Palette.PetrolDark);
            // emendas verticais
            for (int i = 1; i < 6; i++)
            {
                float x = 0.17f + i * 0.11f;
                p.Line(x, 0.07f, x, 0.40f, 0.004f, Palette.ConcreteDark.WithAlpha(0.5f));
            }
            // escotilha no topo
            p.Rect01(0.46f, 0.88f, 0.54f, 0.91f, Palette.ConcreteDark, 0.01f);
            // porta
            p.Rect01(0.44f, 0.07f, 0.56f, 0.2f, Palette.PetrolDark, 0.015f);
            p.Rect01(0.455f, 0.08f, 0.545f, 0.19f, Palette.Petrol, 0.01f);
            return p.ToSprite(3f, BottomCenter, "reactor");
        });

        public static Sprite CoolingTower => Get("coolingTower", () =>
        {
            var p = new Painter(192, 256);
            var pts = new List<Vector2>();
            int n = 24;
            for (int i = 0; i <= n; i++) { float y = 0.02f + 0.93f * i / n; pts.Add(V(0.5f - TowerHalf(y), y)); }
            for (int i = n; i >= 0; i--) { float y = 0.02f + 0.93f * i / n; pts.Add(V(0.5f + TowerHalf(y), y)); }
            float aspect = 192f / 256f;
            p.Poly(q =>
            {
                float nx = q.x / aspect;
                float hw = TowerHalf(q.y);
                Color c = Shade(nx, 0.5f - hw, 0.5f + hw, Palette.ConcreteLight, Palette.ConcreteDark);
                if (q.y > 0.84f && q.y < 0.88f) c = Color.Lerp(c, Palette.PetrolDark, 0.6f);
                return c;
            }, pts.ToArray());
            // pilares da base
            for (int i = 0; i < 7; i++)
            {
                float x = 0.12f + i * 0.127f;
                p.Rect01(x - 0.012f, 0.0f, x + 0.012f, 0.05f, Palette.ConcreteShadow);
            }
            return p.ToSprite(2.6f, BottomCenter, "coolingTower");
        });

        static float TowerHalf(float y) => 0.2f + 0.5f * (y - 0.68f) * (y - 0.68f);

        public static Sprite Rotor => Get("rotor", () =>
        {
            var p = new Painter(128, 128);
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                p.Line(0.5f, 0.5f, 0.5f + Mathf.Cos(a) * 0.42f, 0.5f + Mathf.Sin(a) * 0.42f, 0.11f, Color.white);
            }
            p.Circle(0.5f, 0.5f, 0.12f, Color.white);
            return p.ToSprite(1f, Center, "rotor");
        });

        public static Sprite Pylon => Get("pylon", () =>
        {
            var p = new Painter(96, 256);
            Color c = Color.white;
            float t = 0.012f;
            // pernas (coordenadas x normalizadas na largura)
            p.Line(0.12f, 0f, 0.42f, 0.95f, t, c);
            p.Line(0.88f, 0f, 0.58f, 0.95f, t, c);
            // travessas
            p.Line(0.0f, 0.78f, 1.0f, 0.78f, t, c);
            p.Line(0.12f, 0.62f, 0.88f, 0.62f, t, c);
            // treliça
            for (int i = 0; i < 4; i++)
            {
                float y0 = i * 0.2f, y1 = y0 + 0.2f;
                float l0 = Mathf.Lerp(0.12f, 0.42f, y0 / 0.95f), r0 = Mathf.Lerp(0.88f, 0.58f, y0 / 0.95f);
                float l1 = Mathf.Lerp(0.12f, 0.42f, y1 / 0.95f), r1 = Mathf.Lerp(0.88f, 0.58f, y1 / 0.95f);
                p.Line(l0, y0, r1, y1, t * 0.7f, c);
                p.Line(r0, y0, l1, y1, t * 0.7f, c);
            }
            p.Circle(0.5f, 0.97f, 0.015f, c);
            return p.ToSprite(2.4f, BottomCenter, "pylon");
        });

        static Color Shade(float x, float left, float right, Color light, Color dark)
        {
            float t = Mathf.InverseLerp(left, right, x);
            // luz vinda da esquerda; leve reflexo na borda direita
            float k = Mathf.SmoothStep(0, 1, t);
            Color c = Color.Lerp(light, dark, k);
            if (t > 0.9f) c = Color.Lerp(c, light, (t - 0.9f) * 1.5f);
            return c;
        }

        // ---------- ícones ----------
        public static Sprite Icon(IconKind kind) => Get("icon_" + kind, () => BuildIcon(kind));

        static Sprite BuildIcon(IconKind kind)
        {
            var p = new Painter(128, 128);
            Color cy = Palette.Cyan, am = Palette.Amber, cl = Palette.ConcreteLight, cd = Palette.ConcreteDark, pt = Palette.PetrolLight, ptd = Palette.Petrol, w = Palette.UiText;
            switch (kind)
            {
                case IconKind.Energy:
                    p.Glow(0.5f, 0.5f, 0.45f, am.WithAlpha(0.35f));
                    p.Poly(am, V(0.56f, 0.92f), V(0.26f, 0.46f), V(0.47f, 0.46f), V(0.40f, 0.08f), V(0.74f, 0.58f), V(0.52f, 0.58f));
                    break;
                case IconKind.ControlPanel:
                    p.Poly(cl, V(0.12f, 0.2f), V(0.88f, 0.2f), V(0.8f, 0.62f), V(0.2f, 0.62f));
                    p.Rect01(0.12f, 0.12f, 0.88f, 0.2f, cd);
                    p.Rect01(0.26f, 0.66f, 0.74f, 0.9f, ptd, 0.03f);
                    p.Rect01(0.3f, 0.7f, 0.7f, 0.86f, cy, 0.02f);
                    p.Circle(0.32f, 0.4f, 0.06f, am); p.Circle(0.5f, 0.4f, 0.06f, cy); p.Circle(0.68f, 0.4f, 0.06f, am);
                    break;
                case IconKind.Turbine:
                    p.Circle(0.5f, 0.5f, 0.42f, cd);
                    p.Circle(0.5f, 0.5f, 0.36f, ptd);
                    for (int i = 0; i < 5; i++)
                    {
                        float a = i * Mathf.PI * 2f / 5f + 0.3f;
                        p.Line(0.5f, 0.5f, 0.5f + Mathf.Cos(a) * 0.3f, 0.5f + Mathf.Sin(a) * 0.3f, 0.09f, cy);
                    }
                    p.Circle(0.5f, 0.5f, 0.09f, cl);
                    break;
                case IconKind.Substation:
                    p.Rect01(0.12f, 0.12f, 0.42f, 0.5f, cl, 0.03f);
                    p.Rect01(0.58f, 0.12f, 0.88f, 0.5f, cl, 0.03f);
                    p.Rect01(0.18f, 0.5f, 0.36f, 0.6f, cd); p.Rect01(0.64f, 0.5f, 0.82f, 0.6f, cd);
                    p.Line(0.27f, 0.6f, 0.27f, 0.85f, 0.03f, cd); p.Line(0.73f, 0.6f, 0.73f, 0.85f, 0.03f, cd);
                    p.Line(0.1f, 0.85f, 0.9f, 0.85f, 0.035f, cy);
                    p.Poly(am, V(0.52f, 0.44f), V(0.45f, 0.3f), V(0.52f, 0.3f), V(0.48f, 0.18f), V(0.57f, 0.34f), V(0.5f, 0.34f));
                    break;
                case IconKind.Automation:
                    p.Rect01(0.1f, 0.3f, 0.9f, 0.85f, cd, 0.05f);
                    p.Rect01(0.15f, 0.35f, 0.85f, 0.8f, ptd, 0.03f);
                    p.Line(0.2f, 0.45f, 0.38f, 0.6f, 0.035f, cy); p.Line(0.38f, 0.6f, 0.52f, 0.5f, 0.035f, cy);
                    p.Line(0.52f, 0.5f, 0.78f, 0.72f, 0.035f, cy);
                    p.Rect01(0.42f, 0.15f, 0.58f, 0.3f, cd); p.Rect01(0.28f, 0.1f, 0.72f, 0.16f, cl, 0.02f);
                    break;
                case IconKind.CoolingTower:
                    p.Poly(q => Shade(q.x, 0.2f, 0.8f, cl, cd), V(0.18f, 0.08f), V(0.82f, 0.08f), V(0.68f, 0.45f), V(0.7f, 0.68f), V(0.3f, 0.68f), V(0.32f, 0.45f));
                    p.Circle(0.45f, 0.78f, 0.1f, w.WithAlpha(0.85f)); p.Circle(0.58f, 0.84f, 0.12f, w.WithAlpha(0.7f)); p.Circle(0.4f, 0.9f, 0.07f, w.WithAlpha(0.55f));
                    break;
                case IconKind.Reactor:
                    p.Rect01(0.2f, 0.1f, 0.8f, 0.5f, cl);
                    p.Shape(q => Mathf.Max((q - V(0.5f, 0.5f)).magnitude - 0.3f, 0.5f - q.y), cl, Rect.MinMaxRect(0.2f, 0.5f, 0.8f, 0.8f));
                    p.Rect01(0.2f, 0.36f, 0.8f, 0.44f, cy);
                    p.Rect01(0.44f, 0.1f, 0.56f, 0.24f, ptd, 0.02f);
                    break;
                case IconKind.Grid:
                    p.Line(0.1f, 0.8f, 0.9f, 0.7f, 0.025f, cy);
                    p.Rect01(0.1f, 0.12f, 0.3f, 0.45f, cd); p.Rect01(0.34f, 0.12f, 0.56f, 0.6f, cl); p.Rect01(0.6f, 0.12f, 0.88f, 0.5f, cd);
                    for (int r = 0; r < 3; r++) { p.Rect01(0.39f, 0.2f + r * 0.12f, 0.44f, 0.26f + r * 0.12f, am); p.Rect01(0.47f, 0.2f + r * 0.12f, 0.52f, 0.26f + r * 0.12f, am); }
                    p.Rect01(0.15f, 0.22f, 0.2f, 0.28f, am); p.Rect01(0.66f, 0.22f, 0.72f, 0.28f, am); p.Rect01(0.76f, 0.34f, 0.82f, 0.4f, am);
                    break;
                case IconKind.Heat:
                    p.Glow(0.42f, 0.56f, 0.4f, am.WithAlpha(0.4f));
                    p.Circle(0.42f, 0.56f, 0.18f, am);
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; p.Line(0.42f + Mathf.Cos(a) * 0.25f, 0.56f + Mathf.Sin(a) * 0.25f, 0.42f + Mathf.Cos(a) * 0.34f, 0.56f + Mathf.Sin(a) * 0.34f, 0.04f, am); }
                    p.Rect01(0.7f, 0.22f, 0.8f, 0.7f, cl, 0.05f); p.Circle(0.75f, 0.2f, 0.08f, Palette.UiNegative); p.Rect01(0.73f, 0.2f, 0.77f, 0.55f, Palette.UiNegative);
                    break;
                case IconKind.University:
                    p.Poly(cl, V(0.1f, 0.68f), V(0.5f, 0.9f), V(0.9f, 0.68f));
                    p.Rect01(0.12f, 0.12f, 0.88f, 0.2f, cl);
                    for (int i = 0; i < 4; i++) { float x = 0.22f + i * 0.19f; p.Rect01(x - 0.04f, 0.2f, x + 0.04f, 0.64f, cd); }
                    p.Circle(0.5f, 0.77f, 0.05f, cy);
                    break;
                case IconKind.Wrench:
                    p.Line(0.28f, 0.28f, 0.66f, 0.66f, 0.12f, cl);
                    p.Circle(0.7f, 0.7f, 0.17f, cl);
                    p.Shape(q => (q - V(0.78f, 0.78f)).magnitude - 0.08f, Palette.UiCard, Rect.MinMaxRect(0.68f, 0.68f, 0.88f, 0.88f));
                    p.Circle(0.25f, 0.25f, 0.09f, cl);
                    p.Glow(0.3f, 0.75f, 0.2f, Palette.UiNegative.WithAlpha(0.8f));
                    p.Line(0.3f, 0.66f, 0.3f, 0.8f, 0.035f, Palette.UiNegative);
                    break;
                case IconKind.Protest:
                    p.Line(0.5f, 0.08f, 0.5f, 0.55f, 0.05f, cd);
                    p.Rect01(0.16f, 0.5f, 0.84f, 0.88f, cl, 0.04f);
                    p.Poly(cy, V(0.3f, 0.69f), V(0.5f, 0.8f), V(0.62f, 0.69f), V(0.5f, 0.58f));
                    p.Poly(cy, V(0.62f, 0.69f), V(0.72f, 0.77f), V(0.72f, 0.61f));
                    p.Circle(0.4f, 0.71f, 0.02f, Palette.UiCard);
                    break;
                case IconKind.Hearing:
                    p.Circle(0.3f, 0.55f, 0.1f, cl); p.Box(0.3f, 0.25f, 0.15f, 0.15f, 0.1f, cl);
                    p.Circle(0.66f, 0.5f, 0.09f, cd); p.Box(0.66f, 0.22f, 0.14f, 0.13f, 0.09f, cd);
                    p.Rect01(0.48f, 0.7f, 0.92f, 0.92f, cy, 0.06f);
                    p.Poly(cy, V(0.55f, 0.72f), V(0.5f, 0.62f), V(0.64f, 0.72f));
                    break;
                case IconKind.Press:
                    p.Rect01(0.14f, 0.12f, 0.86f, 0.86f, cl, 0.04f);
                    p.Rect01(0.22f, 0.68f, 0.78f, 0.78f, Palette.UiCard);
                    p.Rect01(0.22f, 0.36f, 0.48f, 0.6f, cy, 0.02f);
                    for (int i = 0; i < 4; i++) p.Rect01(0.54f, 0.56f - i * 0.07f, 0.78f, 0.59f - i * 0.07f, cd);
                    p.Rect01(0.22f, 0.22f, 0.78f, 0.25f, cd); p.Rect01(0.22f, 0.29f, 0.7f, 0.32f, cd);
                    break;
                case IconKind.Nucleo:
                    p.Glow(0.5f, 0.5f, 0.48f, cy.WithAlpha(0.45f));
                    p.Ring(0.5f, 0.5f, 0.34f, 0.035f, cy.WithAlpha(0.9f));
                    p.Ring(0.5f, 0.5f, 0.22f, 0.03f, am.WithAlpha(0.9f));
                    p.Circle(0.5f, 0.5f, 0.11f, w);
                    p.Circle(0.84f, 0.5f, 0.05f, cy); p.Circle(0.33f, 0.79f, 0.05f, cy); p.Circle(0.66f, 0.31f, 0.045f, am);
                    break;
                case IconKind.Tap:
                    p.Ring(0.5f, 0.56f, 0.3f, 0.03f, cy.WithAlpha(0.5f));
                    p.Ring(0.5f, 0.56f, 0.2f, 0.035f, cy.WithAlpha(0.8f));
                    p.Circle(0.5f, 0.56f, 0.1f, cy);
                    p.Poly(am, V(0.52f, 0.36f), V(0.4f, 0.12f), V(0.5f, 0.12f), V(0.46f, 0.02f), V(0.62f, 0.22f), V(0.53f, 0.22f));
                    break;
                case IconKind.Passive:
                    p.Arc(0.5f, 0.5f, 0.32f, 0.07f, 20f, 300f, cy);
                    p.Poly(cy, V(0.78f, 0.72f), V(0.92f, 0.52f), V(0.66f, 0.5f));
                    p.Poly(am, V(0.54f, 0.72f), V(0.38f, 0.46f), V(0.5f, 0.46f), V(0.45f, 0.26f), V(0.63f, 0.54f), V(0.52f, 0.54f));
                    break;
                case IconKind.Discount:
                    p.Poly(cl, V(0.12f, 0.5f), V(0.42f, 0.86f), V(0.86f, 0.86f), V(0.86f, 0.14f), V(0.42f, 0.14f));
                    p.Circle(0.3f, 0.5f, 0.05f, Palette.UiCard);
                    p.Line(0.5f, 0.3f, 0.74f, 0.7f, 0.04f, Palette.Petrol);
                    p.Ring(0.53f, 0.64f, 0.05f, 0.03f, Palette.Petrol); p.Ring(0.71f, 0.36f, 0.05f, 0.03f, Palette.Petrol);
                    break;
                case IconKind.Sound:
                    p.Poly(cl, V(0.14f, 0.38f), V(0.3f, 0.38f), V(0.5f, 0.2f), V(0.5f, 0.8f), V(0.3f, 0.62f), V(0.14f, 0.62f));
                    p.Arc(0.5f, 0.5f, 0.16f, 0.05f, -50f, 50f, cy);
                    p.Arc(0.5f, 0.5f, 0.3f, 0.05f, -50f, 50f, cy);
                    break;
                case IconKind.Reputation:
                    p.Poly(pt, V(0.5f, 0.92f), V(0.84f, 0.78f), V(0.8f, 0.4f), V(0.5f, 0.08f), V(0.2f, 0.4f), V(0.16f, 0.78f));
                    p.Line(0.34f, 0.5f, 0.46f, 0.38f, 0.07f, cy); p.Line(0.46f, 0.38f, 0.68f, 0.66f, 0.07f, cy);
                    break;
                case IconKind.Settings:
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4f; p.Line(0.5f, 0.5f, 0.5f + Mathf.Cos(a) * 0.4f, 0.5f + Mathf.Sin(a) * 0.4f, 0.14f, cl); }
                    p.Circle(0.5f, 0.5f, 0.3f, cl);
                    p.Circle(0.5f, 0.5f, 0.13f, Palette.UiCard);
                    break;
                case IconKind.Lock:
                    p.Arc(0.5f, 0.56f, 0.18f, 0.07f, 0f, 180f, cd);
                    p.Line(0.32f, 0.5f, 0.32f, 0.56f, 0.07f, cd); p.Line(0.68f, 0.5f, 0.68f, 0.56f, 0.07f, cd);
                    p.Rect01(0.22f, 0.12f, 0.78f, 0.52f, cl, 0.05f);
                    p.Circle(0.5f, 0.34f, 0.06f, Palette.UiCard);
                    break;
            }
            return p.ToSprite(1f, Center, "icon_" + kind);
        }
    }
}
