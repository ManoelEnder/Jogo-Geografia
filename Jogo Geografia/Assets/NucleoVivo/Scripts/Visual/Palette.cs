using UnityEngine;

namespace NucleoVivo
{
    /// <summary>Paleta única para cenário, ícones e interface.</summary>
    public static class Palette
    {
        static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        // Céu de fim de tarde
        public static readonly Color SkyTop = H("#0A1A26");
        public static readonly Color SkyMid = H("#163947");
        public static readonly Color SkyHorizon = H("#3D6670");

        // Estruturas
        public static readonly Color Petrol = H("#1F5160");
        public static readonly Color PetrolDark = H("#143A47");
        public static readonly Color PetrolLight = H("#2E7183");
        public static readonly Color Concrete = H("#A3A9A6");
        public static readonly Color ConcreteLight = H("#C9CCC6");
        public static readonly Color ConcreteDark = H("#6E7774");
        public static readonly Color ConcreteShadow = H("#4B5553");
        public static readonly Color Roof = H("#3B4A4C");

        // Natureza
        public static readonly Color GreenDark = H("#173326");
        public static readonly Color Green = H("#21452F");
        public static readonly Color GreenLight = H("#2E5A3B");
        public static readonly Color HillFar = H("#22414A");
        public static readonly Color HillMid = H("#1B3A30");
        public static readonly Color River = H("#18445A");
        public static readonly Color RiverLight = H("#2B6B82");

        // Luzes
        public static readonly Color Amber = H("#FFB54A");
        public static readonly Color AmberDim = H("#6B5236");
        public static readonly Color WindowOff = H("#2A3A40");
        public static readonly Color Cyan = H("#3BF0FF");
        public static readonly Color CyanSoft = H("#1FB7C9");

        // Interface
        public static readonly Color UiBackground = H("#0C1820");
        public static readonly Color UiCard = H("#132631");
        public static readonly Color UiCardLight = H("#1B3442");
        public static readonly Color UiCardHighlight = H("#21455A");
        public static readonly Color UiBorder = H("#2C5466");
        public static readonly Color UiText = H("#EAF3F4");
        public static readonly Color UiMuted = H("#8FA9B3");
        public static readonly Color UiPositive = H("#6BE38F");
        public static readonly Color UiNegative = H("#FF7466");
        public static readonly Color UiButton = H("#1AA9BD");
        public static readonly Color UiButtonDisabled = H("#2A3D47");

        public static Color WithAlpha(this Color c, float a) { c.a = a; return c; }
    }
}
