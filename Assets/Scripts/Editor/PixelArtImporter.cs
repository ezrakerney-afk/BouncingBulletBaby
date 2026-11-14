#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// Everything under Assets/Art imports as pixel art: 32 ppu, point filter, uncompressed.
public sealed class PixelArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/")) return;
        var imp = (TextureImporter)assetImporter;
        // only on first import so hand-sliced sheets stay sliced
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
