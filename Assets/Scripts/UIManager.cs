using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene"; // Your game scene name
    [SerializeField] private TMP_InputField portInputField; // Reference to the port input field

    // Define valid port range
    private const int MIN_PORT = 1024; // Common starting point for dynamic ports
    private const int MAX_PORT = 65535; // Maximum possible TCP/UDP port

    private void Start()
    {
        // Optional: Set a default port number in the input field when the scene loads
        if (portInputField != null)
        {
            if (string.IsNullOrEmpty(portInputField.text))
            {
                portInputField.text = "7777"; // Pre-fill with a common port
            }
        }
    }

    public void StartGame()
    {
        string portText = portInputField.text.Trim();
        int portNumber;

        // 1. Check if the input is empty
        if (string.IsNullOrEmpty(portText))
        {
            Debug.LogWarning("Port number cannot be empty!"); // Log to console
            return; // Stop the function here, do not load the game
        }

        // 2. Try to parse the input as an integer
        if (!int.TryParse(portText, out portNumber))
        {
            Debug.LogWarning("Invalid port number. Please enter digits only."); // Log to console
            return;
        }

        // 3. Validate the port number range
        if (portNumber < MIN_PORT || portNumber > MAX_PORT)
        {
            Debug.LogWarning($"Port must be between {MIN_PORT} and {MAX_PORT}."); // Log to console
            return;
        }

        // If all validations pass, proceed to load the game scene
        Debug.Log($"Starting game with port: {portNumber}");
        SceneManager.LoadScene(gameSceneName);

        // Optional: Save the port number for the next session
        PlayerPrefs.SetInt("LastUsedPort", portNumber);
        PlayerPrefs.Save();
    }

    public void ExitGame()
    {
        Debug.Log("Exit Game button clicked! Quitting application...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}