// CardSO.cs
using UnityEngine;
using System.Collections.Generic;

// This attribute allows you to create CardSO assets from the Unity Editor menu
[CreateAssetMenu(fileName = "New Card", menuName = "Card System/Card")]
public class CardSO : ScriptableObject // Inherit from ScriptableObject
{
    public enum CardType
    {
        Ingredient,
        Spice,
        Tool,
        Technique,
        // Add more types if needed
    }

    [Header("Basic Card Info")]
    public string cardName = "New Card"; // Default name
    public CardType cardType = CardType.Ingredient; // Default type
    [TextArea] // Makes the string field a multi-line text area in the Inspector
    public string description = "Card description here.";
    public Sprite artwork; // Assign artwork texture here

    // Use a serializable class to make the Dictionary editable in the Inspector
    [System.Serializable]
    public class StatEntry
    {
        public string name;
        public float value;
    }
    // List of StatEntry objects to show stats in the Inspector
    public List<StatEntry> statsList = new List<StatEntry>();

    // Runtime dictionary for easy access (populated from statsList)
    private Dictionary<string, float> _statsDictionary;
    private bool _isStatsDictionaryInitialized = false;

    // Method to get the stats dictionary (populates it if needed)
    public Dictionary<string, float> GetStatsDictionary()
    {
        if (!_isStatsDictionaryInitialized)
        {
            _statsDictionary = new Dictionary<string, float>();
            foreach (var entry in statsList)
            {
                if (!string.IsNullOrEmpty(entry.name)) // Avoid adding entries with no name
                {
                    _statsDictionary[entry.name] = entry.value;
                }
            }
            _isStatsDictionaryInitialized = true;
        }
        return _statsDictionary;
    }

    // Method to get a stat value (convenience)
    public float GetStat(string statName, float defaultValue = 0f)
    {
        var stats = GetStatsDictionary();
        if (stats.TryGetValue(statName, out float value))
        {
            return value;
        }
        return defaultValue;
    }

    // Optional: Reset the dictionary if statsList is modified in editor
    // Useful if you modify statsList while playing in editor
    void OnValidate()
    {
        _isStatsDictionaryInitialized = false; // Mark for re-initialization
    }
}