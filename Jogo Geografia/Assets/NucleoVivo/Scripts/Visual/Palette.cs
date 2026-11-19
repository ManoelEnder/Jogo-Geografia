using UnityEngine;

namespace NucleoVivo
{
    /// <summary>Paleta única para cenário, ícones e interface.</summary>
    public static class Palette
    {
        static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        // Céu de fim de tarde
        public static readonly Color SkyTop = H("#15181A");
        public static readonly Color SkyMid = H("#272C2D");
        public static readonly Color SkyHorizon = H("#4B504C");

        // Estruturas
        public static readonly Color Petrol = H("#384A4C");
        public static readonly Color PetrolDark = H("#283638");
        public static readonly Color PetrolLight = H("#506366");
        public static readonly Color Concrete = H("#A3A9A6");
        public static readonly Color ConcreteLight = H("#C9CCC6");
        public static readonly Color ConcreteDark = H("#6E7774");
        public static readonly Color ConcreteShadow = H("#4B5553");
        public static readonly Color Roof = H("#3B4A4C");

        // Natureza
        public static readonly Color GreenDark = H("#173326");
        public static readonly Color Green = H("#21452F");
        public static readonly Color GreenLight = H("#2E5A3B");
        public static readonly Color HillFar = H("#323C38");
        public static readonly Color HillMid = H("#26362C");
        public static readonly Color River = H("#2C3B3E");
        public static readonly Color RiverLight = H("#536669");

        // Luzes
        public static readonly Color Amber = H("#FFB54A");
        public static readonly Color AmberDim = H("#6B5236");
        public static readonly Color WindowOff = H("#303634");
        public static readonly Color Cyan = H("#3BF0FF");
        public static readonly Color CyanSoft = H("#1FB7C9");

        // Interface
        public static readonly Color UiBackground = H("#2B231D");
        public static readonly Color UiCard = H("#3A3027");
        public static readonly Color UiCardLight = H("#46392E");
        public static readonly Color UiCardHighlight = H("#5A4836");
        public static readonly Color UiBorder = H("#6B5440");
        public static readonly Color UiText = H("#F1E6CF");
        public static readonly Color UiMuted = H("#BFAF95");
        public static readonly Color UiPositive = H("#A9C77A");
        public static readonly Color UiNegative = H("#E08A6E");
        public static readonly Color UiButton = H("#6A7A3E");
        public static readonly Color UiButtonDisabled = H("#4A423A");

        public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }
    }
}
