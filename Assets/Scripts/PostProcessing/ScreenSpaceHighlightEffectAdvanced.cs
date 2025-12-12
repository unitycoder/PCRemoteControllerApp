using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(ScreenSpaceHighlightRenderer), PostProcessEvent.AfterStack, "Custom/Screen Space Highlights")]
public sealed class ScreenSpaceHighlight : PostProcessEffectSettings
{
    [Header("Streak Texture")]
    [Tooltip("The streak texture to use for highlights (white streaks on black background)")]
    public TextureParameter streakTexture = new TextureParameter { value = null };

    [Header("Detection Settings")]
    [Range(0f, 1f), Tooltip("Brightness threshold for detecting areas")]
    public FloatParameter brightnessThreshold = new FloatParameter { value = 0.8f };

    [Tooltip("Use color detection instead of brightness")]
    public BoolParameter useColorMask = new BoolParameter { value = false };

    [Tooltip("Target color to detect")]
    public ColorParameter colorMask = new ColorParameter { value = Color.white };

    [Range(0f, 1f), Tooltip("How closely colors need to match")]
    public FloatParameter colorTolerance = new FloatParameter { value = 0.2f };

    [Header("Highlight Settings")]
    [Range(0f, 50f), Tooltip("Intensity of the highlight effect")]
    public FloatParameter highlightIntensity = new FloatParameter { value = 1.5f };

    [Tooltip("HDR color tint for the streak highlights")]
    public ColorParameter streakColor = new ColorParameter { value = Color.white };

    [Range(0f, 100f), Tooltip("Additional emission boost for bloom effect")]
    public FloatParameter emissionBoost = new FloatParameter { value = 0f };

    [Range(-1f, 1f), Tooltip("Offset position of the streak texture")]
    public FloatParameter streakOffset = new FloatParameter { value = 0f };

    [Range(0.001f, 0.05f), Tooltip("Thickness of detected edges")]
    public FloatParameter edgeThickness = new FloatParameter { value = 0.01f };

    [Header("Animation")]
    [Tooltip("Automatically animate the streak offset")]
    public BoolParameter animateStreak = new BoolParameter { value = false };

    [Tooltip("Speed of the automatic animation")]
    public FloatParameter animationSpeed = new FloatParameter { value = 0.5f };
}

public sealed class ScreenSpaceHighlightRenderer : PostProcessEffectRenderer<ScreenSpaceHighlight>
{
    private float animationTime = 0f;

    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/PostProcessing/ScreenSpaceHighlights"));

        if (settings.streakTexture.value == null)
        {
            // If no streak texture, just pass through
            context.command.BlitFullscreenTriangle(context.source, context.destination);
            return;
        }

        // Handle animation
        float currentOffset = settings.streakOffset.value;
        if (settings.animateStreak.value)
        {
            animationTime += Time.deltaTime * settings.animationSpeed.value;
            if (animationTime > 2f) animationTime -= 2f;
            currentOffset = (animationTime - 1f); // Convert 0-2 to -1 to 1
        }

        // Set shader properties
        sheet.properties.SetTexture("_StreakTex", settings.streakTexture.value);
        sheet.properties.SetFloat("_Threshold", settings.brightnessThreshold.value);
        sheet.properties.SetFloat("_HighlightIntensity", settings.highlightIntensity.value);
        sheet.properties.SetColor("_StreakColor", settings.streakColor.value);
        sheet.properties.SetFloat("_EmissionBoost", settings.emissionBoost.value);
        sheet.properties.SetFloat("_StreakOffset", currentOffset);
        sheet.properties.SetFloat("_EdgeThickness", settings.edgeThickness.value);
        sheet.properties.SetColor("_ColorMask", settings.colorMask.value);
        sheet.properties.SetFloat("_ColorTolerance", settings.colorTolerance.value);
        sheet.properties.SetFloat("_UseColorMask", settings.useColorMask.value ? 1f : 0f);

        // Render the effect
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}
