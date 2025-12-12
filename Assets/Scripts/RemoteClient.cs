using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class PCRemoteClient : MonoBehaviour
{
    [Header("WebSocket Settings")]
    [Tooltip("Example: ws://192.168.1.188:8081/keys")]
    public string serverUrl = "ws://192.168.1.188:8081/keys";

    [Tooltip("Connect automatically in Start()")]
    public bool connectOnStart = true;

    [Header("UI (optional)")]
    [Tooltip("Status label text (optional).")]
    public Text statusText;

    [Tooltip("Input field for custom command (optional).")]
    public InputField customCommandInput;

    [Header("Debug")]
    public bool verboseLogging = true;

    private WebSocket ws;

    public bool IsConnected
    {
        get { return ws != null && ws.ReadyState == WebSocketState.Open; }
    }

    private void Start()
    {
        if (connectOnStart)
        {
            Connect();
        }
        UpdateStatusLabel();
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    public void Connect()
    {
        if (ws != null && (ws.ReadyState == WebSocketState.Open || ws.ReadyState == WebSocketState.Connecting))
        {
            Log("Already connected or connecting.");
            return;
        }

        Log("Connecting to " + serverUrl + " ...");
        ws = new WebSocket(serverUrl);

        ws.OnOpen += (sender, e) =>
        {
            Log("Connected.");
        };

        ws.OnClose += (sender, e) =>
        {
            Log("Disconnected. Code: " + e.Code + " Reason: " + e.Reason);
        };

        ws.OnError += (sender, e) =>
        {
            Log("Error: " + e.Message);
        };

        ws.OnMessage += (sender, e) =>
        {
            Log("Server: " + e.Data);
        };

        ws.ConnectAsync();
        UpdateStatusLabel();
    }

    public void Disconnect()
    {
        if (ws == null)
        {
            return;
        }

        if (ws.ReadyState == WebSocketState.Closing || ws.ReadyState == WebSocketState.Closed)
        {
            return;
        }

        Log("Closing connection...");
        ws.CloseAsync();
        ws = null;
        UpdateStatusLabel();
    }

    public void SendCommand(string cmd)
    {
        if (ws == null || ws.ReadyState != WebSocketState.Open)
        {
            Log("Cannot send, not connected. Command: " + cmd);
            return;
        }

        if (string.IsNullOrWhiteSpace(cmd))
        {
            Log("Cannot send empty command.");
            return;
        }

        Log("Sending: " + cmd);
        ws.Send(cmd);
    }

    private void Log(string msg)
    {
        if (!verboseLogging)
        {
            return;
        }

        Debug.Log("[PCRemoteClient] " + msg);
    }

    private void UpdateStatusLabel()
    {
        if (statusText == null)
        {
            return;
        }

        if (IsConnected)
        {
            statusText.text = "Connected";
        }
        else
        {
            statusText.text = "Disconnected";
        }
    }

    // UI helper: call from a button for connect
    public void UiConnect()
    {
        Connect();
        UpdateStatusLabel();
    }

    // UI helper: call from a button for disconnect
    public void UiDisconnect()
    {
        Disconnect();
        UpdateStatusLabel();
    }

    // UI helper: send text from InputField
    public void UiSendCustomCommand()
    {
        if (customCommandInput == null)
        {
            Log("No customCommandInput assigned.");
            return;
        }

        string cmd = customCommandInput.text.Trim();
        if (!string.IsNullOrEmpty(cmd))
        {
            SendCommand(cmd);
        }
    }

    // Convenience methods for common commands

    public void SendAltTab()
    {
        SendCommand("alt+tab");
    }

    public void SendMediaPlayPause()
    {
        SendCommand("media_play_pause");
    }

    public void SendMediaNext()
    {
        SendCommand("media_next");
    }

    public void SendMediaPrev()
    {
        SendCommand("media_prev");
    }

    public void SendVolumeUp()
    {
        SendCommand("vol_up");
    }

    public void SendVolumeDown()
    {
        SendCommand("vol_down");
    }

    public void SendVolumeMute()
    {
        SendCommand("vol_mute");
    }

    public void SendEsc()
    {
        SendCommand("esc");
    }

    public void SendSpace()
    {
        SendCommand("space");
    }

    public void SendEnter()
    {
        SendCommand("enter");
    }

    public void SendArrowUp()
    {
        SendCommand("arrow_up");
    }

    public void SendArrowDown()
    {
        SendCommand("arrow_down");
    }

    public void SendArrowLeft()
    {
        SendCommand("arrow_left");
    }

    public void SendArrowRight()
    {
        SendCommand("arrow_right");
    }
}







