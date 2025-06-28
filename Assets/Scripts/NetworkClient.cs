using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Threading; // For CancellationTokenSource
using System.Collections.Generic; // For StringBuilder

public class NetworkClient : MonoBehaviour
{
    [Header("Network Settings")]
    [SerializeField] private string serverAddress = "127.0.0.1"; // Always localhost for now, as per your previous request

    private int currentServerPort;
    private TcpClient client;
    private NetworkStream stream;
    public bool isConnected { get; private set; }

    // Event to notify listeners (like Board.cs) when a message is received
    public Action<string> OnMessageReceived;

    private CancellationTokenSource cts; // For cancelling the receive loop

    public static NetworkClient Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Ensures NetworkClient persists across scene loads
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        // Retrieve the port number from PlayerPrefs, set by UIManager in the StartScene
        currentServerPort = PlayerPrefs.GetInt("LastUsedPort", 9999); 
        Debug.Log($"NetworkClient attempting to use port: {currentServerPort} at {serverAddress}");

        await ConnectToServer();
    }

    public async Task ConnectToServer()
    {
        if (isConnected)
        {
            Debug.Log("Already connected to server.");
            return;
        }

        try
        {
            client = new TcpClient();
            Debug.Log($"Attempting to connect to {serverAddress}:{currentServerPort}...");
            await client.ConnectAsync(serverAddress, currentServerPort);

            stream = client.GetStream();
            isConnected = true;
            Debug.Log("Successfully connected to server.");

            // Start listening for incoming messages in a separate task
            cts = new CancellationTokenSource();
            _ = ReceiveMessagesAsync(cts.Token); // "_" discards the Task, but allows it to run independently
        }
        catch (SocketException e)
        {
            Debug.LogError($"SocketException: {e.Message}. Make sure a server is running at {serverAddress}:{currentServerPort}.");
            isConnected = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error connecting to server: {e.Message}");
            isConnected = false;
        }
    }

    private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096]; // Buffer to read incoming data
        StringBuilder messageBuilder = new StringBuilder(); // To accumulate message fragments

        while (!cancellationToken.IsCancellationRequested && isConnected)
        {
            try
            {
                // Read from the network stream
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (bytesRead == 0) // Connection closed by the remote host
                {
                    Debug.Log("Server disconnected.");
                    CloseConnection();
                    break;
                }

                // Append received bytes to the message builder
                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                messageBuilder.Append(receivedData);

                // Process complete messages delimited by newline
                int newlineIndex;
                while ((newlineIndex = messageBuilder.ToString().IndexOf('\n')) != -1)
                {
                    string completeMessage = messageBuilder.ToString(0, newlineIndex).Trim();
                    messageBuilder.Remove(0, newlineIndex + 1); // Remove processed message + newline

                    if (!string.IsNullOrWhiteSpace(completeMessage))
                    {
                        Debug.Log($"Received: {completeMessage}");
                        // Dispatch the message to the main Unity thread for processing
                        UnityMainThreadDispatcher.Instance().Enqueue(() => OnMessageReceived?.Invoke(completeMessage));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Receive messages operation cancelled.");
                break;
            }
            catch (ObjectDisposedException) // Stream or client disposed, often due to CloseConnection
            {
                Debug.Log("Network stream or client disposed during receive.");
                break;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error receiving message: {e.Message}");
                CloseConnection();
                break;
            }
        }
    }

    public async Task SendTicTacToeMessage(TicTacToeMessage message)
    {
        if (!isConnected || client == null || !client.Connected || stream == null)
        {
            Debug.LogError("Not connected to server. Cannot send message.");
            return;
        }

        try
        {
            string jsonString = JsonUtility.ToJson(message);
            Debug.Log($"Sending: {jsonString}");

            // Convert JSON string to bytes and append a newline character as a delimiter for the server
            byte[] data = Encoding.UTF8.GetBytes(jsonString + "\n");
            await stream.WriteAsync(data, 0, data.Length);
            Debug.Log("Message sent successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending message: {e.Message}");
            isConnected = false;
            CloseConnection();
        }
    }

    private void OnApplicationQuit()
    {
        CloseConnection();
    }

    public void CloseConnection()
    {
        // Cancel the receive loop if it's running
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }

        if (stream != null)
        {
            stream.Close();
            stream.Dispose();
            stream = null;
        }
        if (client != null)
        {
            client.Close();
            client.Dispose();
            client = null;
        }
        isConnected = false;
        Debug.Log("Disconnected from server.");
    }
}