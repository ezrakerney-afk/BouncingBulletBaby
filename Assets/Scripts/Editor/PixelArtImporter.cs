#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Any image dropped under Assets/Art imports as crisp pixel art: 32 px per unit, no smoothing, no compression.
public sealed class PixelArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/")) return;
        var imp = (TextureImporter)assetImporter;
        // Only choose the sprite mode on first import, so a sheet sliced by hand later stays sliced.
        if (imp.importSettingsMissing)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
        }
        imp.spritePixelsPerUnit = 32;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.alphaIsTransparency = true;
    }
}
#endif
