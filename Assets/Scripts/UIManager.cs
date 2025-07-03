using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Net.Sockets;
using System;

public class UIManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private TMP_InputField portInputField;
    [SerializeField] private GameObject loadingPanel; // Optional: Show loading UI

    private const int MIN_PORT = 1024;
    private const int MAX_PORT = 65535;

    private void Start()
    {
        if (portInputField != null)
        {
            if (string.IsNullOrEmpty(portInputField.text))
            {
                portInputField.text = "9999";
            }
        }
    }

    public void StartGame()
    {
        string portText = portInputField.text.Trim();
        int portNumber;

        // Validation (same as before)
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

        if (portNumber < MIN_PORT || portNumber > MAX_PORT)
        {
            Debug.LogWarning($"Port must be between {MIN_PORT} and {MAX_PORT}.");
            return;
        }
        PlayerPrefs.SetInt("LastUsedPort", portNumber);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
        // Test connection before loading the scene
        //TestConnectionAndLoadScene(portNumber);
    }

    //private void TestConnectionAndLoadScene(int portNumber)
    //{
    //    // Optional: Show loading UI
    //    if (loadingPanel != null)
    //        loadingPanel.SetActive(true);

    //    try
    //    {
    //        // Test the connection
    //        TcpClient testClient = new TcpClient();
    //        testClient.Connect("127.0.0.1", portNumber);

    //        Debug.Log($"Connection successful! Loading game scene...");

    //        // Save the port and load the scene
    //        PlayerPrefs.SetInt("LastUsedPort", portNumber);
    //        PlayerPrefs.Save();

    //        // Clean up test connection
    //        testClient.Close();

    //        // Load the game scene
    //        SceneManager.LoadScene(gameSceneName);
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.LogError($"Cannot connect to server on port {portNumber}: {e.Message}");

    //        // Hide loading UI if connection failed
    //        if (loadingPanel != null)
    //            loadingPanel.SetActive(false);

    //        // Optionally show error message to user
    //        ShowErrorMessage($"Cannot connect to server on port {portNumber}. Make sure the server is running.");
    //    }
    //}

    private void ShowErrorMessage(string message)
    {
        // You can implement this to show error UI to the user
        Debug.LogWarning(message);
        // Example: errorText.text = message; errorPanel.SetActive(true);
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