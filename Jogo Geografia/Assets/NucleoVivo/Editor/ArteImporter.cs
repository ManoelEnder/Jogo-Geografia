using UnityEditor;
using UnityEngine;

namespace NucleoVivo.EditorTools
{
    /// <summary>
    /// Configura automaticamente os PNGs pintados (Resources/Arte), gerados pelo projeto Remotion em /Arte.
    /// </summary>
    public class ArteImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace("\\", "/").Contains("/Resources/Arte/")) return;
            var ti = (TextureImporter)assetImporter;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.spritePixelsPerUnit = 100;
            ti.maxTextureSize = 2048;

            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = Pivot(name);
            ti.SetTextureSettings(s);
            ti.wrapMode = name.StartsWith("ground_") || name == "river" || name == "road" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.spriteBorder = Border(name);
        }

        static Vector2 Pivot(string n)
        {
            if (n.StartsWith("ui_") || n.StartsWith("icon_") || n.StartsWith("ground_") || n == "river" || n == "road" || n == "fan"
                || n == "soft_glow" || n == "vignette" || n == "light_shafts" || n == "mist" || n == "steam" || n.StartsWith("bird")
                || n.StartsWith("dirt") || n == "clover" || n.StartsWith("flowers"))
                return new Vector2(0.5f, 0.5f);
            return new Vector2(0.5f, 0.06f); // base do objeto, um pouco acima da borda por causa da sombra
        }

        static Vector4 Border(string n)
        {
            switch (n)
            {
                case "ui_panel": case "ui_panel_light": return new Vector4(30, 30, 30, 30);
                case "ui_button": case "ui_button_off": case "ui_button_alt": return new Vector4(24, 24, 24, 24);
                default: return Vector4.zero;
            }
        }
    }
}
