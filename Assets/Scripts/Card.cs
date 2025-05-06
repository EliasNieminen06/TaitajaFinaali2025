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
    // NOTE: Modifying this list at runtime directly can be complex with ScriptableObjects.
    // We will modify the runtime dictionary instead for gameplay effects.
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

    // New method to add to a stat's value at runtime
    // This modifies the runtime dictionary.
    public void AddToStat(string statName, float valueToAdd)
    {
        // Ensure the dictionary is initialized
        GetStatsDictionary();

        if (_statsDictionary.ContainsKey(statName))
        {
            _statsDictionary[statName] += valueToAdd;
            Debug.Log($"Added {valueToAdd} to stat '{statName}'. New value: {_statsDictionary[statName]} on card '{cardName}'.");
        }
        else
        {
            // If the stat doesn't exist, add it.
            _statsDictionary[statName] = valueToAdd;
            Debug.Log($"Stat '{statName}' did not exist on card '{cardName}', added with initial value: {valueToAdd}");

            // Optional: If you want this reflected in the editor's statsList after playing
            // (Be cautious, runtime modification of assets can be complex)
            // statsList.Add(new StatEntry { name = statName, value = valueToAdd });
        }
        // No need to re-initialize _isStatsDictionaryInitialized = false; here
        // as we are directly modifying the dictionary.
    }


    // Optional: Reset the dictionary if statsList is modified in editor
    void OnValidate()
    {
        _isStatsDictionaryInitialized = false; // Mark for re-initialization
    }
}