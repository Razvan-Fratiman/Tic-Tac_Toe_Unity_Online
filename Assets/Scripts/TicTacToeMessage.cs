using System;
using UnityEngine; // Required for Debug.LogWarning

// Root object for your JSON message
[Serializable]
public class TicTacToeMessage
{
    public string action;
    public string role; // "X", "O", or null (for actions like "initial_state", "draw", or "game_over" where a specific role might not be the 'sender' of the action)
    public GameMap board; // Matches "board" in your JSON example

    // Default constructor is good practice for JsonUtility
    public TicTacToeMessage() { }
}

// Nested object for the "board" field
[Serializable]
public class GameMap
{
    // These fields will map to "0", "1", ... "8" in your JSON.
    // JsonUtility handles this conversion for fields starting with '_' and then a number.
    public string _0;
    public string _1;
    public string _2;
    public string _3;
    public string _4;
    public string _5;
    public string _6;
    public string _7;
    public string _8;

    // Helper to set values by index
    public void SetValue(int index, string value)
    {
        switch (index)
        {
            case 0: _0 = value; break;
            case 1: _1 = value; break;
            case 2: _2 = value; break;
            case 3: _3 = value; break;
            case 4: _4 = value; break;
            case 5: _5 = value; break;
            case 6: _6 = value; break;
            case 7: _7 = value; break;
            case 8: _8 = value; break;
            default: Debug.LogWarning("Invalid board index: " + index); break;
        }
    }

    // Helper to get values by index
    public string GetValue(int index)
    {
        switch (index)
        {
            case 0: return _0;
            case 1: return _1;
            case 2: return _2;
            case 3: return _3;
            case 4: return _4;
            case 5: return _5;
            case 6: return _6;
            case 7: return _7;
            case 8: return _8;
            default: return "-"; // Return default for invalid index
        }
    }

    // Constructor to initialize all board values to '-'
    public GameMap()
    {
        _0 = "-";
        _1 = "-";
        _2 = "-";
        _3 = "-";
        _4 = "-";
        _5 = "-";
        _6 = "-";
        _7 = "-";
        _8 = "-";
    }
}