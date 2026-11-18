using System.Collections.Generic;
using UnityEngine;

namespace NucleoVivo
{
    /// <summary>
    /// Fonte bitmap própria, desenhada pixel a pixel (5x7 com acentos e descendentes).
    /// Célula de 11 linhas: 0-1 acentos de maiúsculas, 2-8 corpo, 9-10 descendentes.
    /// </summary>
    public static class PixelFont
    {
        public const int CellHeight = 11;
        public const int LineHeight = 13;
        public const int Spacing = 1;

        public class Glyph
        {
            public int width;
            public bool[,] px; // [linha, coluna], linha 0 = topo
        }

        static Dictionary<char, Glyph> glyphs;

        public static Glyph Get(char c)
        {
            if (glyphs == null) Build();
            if (glyphs.TryGetValue(c, out var g)) return g;
            return glyphs['?'];
        }

        public static int Advance(char c) => c == ' ' ? 3 : Get(c).width + Spacing;

        // ---------- desenho dos caracteres ----------
        static readonly Dictionary<char, string[]> Defs = new Dictionary<char, string[]>
        {
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
            ['C'] = new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." },
            ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
            ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
            ['G'] = new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".###." },
            ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['I'] = new[] { "###", ".#.", ".#.", ".#.", ".#.", ".#.", "###" },
            ['J'] = new[] { "..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.." },
            ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
            ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
            ['M'] = new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
            ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
            ['Q'] = new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" },
            ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." },
            ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "#.#.#", ".#.#." },
            ['X'] = new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" },
            ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
            ['Z'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },

            ['a'] = new[] { ".....", ".....", ".###.", "....#", ".####", "#...#", ".####" },
            ['b'] = new[] { "#....", "#....", "####.", "#...#", "#...#", "#...#", "####." },
            ['c'] = new[] { ".....", ".....", ".###.", "#....", "#....", "#....", ".###." },
            ['d'] = new[] { "....#", "....#", ".####", "#...#", "#...#", "#...#", ".####" },
            ['e'] = new[] { ".....", ".....", ".###.", "#...#", "#####", "#....", ".###." },
            ['f'] = new[] { "..##", ".#..", "####", ".#..", ".#..", ".#..", ".#.." },
            ['g'] = new[] { ".....", ".....", ".####", "#...#", "#...#", "#...#", ".####", "....#", ".###." },
            ['h'] = new[] { "#....", "#....", "####.", "#...#", "#...#", "#...#", "#...#" },
            ['i'] = new[] { "#", ".", "#", "#", "#", "#", "#" },
            ['j'] = new[] { "..#", "...", "..#", "..#", "..#", "..#", "..#", "#.#", ".#." },
            ['k'] = new[] { "#...", "#...", "#..#", "#.#.", "##..", "#.#.", "#..#" },
            ['l'] = new[] { "#.", "#.", "#.", "#.", "#.", "#.", ".#" },
            ['m'] = new[] { ".....", ".....", "##.#.", "#.#.#", "#.#.#", "#.#.#", "#.#.#" },
            ['n'] = new[] { ".....", ".....", "####.", "#...#", "#...#", "#...#", "#...#" },
            ['o'] = new[] { ".....", ".....", ".###.", "#...#", "#...#", "#...#", ".###." },
            ['p'] = new[] { ".....", ".....", "####.", "#...#", "#...#", "#...#", "####.", "#....", "#...." },
            ['q'] = new[] { ".....", ".....", ".####", "#...#", "#...#", "#...#", ".####", "....#", "....#" },
            ['r'] = new[] { "....", "....", "#.##", "##..", "#...", "#...", "#..." },
            ['s'] = new[] { ".....", ".....", ".####", "#....", ".###.", "....#", "####." },
            ['t'] = new[] { ".#..", ".#..", "####", ".#..", ".#..", ".#..", "..##" },
            ['u'] = new[] { ".....", ".....", "#...#", "#...#", "#...#", "#...#", ".####" },
            ['v'] = new[] { ".....", ".....", "#...#", "#...#", "#...#", ".#.#.", "..#.." },
            ['w'] = new[] { ".....", ".....", "#...#", "#...#", "#.#.#", "#.#.#", ".#.#." },
            ['x'] = new[] { ".....", ".....", "#...#", ".#.#.", "..#..", ".#.#.", "#...#" },
            ['y'] = new[] { ".....", ".....", "#...#", "#...#", "#...#", "#...#", ".####", "....#", ".###." },
            ['z'] = new[] { ".....", ".....", "#####", "...#.", "..#..", ".#...", "#####" },

            ['0'] = new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
            ['1'] = new[] { ".#.", "##.", ".#.", ".#.", ".#.", ".#.", "###" },
            ['2'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" },
            ['3'] = new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." },
            ['4'] = new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." },
            ['5'] = new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
            ['6'] = new[] { ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###." },
            ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
            ['8'] = new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
            ['9'] = new[] { ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###." },

            ['.'] = new[] { ".", ".", ".", ".", ".", ".", "#" },
            [','] = new[] { "..", "..", "..", "..", "..", "..", ".#", "#." },
            [':'] = new[] { ".", ".", "#", ".", ".", ".", "#" },
            [';'] = new[] { "..", "..", ".#", "..", "..", "..", ".#", "#." },
            ['!'] = new[] { "#", "#", "#", "#", "#", ".", "#" },
            ['?'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.." },
            ['('] = new[] { ".#", "#.", "#.", "#.", "#.", "#.", ".#" },
            [')'] = new[] { "#.", ".#", ".#", ".#", ".#", ".#", "#." },
            ['['] = new[] { "##", "#.", "#.", "#.", "#.", "#.", "##" },
            [']'] = new[] { "##", ".#", ".#", ".#", ".#", ".#", "##" },
            ['-'] = new[] { "...", "...", "...", "###", "...", "...", "..." },
            ['−'] = new[] { "....", "....", "....", "####", "....", "....", "...." },
            ['—'] = new[] { ".....", ".....", ".....", "#####", ".....", ".....", "....." },
            ['+'] = new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." },
            ['='] = new[] { ".....", ".....", "#####", ".....", "#####", ".....", "....." },
            ['/'] = new[] { "..#", "..#", ".#.", ".#.", ".#.", "#..", "#.." },
            ['%'] = new[] { "##..#", "##..#", "...#.", "..#..", ".#...", "#..##", "#..##" },
            ['"'] = new[] { "#.#", "#.#", "...", "...", "...", "...", "..." },
            ['\''] = new[] { "#", "#", ".", ".", ".", ".", "." },
            ['·'] = new[] { ".", ".", ".", "#", ".", ".", "." },
            ['•'] = new[] { "..", "..", "..", "##", "##", "..", ".." },
            ['→'] = new[] { ".....", "..#..", "...#.", "#####", "...#.", "..#..", "....." },
            ['…'] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "#.#.#" },
            ['°'] = new[] { "###", "#.#", "###", "...", "...", "...", "..." },
            ['ª'] = new[] { "###", "..#", "###", "#.#", "###", "...", "..." },
        };

        static readonly string[] Acute = { "..#", ".#." };
        static readonly string[] Grave = { "#..", ".#." };
        static readonly string[] Circ = { ".#.", "#.#" };
        static readonly string[] Tilde = { ".#.#", "#.#." };
        static readonly string[] Dots = { "#.#", "..." };

        static void Build()
        {
            glyphs = new Dictionary<char, Glyph>();
            foreach (var kv in Defs) glyphs[kv.Key] = FromRows(kv.Value, 2);
            glyphs[' '] = new Glyph { width = 2, px = new bool[CellHeight, 2] };

            // Acentos: maiúsculas recebem a marca nas linhas 0-1; minúsculas nas linhas 2-3.
            Accent('Á', 'A', Acute, true); Accent('À', 'A', Grave, true); Accent('Â', 'A', Circ, true); Accent('Ã', 'A', Tilde, true);
            Accent('É', 'E', Acute, true); Accent('Ê', 'E', Circ, true); Accent('Í', 'I', Acute, true);
            Accent('Ó', 'O', Acute, true); Accent('Ô', 'O', Circ, true); Accent('Õ', 'O', Tilde, true);
            Accent('Ú', 'U', Acute, true); Accent('Ü', 'U', Dots, true);
            Accent('á', 'a', Acute, false); Accent('à', 'a', Grave, false); Accent('â', 'a', Circ, false); Accent('ã', 'a', Tilde, false);
            Accent('é', 'e', Acute, false); Accent('ê', 'e', Circ, false);
            Accent('ó', 'o', Acute, false); Accent('ô', 'o', Circ, false); Accent('õ', 'o', Tilde, false);
            Accent('ú', 'u', Acute, false); Accent('ü', 'u', Dots, false);
            glyphs['í'] = FromRows(new[] { ".#", "#.", ".#", ".#", ".#", ".#", ".#" }, 2);
            Cedilla('ç', 'c'); Cedilla('Ç', 'C');

            // Fonte proporcional: remove colunas vazias à esquerda e à direita.
            var keys = new List<char>(glyphs.Keys);
            foreach (var k in keys) if (k != ' ') glyphs[k] = Trim(glyphs[k]);
        }

        static Glyph Trim(Glyph g)
        {
            int min = g.width, max = -1;
            for (int y = 0; y < CellHeight; y++)
                for (int x = 0; x < g.width; x++)
                    if (g.px[y, x]) { if (x < min) min = x; if (x > max) max = x; }
            if (max < 0) return g;
            int w = max - min + 1;
            var t = new Glyph { width = w, px = new bool[CellHeight, w] };
            for (int y = 0; y < CellHeight; y++)
                for (int x = 0; x < w; x++)
                    t.px[y, x] = g.px[y, x + min];
            return t;
        }

        static Glyph FromRows(string[] rows, int top)
        {
            int w = 0;
            foreach (var r in rows) w = Mathf.Max(w, r.Length);
            var g = new Glyph { width = w, px = new bool[CellHeight, w] };
            for (int y = 0; y < rows.Length && top + y < CellHeight; y++)
                for (int x = 0; x < rows[y].Length; x++)
                    g.px[top + y, x] = rows[y][x] == '#';
            return g;
        }

        static void Accent(char target, char baseChar, string[] mark, bool upper)
        {
            var b = glyphs[baseChar];
            int markW = mark[0].Length;
            int w = Mathf.Max(b.width, markW);
            var g = new Glyph { width = w, px = new bool[CellHeight, w] };
            int bx = (w - b.width) / 2;
            for (int y = 0; y < CellHeight; y++)
                for (int x = 0; x < b.width; x++)
                    g.px[y, x + bx] = b.px[y, x];
            int top = upper ? 0 : 2;
            int mx = (w - markW + 1) / 2;
            for (int y = 0; y < 2; y++)
                for (int x = 0; x < markW; x++)
                    if (mark[y][x] == '#') g.px[top + y, x + mx] = true;
            glyphs[target] = g;
        }

        static void Cedilla(char target, char baseChar)
        {
            var b = glyphs[baseChar];
            var g = new Glyph { width = b.width, px = (bool[,])b.px.Clone() };
            g.px[9, 2] = true; g.px[10, 1] = true;
            glyphs[target] = g;
        }

        // ---------- medidas e textura ----------
        public static int MeasureWidth(string s)
        {
            int w = 0, line = 0;
            foreach (char c in s)
            {
                if (c == '\n') { w = Mathf.Max(w, line); line = 0; continue; }
                line += Advance(c);
            }
            return Mathf.Max(w, line) - (s.Length > 0 ? Spacing : 0);
        }

        /// <summary>Desenha um texto numa textura (1 pixel da fonte = 1 texel), com sombra opcional.</summary>
        public static Texture2D Render(string s, Color color, Color? shadow = null)
        {
            int w = Mathf.Max(1, MeasureWidth(s) + 1);
            int h = CellHeight + 1;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[w * h];
            int pen = 0;
            void Plot(int x, int y, Color c) { if (x >= 0 && x < w && y >= 0 && y < h) px[(h - 1 - y) * w + x] = c; }
            foreach (char ch in s)
            {
                var g = Get(ch);
                for (int y = 0; y < CellHeight; y++)
                    for (int x = 0; x < g.width; x++)
                        if (g.px[y, x])
                        {
                            if (shadow.HasValue) Plot(pen + x + 1, y + 1, shadow.Value);
                        }
                pen += Advance(ch);
            }
            pen = 0;
            foreach (char ch in s)
            {
                var g = Get(ch);
                for (int y = 0; y < CellHeight; y++)
                    for (int x = 0; x < g.width; x++)
                        if (g.px[y, x]) Plot(pen + x, y, color);
                pen += Advance(ch);
            }
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }
    }
}
