using UnityEditor;
using UnityEngine;

/// Automatically configures all PNGs in the Cereals folder as sprites.
public class CerealAssetPostprocessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("Resources/Cereals")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 256f; // 256px sprite = 1 world unit = 1 grid cell
        importer.mipmapEnabled = false;

        // UI textures are 9-sliced (panel backdrop, buttons)
        if (assetPath.Contains("ui_"))
            importer.spriteBorder = new Vector4(48f, 48f, 48f, 48f);
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
    }
}
