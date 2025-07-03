using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using UnityEngine.UI; // Add this for UI components
using TMPro; // Add this for TextMeshPro
using System;
using System.Net.Sockets;  // For TcpClient and NetworkStream
using System.Text;        // For Encoding.UTF8

public class Board : MonoBehaviour
{
    [Header("Input Settings : ")]
    [SerializeField] private LayerMask boxesLayerMask;
    [SerializeField] private float touchRadius;

    [Header("Mark Sprites : ")]
    [SerializeField] private Sprite spriteX;
    [SerializeField] private Sprite spriteO;

    [Header("Mark Colors : ")]
    [SerializeField] private Color colorX;
    [SerializeField] private Color colorO;

    [Header("UI References : ")]
    [SerializeField] private Button restartButton; // Reference to the restart button
    [SerializeField] private TextMeshProUGUI winnerText; // Reference to the winner display text

    [System.Serializable]
    public class GameMove
    {
        public string action = "new-move";
        public int position;
        public string mark;
    }

    public Mark[] marks;
    private Camera cam;
    private Mark currentMark;
    private bool gameEnded = false; // Track if game has ended

    private TcpClient gameClient;
    private NetworkStream clientStream;
    private int serverPort;


    private void Start()
    {

        cam = Camera.main;
        currentMark = Mark.X;
        marks = new Mark[9];
        
        // Set up the restart button if assigned
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
            serverPort = PlayerPrefs.GetInt("LastUsedPort", 9999);
            ConnectToServer();
        }

        // Initialize winner text
        if (winnerText != null)
        {
            winnerText.text = ""; // Start with empty text
        }
    }

    private void ConnectToServer()
    {
        try
        {
            gameClient = new TcpClient("127.0.0.1", serverPort);
            clientStream = gameClient.GetStream();
            Debug.Log($"Connected to server on port {serverPort}");
        }
        catch (System.Exception e)
        {
            Debug.Log($"Game connection failed: {e.Message}");
            // Handle offline mode or retry logic here
        }
    }

    private void Update()
    {
        // Only allow input if game hasn't ended
        if (!gameEnded && Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Vector2 touchPosition = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapCircle(touchPosition, touchRadius, boxesLayerMask);

            if (hit)
            {
                Box hitBox = hit.GetComponent<Box>();
                if (hitBox != null)
                {
                    HitBox(hitBox);
                    // Send JSON to server when hitbox is triggered
                    SendJsonMove(hitBox);
                    //Debug.Log(marks[hitBox.index]);
                }
            }
        }
    }

    private bool IsConnected()
    {
        return gameClient != null &&
               gameClient.Connected &&
               clientStream != null;
    }

    void SendJsonMove(Box box)
    {
        if (!IsConnected())
        {
            Debug.LogWarning("Not connected to server");
            return;
        }

        // Create structured move data
        GameMove move = new GameMove
        {
            position = box.index,
            mark = marks[box.index].ToString()
        };

        // Convert to JSON
        string json = JsonUtility.ToJson(move);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(json + "\n"); // Add newline terminator
        Debug.Log(jsonBytes);
        try
        {
            clientStream.Write(jsonBytes, 0, jsonBytes.Length);
            Debug.Log($"Sent JSON: {json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send move: {e.Message}");
            // Handle disconnect
        }
    }

    //private void SendMoveToServer(Box boxIndex)
    //{
    //    if (gameClient != null && gameClient.Connected && clientStream != null)
    //    {
    //        string json = JsonUtility.ToJson(move);
    //        string json = $"{{\"action\":\"new-move\",\"board\":{{\"{boxIndex.index}\":\"{marks[boxIndex.index]}\"}}}}";
    //        networkClient.SendCustomMessage(json);
    //        Debug.Log($"Sent JSON: {json}");
    //    }
    //}


    private void HitBox(Box box)
    {
        if (!box.isMarked)
        {
            marks[box.index] = currentMark;
            box.SetAsMarked(GetSprite(), currentMark, GetColor());

            bool won = CheckIfWin();
            if (won)
            {
                Debug.Log(currentMark.ToString() + " Wins!");
                DisplayWinner(currentMark.ToString() + " Wins!");
                gameEnded = true;
                return;
            }
            if (CheckIfDraw())
            {
                Debug.Log("It's a Draw!");
                DisplayWinner("It's a Draw!");
                gameEnded = true;
                return;
            }
            SwitchPlayer();
        }
    }

    public void RestartGame()
    {
        // Reset game state
        gameEnded = false;
        currentMark = Mark.X;

        // Clear the marks array
        for (int i = 0; i < marks.Length; i++)
        {
            marks[i] = Mark.None;
        }

        // Reset all boxes
        Box[] boxes = FindObjectsByType<Box>(FindObjectsSortMode.None);
        foreach (Box box in boxes)
        {
            box.ResetBox();
        }

        Debug.Log("Game Restarted!");

        // Clear winner text
        if (winnerText != null)
        {
            winnerText.text = "";
        }
    }

    private void DisplayWinner(string message)
    {
        if (winnerText != null)
        {
            winnerText.text = message;
        }
    }

    private bool CheckIfDraw()
    {
        for (int i = 0; i < marks.Length; i++)
        {
            if (marks[i] == Mark.None)
            {
                return false;
            }
        }
        return true;
    }

    private bool AreBoxesMatched(int i, int j, int k)
    {
        Mark m = currentMark;
        bool matched = (marks[i] == m && marks[j] == m && marks[k] == m);
        return matched;
    }

    private bool CheckIfWin()
    {
        return
        AreBoxesMatched(0, 1, 2) || AreBoxesMatched(3, 4, 5) || AreBoxesMatched(6, 7, 8) ||
        AreBoxesMatched(0, 3, 6) || AreBoxesMatched(1, 4, 7) || AreBoxesMatched(2, 5, 8) ||
        AreBoxesMatched(0, 4, 8) || AreBoxesMatched(2, 4, 6);
    }

    private void SwitchPlayer()
    {
        currentMark = (currentMark == Mark.X) ? Mark.O : Mark.X;
    }

    private Color GetColor()
    {
        return (currentMark == Mark.X) ? colorX : colorO;
    }

    private Sprite GetSprite()
    {
        return (currentMark == Mark.X) ? spriteX : spriteO;
    }
}