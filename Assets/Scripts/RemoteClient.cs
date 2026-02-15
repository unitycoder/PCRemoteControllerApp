using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using WebSocketSharp;

public class PCRemoteClient : MonoBehaviour
{
    [Header("WebSocket Settings")]
    [Tooltip("Example: ws://192.168.1.100:8080/remote")]
    public string serverUrl = "ws://192.168.1.100:8080/remote";
    public bool usePinCode = false;
    public string pinCode = "123456";

    const string defaultIPAddress = "192.168.1.100";
    const string defaultPort = "8080";
    const string websocketPath = "/remote";

    [Tooltip("Connect automatically in Start()")]
    public bool connectOnStart = true;

    [Header("Reconnection")]
    [Tooltip("Automatically reconnect when disconnected unexpectedly")]
    public bool autoReconnect = true;

    [Tooltip("Maximum number of reconnection attempts before giving up (0 = unlimited)")]
    public int maxReconnectAttempts = 2;

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
    public UnityEvent onReconnectFailed;


    private WebSocket ws;
    private readonly ConcurrentQueue<System.Action> mainThreadActions = new ConcurrentQueue<System.Action>();
    private readonly object wsLock = new object();
    private bool intentionalDisconnect;
    private Coroutine reconnectCoroutine;
    private int reconnectAttemptCount;
    private CancellationTokenSource connectCts;
    private volatile bool isConnecting;
    private int connectionGeneration;

    public bool IsConnected
    {
        get { return ws != null && ws.ReadyState == WebSocketState.Open; }
    }

    private void Start()
    {
        LoadSettings();
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
        connectCts?.Cancel();
        connectCts?.Dispose();
        connectCts = null;
        Disconnect();
    }

    public void Connect()
    {
        if (isConnecting)
        {
            Log("Already connecting.");
            return;
        }

        if (ws != null && (ws.ReadyState == WebSocketState.Open || ws.ReadyState == WebSocketState.Connecting))
        {
            Log("Already connected or connecting.");
            return;
        }

        StopReconnect();
        intentionalDisconnect = false;

        LoadSettings();

        connectCts?.Cancel();
        connectCts?.Dispose();
        connectCts = new CancellationTokenSource();

        CleanupWebSocket();

        Log("Connecting to " + serverUrl + " ...");

        var token = connectCts.Token;
        isConnecting = true;
        var myGeneration = Interlocked.Increment(ref connectionGeneration);

        Task.Run(() =>
        {
            if (token.IsCancellationRequested) return;

            WebSocket newWs = null;
            try
            {
                newWs = new WebSocket(serverUrl);

                newWs.OnOpen += (sender, e) =>
                {
                    if (myGeneration != Volatile.Read(ref connectionGeneration)) return;

                    if (usePinCode && !string.IsNullOrEmpty(pinCode))
                    {
                        Log("Sending pin code for authentication...");
                        try { newWs.Send(pinCode); } catch { }
                    }

                    mainThreadActions.Enqueue(() =>
                    {
                        if (myGeneration != connectionGeneration) return;
                        isConnecting = false;
                        StopReconnect();
                        onConnected?.Invoke();
                        UpdateStatusLabel();
                        Log("Connected.");
                    });
                };

                newWs.OnClose += (sender, e) =>
                {
                    if (myGeneration != Volatile.Read(ref connectionGeneration)) return;

                    mainThreadActions.Enqueue(() =>
                    {
                        if (myGeneration != connectionGeneration) return;
                        isConnecting = false;
                        onDisconnected?.Invoke();
                        UpdateStatusLabel();
                        Log("Disconnected. Code: " + e.Code + " Reason: " + e.Reason);
                        if (!intentionalDisconnect && autoReconnect)
                        {
                            StartReconnect();
                        }
                    });
                };

                newWs.OnError += (sender, e) =>
                {
                    if (myGeneration != Volatile.Read(ref connectionGeneration)) return;

                    mainThreadActions.Enqueue(() =>
                    {
                        if (myGeneration != connectionGeneration) return;
                        isConnecting = false;
                        onError?.Invoke();
                        Log("Error: " + e.Message);
                    });
                };

                newWs.OnMessage += (sender, e) =>
                {
                    if (myGeneration != Volatile.Read(ref connectionGeneration)) return;

                    mainThreadActions.Enqueue(() =>
                    {
                        if (myGeneration != connectionGeneration) return;
                        Log("Server: " + e.Data);
                    });
                };

                lock (wsLock)
                {
                    if (token.IsCancellationRequested || myGeneration != connectionGeneration)
                    {
                        try { newWs.Close(); } catch { }
                        return;
                    }
                    ws = newWs;
                }

                newWs.ConnectAsync();

                mainThreadActions.Enqueue(UpdateStatusLabel);
            }
            catch (Exception ex)
            {
                try { newWs?.Close(); } catch { }
                mainThreadActions.Enqueue(() =>
                {
                    if (myGeneration != connectionGeneration) return;
                    isConnecting = false;
                    onError?.Invoke();
                    UpdateStatusLabel();
                    Log("Error: " + ex.Message);
                });
            }
        }, token);
    }

    public void Disconnect()
    {
        intentionalDisconnect = true;
        StopReconnect();

        // capture current socket before invalidating generation so we can
        // reliably close it and still report a local "disconnected" state.
        WebSocket toClose = null;
        lock (wsLock)
        {
            toClose = ws;
            ws = null;
        }

        // mark any existing connect attempt / callbacks as stale
        Interlocked.Increment(ref connectionGeneration);
        isConnecting = false;

        connectCts?.Cancel();

        // Always notify locally that we are disconnected (OnClose may be ignored
        // because of generation mismatch during intentional disconnect).
        mainThreadActions.Enqueue(() =>
        {
            onDisconnected?.Invoke();
            UpdateStatusLabel();
            Log("Disconnected.");
        });

        if (toClose == null)
        {
            return;
        }

        Task.Run(() =>
        {
            try
            {
                if (toClose.ReadyState != WebSocketState.Closing && toClose.ReadyState != WebSocketState.Closed)
                {
                    Log("Closing connection...");
                    toClose.Close(CloseStatusCode.Normal, "Client disconnecting");
                }
            }
            catch { }
            finally
            {
                mainThreadActions.Enqueue(UpdateStatusLabel);
            }
        });
    }

    private void CleanupWebSocket()
    {
        WebSocket old;
        lock (wsLock)
        {
            old = ws;
            ws = null;
        }

        if (old == null) return;

        Task.Run(() =>
        {
            try { old.Close(); } catch { }
        });
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

        // Debug.Log should be called from Unity main thread.
        mainThreadActions.Enqueue(() => Debug.Log("[PCRemoteClient] " + msg));
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
        reconnectAttemptCount = 0;

        while (autoReconnect && !intentionalDisconnect)
        {
            reconnectAttemptCount++;

            if (maxReconnectAttempts > 0 && reconnectAttemptCount > maxReconnectAttempts)
            {
                Log("Max reconnect attempts (" + maxReconnectAttempts + ") reached. Giving up.");
                onReconnectFailed?.Invoke();
                break;
            }

            Log("Reconnecting in " + delay.ToString("F1") + "s ... (attempt " + reconnectAttemptCount + "/" + maxReconnectAttempts + ")");
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

            // Ensure previous instance is closed without blocking the main thread
            CleanupWebSocket();

            // Use the same Connect path (which runs connect off-thread)
            Connect();

            // Wait a moment to let ConnectAsync settle before checking
            yield return new WaitForSeconds(2f);

            if (ws != null && ws.ReadyState == WebSocketState.Open)
            {
                break;
            }

            delay = Mathf.Min(delay * reconnectBackoffMultiplier, reconnectDelayMax);
        }

        reconnectCoroutine = null;
    }

    private void LoadSettings()
    {
        string savedIP = PlayerPrefs.GetString("IPAddress", "");
        string savedPort = PlayerPrefs.GetString("Port", "");
        string savedPinCode = PlayerPrefs.GetString("PinCode", "");

        string ip = string.IsNullOrEmpty(savedIP) ? defaultIPAddress : savedIP;
        string port = string.IsNullOrEmpty(savedPort) ? defaultPort : savedPort;

        serverUrl = "ws://" + ip + ":" + port + websocketPath;

        if (!string.IsNullOrEmpty(savedPinCode))
        {
            usePinCode = true;
            pinCode = savedPinCode;
        }
        else
        {
            usePinCode = false;
            pinCode = "";
        }

        Log("Settings loaded. URL: " + serverUrl + " PinCode: " + (usePinCode ? "set" : "not set"));
    }

    internal void Reconnect()
    {
        Disconnect();
        Connect();
    }
}
