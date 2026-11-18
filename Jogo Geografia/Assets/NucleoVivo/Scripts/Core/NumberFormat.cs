using System;
using System.Globalization;

namespace NucleoVivo
{
    /// <summary>Formata números grandes em português: 950, 1,2 mil, 3,5 mi, 7 bi...</summary>
    public static class NumberFormat
    {
        static readonly string[] Suffixes = { "", " mil", " mi", " bi", " tri", " qua", " qui", " sx", " sp", " oc" };

        public static string Short(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "0";
            double abs = Math.Abs(value);
            if (abs < 1000)
            {
                // Valores pequenos com fração (ex.: 0,5/s) mostram uma casa decimal.
                double r1 = Math.Round(value, 1);
                if (abs < 100 && Math.Abs(r1 - Math.Round(r1)) > 0.0001)
                    return Pt(r1.ToString("0.0", CultureInfo.InvariantCulture));
                return Pt(Math.Floor(value + 0.0001).ToString("0", CultureInfo.InvariantCulture));
            }

            int i = 0;
            double v = value;
            while (Math.Abs(v) >= 1000 && i < Suffixes.Length - 1) { v /= 1000; i++; }

            int decimals = Math.Abs(v) >= 100 ? 0 : 1;
            double rounded = Math.Round(v, decimals);
            if (Math.Abs(rounded) >= 1000 && i < Suffixes.Length - 1) { rounded /= 1000; i++; decimals = 1; }

            string fmt = decimals == 0 ? "0" : "0.#";
            return Pt(rounded.ToString(fmt, CultureInfo.InvariantCulture)) + Suffixes[i];
        }

        /// <summary>Número inteiro com separador de milhar (25.000).</summary>
        public static string Full(double value)
        {
            return Math.Floor(value).ToString("#,0", CultureInfo.InvariantCulture).Replace(',', '.');
        }

        public static string Duration(float seconds)
        {
            int s = Math.Max(0, (int)Math.Ceiling(seconds));
            if (s < 60) return s + " s";
            int m = s / 60, r = s % 60;
            return r == 0 ? m + " min" : m + " min " + r + " s";
        }

        public static string Percent(float fraction)
        {
            return Pt(Math.Round(fraction * 100f).ToString("0", CultureInfo.InvariantCulture)) + "%";
        }

        static string Pt(string invariant) => invariant.Replace('.', ',');
    }
}
