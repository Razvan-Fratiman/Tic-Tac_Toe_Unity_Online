using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using UnityEngine;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Handles the connection to the server.
/// Responsible only for sending and reading messages.
/// Does not implement game logic.
/// </summary>
public class TCPClientManager : Singleton<TCPClientManager>
{
    public int serverPort = -1;
    //private string serverIP = "172.19.42.135"; // WSL IP
    private string serverIP = "localhost";

    private TcpClient client;
    private NetworkStream stream;
    private StreamReader reader;
    private StreamWriter writer;

    private bool isConnected = false;

    public async Task InitialConnection()
    {
        try
        {
            client = new TcpClient();
            var connectTask = client.ConnectAsync(serverIP, serverPort);
            var timeoutTask = Task.Delay(5000); // 5-second timeout

            if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
            {
                Debug.LogError("Connection timed out.");
                return;
            }
            stream = client.GetStream();

            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { AutoFlush = true };

            isConnected = true;

            Debug.Log("Connected to server. Waiting for initial message...");
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection error: {e.Message}");
        }
    }

    public async Task<Message> ReadMessageAsync()
    {
        string jsonMessage = await reader.ReadLineAsync();

        Message msg = JsonConvert.DeserializeObject<Message>(jsonMessage);

        return msg;
    }

    public async Task SendMessageAsync(Message msg)
    {
        if (writer == null) return;

        string json = JsonConvert.SerializeObject(msg) + "\n";
        Debug.Log("Sending the following data to the server: '" + json + "'");
        await writer.WriteLineAsync(json);  // Sends JSON + newline
        GameManager.Instance.SetState(State.WAITING_FOR_SERVER_RESPONSE);
    }

    public bool IsConnected()
    {
        return isConnected;
    }

    private void OnApplicationQuit()
    {
        writer?.Close();
        reader?.Close();
        stream?.Close();
        client?.Close();
    }
}

[Serializable]
public class Message
{
    public string SessionName { get; set; }
    public string Action { get; set; } = "new_move";
    public string Role { get; set; } = BoardManager.Instance.GetRole();
    public Dictionary<string, string> Board { get; set; }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}
