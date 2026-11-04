using UnityEditor;
using UnityEngine;

// Import rules for the v5 asset delivery (Design/dad-asset-request-v5.md). Applied automatically whenever a file lands
// under Resources/DadV5, so replacements keep mobile-friendly sizes without hand-editing every importer.
public sealed class DadV5AssetImport : AssetPostprocessor
{
    const string Root = "Assets/DadSchoolBoss/Resources/DadV5/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root) && assetPath != "Assets/DadSchoolBoss/Art/V5/app_icon.png") return;
        var t = (TextureImporter)assetImporter;
        bool background = assetPath.Contains("/bg/") || assetPath.Contains("/story/");
        t.textureType = TextureImporterType.Default;
        t.alphaIsTransparency = !background;
        t.mipmapEnabled = false;
        t.npotScale = TextureImporterNPOTScale.None;
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        t.isReadable = false;
        // Characters: 768 canvases shown at most ~500 device px -> 512. Icons stay at 256. Backgrounds: 1080x1920 -> 2048 cap.
        t.maxTextureSize = assetPath.Contains("/chars/") ? 512 : background ? 2048 : assetPath.EndsWith("app_icon.png") ? 1024 : 256;
        t.textureCompression = TextureImporterCompression.CompressedHQ;
    }

    void OnPreprocessAudio()
    {
        if (!assetPath.StartsWith(Root)) return;
        var a = (AudioImporter)assetImporter;
        bool music = System.IO.Path.GetFileName(assetPath).StartsWith("bgm_");
        a.forceToMono = true;
        var s = a.defaultSampleSettings;
        s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
        s.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
        s.quality = .6f;
        a.defaultSampleSettings = s;
    }
}
