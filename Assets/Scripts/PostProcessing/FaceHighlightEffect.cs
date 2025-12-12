using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[System.Serializable]
[PostProcess(typeof(FaceHighlightRenderer), PostProcessEvent.AfterStack, "Custom/FaceHighlight")]
public sealed class FaceHighlightEffect : PostProcessEffectSettings
{
    [Tooltip("Threshold for bright areas (0..1). Used when Use Color Key is off.")]
    public FloatParameter threshold = new FloatParameter { value = 0.8f };

    [Tooltip("Use color key instead of brightness threshold.")]
    public BoolParameter useColorKey = new BoolParameter { value = false };

    [Tooltip("Color to key out when Use Color Key is enabled.")]
    public ColorParameter keyColor = new ColorParameter { value = Color.white };

    [Tooltip("How close color must be to keyColor (0..1). Smaller is stricter.")]
    public FloatParameter colorTolerance = new FloatParameter { value = 0.1f };

    [Tooltip("Edge sharpness (1..10). Higher = thinner, sharper edges.")]
    public FloatParameter edgeSharpness = new FloatParameter { value = 4f };

    [Tooltip("Highlight intensity.")]
    public FloatParameter intensity = new FloatParameter { value = 1.0f };

    [Tooltip("Streak texture used for highlight. Typically a horizontal streaks texture.")]
    public TextureParameter highlightTexture = new TextureParameter { value = null };

    [Tooltip("Offset for highlight texture in UV space. -1..1")]
    public FloatParameter highlightOffset = new FloatParameter { value = 0.0f };

    [Tooltip("Blend amount with original image.")]
    public FloatParameter blend = new FloatParameter { value = 1.0f };
}

public sealed class FaceHighlightRenderer : PostProcessEffectRenderer<FaceHighlightEffect>
{
    private Shader _shader;

    public override void Init()
    {
        _shader = Shader.Find("Hidden/Custom/FaceHighlight");
    }

    public override void Render(PostProcessRenderContext context)
    {
        if (_shader == null)
            return;

        var sheet = context.propertySheets.Get(_shader);

        sheet.properties.SetFloat("_Threshold", settings.threshold.value);
        sheet.properties.SetFloat("_UseColorKey", settings.useColorKey.value ? 1f : 0f);
        sheet.properties.SetColor("_KeyColor", settings.keyColor.value);
        sheet.properties.SetFloat("_ColorTolerance", Mathf.Max(0.0001f, settings.colorTolerance.value));
        sheet.properties.SetFloat("_EdgeSharpness", settings.edgeSharpness.value);
        sheet.properties.SetFloat("_Intensity", settings.intensity.value);
        sheet.properties.SetFloat("_HighlightOffset", settings.highlightOffset.value);
        sheet.properties.SetFloat("_Blend", settings.blend.value);

        if (settings.highlightTexture.value != null)
        {
            sheet.properties.SetTexture("_HighlightTex", settings.highlightTexture.value);
        }

        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}
