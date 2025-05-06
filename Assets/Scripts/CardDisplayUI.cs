// CardDisplayUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CardDisplayUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    // Use this single TextMeshProUGUI for both description and stats
    public TextMeshProUGUI descriptionAndStatsText;
    public Image artworkImage;

    // Optional: Reference to the CardSO this UI is displaying
    private CardSO cardData;

    // Optional: Reference to the GameManager for interactivity
    private GameManager gameManager;

    // Method to set the card data and update the UI
    public void SetCard(CardSO card)
    {
        cardData = card; // Store the CardSO reference

        if (cardData == null)
        {
            Debug.LogError("CardDisplayUI received a null CardSO.");
            // Clear the UI if the card data is null
            if (nameText != null) nameText.text = "";
            if (descriptionAndStatsText != null) descriptionAndStatsText.text = "";
            if (artworkImage != null) artworkImage.sprite = null;
            return;
        }

        // Update UI elements with data from the CardSO
        if (nameText != null) nameText.text = cardData.cardName;
        if (artworkImage != null && cardData.artwork != null) artworkImage.sprite = cardData.artwork;

        // Combine Description and Stats into a single string
        string combinedText = cardData.description;

        // Add a separator if there's a description and also stats
        if (!string.IsNullOrEmpty(cardData.description) && cardData.GetStatsDictionary().Count > 0)
        {
            combinedText += "\n\n"; // Add a couple of new lines between description and stats
        }

        // Append stats to the combined text
        foreach (var stat in cardData.GetStatsDictionary())
        {
            combinedText += $"{stat.Key}: {stat.Value}\n";
        }

        // Assign the combined text to the designated TextMeshProUGUI element
        if (descriptionAndStatsText != null)
        {
            descriptionAndStatsText.text = combinedText;
        }
        else
        {
            Debug.LogWarning("descriptionAndStatsText TextMeshProUGUI is not assigned in CardDisplayUI.");
        }

        // TODO: Implement displaying stats in a more visually appealing way if needed
        // (e.g., using different formatting for stat names/values)
    }

    // Optional: Method to set interactivity (e.g., enable a Button component)
    public void SetInteractive(bool isInteractive)
    {
        // Example if you have a Button component on your card prefab:
        Button cardButton = GetComponent<Button>();
        if (cardButton != null)
        {
            cardButton.interactable = isInteractive;
        }
        // TODO: Add logic for custom interaction handling (e.g., detecting clicks)
    }

    // Optional: Method to store a reference to the GameManager
    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
    }

    // Optional: Method called when the card is clicked (if interactive)
    // You would hook this up to a Button's OnClick event or a custom input handler.
    // public void OnCardClicked()
    // {
    //     if (gameManager != null && cardData != null)
    //     {
    //         // Example: Call a method in GameManager to play this card during the CookingPhase
    //         // gameManager.PlayCard(cardData);
    //     }
    // }
}