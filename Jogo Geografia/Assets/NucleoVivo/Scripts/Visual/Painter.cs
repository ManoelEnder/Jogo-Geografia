using System;
using UnityEngine;

namespace NucleoVivo
{
    /// <summary>
    /// Pequeno rasterizador por campos de distância (SDF) com antisserrilhado.
    /// Coordenadas normalizadas: (0,0) canto inferior esquerdo, (1,1) superior direito.
    /// Usado para gerar todos os sprites e ícones do jogo em tempo de execução.
    /// </summary>
    public class Painter
    {
        public readonly int Width, Height;
        readonly Color[] px;
        readonly float aspect;

        public Painter(int width, int height)
        {
            Width = width; Height = height;
            px = new Color[width * height];
            aspect = (float)width / height;
        }

        // Converte para um espaço onde x vai de 0..aspect e y de 0..1, para que círculos fiquem redondos.
        Vector2 P(int x, int y) => new Vector2((x + 0.5f) / Height, (y + 0.5f) / Height);
        float X(float nx) => nx * aspect; // x normalizado (0..1) para o espaço interno

        void Blend(int i, Color c, float coverage)
        {
            if (coverage <= 0f) return;
            float a = c.a * Mathf.Clamp01(coverage);
            Color d = px[i];
            float outA = a + d.a * (1f - a);
            if (outA <= 1e-5f) return;
            Color o = (c * a + d * (d.a * (1f - a))) / outA;
            o.a = outA;
            px[i] = o;
        }

        /// <summary>Desenha qualquer forma dada por uma função de distância (negativa dentro).</summary>
        public void Shape(Func<Vector2, float> sdf, Func<Vector2, Color> color, Rect boundsNorm)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(boundsNorm.xMin * Width) - 2);
            int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt(boundsNorm.xMax * Width) + 2);
            int y0 = Mathf.Max(0, Mathf.FloorToInt(boundsNorm.yMin * Height) - 2);
            int y1 = Mathf.Min(Height - 1, Mathf.CeilToInt(boundsNorm.yMax * Height) + 2);
            float pxSize = 1f / Height;
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    Vector2 p = P(x, y);
                    float d = sdf(p);
                    float cov = Mathf.Clamp01(0.5f - d / pxSize);
                    if (cov > 0) Blend(y * Width + x, color(p), cov);
                }
        }

        public void Shape(Func<Vector2, float> sdf, Color color, Rect boundsNorm) => Shape(sdf, _ => color, boundsNorm);

        // ---------- primitivas (coordenadas normalizadas 0..1) ----------

        public void Circle(float cx, float cy, float r, Color c)
        {
            Vector2 ctr = new Vector2(X(cx), cy);
            float rr = r;
            Shape(p => (p - ctr).magnitude - rr, c, Bounds(cx, cy, r / aspect, r));
        }

        public void Ring(float cx, float cy, float r, float thickness, Color c)
        {
            Vector2 ctr = new Vector2(X(cx), cy);
            Shape(p => Mathf.Abs((p - ctr).magnitude - r) - thickness * 0.5f, c, Bounds(cx, cy, (r + thickness) / aspect, r + thickness));
        }

        /// <summary>Arco de anel entre dois ângulos (graus, 0 = direita, sentido anti-horário).</summary>
        public void Arc(float cx, float cy, float r, float thickness, float fromDeg, float toDeg, Color c)
        {
            Vector2 ctr = new Vector2(X(cx), cy);
            float a0 = fromDeg * Mathf.Deg2Rad, a1 = toDeg * Mathf.Deg2Rad;
            Vector2 e0 = ctr + new Vector2(Mathf.Cos(a0), Mathf.Sin(a0)) * r;
            Vector2 e1 = ctr + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * r;
            Shape(p =>
            {
                Vector2 v = p - ctr;
                float ang = Mathf.Atan2(v.y, v.x);
                if (ang < a0) ang += Mathf.PI * 2f;
                bool inside = ang >= a0 && ang <= a1;
                if (inside) return Mathf.Abs(v.magnitude - r) - thickness * 0.5f;
                return Mathf.Min((p - e0).magnitude, (p - e1).magnitude) - thickness * 0.5f;
            }, c, Bounds(cx, cy, (r + thickness) / aspect, r + thickness));
        }

        public void Box(float cx, float cy, float halfW, float halfH, float radius, Color c)
        {
            Vector2 ctr = new Vector2(X(cx), cy);
            Vector2 half = new Vector2(X(halfW), halfH);
            Shape(p => SdBox(p - ctr, half, radius), c, Bounds(cx, cy, halfW, halfH));
        }

        public void Box(float cx, float cy, float halfW, float halfH, float radius, Func<Vector2, Color> c)
        {
            Vector2 ctr = new Vector2(X(cx), cy);
            Vector2 half = new Vector2(X(halfW), halfH);
            Shape(p => SdBox(p - ctr, half, radius), c, Bounds(cx, cy, halfW, halfH));
        }

        public void Rect01(float xMin, float yMin, float xMax, float yMax, Color c, float radius = 0)
        {
            Box((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, (xMax - xMin) * 0.5f, (yMax - yMin) * 0.5f, radius, c);
        }

        public void Line(float x0, float y0, float x1, float y1, float thickness, Color c)
        {
            Vector2 a = new Vector2(X(x0), y0), b = new Vector2(X(x1), y1);
            float h = thickness * 0.5f;
            Shape(p => SdSegment(p, a, b) - h, c,
                Rect.MinMaxRect(Mathf.Min(x0, x1) - h / aspect, Mathf.Min(y0, y1) - h, Mathf.Max(x0, x1) + h / aspect, Mathf.Max(y0, y1) + h));
        }

        public void Poly(Color c, params Vector2[] ptsNorm)
        {
            var pts = new Vector2[ptsNorm.Length];
            float minX = 1, minY = 1, maxX = 0, maxY = 0;
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i] = new Vector2(X(ptsNorm[i].x), ptsNorm[i].y);
                minX = Mathf.Min(minX, ptsNorm[i].x); maxX = Mathf.Max(maxX, ptsNorm[i].x);
                minY = Mathf.Min(minY, ptsNorm[i].y); maxY = Mathf.Max(maxY, ptsNorm[i].y);
            }
            Shape(p => SdPolygon(p, pts), c, Rect.MinMaxRect(minX, minY, maxX, maxY));
        }

        public void Poly(Func<Vector2, Color> c, params Vector2[] ptsNorm)
        {
            var pts = new Vector2[ptsNorm.Length];
            float minX = 1, minY = 1, maxX = 0, maxY = 0;
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i] = new Vector2(X(ptsNorm[i].x), ptsNorm[i].y);
                minX = Mathf.Min(minX, ptsNorm[i].x); maxX = Mathf.Max(maxX, ptsNorm[i].x);
                minY = Mathf.Min(minY, ptsNorm[i].y); maxY = Mathf.Max(maxY, ptsNorm[i].y);
            }
            Shape(p => SdPolygon(p, pts), c, Rect.MinMaxRect(minX, minY, maxX, maxY));
        }

        /// <summary>Brilho radial suave (para luzes e halos).</summary>
        public void Glow(float cx, float cy, float r, Color c, float power = 2f)
        {
            Vector2 ctr = new Vector2(X(cx), cy);
            int x0 = Mathf.Max(0, Mathf.FloorToInt((cx - r / aspect) * Width));
            int x1 = Mathf.Min(Width - 1, Mathf.CeilToInt((cx + r / aspect) * Width));
            int y0 = Mathf.Max(0, Mathf.FloorToInt((cy - r) * Height));
            int y1 = Mathf.Min(Height - 1, Mathf.CeilToInt((cy + r) * Height));
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float d = (P(x, y) - ctr).magnitude / r;
                    if (d >= 1f) continue;
                    Blend(y * Width + x, c, Mathf.Pow(1f - d, power));
                }
        }

        public void VerticalGradient(Color bottom, Color top)
        {
            for (int y = 0; y < Height; y++)
            {
                Color c = Color.Lerp(bottom, top, (y + 0.5f) / Height);
                for (int x = 0; x < Width; x++) px[y * Width + x] = c;
            }
        }

        public void Fill(Color c)
        {
            for (int i = 0; i < px.Length; i++) px[i] = c;
        }

        static Rect Bounds(float cx, float cy, float hw, float hh) => Rect.MinMaxRect(cx - hw, cy - hh, cx + hw, cy + hh);

        // ---------- SDFs ----------
        public static float SdBox(Vector2 p, Vector2 half, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + new Vector2(r, r);
            Vector2 mq = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0));
            return mq.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
        }

        public static float SdSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(1e-8f, Vector2.Dot(ba, ba)));
            return (pa - ba * h).magnitude;
        }

        public static float SdPolygon(Vector2 p, Vector2[] v)
        {
            float d = Vector2.Dot(p - v[0], p - v[0]);
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i];
                Vector2 w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
                d = Mathf.Min(d, Vector2.Dot(b, b));
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        // ---------- saída ----------
        public Texture2D ToTexture(bool mipmaps = false)
        {
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, mipmaps);
            // Pré-ajusta a cor dos pixels transparentes para evitar bordas escuras na filtragem.
            var outPx = new Color[px.Length];
            for (int i = 0; i < px.Length; i++)
            {
                Color c = px[i];
                outPx[i] = c.a <= 0.001f ? new Color(c.r, c.g, c.b, 0) : c;
            }
            tex.SetPixels(outPx);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply(mipmaps, true);
            return tex;
        }

        /// <summary>Cria um sprite em que a altura da textura corresponde a <paramref name="worldHeight"/> unidades.</summary>
        public Sprite ToSprite(float worldHeight, Vector2 pivot, string name = "sprite", Vector4 border = default)
        {
            var tex = ToTexture();
            tex.name = name;
            var s = Sprite.Create(tex, new Rect(0, 0, Width, Height), pivot, Height / worldHeight, 0, SpriteMeshType.FullRect, border);
            s.name = name;
            return s;
        }
    }
}
