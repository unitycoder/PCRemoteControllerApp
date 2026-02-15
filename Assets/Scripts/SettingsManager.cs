using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    enum EditTarget { PinCode, IPAddress, Port }

    public TextMeshProUGUI textPinCode;
    public TextMeshProUGUI textIPAddress;
    public TextMeshProUGUI textPort;
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
    public Button buttonDot;

    [ColorUsage(true, true)]
    public Color highlightColor;
    public Color errorColor;
    Color origColor;
    public Image highlightPinCodeBorder;
    public Image highlightPortBorder;
    public Image highlightIPBorder;

    AudioSource aus;

    TextMeshProUGUI currentTextTarget;
    Image currentImageBorderTarget;
    EditTarget currentEditTarget = EditTarget.IPAddress;

    public PCRemoteClient remoteClient;

    void Start()
    {
        origColor = highlightPinCodeBorder.color;

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
        buttonDot.onClick.AddListener(() => AppendString("."));


        buttonBackspace.onClick.AddListener(() => Backspace());

        // Set initial target
        SetTargetText(textIPAddress);
    }

    private void OnEnable()
    {
        textPinCode.text = PlayerPrefs.GetString("PinCode", "");
        textIPAddress.text = PlayerPrefs.GetString("IPAddress", "192.168.0.100");
        textPort.text = PlayerPrefs.GetString("Port", "8080");
        ValidateIPAddress();
        ValidatePort();
    }

    private void Backspace()
    {
        if (currentTextTarget.text.Length > 0)
        {
            currentTextTarget.text = currentTextTarget.text.Substring(0, currentTextTarget.text.Length - 1);
            aus.pitch = 0.8f + 0.4f * (currentTextTarget.text.Length / (float)pinCodeLength);
        }
        else
        {
            aus.pitch = 0.5f;
        }
        aus.Play();

        if (currentEditTarget == EditTarget.IPAddress)
            ValidateIPAddress();
        else if (currentEditTarget == EditTarget.Port)
            ValidatePort();
    }

    public void SetTargetText(TextMeshProUGUI target)
    {
        currentTextTarget = target;

        if (target == textPinCode)
            currentEditTarget = EditTarget.PinCode;
        else if (target == textPort)
            currentEditTarget = EditTarget.Port;
        else
            currentEditTarget = EditTarget.IPAddress;

        currentImageBorderTarget = currentEditTarget == EditTarget.PinCode ? highlightPinCodeBorder
            : currentEditTarget == EditTarget.Port ? highlightPortBorder
            : highlightIPBorder;

        highlightPinCodeBorder.color = currentEditTarget == EditTarget.PinCode ? highlightColor : origColor;

        buttonDot.enabled = currentEditTarget == EditTarget.IPAddress;

        ValidateIPAddress();
        ValidatePort();
    }


    private void AppendDigit(int v)
    {
        if ((currentEditTarget == EditTarget.PinCode && currentTextTarget.text.Length < pinCodeLength) || currentEditTarget != EditTarget.PinCode)
        {
            currentTextTarget.text += v.ToString();
            aus.pitch = 0.8f + 0.4f * (currentTextTarget.text.Length / (float)pinCodeLength);
        }
        else
        {
            aus.pitch = 0.5f;
        }
        aus.Play();

        if (currentEditTarget == EditTarget.IPAddress)
            ValidateIPAddress();
        else if (currentEditTarget == EditTarget.Port)
            ValidatePort();
    }

    private void AppendString(string v)
    {
        if ((currentEditTarget == EditTarget.PinCode && currentTextTarget.text.Length < pinCodeLength) || currentEditTarget != EditTarget.PinCode)
        {
            currentTextTarget.text += v.ToString();
            aus.pitch = 0.8f + 0.4f * (currentTextTarget.text.Length / (float)pinCodeLength);
        }
        else
        {
            aus.pitch = 0.5f;
        }
        aus.Play();

        if (currentEditTarget == EditTarget.IPAddress)
            ValidateIPAddress();
        else if (currentEditTarget == EditTarget.Port)
            ValidatePort();
    }

    private void ValidateIPAddress()
    {
        string ip = textIPAddress.text;
        bool valid = false;

        if (!string.IsNullOrEmpty(ip))
        {
            string[] parts = ip.Split('.');
            if (parts.Length == 4)
            {
                valid = true;
                for (int i = 0; i < 4; i++)
                {
                    if (!int.TryParse(parts[i], out int octet) || octet < 0 || octet > 255 || parts[i].Length == 0)
                    {
                        valid = false;
                        break;
                    }
                }
            }
        }

        if (!valid)
            highlightIPBorder.color = errorColor;
        else if (currentEditTarget == EditTarget.IPAddress)
            highlightIPBorder.color = highlightColor;
        else
            highlightIPBorder.color = origColor;
    }

    private void ValidatePort()
    {
        string portText = textPort.text;
        bool valid = !string.IsNullOrEmpty(portText) && int.TryParse(portText, out int port) && port >= 1 && port <= 65535;

        if (!valid)
            highlightPortBorder.color = errorColor;
        else if (currentEditTarget == EditTarget.Port)
            highlightPortBorder.color = highlightColor;
        else
            highlightPortBorder.color = origColor;
    }

    public void CloseSettings()
    {
        PlayerPrefs.SetString("PinCode", textPinCode.text);
        PlayerPrefs.SetString("IPAddress", textIPAddress.text);
        PlayerPrefs.SetString("Port", textPort.text);
        PlayerPrefs.Save();
        gameObject.SetActive(false);

        remoteClient.onReconnectFailed.RemoveListener(OnReconnectFailed);
        remoteClient.onReconnectFailed.AddListener(OnReconnectFailed);
        remoteClient.Reconnect();
    }

    private void OnReconnectFailed()
    {
        remoteClient.onReconnectFailed.RemoveListener(OnReconnectFailed);
        gameObject.SetActive(true);
    }

} // class
