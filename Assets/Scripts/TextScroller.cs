using System.Collections;
using UnityEngine;

public class TextScroller : MonoBehaviour
{
    public float speed = 10;

    float MinX = -500;
    float MaxX = 500;

    RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        StartCoroutine(InitializeAfterLayout());
    }

    IEnumerator InitializeAfterLayout()
    {
        // Wait for end of frame to ensure layout is calculated
        yield return new WaitForEndOfFrame();

        // Force canvas update
        Canvas.ForceUpdateCanvases();

        float screenWidth = Screen.width;
        MinX = -rectTransform.rect.width - 32;

        var parentRect = rectTransform.parent.GetComponent<RectTransform>();
        MaxX = parentRect.rect.width + 32;

        Debug.Log("Screen width: " + screenWidth + ", MinX: " + MinX + ", MaxX: " + MaxX + " rectTransform.rect.width: " + rectTransform.rect.width + " parentRect.rect.width:" + parentRect.rect.width);
    }

    private void Update()
    {
        rectTransform.anchoredPosition += Vector2.left * speed * Time.deltaTime;
        if (rectTransform.anchoredPosition.x < MinX)
        {
            rectTransform.anchoredPosition = new Vector2(MaxX, rectTransform.anchoredPosition.y);
        }
    }
}