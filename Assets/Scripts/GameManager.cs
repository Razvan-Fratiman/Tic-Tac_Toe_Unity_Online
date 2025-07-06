using TMPro;
using UnityEngine;

/// <summary>
/// Handles initial state & role assignment from the server.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    [SerializeField]
    private TMP_InputField portInputField;

    private State state;
    void Start()
    {
        SetState(State.WAITING_FOR_PORT);
    }

    public async void StartGame()
    {
        string portText = portInputField.text.Trim();
        int portNumber;

        if (string.IsNullOrEmpty(portText))
        {
            Debug.LogWarning("Port number cannot be empty!");
            return;
        }

        if (!int.TryParse(portText, out portNumber))
        {
            Debug.LogWarning("Invalid port number. Please enter digits only.");
            return;
        }

        Debug.Log("connection before");
        TCPClientManager.Instance.serverPort = portNumber;
        await TCPClientManager.Instance.InitialConnection().ConfigureAwait(false);

        if (!TCPClientManager.Instance.IsConnected())
        {
            UIManager.Instance.DisplayMessage("Connection to server failed...");
            return;
        }

        SetState(State.WAITING_FOR_ROLE);
        BoardManager.Instance.GetInitialRole();
    }

    public void GameOver()
    {
        string role = BoardManager.Instance.GetRole();
        bool win = (state == State.WIN_X && role == "x") || (state == State.WIN_O && role == "o");
        bool lose = (state == State.WIN_X && role == "o") || (state == State.WIN_O && role == "x");
        
        if (win) UIManager.Instance.DisplayMessage("You win!");
        else if (lose) UIManager.Instance.DisplayMessage("You lose!");
        else UIManager.Instance.DisplayMessage("It's a draw!");

        BoardManager.Instance.DisableBoard();
        UIManager.Instance.LoadQuitButton();
    }
    public void QuitGame()
    {
        Debug.Log("Exit Game button clicked! Quitting application...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void SetState(State state)
    {
        Debug.Log("changed state " + this.state + " to state " + state);
        this.state = state;
    }

    public State GetState()
    {
        return state;
    }
}

public enum State
{
    NONE,
    WAITING_FOR_PORT,
    WAITING_FOR_ROLE,
    WAITING_FOR_PLAYER_INPUT,
    WAITING_FOR_SERVER_RESPONSE,
    READING_SERVER_REPSONE,
    WIN_X,
    WIN_O,
    DRAW
}