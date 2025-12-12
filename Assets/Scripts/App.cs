using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// TODO append message to LED display queue when pressed
// TODO swipe to change layouts!! (but if too sensitive, annoying)
// message panel can display currently active window title

public class App : MonoBehaviour
{
    public AudioClip keyDownClip;
    public AudioClip keyUpClip;

    public static App Instance { get; private set; }


    public PCRemoteClient remoteClient;

    [Header("Buttons")]
    public Image powerLed;

    [ColorUsage(true, true)]
    public Color powerLedTransmitColor;
    [ColorUsage(true, true)]
    public Color powerLedOnColor;
    [ColorUsage(true, true)]
    public Color powerLedOffColor;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("duplicate app instance", gameObject);
            Destroy(gameObject);
        }

        powerLed.color = remoteClient.IsConnected ? powerLedOnColor : powerLedOffColor;

    }

    public void SendCommand(string cmd)
    {
        remoteClient.SendCommand(cmd);
        StartCoroutine(LedBlinkCoroutine());
    }

    private IEnumerator LedBlinkCoroutine()
    {
        powerLed.color = remoteClient.IsConnected ? powerLedTransmitColor : powerLedOffColor*1.2f;
        yield return new WaitForSeconds(0.16f);
        powerLed.color = remoteClient.IsConnected ? powerLedOnColor : powerLedOffColor;
    }
}
