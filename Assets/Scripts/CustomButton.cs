using UnityEngine;
using UnityEngine.EventSystems;


[RequireComponent(typeof(AudioSource))]
public class CustomButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public KeyCommand keyCommand;

    float downScale = 0.95f;
    private Vector3 originalScale;

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = originalScale * downScale;
        AudioSource.PlayClipAtPoint(App.Instance.keyDownClip, Vector3.zero, 0.4f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = originalScale;
        AudioSource.PlayClipAtPoint(App.Instance.keyUpClip, Vector3.zero, 0.4f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        App.Instance.SendCommand(keyCommand.ToString());
    }
}
