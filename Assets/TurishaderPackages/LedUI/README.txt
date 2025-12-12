LED UI VFX turns any UI Image or TextMeshPro object into a LED display made of small light dots.
This package includes two shaders:

LedImage.shader – LED effect for UI Images / Sprites.

LedText.shader – LED effect for TextMeshPro text.

Assign the provided materials to your UI elements and tweak the parameters described below.

1. GENERAL UI PARAMETERS (BOTH SHADERS)

These parameters are standard Unity UI shader properties:

_MainTex (Sprite Texture)
Base texture of the UI element (Image or Text atlas for TMP).

_Color (Tint)
Multiplies the final color. You can use it as a global tint for the LED effect.

_StencilComp, _Stencil, _StencilOp, _StencilWriteMask, _StencilReadMask
Standard UI stencil options. Use them if you work with Unity’s mask components.

_ColorMask
Controls which color channels are written. Normally you don’t need to change this.

_UseUIAlphaClip (Use Alpha Clip)
Toggles alpha clipping for UI. Enable it to get hard cutouts based on alpha instead of soft blending.

2. LEDIMAGE.SHADER – PARAMETERS

Use this shader on UI Images (RawImage/Image).
Main idea: the UVs are quantized into a grid and each cell becomes one LED.

2.1 LED GRID & SHAPE

_LedsAmount (LedsAmount)
Controls how many LEDs are generated over the texture.

Higher values → more, smaller LEDs (denser grid).

Lower values → fewer, bigger LEDs (blockier look).

_LedMaks (LedMaks, 2D)
Texture used as the shape mask of a single LED.
The shader tiles this texture inside each LED cell and reads its red channel to define the LED form (circle, square, stripe, etc.).

Assign the included mask texture to change between dot-like or rectangular LEDs.

You can also plug your own mask to design custom LED shapes.

_Colorhdr (Colorhdr)
HDR multiplier applied to the LED contribution.
Use it as LED intensity / strength:

Lower values → dimmer LEDs, more subtle effect.

Higher values → brighter LEDs and stronger visibility of the mask pattern.

2.2 CLIPPING & BRIGHTNESS

_ClipValue (ClipValue)
Threshold applied on the sampled main texture and mask.
Controls how easily a pixel turns into an “active” LED.

Lower values → more LEDs lit (image appears brighter and fuller).

Higher values → only the strongest parts of the image light up, giving a more sparse / contrasted look.

2.3 ROTATION NOISE (LED VARIATION)

These parameters add variation by rotating the LED mask per cell based on a procedural noise:

_RotationVariationAmount (RotationVariationAmount)
Strength of the rotation applied to each LED cell.

0 → all LEDs use the same orientation (no variation).

Higher values → each LED rotates differently, using noise; good for slightly chaotic / analog LED panel feeling.

_RotationVariationNoiseScale (RotationVariationNoiseScale)
Controls the scale of the noise used to drive rotation.

Low values → large, smooth regions of similar rotation (slow, soft variation across the surface).

High values → more frequent changes in rotation between neighboring LEDs (more noisy, detailed variation).

3. LEDTEXT.SHADER – PARAMETERS

Use this shader on TextMeshPro components.
It converts SDF text into LED cells while keeping TMP features like tint, italics, strikethrough and underline.

3.1 LED GRID & SHAPE (TEXT)

_LedsAmount (LedsAmount)
Same as in LedImage.shader:

Controls how many LEDs per area are used for the text.

Higher = smaller LEDs, more detail.

Lower = bigger LEDs, more chunky display-style text.

_LedMaks (LedMaks, 2D)
Same concept as in the image shader: LED shape mask.

Defines how each LED dot looks for the text.

Use the provided mask or your own.

_Colorhdr (Colorhdr)
HDR multiplier for the LED contribution on the text.

Lower → softer LED effect.

Higher → stronger, brighter LED dots.

3.2 TEXT EDGE CONTROL (SDF)

These parameters control how the SDF text is converted into on/off LEDs:

_TextDilate (TextDilate)
Grows or shrinks the effective thickness of the text before applying the LED mask.

Lower values → thinner characters; LEDs only light up very close to the original glyph edge.

Higher values → thicker characters; more LEDs inside the glyph body light up.

_TextDilateSmoothness (TextDilateSmoothness)
Controls how soft or hard the transition is at the edges when evaluating the SDF.

Low values → sharper edges, more “digital” and crisp.

Higher values → smoother transitions and softer edges in the LED text.

4. BASIC WORKFLOW

UI Images

Create a UI Image.

Assign a material using LedImage.shader.

Set _MainTex to your source sprite.

Adjust _LedsAmount to define resolution of the LED grid.

Tweak _LedMaks and _Colorhdr to get the desired LED shape and brightness.

Optionally use _ClipValue and rotation noise parameters for more stylized looks.

TextMeshPro

Create a TMP Text object.

Assign a material using LedText.shader.

Adjust _LedsAmount to control LED density in the text.

Use _TextDilate and _TextDilateSmoothness to refine the thickness and readability of the LED text.

Tune _LedMaks and _Colorhdr to customize LED dot shape and intensity.

UI Integration

The effect works with CanvasGroup and standard UI tinting.

Stencil and alpha clip options are available if you use masks or complex UI hierarchies.

5. SUPPORT

For any question, bug or feature request, contact:

Turishader@gmail.com