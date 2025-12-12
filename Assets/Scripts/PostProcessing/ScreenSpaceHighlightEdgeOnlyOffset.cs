using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(ScreenSpaceHighlightEdgeOnlyOffsetRenderer), PostProcessEvent.BeforeStack, "Custom/Screen Space Highlights Offset (Edge Only)")]
public sealed class ScreenSpaceHighlightEdgeOnlyOffset : PostProcessEffectSettings
{
    [Header("Streak Texture")]
    [Tooltip("The streak texture to use for highlights (white streaks on black background)")]
    public TextureParameter streakTexture = new TextureParameter { value = null };

    [Header("Edge Detection Settings")]
    [Range(0f, 1f), Tooltip("Threshold for edge detection - lower = more edges detected")]
    public FloatParameter edgeThreshold = new FloatParameter { value = 0.1f };

    [Range(0.001f, 1f), Tooltip("Sensitivity to luminosity changes - lower = more sensitive")]
    public FloatParameter edgeSensitivity = new FloatParameter { value = 0.1f };

    [Range(0f, 1f), Tooltip("Minimum luminosity difference required to detect edge")]
    public FloatParameter minLuminosityDifference = new FloatParameter { value = 0.05f };

    [Range(0.001f, 0.05f), Tooltip("Thickness of detected edges")]
    public FloatParameter edgeThickness = new FloatParameter { value = 0.01f };

    [Range(1f, 10f), Tooltip("Sharpness of edge detection")]
    public FloatParameter edgeSharpness = new FloatParameter { value = 3f };

    [Header("Color Inclusion")]
    [Tooltip("Enable color-based inclusion - only matching colors get the effect")]
    public BoolParameter useColorInclusion = new BoolParameter { value = false };

    [Tooltip("Color to include in the effect - only this color (and similar) will get streaks")]
    public ColorParameter includeColor = new ColorParameter { value = Color.white };

    [Range(0f, 2f), Tooltip("Tolerance for color matching - higher values include more similar colors")]
    public FloatParameter colorTolerance = new FloatParameter { value = 0.3f };

    [Header("Highlight Settings")]
    [Range(0f, 50f), Tooltip("Intensity of the highlight effect")]
    public FloatParameter highlightIntensity = new FloatParameter { value = 1.5f };

    [Tooltip("HDR color tint for the streak highlights")]
    public ColorParameter streakColor = new ColorParameter { value = Color.white };

    [Range(0f, 1000f), Tooltip("Additional emission boost for bloom effect")]
    public FloatParameter emissionBoost = new FloatParameter { value = 0f };

    [Tooltip("XY offset position of the streak texture")]
    public Vector2Parameter streakOffset = new Vector2Parameter { value = Vector2.zero };

    [Header("Animation")]
    [Tooltip("Automatically animate the streak offset")]
    public BoolParameter animateStreak = new BoolParameter { value = false };

    [Tooltip("Speed of the automatic animation (XY)")]
    public Vector2Parameter animationSpeed = new Vector2Parameter { value = new Vector2(0.5f, 0f) };
}

public sealed class ScreenSpaceHighlightEdgeOnlyOffsetRenderer : PostProcessEffectRenderer<ScreenSpaceHighlightEdgeOnlyOffset>
{
    private Vector2 animationTime = Vector2.zero;

    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/PostProcessing/ScreenSpaceHighlightsEdgeOnlyOffset"));

        if (settings.streakTexture.value == null)
        {
            // If no streak texture, just pass through
            context.command.BlitFullscreenTriangle(context.source, context.destination);
            return;
        }

        // Handle animation
        Vector2 currentOffset = settings.streakOffset.value;
        if (settings.animateStreak.value)
        {
            animationTime += new Vector2(
                Time.deltaTime * settings.animationSpeed.value.x,
                Time.deltaTime * settings.animationSpeed.value.y
            );

            // Wrap animation time to prevent overflow
            if (animationTime.x > 2f) animationTime.x -= 2f;
            if (animationTime.x < 0f) animationTime.x += 2f;
            if (animationTime.y > 2f) animationTime.y -= 2f;
            if (animationTime.y < 0f) animationTime.y += 2f;

            currentOffset = new Vector2(
                (animationTime.x - 1f), // Convert 0-2 to -1 to 1
                (animationTime.y - 1f)
            );
        }

        // Set shader properties
        sheet.properties.SetTexture("_StreakTex", settings.streakTexture.value);
        sheet.properties.SetFloat("_EdgeThreshold", settings.edgeThreshold.value);
        sheet.properties.SetFloat("_EdgeSensitivity", settings.edgeSensitivity.value);
        sheet.properties.SetFloat("_MinLuminosityDifference", settings.minLuminosityDifference.value);
        sheet.properties.SetFloat("_HighlightIntensity", settings.highlightIntensity.value);
        sheet.properties.SetColor("_StreakColor", settings.streakColor.value);
        sheet.properties.SetFloat("_EmissionBoost", settings.emissionBoost.value);
        sheet.properties.SetVector("_StreakOffset", currentOffset);
        sheet.properties.SetFloat("_EdgeThickness", settings.edgeThickness.value);
        sheet.properties.SetFloat("_EdgeSharpness", settings.edgeSharpness.value);

        // Color inclusion settings
        sheet.properties.SetInt("_UseColorInclusion", settings.useColorInclusion.value ? 1 : 0);
        sheet.properties.SetColor("_IncludeColor", settings.includeColor.value);
        sheet.properties.SetFloat("_ColorTolerance", settings.colorTolerance.value);

        // Render the effect
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}