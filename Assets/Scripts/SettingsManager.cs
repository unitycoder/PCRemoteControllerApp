using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public TextMeshProUGUI textPinCode;
    const int pinCodeLength = 6;

    public Button button0;
    public Button button1;
    public Button button2;
    public Button button3;
    public Button button4;
    public Button button5;
    public Button button6;
    public Button button7;
    public Button button8;
    public Button button9;
    public Button buttonBackspace;

    AudioSource aus;

    void Start()
    {
        aus = GetComponent<AudioSource>();

        button0.onClick.AddListener(() => AppendDigit(0));
        button1.onClick.AddListener(() => AppendDigit(1));
        button2.onClick.AddListener(() => AppendDigit(2));
        button3.onClick.AddListener(() => AppendDigit(3));
        button4.onClick.AddListener(() => AppendDigit(4));
        button5.onClick.AddListener(() => AppendDigit(5));
        button6.onClick.AddListener(() => AppendDigit(6));
        button7.onClick.AddListener(() => AppendDigit(7));
        button8.onClick.AddListener(() => AppendDigit(8));
        button9.onClick.AddListener(() => AppendDigit(9));

        buttonBackspace.onClick.AddListener(() =>
        {
            if (textPinCode.text.Length > 0)
            {
                textPinCode.text = textPinCode.text.Substring(0, textPinCode.text.Length - 1);
                aus.pitch = 1.3f;
                aus.Play();
            }
        });
    }

    private void AppendDigit(int v)
    {
        if (textPinCode.text.Length < pinCodeLength)
        {
            textPinCode.text += v.ToString();
            aus.pitch = 0.8f + 0.4f * (textPinCode.text.Length / (float)pinCodeLength);
            aus.Play();
        }
    }
}
