// RecipeSO.cs
using UnityEngine;
using System.Collections.Generic;

// This attribute allows you to create RecipeSO assets from the Unity Editor menu
[CreateAssetMenu(fileName = "New Recipe", menuName = "Recipe System/Recipe")]
public class RecipeSO : ScriptableObject // Inherit from ScriptableObject
{
    [Header("Recipe Info")]
    public string recipeName = "New Dish"; // The name of the dish
    [TextArea]
    public string description = "A delicious dish."; // Description of the dish
    public Sprite dishArtwork; // Optional: Artwork for the finished dish

    [Header("Requirements")]
    // Define the cards (by reference to CardSO) required or recommended for this recipe.
    // You can adjust this structure based on how strict your recipes are.
    // Example: A list of specific ingredients, tools, techniques needed.
    public List<CardSO> requiredIngredients = new List<CardSO>();
    public List<CardSO> requiredTools = new List<CardSO>();
    public List<CardSO> requiredTechniques = new List<CardSO>();

    // Define the desired combined stats for the finished dish.
    // Using the StatEntry structure from CardSO is convenient for consistency.
    [System.Serializable]
    public class RecipeStatEntry // Can reuse CardSO.StatEntry if it's public or define a new one
    {
        public string name; // e.g., "Saltiness", "Spiciness"
        public float targetValue; // The desired value for this stat
    }
    public List<RecipeStatEntry> targetStats = new List<RecipeStatEntry>();

    // Optional: A simple description of the flavor profile or desired outcome
    [TextArea]
    public string targetOutcomeDescription = "Should be perfectly balanced.";

    // TODO: Add any other recipe properties, e.g., difficulty, base score, specific cooking order requirements.


    // Optional: Method to get the target stats as a dictionary for easier lookup during scoring
    private Dictionary<string, float> _targetStatsDictionary;
    private bool _isTargetStatsDictionaryInitialized = false;

    public Dictionary<string, float> GetTargetStatsDictionary()
    {
        if (!_isTargetStatsDictionaryInitialized)
        {
            _targetStatsDictionary = new Dictionary<string, float>();
            foreach (var entry in targetStats)
            {
                if (!string.IsNullOrEmpty(entry.name))
                {
                    _targetStatsDictionary[entry.name] = entry.targetValue;
                }
            }
            _isTargetStatsDictionaryInitialized = true;
        }
        return _targetStatsDictionary;
    }

    // Optional: Reset the dictionary if targetStats list is modified in editor
    void OnValidate()
    {
        _isTargetStatsDictionaryInitialized = false; // Mark for re-initialization
    }
}