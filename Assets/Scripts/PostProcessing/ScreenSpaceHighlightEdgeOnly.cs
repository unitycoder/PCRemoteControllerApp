using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[Serializable]
[PostProcess(typeof(ScreenSpaceHighlightEdgeOnlyRenderer), PostProcessEvent.BeforeStack, "Custom/Screen Space Highlights (Edge Only)")]
public sealed class ScreenSpaceHighlightEdgeOnly : PostProcessEffectSettings
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
    
    [Header("Highlight Settings")]
    [Range(0f, 50f), Tooltip("Intensity of the highlight effect")]
    public FloatParameter highlightIntensity = new FloatParameter { value = 1.5f };
    
    [Tooltip("HDR color tint for the streak highlights")]
    public ColorParameter streakColor = new ColorParameter { value = Color.white };
    
    [Range(0f, 1000f), Tooltip("Additional emission boost for bloom effect")]
    public FloatParameter emissionBoost = new FloatParameter { value = 0f };
    
    [Range(-1f, 1f), Tooltip("Offset position of the streak texture")]
    public FloatParameter streakOffset = new FloatParameter { value = 0f };
    
    [Header("Animation")]
    [Tooltip("Automatically animate the streak offset")]
    public BoolParameter animateStreak = new BoolParameter { value = false };
    
    [Tooltip("Speed of the automatic animation")]
    public FloatParameter animationSpeed = new FloatParameter { value = 0.5f };
}

public sealed class ScreenSpaceHighlightEdgeOnlyRenderer : PostProcessEffectRenderer<ScreenSpaceHighlightEdgeOnly>
{
    private float animationTime = 0f;
    
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/PostProcessing/ScreenSpaceHighlightsEdgeOnly"));
        
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
        sheet.properties.SetFloat("_EdgeThreshold", settings.edgeThreshold.value);
        sheet.properties.SetFloat("_EdgeSensitivity", settings.edgeSensitivity.value);
        sheet.properties.SetFloat("_MinLuminosityDifference", settings.minLuminosityDifference.value);
        sheet.properties.SetFloat("_HighlightIntensity", settings.highlightIntensity.value);
        sheet.properties.SetColor("_StreakColor", settings.streakColor.value);
        sheet.properties.SetFloat("_EmissionBoost", settings.emissionBoost.value);
        sheet.properties.SetFloat("_StreakOffset", currentOffset);
        sheet.properties.SetFloat("_EdgeThickness", settings.edgeThickness.value);
        sheet.properties.SetFloat("_EdgeSharpness", settings.edgeSharpness.value);
        
        // Render the effect
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}
