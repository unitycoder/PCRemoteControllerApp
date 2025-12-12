using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class GyroToShader : MonoBehaviour
{
    public PostProcessVolume volume;
    public float strength = 1.0f;
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.1f;

    ScreenSpaceHighlightEdgeOnlyOffset screenSpaceHighlightEdgeOnlyOffset;

    private Vector2 smoothedRotation = Vector2.zero;

    void Start()
    {
        Input.gyro.enabled = true;

        volume.profile.TryGetSettings(out screenSpaceHighlightEdgeOnlyOffset);
    }

    void Update()
    {
#if UNITY_EDITOR
        Vector3 rotation = Mathf.PingPong(Time.time * 4, 100) * Vector3.one;
#else
        Vector3 rotation = Input.gyro.attitude.eulerAngles;
        rotation.y=0;
        rotation.z=0;
#endif

        // Convert X rotation from 0-360 to -180 to 180
        float normalizedX = rotation.x;
        if (normalizedX > 180f)
        {
            normalizedX -= 360f;
        }

        // Convert Y rotation from 0-360 to -180 to 180
        float normalizedY = rotation.y;
        if (normalizedY > 180f)
        {
            normalizedY -= 360f;
        }

        Vector2 targetRotation = new Vector2(normalizedX, normalizedY);

        // Smooth the rotation using Lerp
        smoothedRotation = Vector2.Lerp(smoothedRotation, targetRotation, smoothSpeed);

        //Debug.Log(smoothedRotation * strength);

        screenSpaceHighlightEdgeOnlyOffset.streakOffset.value = smoothedRotation * strength;
    }
}
