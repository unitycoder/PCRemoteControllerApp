using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using WebSocketSharp;

public class PCRemoteClient : MonoBehaviour
{
    [Header("WebSocket Settings")]
    [Tooltip("Example: ws://192.168.1.188:8081/remote")]
    public string serverUrl = "ws://192.168.1.188:8081/remote";
    public bool usePinCode = false;
    public string pinCode = "123456";

    [Tooltip("Connect automatically in Start()")]
    public bool connectOnStart = true;

    [Header("Reconnection")]
    [Tooltip("Automatically reconnect when disconnected unexpectedly")]
    public bool autoReconnect = true;

    [Tooltip("Initial delay before first reconnection attempt (seconds)")]
    public float reconnectDelay = 2f;

    [Tooltip("Maximum delay between reconnection attempts (seconds)")]
    public float reconnectDelayMax = 30f;

    [Tooltip("Multiplier applied to delay after each failed attempt")]
    public float reconnectBackoffMultiplier = 1.5f;

    [Header("UI (optional)")]
    [Tooltip("Status label text (optional).")]
    public Text statusText;

    [Tooltip("Input field for custom command (optional).")]
    public InputField customCommandInput;

    [Header("Debug")]
    public bool verboseLogging = true;
        
    [Header("Events")]
    public UnityEvent onConnected;
    public UnityEvent onDisconnected;
    public UnityEvent onError;


    private WebSocket ws;
    private readonly ConcurrentQueue<System.Action> mainThreadActions = new ConcurrentQueue<System.Action>();
    private bool intentionalDisconnect;
    private Coroutine reconnectCoroutine;

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

    private void Update()
    {
        while (mainThreadActions.TryDequeue(out var action))
        {
            action?.Invoke();
        }
    }

    private void OnDestroy()
    {
        StopReconnect();
        Disconnect();
    }

    public void Connect()
    {
        if (ws != null && (ws.ReadyState == WebSocketState.Open || ws.ReadyState == WebSocketState.Connecting))
        {
            Log("Already connected or connecting.");
            return;
        }

        StopReconnect();
        intentionalDisconnect = false;

        // Clean up any leftover instance from a previous session
        if (ws != null)
        {
            try
            {
                ws.Close();
            }
            catch
            {
                // Ignore errors during cleanup of stale connection
            }
            ws = null;
        }

        Log("Connecting to " + serverUrl + " ...");
        ws = new WebSocket(serverUrl);

        ws.OnOpen += (sender, e) =>
        {
            mainThreadActions.Enqueue(() =>
            {
                StopReconnect();
                onConnected?.Invoke();
                UpdateStatusLabel();
                Log("Connected.");
            });
        };

        ws.OnClose += (sender, e) =>
        {
            mainThreadActions.Enqueue(() =>
            {
                onDisconnected?.Invoke();
                UpdateStatusLabel();
                Log("Disconnected. Code: " + e.Code + " Reason: " + e.Reason);
                if (!intentionalDisconnect && autoReconnect)
                {
                    StartReconnect();
                }
            });
        };

        ws.OnError += (sender, e) =>
        {
            mainThreadActions.Enqueue(() =>
            {
                onError?.Invoke();
                Log("Error: " + e.Message);
            });
        };

        ws.OnMessage += (sender, e) =>
        {
            mainThreadActions.Enqueue(() =>
            {
                Log("Server: " + e.Data);
            });
        };

        ws.ConnectAsync();
        UpdateStatusLabel();
    }

    public void Disconnect()
    {
        intentionalDisconnect = true;
        StopReconnect();

        if (ws == null)
        {
            return;
        }

        if (ws.ReadyState == WebSocketState.Closing || ws.ReadyState == WebSocketState.Closed)
        {
            ws = null;
            return;
        }

        Log("Closing connection...");
        ws.Close(CloseStatusCode.Normal, "Client disconnecting");
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

    private void StartReconnect()
    {
        if (reconnectCoroutine != null)
        {
            return;
        }

        reconnectCoroutine = StartCoroutine(ReconnectCoroutine());
    }

    private void StopReconnect()
    {
        if (reconnectCoroutine != null)
        {
            StopCoroutine(reconnectCoroutine);
            reconnectCoroutine = null;
        }
    }

    private IEnumerator ReconnectCoroutine()
    {
        float delay = reconnectDelay;

        while (autoReconnect && !intentionalDisconnect)
        {
            Log("Reconnecting in " + delay.ToString("F1") + "s ...");
            yield return new WaitForSeconds(delay);

            if (intentionalDisconnect || !autoReconnect)
            {
                break;
            }

            if (ws != null && ws.ReadyState == WebSocketState.Open)
            {
                break;
            }

            Log("Attempting reconnection...");
            Connect();

            // Wait a moment to let ConnectAsync settle before checking
            yield return new WaitForSeconds(1f);

            if (ws != null && ws.ReadyState == WebSocketState.Open)
            {
                break;
            }

            delay = Mathf.Min(delay * reconnectBackoffMultiplier, reconnectDelayMax);
        }

        reconnectCoroutine = null;
    }
}







