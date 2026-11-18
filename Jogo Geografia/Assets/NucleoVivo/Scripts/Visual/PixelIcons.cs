using System.Collections.Generic;
using UnityEngine;

namespace NucleoVivo
{
    /// <summary>
    /// Ícones 16x16 desenhados à mão, um caractere por pixel. O contorno escuro é adicionado
    /// automaticamente ao redor dos pixels preenchidos.
    /// </summary>
    public static class PixelIcons
    {
        public const int Size = 16;

        static readonly Dictionary<char, Color> Ink = new Dictionary<char, Color>
        {
            ['w'] = new Color32(0xEA, 0xF3, 0xF4, 255),
            ['c'] = new Color32(0xC9, 0xCC, 0xC6, 255),
            ['m'] = new Color32(0x9A, 0xA1, 0x9E, 255),
            ['d'] = new Color32(0x62, 0x6C, 0x6A, 255),
            ['p'] = new Color32(0x1F, 0x51, 0x60, 255),
            ['P'] = new Color32(0x2E, 0x71, 0x83, 255),
            ['y'] = new Color32(0x3B, 0xF0, 0xFF, 255),
            ['Y'] = new Color32(0x1F, 0xA7, 0xBA, 255),
            ['a'] = new Color32(0xFF, 0xB5, 0x4A, 255),
            ['A'] = new Color32(0xC7, 0x7A, 0x1E, 255),
            ['r'] = new Color32(0xFF, 0x5E, 0x4F, 255),
            ['g'] = new Color32(0x6B, 0xE3, 0x8F, 255),
            ['G'] = new Color32(0x2F, 0x8A, 0x52, 255),
        };

        public static readonly Color Outline = new Color32(0x08, 0x10, 0x14, 255);

        static readonly Dictionary<IconKind, string[]> Maps = new Dictionary<IconKind, string[]>
        {
            [IconKind.Energy] = new[]
            {
                "................",
                ".........aaa....",
                "........aaA.....",
                ".......aaA......",
                "......aaA.......",
                ".....aaA........",
                "....aaaaaaaa....",
                "....AAAAaaA.....",
                "........aA......",
                ".......aA.......",
                "......aA........",
                ".....aA.........",
                "....aA..........",
                "...a............",
                "................",
                "................",
            },
            [IconKind.ControlPanel] = new[]
            {
                "................",
                "...pppppppppp...",
                "...pyyyyyyyyp...",
                "...pyYyYyyyyp...",
                "...pyyyyYyyyp...",
                "...pppppppppp...",
                "......dddd......",
                "..cccccccccccc..",
                "..caaccyyccrrc..",
                "..cccccccccccc..",
                "..mmmmmmmmmmmm..",
                "..dddddddddddd..",
                "................",
                "................",
                "................",
                "................",
            },
            [IconKind.Turbine] = new[]
            {
                "................",
                ".....dddddd.....",
                "....dppppppd....",
                "...dpyyppppPd...",
                "..dppyyypppPPd..",
                "..dpppyyypyyPd..",
                "..dppppywyyyPd..",
                "..dppppwwwpppd..",
                "..dpyyywyppppd..",
                "..dPyypyyyPppd..",
                "..dPPpppyyyPpd..",
                "...dPPppppyyd...",
                "....dPPPPPPd....",
                ".....dddddd.....",
                "................",
                "................",
            },
            [IconKind.Substation] = new[]
            {
                "................",
                ".yyyyyyyyyyyyyy.",
                "...d........d...",
                "...d........d...",
                "..aaa......aaa..",
                "..ccc......ccc..",
                ".ccccc....ccccc.",
                ".cmmmc....cmmmc.",
                ".cccccaa..ccccc.",
                ".cmmmca...cmmmc.",
                ".ccccc.aa.ccccc.",
                ".cmmmc..a.cmmmc.",
                ".ddddd....ddddd.",
                "dddddddddddddddd",
                "................",
                "................",
            },
            [IconKind.Automation] = new[]
            {
                "................",
                ".dddddddddddddd.",
                ".dppppppppppppd.",
                ".dpppppppppyypd.",
                ".dpppppppyyppppd",
                ".dppppyyypppppd.",
                ".dpyyypppppppppd",
                ".dppppppppppppd.",
                ".dddddddddddddd.",
                "......dddd......",
                "......dddd......",
                "...cccccccccc...",
                "...mmmmmmmmmm...",
                "................",
                "................",
                "................",
            },
            [IconKind.CoolingTower] = new[]
            {
                "......wwww......",
                "....wwwwwww.....",
                ".....wwwwwww....",
                "......wwww......",
                "....cccccccc....",
                "....pppppppp....",
                ".....cccccm.....",
                ".....ccccmm.....",
                ".....ccccmm.....",
                "....cccccmmm....",
                "....ccccccmm....",
                "...cccccccmmm...",
                "...ccccccccmm...",
                "..cccccccccmmm..",
                "..dddddddddddd..",
                "................",
            },
            [IconKind.Reactor] = new[]
            {
                "................",
                "......cccc......",
                "....cccccccm....",
                "...cccccccccm...",
                "..ccccccccccmm..",
                "..ccccccccccmm..",
                "..pyypyypyypyp..",
                "..ccccccccccmm..",
                "..ccccccccccmm..",
                "..cccccppcccmm..",
                "..cccccPPcccmm..",
                "..cccccPPcccmm..",
                ".dddddddddddddd.",
                "................",
                "................",
                "................",
            },
            [IconKind.Grid] = new[]
            {
                "................",
                "yyyyyyyyyyyyyyyy",
                "......d.........",
                ".....ddd........",
                "....dd.dd.......",
                "...pppppppp.....",
                "...papappap.ppp.",
                ".pppppppppp.pap.",
                ".papppapapp.ppp.",
                ".pppppppppppppp.",
                ".pappappapppapp.",
                ".pppppppppppppp.",
                ".papapppappapap.",
                ".pppppppppppppp.",
                "................",
                "................",
            },
            [IconKind.Heat] = new[]
            {
                "................",
                "....a.....a.....",
                ".....a...a......",
                "......aaa.......",
                "..a..aaaaa..a...",
                "....aaaaaaa.....",
                "...aaaaaaaaa....",
                "....aaaaaaa.....",
                "..a..aaaaa..a...",
                "......aaa...cc..",
                ".....a...a..cc..",
                "....a.....a.rr..",
                "............rr..",
                "...........rrrr.",
                "...........rrrr.",
                "................",
            },
            [IconKind.University] = new[]
            {
                "................",
                ".......cc.......",
                ".....ccccc......",
                "...ccccyyccc....",
                ".cccccccccccccc.",
                ".mmmmmmmmmmmmmm.",
                "..cc..cc..cc.cc.",
                "..cm..cm..cm.cm.",
                "..cm..cm..cm.cm.",
                "..cm..cm..cm.cm.",
                "..cm..cm..cm.cm.",
                ".cccccccccccccc.",
                "dddddddddddddddd",
                "................",
                "................",
                "................",
            },
            [IconKind.Wrench] = new[]
            {
                "................",
                "..r.......cc....",
                "..r......c..c...",
                "..r.....c....c..",
                "..r.....cc..cc..",
                "........mccccc..",
                "..r....mccccc...",
                "......mcm.......",
                ".....mcm........",
                "....mcm.........",
                "...mcm..........",
                "..ccm...........",
                ".ccc............",
                ".cc.............",
                "................",
                "................",
            },
            [IconKind.Protest] = new[]
            {
                "................",
                ".cccccccccccccc.",
                ".cccccccccccccc.",
                ".ccyyyyyccccccc.",
                ".cyyyyyyyyycyyc.",
                ".cypyyyyyyyyyc..",
                ".cyyyyyyyyycyyc.",
                ".ccyyyyyccccccc.",
                ".cccccccccccccc.",
                "......dd........",
                "......dd........",
                "......dd........",
                "......dd........",
                "......dd........",
                "................",
                "................",
            },
            [IconKind.Hearing] = new[]
            {
                "................",
                ".........yyyyyy.",
                ".........yyyyyy.",
                ".........yyyyyy.",
                "..cc......yy....",
                ".cccc.....y.....",
                ".cccc...mm......",
                "..cc...mmmm.....",
                "........mm......",
                ".cccc..........",
                "cccccc..mmmm....",
                "cccccc.mmmmmm...",
                "cccccc.mmmmmm...",
                "................",
                "................",
                "................",
            },
            [IconKind.Press] = new[]
            {
                "................",
                ".wwwwwwwwwwwwww.",
                ".wddddddddddddw.",
                ".wwwwwwwwwwwwww.",
                ".wyyyyyw.ddddww.",
                ".wyyyyyw.......w",
                ".wyyyyyw.ddddww.",
                ".wyyyyyw.......w",
                ".wwwwwwwwwwwwww.",
                ".w.dddddddddd.w.",
                ".w............w.",
                ".w.ddddddddd..w.",
                ".wwwwwwwwwwwwww.",
                "................",
                "................",
                "................",
            },
            [IconKind.Nucleo] = new[]
            {
                "................",
                ".....yyyyyy.....",
                "....y......y....",
                "...y..aaaa..y.y.",
                "..y..a....a..y..",
                "..y.a..ww..a.y..",
                "..y.a.wwww.a.y..",
                "..y.a.wwww.a.y..",
                "..y.a..ww..a.y..",
                "..y..a....a..y..",
                "...y..aaaa..y...",
                "....y......y....",
                ".y...yyyyyy.....",
                "................",
                "................",
                "................",
            },
            [IconKind.Tap] = new[]
            {
                "................",
                ".....yyyyyy.....",
                "....y......y....",
                "...y..yyyy..y...",
                "...y.y....y.y...",
                "...y.y.ww.y.y...",
                "...y.y.ww.y.y...",
                "...y.y....y.y...",
                "...y..yyyy..y...",
                "....y..aa..y....",
                ".......aA.......",
                "......aA........",
                ".....aaaaA......",
                ".......aA.......",
                "......aA........",
                "................",
            },
            [IconKind.Passive] = new[]
            {
                "................",
                ".....yyyyyy.....",
                "....yy....yy....",
                "...yy......yy...",
                "...y...aa...yyy.",
                "..yy..aA.....y..",
                "..y..aA.........",
                "..y.aaaaa.......",
                "..y...aA........",
                "..yy.aA.....yy..",
                "...y.a......y...",
                "...yy......yy...",
                "....yy....yy....",
                ".....yyyyyy.....",
                "................",
                "................",
            },
            [IconKind.Discount] = new[]
            {
                "................",
                ".......cccccccc.",
                "......ccccccccc.",
                ".....cccPccPccc.",
                "....ccccPccPccc.",
                "...cccccccPcccc.",
                "..cc.ccccPccccc.",
                "..cc.cccPcccccc.",
                "...ccccPccPcccc.",
                "....cccPccPccc..",
                ".....ccccccccc..",
                "......cccccccc..",
                ".......mmmmmmm..",
                "................",
                "................",
                "................",
            },
            [IconKind.Sound] = new[]
            {
                "................",
                "................",
                "......c.....y...",
                ".....cc...y..y..",
                "....ccc....y..y.",
                ".cccccc..y..y.y.",
                ".cccccc...y.y.y.",
                ".cccccc...y.y.y.",
                ".cccccc..y..y.y.",
                "....ccc....y..y.",
                ".....cc...y..y..",
                "......c.....y...",
                "................",
                "................",
                "................",
                "................",
            },
            [IconKind.Reputation] = new[]
            {
                "................",
                "...PPPPPPPPPP...",
                "..PPPPPPPPPPPP..",
                "..PPPPPPPPPPyP..",
                "..PPPPPPPPPyyP..",
                "..PPPPPPPPyyPP..",
                "..PPyPPPPyyPPP..",
                "..PPyyPPyyPPPP..",
                "...PPyyyyPPPP...",
                "...PPPyyPPPPP...",
                "....PPPPPPPP....",
                ".....PPPPPP.....",
                "......PPPP......",
                ".......PP.......",
                "................",
                "................",
            },
            [IconKind.Settings] = new[]
            {
                "................",
                ".......cc.......",
                "...cc..cc..cc...",
                "...cccccccccc...",
                "....cccccccc....",
                "...ccc....ccc...",
                ".cccc......cccc.",
                ".cccc......cccc.",
                "...ccc....ccc...",
                "....cccccccc....",
                "...cccccccccc...",
                "...cc..cc..cc...",
                ".......cc.......",
                "................",
                "................",
                "................",
            },
            [IconKind.Lock] = new[]
            {
                "................",
                "......dddd......",
                ".....d....d.....",
                "....d......d....",
                "....d......d....",
                "....d......d....",
                "..aaaaaaaaaaaa..",
                "..aAAAAAAAAAAa..",
                "..aaaaakkaaaaa..",
                "..aaaaakkaaaaa..",
                "..aaaaaakaaaaa..",
                "..aaaaaaaaaaaa..",
                "..AAAAAAAAAAAA..",
                "................",
                "................",
                "................",
            },
        };

        static readonly Dictionary<IconKind, string[]> Extra = new Dictionary<IconKind, string[]>
        {
            [IconKind.House] = new[]
            {
                "................",
                "......rr........",
                ".....rrrr...d...",
                "....rrrrrr..d...",
                "...rrrrrrrr.d...",
                "..rrrrrrrrrrd...",
                "...cccccccc.....",
                "...caacccmc.....",
                "...caacccmc.....",
                "...ccccppcc.....",
                "...ccccppcc.....",
                "...ccccppcc.....",
                ".GGGGGGGGGGGGGG.",
                "................",
                "................",
                "................",
            },
            [IconKind.Port] = new[]
            {
                "................",
                ".....dddddddddd.",
                ".....d.......d..",
                ".....d.......d..",
                ".....d......ccc.",
                ".....d......ccc.",
                ".....d..........",
                "..rrraamm.......",
                "..rrraamm.......",
                ".ddddddddddd....",
                "..ddddddddd.....",
                "PPPPPPPPPPPPPPPP",
                "PpPPpPPPpPPPpPPP",
                "pppppppppppppppp",
                "................",
                "................",
            },
            [IconKind.Map] = new[]
            {
                "................",
                ".cccc.mmmm.cccc.",
                ".cccc.mmmm.cccc.",
                ".cGcc.mmmm.ccGc.",
                ".cGGc.mGGm.cGGc.",
                ".ccGc.mmGm.cGcc.",
                ".cccc.mmmG.cccc.",
                ".ccrc.mmmm.cccc.",
                ".crrr.mmmm.cccc.",
                ".ccrc.mpmm.cccc.",
                ".cccc.ppmm.cccc.",
                ".cccc.pmmm.cccc.",
                ".cccc.mmmm.cccc.",
                "................",
                "................",
                "................",
            },
        };

        static readonly Dictionary<IconKind, Sprite> cache = new Dictionary<IconKind, Sprite>();

        public static Sprite Get(IconKind kind)
        {
            if (cache.TryGetValue(kind, out var s) && s != null) return s;
            if (!Maps.TryGetValue(kind, out var map) && !Extra.TryGetValue(kind, out map)) map = Maps[IconKind.Energy];
            s = Build(map, "ícone " + kind);
            cache[kind] = s;
            return s;
        }

        /// <summary>Converte o mapa em sprite, com contorno de 1 pixel (textura 18x18).</summary>
        public static Sprite Build(string[] map, string name)
        {
            int n = Size + 2;
            var fill = new Color?[n, n];
            for (int y = 0; y < Size && y < map.Length; y++)
            {
                string row = map[y];
                for (int x = 0; x < Size && x < row.Length; x++)
                {
                    char ch = row[x];
                    if (ch == 'k') fill[y + 1, x + 1] = Outline;
                    else if (Ink.TryGetValue(ch, out var c)) fill[y + 1, x + 1] = c;
                }
            }
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Color c = new Color(0, 0, 0, 0);
                    if (fill[y, x].HasValue) c = fill[y, x].Value;
                    else if (Neighbor(fill, x, y, n)) c = Outline;
                    px[(n - 1 - y) * n + x] = c;
                }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = name };
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        static bool Neighbor(Color?[,] f, int x, int y, int n)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if ((dx == 0 && dy == 0) || (dx != 0 && dy != 0)) continue; // só vizinhos ortogonais
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && ny >= 0 && nx < n && ny < n && f[ny, nx].HasValue) return true;
                }
            return false;
        }
    }
}
