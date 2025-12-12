using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIBevelOutlineGeneratorWindow : EditorWindow
{
    private const string EditorPrefsKey = "UIBevelOutlineGeneratorWindow_Settings";

    [Header("Target")]
    [SerializeField] private Image targetImage;

    [Header("Outline Settings")]
    [SerializeField] private int mainOutlineWidth = 2;
    [SerializeField] private Color mainOutlineColor = Color.black;

    [SerializeField] private int bevelWidth = 2;
    [SerializeField] private Color topLeftBevelColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    [SerializeField] private Color bottomRightBevelColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Header("Preview")]
    [SerializeField] private Color previewBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    private Texture2D previewTexture;
    private Sprite sourceSprite;
    private Texture2D sourceTexture;

    // Cache for change detection
    private Image lastTargetImage;
    private int lastMainWidth;
    private int lastBevelWidth;
    private Color lastMainColor;
    private Color lastTopLeftColor;
    private Color lastBottomRightColor;
    private Color lastPreviewBgColor;
    private Sprite lastSourceSprite;
    private int lastTargetPixelWidth;
    private int lastTargetPixelHeight;

    [System.Serializable]
    private class SettingsData
    {
        public int mainOutlineWidth;
        public Color mainOutlineColor;
        public int bevelWidth;
        public Color topLeftBevelColor;
        public Color bottomRightBevelColor;
        public Color previewBackgroundColor;
    }

    [MenuItem("Tools/UI Bevel Outline Generator")]
    private static void ShowWindow()
    {
        var window = GetWindow<UIBevelOutlineGeneratorWindow>("UI Bevel Outline");
        window.minSize = new Vector2(360f, 440f);
    }

    private void OnEnable()
    {
        LoadSettings();
        Selection.selectionChanged += OnSelectionChanged;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnSelectionChanged;
        SaveSettings();

        if (previewTexture != null)
        {
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }

    private void OnSelectionChanged()
    {
        var go = Selection.activeGameObject;
        if (go != null)
        {
            var img = go.GetComponent<Image>();
            if (img != null)
            {
                targetImage = img;
                Repaint();
                return;
            }
        }

        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Target UI Image", EditorStyles.boldLabel);

        if (targetImage == null)
        {
            TryAssignFromSelection();
        }

        targetImage = (Image)EditorGUILayout.ObjectField("Image", targetImage, typeof(Image), true);

        if (targetImage == null)
        {
            EditorGUILayout.HelpBox("Select a UI Image in the scene or drag it here.", MessageType.Info);
            return;
        }

        sourceSprite = targetImage.sprite;

        if (sourceSprite == null)
        {
            EditorGUILayout.HelpBox("The selected Image has no sprite assigned.", MessageType.Warning);
            return;
        }

        sourceTexture = sourceSprite.texture;

        if (sourceTexture == null)
        {
            EditorGUILayout.HelpBox("Sprite texture is missing.", MessageType.Error);
            return;
        }

        int targetPixelWidth, targetPixelHeight;
        GetTargetPixelSize(targetImage, out targetPixelWidth, out targetPixelHeight);
        EditorGUILayout.LabelField("Target Size (pixels)", $"{targetPixelWidth} x {targetPixelHeight}");

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Outline Settings", EditorStyles.boldLabel);

        mainOutlineWidth = EditorGUILayout.IntSlider("Main Width", mainOutlineWidth, 1, 64);
        mainOutlineColor = EditorGUILayout.ColorField("Main Color", mainOutlineColor);

        bevelWidth = EditorGUILayout.IntSlider("Bevel Width", bevelWidth, 0, 64);
        topLeftBevelColor = EditorGUILayout.ColorField("Top/Left Bevel Color", topLeftBevelColor);
        bottomRightBevelColor = EditorGUILayout.ColorField("Bottom/Right Bevel Color", bottomRightBevelColor);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview Settings", EditorStyles.boldLabel);
        previewBackgroundColor = EditorGUILayout.ColorField("Preview Background", previewBackgroundColor);

        EditorGUILayout.Space();

        bool isReadable = IsTextureReadable(sourceTexture);

        if (!isReadable)
        {
            EditorGUILayout.HelpBox("Texture is not readable. Enable Read/Write on the texture importer to generate the outline.", MessageType.Warning);

            if (GUILayout.Button("Make Texture Readable"))
            {
                MakeTextureReadable(sourceTexture);
            }
        }

        EditorGUILayout.Space();
        DrawPreview(isReadable, targetPixelWidth, targetPixelHeight);

        EditorGUILayout.Space();

        GUI.enabled = isReadable;
        if (GUILayout.Button("Save Bevel Outline Sprite"))
        {
            SaveOutlineSprite(targetPixelWidth, targetPixelHeight);
            SaveSettings();
        }
        GUI.enabled = true;
    }

    private void TryAssignFromSelection()
    {
        var go = Selection.activeGameObject;
        if (go == null)
            return;

        var img = go.GetComponent<Image>();
        if (img != null)
            targetImage = img;
    }

    private void DrawPreview(bool canGenerate, int targetPixelWidth, int targetPixelHeight)
    {
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        UpdatePreviewTextureIfNeeded(canGenerate, targetPixelWidth, targetPixelHeight);

        Rect previewRect = GUILayoutUtility.GetRect(256, 256, GUILayout.ExpandWidth(true));

        if (Event.current.type == EventType.Repaint)
        {
            EditorGUI.DrawRect(previewRect, previewBackgroundColor);
        }

        if (previewTexture != null)
        {
            float padding = 12f;
            Rect innerRect = new Rect(
                previewRect.x + padding,
                previewRect.y + padding,
                previewRect.width - padding * 2f,
                previewRect.height - padding * 2f
            );

            GUI.DrawTexture(innerRect, previewTexture, ScaleMode.ScaleToFit, true);
        }
        else
        {
            EditorGUI.LabelField(previewRect, "Preview not available", EditorStyles.centeredGreyMiniLabel);
        }
    }

    private void UpdatePreviewTextureIfNeeded(bool canGenerate, int targetPixelWidth, int targetPixelHeight)
    {
        bool changed =
            previewTexture == null ||
            lastTargetImage != targetImage ||
            lastMainWidth != mainOutlineWidth ||
            lastBevelWidth != bevelWidth ||
            lastMainColor != mainOutlineColor ||
            lastTopLeftColor != topLeftBevelColor ||
            lastBottomRightColor != bottomRightBevelColor ||
            lastPreviewBgColor != previewBackgroundColor ||
            lastSourceSprite != sourceSprite ||
            lastTargetPixelWidth != targetPixelWidth ||
            lastTargetPixelHeight != targetPixelHeight;

        if (!changed)
            return;

        lastTargetImage = targetImage;
        lastMainWidth = mainOutlineWidth;
        lastBevelWidth = bevelWidth;
        lastMainColor = mainOutlineColor;
        lastTopLeftColor = topLeftBevelColor;
        lastBottomRightColor = bottomRightBevelColor;
        lastPreviewBgColor = previewBackgroundColor;
        lastSourceSprite = sourceSprite;
        lastTargetPixelWidth = targetPixelWidth;
        lastTargetPixelHeight = targetPixelHeight;

        if (!canGenerate)
        {
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
                previewTexture = null;
            }
            return;
        }

        GeneratePreviewTexture(targetPixelWidth, targetPixelHeight);
    }

    private void GeneratePreviewTexture(int targetPixelWidth, int targetPixelHeight)
    {
        if (sourceSprite == null || sourceTexture == null)
            return;

        Rect rect = sourceSprite.rect;
        Texture2D outlined = GenerateBevelOutlineTextureAtSize(
            sourceTexture,
            rect,
            targetPixelWidth,
            targetPixelHeight,
            mainOutlineWidth,
            bevelWidth,
            mainOutlineColor,
            topLeftBevelColor,
            bottomRightBevelColor
        );

        if (previewTexture != null)
            DestroyImmediate(previewTexture);

        previewTexture = outlined;
    }

    // Computes the actual on-screen pixel size of the Image
    private void GetTargetPixelSize(Image image, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (image == null)
            return;

        Rect rect = image.rectTransform.rect;
        float scaleFactor = 1f;

        Canvas canvas = image.canvas;
        if (canvas != null &&
            (canvas.renderMode == RenderMode.ScreenSpaceOverlay ||
             canvas.renderMode == RenderMode.ScreenSpaceCamera))
        {
            scaleFactor = canvas.scaleFactor;
        }

        width = Mathf.Max(2, Mathf.RoundToInt(Mathf.Abs(rect.width * scaleFactor)));
        height = Mathf.Max(2, Mathf.RoundToInt(Mathf.Abs(rect.height * scaleFactor)));

        if (width <= 0 || height <= 0)
        {
            Rect spriteRect = image.sprite != null ? image.sprite.rect : new Rect(0, 0, 64, 64);
            width = Mathf.Max(2, Mathf.RoundToInt(spriteRect.width));
            height = Mathf.Max(2, Mathf.RoundToInt(spriteRect.height));
        }
    }

    // Generates texture with outline and bevel only, outside original shape, at given target size.
    // Highlight => bottom/right, shadow => top/left.
    private Texture2D GenerateBevelOutlineTextureAtSize(
        Texture2D srcTexture,
        Rect spriteRect,
        int targetWidth,
        int targetHeight,
        int mainWidth,
        int bevelWidth,
        Color mainColor,
        Color topLeftColor,
        Color bottomRightColor)
    {
        int srcXMin = Mathf.RoundToInt(spriteRect.x);
        int srcYMin = Mathf.RoundToInt(spriteRect.y);
        int srcWidth = Mathf.RoundToInt(spriteRect.width);
        int srcHeight = Mathf.RoundToInt(spriteRect.height);

        Color[] srcPixels = srcTexture.GetPixels(srcXMin, srcYMin, srcWidth, srcHeight);

        // Build inside mask at target size by sampling sprite alpha
        bool[] insideMask = new bool[targetWidth * targetHeight];
        float alphaThreshold = 0.01f;

        int twMinus = Mathf.Max(1, targetWidth - 1);
        int thMinus = Mathf.Max(1, targetHeight - 1);

        for (int y = 0; y < targetHeight; y++)
        {
            float v = (float)y / thMinus;
            int row = y * targetWidth;

            for (int x = 0; x < targetWidth; x++)
            {
                float u = (float)x / twMinus;
                float a = SampleAlphaBilinear(srcPixels, srcWidth, srcHeight, u, v);
                int idx = row + x;
                insideMask[idx] = a > alphaThreshold;
            }
        }

        int totalOutlineWidth = Mathf.Max(1, mainWidth) + Mathf.Max(0, bevelWidth);
        int dstWidth = targetWidth + totalOutlineWidth * 2;
        int dstHeight = targetHeight + totalOutlineWidth * 2;

        Color[] dstPixels = new Color[dstWidth * dstHeight];
        for (int i = 0; i < dstPixels.Length; i++)
            dstPixels[i] = new Color(0, 0, 0, 0);

        bool[] dstInsideMask = new bool[dstWidth * dstHeight];

        // Mark where original shape would be in destination
        for (int y = 0; y < targetHeight; y++)
        {
            int srcRow = y * targetWidth;
            for (int x = 0; x < targetWidth; x++)
            {
                int srcIdx = srcRow + x;
                if (!insideMask[srcIdx])
                    continue;

                int dstX = x + totalOutlineWidth;
                int dstY = y + totalOutlineWidth;
                int dstIdx = dstY * dstWidth + dstX;
                dstInsideMask[dstIdx] = true;
            }
        }

        float[] bestDist = new float[dstWidth * dstHeight];
        for (int i = 0; i < bestDist.Length; i++)
            bestDist[i] = float.MaxValue;

        int maxR = totalOutlineWidth;

        // Expand from original shape to generate outline and bevel
        for (int sy = 0; sy < targetHeight; sy++)
        {
            int srcRow = sy * targetWidth;
            for (int sx = 0; sx < targetWidth; sx++)
            {
                int srcIdx = srcRow + sx;
                if (!insideMask[srcIdx])
                    continue;

                int centerX = sx + totalOutlineWidth;
                int centerY = sy + totalOutlineWidth;

                for (int dy = -maxR; dy <= maxR; dy++)
                {
                    int ny = centerY + dy;
                    if (ny < 0 || ny >= dstHeight)
                        continue;

                    int nyRow = ny * dstWidth;

                    for (int dx = -maxR; dx <= maxR; dx++)
                    {
                        int nx = centerX + dx;
                        if (nx < 0 || nx >= dstWidth)
                            continue;

                        int dstIdx = nyRow + nx;

                        if (dstInsideMask[dstIdx])
                            continue;

                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist <= 0.5f || dist > maxR)
                            continue;

                        if (dist >= bestDist[dstIdx])
                            continue;

                        Color finalColor;

                        if (dist <= mainWidth)
                        {
                            finalColor = mainColor;
                        }
                        else
                        {
                            if (bevelWidth <= 0)
                            {
                                finalColor = mainColor;
                            }
                            else
                            {
                                float t = (dist - mainWidth) / Mathf.Max(1f, bevelWidth);
                                t = Mathf.Clamp01(t);

                                Color targetBevelColor;
                                float absDx = Mathf.Abs(dx);
                                float absDy = Mathf.Abs(dy);

                                if (absDx > absDy)
                                {
                                    // Left vs right
                                    targetBevelColor = dx < 0 ? topLeftColor : bottomRightColor;
                                }
                                else
                                {
                                    // Top vs bottom
                                    targetBevelColor = dy > 0 ? topLeftColor : bottomRightColor;
                                }

                                finalColor = Color.Lerp(mainColor, targetBevelColor, t);
                            }
                        }

                        bestDist[dstIdx] = dist;
                        dstPixels[dstIdx] = finalColor;
                    }
                }
            }
        }

        Texture2D result = new Texture2D(dstWidth, dstHeight, TextureFormat.RGBA32, false);
        result.wrapMode = TextureWrapMode.Clamp;
        result.filterMode = FilterMode.Point;
        result.SetPixels(dstPixels);
        result.Apply();

        return result;
    }

    private float SampleAlphaBilinear(Color[] srcPixels, int srcWidth, int srcHeight, float u, float v)
    {
        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        float x = u * (srcWidth - 1);
        float y = v * (srcHeight - 1);

        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        int x1 = Mathf.Min(x0 + 1, srcWidth - 1);
        int y1 = Mathf.Min(y0 + 1, srcHeight - 1);

        float tx = x - x0;
        float ty = y - y0;

        int idx00 = y0 * srcWidth + x0;
        int idx10 = y0 * srcWidth + x1;
        int idx01 = y1 * srcWidth + x0;
        int idx11 = y1 * srcWidth + x1;

        float a00 = srcPixels[idx00].a;
        float a10 = srcPixels[idx10].a;
        float a01 = srcPixels[idx01].a;
        float a11 = srcPixels[idx11].a;

        float a0 = Mathf.Lerp(a00, a10, tx);
        float a1 = Mathf.Lerp(a01, a11, tx);
        return Mathf.Lerp(a0, a1, ty);
    }

    private void SaveOutlineSprite(int targetPixelWidth, int targetPixelHeight)
    {
        if (sourceSprite == null || sourceTexture == null)
        {
            Debug.LogError("Source sprite or texture is missing.");
            return;
        }

        if (!IsTextureReadable(sourceTexture))
        {
            Debug.LogError("Texture is not readable. Enable Read/Write before saving outline.");
            return;
        }

        Rect rect = sourceSprite.rect;

        Texture2D outlinedTex = GenerateBevelOutlineTextureAtSize(
            sourceTexture,
            rect,
            targetPixelWidth,
            targetPixelHeight,
            mainOutlineWidth,
            bevelWidth,
            mainOutlineColor,
            topLeftBevelColor,
            bottomRightBevelColor
        );

        string originalPath = AssetDatabase.GetAssetPath(sourceTexture);
        if (string.IsNullOrEmpty(originalPath))
        {
            Debug.LogError("Could not determine original texture path.");
            return;
        }

        string dir = Path.GetDirectoryName(originalPath);
        string fileName = Path.GetFileNameWithoutExtension(originalPath);
        string newPath = EditorUtility.SaveFilePanelInProject(
            "Save Bevel Outline Sprite",
            fileName + "_bevelOutline.png",
            "png",
            "Choose a location to save the bevel outline sprite.",
            dir
        );

        if (string.IsNullOrEmpty(newPath))
        {
            DestroyImmediate(outlinedTex);
            return;
        }

        byte[] pngData = outlinedTex.EncodeToPNG();
        File.WriteAllBytes(newPath, pngData);
        AssetDatabase.ImportAsset(newPath);

        TextureImporter importer = AssetImporter.GetAtPath(newPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            try
            {
                var settings = importer.GetPlatformTextureSettings("Standalone");
                settings.overridden = true;
                settings.format = TextureImporterFormat.RGBA32;
                importer.SetPlatformTextureSettings(settings);
            }
            catch { }

            importer.spritePixelsPerUnit = sourceSprite.pixelsPerUnit;
            importer.SaveAndReimport();
        }

        Sprite newSprite = AssetDatabase.LoadAssetAtPath<Sprite>(newPath);
        if (newSprite != null)
        {
            Selection.activeObject = newSprite;
            EditorGUIUtility.PingObject(newSprite);
            Debug.Log("Saved bevel outline sprite at: " + newPath);
        }

        DestroyImmediate(outlinedTex);
    }

    private bool IsTextureReadable(Texture2D tex)
    {
        if (tex == null)
            return false;

        try
        {
            tex.GetPixel(0, 0);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void MakeTextureReadable(Texture2D tex)
    {
        if (tex == null)
            return;

        string path = AssetDatabase.GetAssetPath(tex);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Could not find asset path for texture.");
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Could not get TextureImporter for texture at: " + path);
            return;
        }

        if (!importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
            Debug.Log("Texture set to Read/Write enabled: " + path);
        }
        else
        {
            Debug.Log("Texture is already readable.");
        }
    }

    private void LoadSettings()
    {
        string json = EditorPrefs.GetString(EditorPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
            return;

        try
        {
            var data = JsonUtility.FromJson<SettingsData>(json);
            if (data == null)
                return;

            mainOutlineWidth = data.mainOutlineWidth;
            mainOutlineColor = data.mainOutlineColor;
            bevelWidth = data.bevelWidth;
            topLeftBevelColor = data.topLeftBevelColor;
            bottomRightBevelColor = data.bottomRightBevelColor;
            previewBackgroundColor = data.previewBackgroundColor;
        }
        catch
        {
        }
    }

    private void SaveSettings()
    {
        var data = new SettingsData
        {
            mainOutlineWidth = mainOutlineWidth,
            mainOutlineColor = mainOutlineColor,
            bevelWidth = bevelWidth,
            topLeftBevelColor = topLeftBevelColor,
            bottomRightBevelColor = bottomRightBevelColor,
            previewBackgroundColor = previewBackgroundColor
        };

        string json = JsonUtility.ToJson(data);
        EditorPrefs.SetString(EditorPrefsKey, json);
    }
}
