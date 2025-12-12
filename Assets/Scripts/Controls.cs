using TMPro;
using UnityEngine;
using WebSocketSharp;

// TODO use webcam to detect light color and send to server?
// TODO use voice controls on pc directly..
// TODO use high pitch audio to send commands to server?

public class Controls : MonoBehaviour
{
    WebSocket websocket;


    public void OnClickSend(TMP_InputField source)
    {
        string msg = source.text;
        Debug.Log("Sending: " + msg);
        SendWebSocketMessage(msg);
    }


    // Start is called before the first frame update
    async void Start()
    {
        websocket = new WebSocket("ws://192.168.1.188:8080/remote");

    }

    void Update()
    {
#if UNITY_EDITOR
        //if (Input.anyKeyDown)
        //{
        //    Debug.Log("anykey pressed!");
        //}

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("ESC pressed!");
        }
#endif

    }

    async void SendWebSocketMessage(string msg)
    {
    }
}
