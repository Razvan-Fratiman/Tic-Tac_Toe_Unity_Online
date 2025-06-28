using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class Board : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private LayerMask boxesLayerMask;
    [SerializeField] private float touchRadius;

    [Header("Mark Sprites")]
    [SerializeField] private Sprite spriteX;
    [SerializeField] private Sprite spriteO;

    [Header("Mark Colors")]
    [SerializeField] private Color colorX;
    [SerializeField] private Color colorO;

    [Header("Game State")]
    [SerializeField] private bool enableInput = true; // Add this to control input

    public Mark[] marks;
    private Camera cam;
    private Mark currentMark;
    private Box[] allBoxes;

    // Game state management
    private bool gameEnded = false;
    private bool waitingForServerResponse = false;

    private void Awake()
    {
        allBoxes = GetComponentsInChildren<Box>();
        if (allBoxes.Length == 0)
        {
            Debug.LogError("No Box components found as children of this Board GameObject.");
        }
    }

    private void Start()
    {
        cam = Camera.main;
        currentMark = Mark.X;
        marks = new Mark[9];

        for (int i = 0; i < marks.Length; i++)
        {
            marks[i] = Mark.None;
        }

        SetupNetworking();
    }

    private void SetupNetworking()
    {
        if (NetworkClient.Instance != null)
        {
            NetworkClient.Instance.OnMessageReceived += HandleServerMessage;
            Debug.Log("Board subscribed to NetworkClient.OnMessageReceived.");
            SendBoardStateToServer("initial_state", null);
        }
        else
        {
            Debug.LogError("NetworkClient instance not found.");
        }
    }

    private void OnDestroy()
    {
        if (NetworkClient.Instance != null)
        {
            NetworkClient.Instance.OnMessageReceived -= HandleServerMessage;
        }
    }

    private void Update()
    {
        // Only process input if game hasn't ended and we're not waiting for server
        if (!enableInput || gameEnded || waitingForServerResponse)
            return;

        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Vector2 touchPosition = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapCircle(touchPosition, touchRadius, boxesLayerMask);

            if (hit)
            {
                Box hitBox = hit.GetComponent<Box>();
                if (hitBox != null)
                {
                    HitBox(hitBox);
                }
            }
        }
    }

    private void HitBox(Box box)
    {
        if (!box.isMarked && !gameEnded)
        {
            // Temporarily update local state for sending to server
            Mark previousMark = marks[box.index];
            marks[box.index] = currentMark;

            // Disable input until server responds
            waitingForServerResponse = true;

            SendBoardStateToServer("new_move", currentMark.ToString());

            // Store previous state in case we need to revert on invalid move
            // (You might want to implement a proper undo mechanism)
        }
    }

    private async void SendBoardStateToServer(string actionType, string rolePlayed)
    {
        if (NetworkClient.Instance == null)
        {
            Debug.LogError("NetworkClient.Instance is not found.");
            waitingForServerResponse = false;
            return;
        }

        if (!NetworkClient.Instance.isConnected)
        {
            Debug.LogWarning("NetworkClient is not connected. Attempting to reconnect...");
            await NetworkClient.Instance.ConnectToServer();
            if (!NetworkClient.Instance.isConnected)
            {
                Debug.LogError("Failed to connect to server.");
                waitingForServerResponse = false;
                return;
            }
        }

        TicTacToeMessage message = new TicTacToeMessage
        {
            action = actionType,
            role = rolePlayed,
            board = new GameMap()
        };

        // Populate board state
        for (int i = 0; i < marks.Length; i++)
        {
            string markChar = marks[i] switch
            {
                Mark.X => "X",
                Mark.O => "O",
                _ => "-"
            };
            message.board.SetValue(i, markChar);
        }

        await NetworkClient.Instance.SendTicTacToeMessage(message);
    }

    private void HandleServerMessage(string jsonMessage)
    {
        try
        {
            TicTacToeMessage receivedMessage = JsonUtility.FromJson<TicTacToeMessage>(jsonMessage);
            Debug.Log($"Received message - Action: {receivedMessage.action}, Role: {receivedMessage.role}");

            // Re-enable input after server response (unless game ended)
            waitingForServerResponse = false;

            switch (receivedMessage.action)
            {
                case "new_board":
                    HandleNewBoard(receivedMessage);
                    break;

                case var action when action.StartsWith("win_"):
                    HandleGameWin(receivedMessage);
                    break;

                case "draw":
                    HandleGameDraw(receivedMessage);
                    break;

                case "invalid_move":
                    HandleInvalidMove(receivedMessage);
                    break;

                case "disconnect":
                    HandleOpponentDisconnect();
                    break;

                default:
                    Debug.LogWarning($"Unhandled server action: {receivedMessage.action}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to parse server message: {e.Message}\nJSON: {jsonMessage}");
            waitingForServerResponse = false;
        }
    }

    private void HandleNewBoard(TicTacToeMessage message)
    {
        if (message.board != null)
        {
            UpdateBoardFromServer(message.board);
            SwitchPlayer(); // Update turn after successful move
        }
    }

    private void HandleGameWin(TicTacToeMessage message)
    {
        gameEnded = true;
        enableInput = false;

        if (message.board != null)
        {
            UpdateBoardFromServer(message.board);
        }

        string winner = message.role ?? "Unknown";
        Debug.Log($"Game Over! {winner} Wins!");

        // TODO: Show win UI
        ShowGameEndUI($"{winner} Wins!");
    }

    private void HandleGameDraw(TicTacToeMessage message)
    {
        gameEnded = true;
        enableInput = false;

        if (message.board != null)
        {
            UpdateBoardFromServer(message.board);
        }

        Debug.Log("Game Over! It's a Draw!");
        ShowGameEndUI("It's a Draw!");
    }

    private void HandleInvalidMove(TicTacToeMessage message)
    {
        Debug.LogWarning("Server rejected the move");

        // Revert local state if server provides correct board state
        if (message.board != null)
        {
            UpdateBoardFromServer(message.board);
        }

        // TODO: Show invalid move feedback to player
    }

    private void HandleOpponentDisconnect()
    {
        gameEnded = true;
        enableInput = false;
        Debug.Log("Opponent disconnected. Game Over.");
        ShowGameEndUI("Opponent Disconnected");
    }

    private void UpdateBoardFromServer(GameMap serverBoardState)
    {
        if (allBoxes == null || allBoxes.Length == 0)
        {
            Debug.LogError("Cannot update UI: allBoxes array is not initialized.");
            return;
        }

        // Update local logical state
        for (int i = 0; i < marks.Length; i++)
        {
            string markChar = serverBoardState.GetValue(i);
            marks[i] = markChar switch
            {
                "X" => Mark.X,
                "O" => Mark.O,
                _ => Mark.None
            };
        }

        // Update visual representation
        foreach (Box box in allBoxes)
        {
            string markChar = serverBoardState.GetValue(box.index);

            if (markChar == "X" && !box.isMarked)
            {
                box.SetAsMarked(spriteX, Mark.X, colorX);
            }
            else if (markChar == "O" && !box.isMarked)
            {
                box.SetAsMarked(spriteO, Mark.O, colorO);
            }
            else if (markChar == "-" && box.isMarked)
            {
                box.ResetBox();
            }
        }

        Debug.Log("Board updated from server state.");
    }

    private void ShowGameEndUI(string message)
    {
        // TODO: Implement game end UI
        // This could show a popup with the result and options to restart or return to menu
        Debug.Log($"Game End UI should show: {message}");
    }

    public void RestartGame()
    {
        gameEnded = false;
        enableInput = true;
        waitingForServerResponse = false;
        currentMark = Mark.X;

        // Reset board state
        for (int i = 0; i < marks.Length; i++)
        {
            marks[i] = Mark.None;
        }

        // Reset visual state
        foreach (Box box in allBoxes)
        {
            box.ResetBox();
        }

        // Notify server of restart (if implemented)
        SendBoardStateToServer("restart_game", null);
    }

    // Helper methods
    private bool AreBoxesMatched(int i, int j, int k)
    {
        Mark m = currentMark;
        return (marks[i] == m && marks[j] == m && marks[k] == m);
    }

    private bool CheckIfWin()
    {
        return
            AreBoxesMatched(0, 1, 2) || AreBoxesMatched(3, 4, 5) || AreBoxesMatched(6, 7, 8) ||
            AreBoxesMatched(0, 3, 6) || AreBoxesMatched(1, 4, 7) || AreBoxesMatched(2, 5, 8) ||
            AreBoxesMatched(0, 4, 8) || AreBoxesMatched(2, 4, 6);
    }

    private bool CheckIfDraw()
    {
        for (int i = 0; i < marks.Length; i++)
        {
            if (marks[i] == Mark.None)
                return false;
        }
        return true;
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