using UnityEngine;
using TMPro;

public class UIManager : Singleton<UIManager>
{   
    [SerializeField]
    private TMP_Text messageText;
    
    [SerializeField] 
    private GameObject mainMenu;

    [SerializeField]
    private GameObject boardMenu;   // The board UI + scripts

    [SerializeField]
    private GameObject quitButton;

    private void Start()
    {
        LoadMainMenu();
    }

    private void Update()
    {
        if (GameManager.Instance.GetState() == State.WAITING_FOR_ROLE)
        {
            DisplayMessage("Connection ok!\nWaiting for role...");
            LoadBoardMenu();
        }
        else if (GameManager.Instance.GetState() == State.WAITING_FOR_PLAYER_INPUT)
        {
            DisplayMessage("Your turn!");
        }
        else if (GameManager.Instance.GetState() == State.WAITING_FOR_SERVER_RESPONSE)
        {
            // Can also add READING_SERVER_RESPONSE


            DisplayMessage("Waiting for opponent...");
        }
    }

    public void LoadBoardMenu()
    {
        mainMenu.SetActive(false);
        boardMenu.SetActive(true);
    }

    public void LoadMainMenu()
    {
        mainMenu.SetActive(true);
        boardMenu.SetActive(false);
    }

    public void LoadQuitButton()
    {
        quitButton.SetActive(true);
    }

    public void DisplayMessage(string message)
    {
        messageText.text = message;
    }
}