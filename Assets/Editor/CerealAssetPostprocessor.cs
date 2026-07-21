using UnityEditor;

/// Stellt alle PNGs im Cereals-Ordner automatisch als Sprites ein.
public class CerealAssetPostprocessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("Resources/Cereals")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 256f; // 256px-Sprite = 1 Welteinheit = 1 Grid-Zelle
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
    }
}
