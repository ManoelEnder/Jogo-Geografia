using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NucleoVivo
{
    /// <summary>Acesso às ilustrações pintadas em Resources/Arte (geradas pelo projeto Remotion em /Arte).</summary>
    public static class Pinturas
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string name)
        {
            if (cache.TryGetValue(name, out var s) && s != null) return s;
            s = Resources.Load<Sprite>("Arte/" + name);
            if (s == null) Debug.LogWarning("[Núcleo Vivo] Ilustração não encontrada: " + name);
            cache[name] = s;
            return s;
        }

        public static Sprite Icon(IconKind kind) => Get("icon_" + Snake(kind.ToString()));

        static string Snake(string pascal)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < pascal.Length; i++)
            {
                char c = pascal[i];
                if (char.IsUpper(c) && i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
    }
}
