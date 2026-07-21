using UnityEditor;

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
        importer.alphaIsTransparency = true;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
    }
}
