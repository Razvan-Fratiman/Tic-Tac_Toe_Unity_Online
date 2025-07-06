using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class BoardManager : Singleton<BoardManager>
{
    [Header("Mark Sprites : ")]
    [SerializeField] private Sprite spriteX;
    [SerializeField] private Sprite spriteO;

    [Header("Mark Colors : ")]
    [SerializeField] private Color colorX;
    [SerializeField] private Color colorO;

    [Header("Tick boxes : ")]
    [SerializeField] private GameObject[] tickBoxArray;

    private string role = "none";
    // Additional flag because ChatGPT said States are not enough?
    private bool isReadingFromServer = false;

    private async void Update()
    {
        if (GameManager.Instance.GetState() == State.WAITING_FOR_SERVER_RESPONSE && !isReadingFromServer)
        {
            isReadingFromServer = true;
            GameManager.Instance.SetState(State.READING_SERVER_REPSONE);
            Message msg = await TCPClientManager.Instance.ReadMessageAsync();
            Debug.Log("[BoardManager] Got from server >" + msg + "<");

            if (msg.Action == "new_board")
            {
                GameManager.Instance.SetState(State.WAITING_FOR_PLAYER_INPUT);
            }
            else if (msg.Action == "win_x")
            {
                GameManager.Instance.SetState(State.WIN_X);
                GameManager.Instance.GameOver();
            }
            else if (msg.Action == "win_o")
            {
                GameManager.Instance.SetState(State.WIN_O);
                GameManager.Instance.GameOver();

            }
            else if (msg.Action == "draw")
            {
                GameManager.Instance.SetState(State.DRAW);
                GameManager.Instance.GameOver();
            }
            else if (msg.Action == "invalid_move")
            {
                // invalidate the last checked box!
                // based on how the client is implemented, it should never reach this part.
                Debug.LogError("Invalid move from the client, fatal error.");
                Application.Quit();
            }

            // Draw new board
            for (int i = 0; i < tickBoxArray.Length; i++)
            {
                if (msg.Board[i.ToString()] == "x")
                {
                    tickBoxArray[i].GetComponent<Image>().sprite = spriteX;
                    tickBoxArray[i].GetComponent<Image>().color = colorX;
                    tickBoxArray[i].GetComponent<Button>().interactable = false;
                }
                else if (msg.Board[i.ToString()] == "o")
                {
                    tickBoxArray[i].GetComponent<Image>().sprite = spriteO;
                    tickBoxArray[i].GetComponent<Image>().color = colorO;
                    tickBoxArray[i].GetComponent<Button>().interactable = false;
                }

            }

            isReadingFromServer = false;
        }
    }

    /// <summary>
    /// Called only once at the start.
    /// Gets the role (either X or O) from the server and saves it locally.
    /// </summary>
    public async void GetInitialRole()
    {
        if (GameManager.Instance.GetState() == State.WAITING_FOR_ROLE)
        {
            Message msg = await TCPClientManager.Instance.ReadMessageAsync();
            Debug.Log("[BoardManager.GetInitialRole] Got from the server : " + msg);

            role = (msg.Role == "x" ? "x" : "o");
            GameManager.Instance.SetState(msg.Role == "x" ? State.WAITING_FOR_PLAYER_INPUT : State.WAITING_FOR_SERVER_RESPONSE);

            // Won't show up more that 1 frame as it will be overwritten quickly
            UIManager.Instance.DisplayMessage("You are " +  role);
        }
    }

    public void RegisterBoxTicked(int index)
    {
        if (role == "none") return;
        if (GameManager.Instance.GetState() != State.WAITING_FOR_PLAYER_INPUT) return;

        if (index < 0 || index > 8)
        {
            Debug.LogError("Index for registered box is incorrect '" + index + "'. The value must be between 0 and 8, both inclusive.");
            return;
        }

        var img = tickBoxArray[index].GetComponent<Image>();
        img.color = GetColor();
        img.sprite = GetSprite();
        tickBoxArray[index].GetComponent<Button>().interactable = false;

        // Send JSON data to server
        Message msg = new Message
        {
            SessionName = "ClientSession",
            Action = "new_move",
            Role = role,
            Board = new Dictionary<string, string>()
        };

        msg.Board[index.ToString()] = role;

        // Use discardable variable
        _ = TCPClientManager.Instance.SendMessageAsync(msg);
    }

    public void DisableBoard()
    {
        for (int i = 0; i < tickBoxArray.Length; i++ ) {
            tickBoxArray[i].GetComponent<Button>().interactable = false;
        }
    }

    private Color GetColor()
    {
        return (role == "x") ? colorX : colorO;
    }

    private Sprite GetSprite()
    {
        return (role == "x") ? spriteX : spriteO;
    }

    public string GetRole()
    {
        return role;
    }
}