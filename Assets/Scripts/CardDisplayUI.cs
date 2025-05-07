// CardDisplayUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems; // Required for event systems like IPointerClickHandler

public class CardDisplayUI : MonoBehaviour // Consider inheriting from IPointerClickHandler if not using Button
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionAndStatsText;
    public Image artworkImage;
    public Button playButton; // Add a Button component to your card prefab and assign it here
    // TODO: Add visual element for highlighting (e.g., an Image for an outline)
    // public GameObject highlightIndicator;


    private CardSO cardData; // Store the CardSO reference
    private GameManager gameManager; // Will be set by GameManager

    void Awake()
    {
        if (playButton != null)
        {
            playButton.onClick.AddListener(OnCardClicked);
        }
        // If not using a Button, you would implement IPointerClickHandler and the OnPointerClick method
    }

    // Public getter for the CardSO data
    public CardSO GetCardData()
    {
        return cardData;
    }


    // Method to set the card data and update the UI
    public void SetCard(CardSO card)
    {
        cardData = card;

        if (cardData == null) { Debug.LogError("CardDisplayUI received a null CardSO."); if (nameText != null) nameText.text = ""; if (descriptionAndStatsText != null) descriptionAndStatsText.text = ""; if (artworkImage != null) artworkImage.sprite = null; if (playButton != null) playButton.interactable = false; return; }

        if (nameText != null) nameText.text = cardData.cardName;
        if (artworkImage != null && cardData.artwork != null) artworkImage.sprite = cardData.artwork;

        string combinedText = cardData.description;
        if (!string.IsNullOrEmpty(cardData.description) && cardData.GetStatsDictionary().Count > 0) { combinedText += "\n\nStats:\n"; }
        foreach (var stat in cardData.GetStatsDictionary()) { combinedText += $"{stat.Key}: {stat.Value}\n"; }
        if (descriptionAndStatsText != null) { descriptionAndStatsText.text = combinedText; } else { Debug.LogWarning("descriptionAndStatsText TextMeshProUGUI is not assigned."); }

        // TODO: Implement displaying stats in a more visually appealing way
    }

    // Method to set interactivity (enable/disable the play button)
    public void SetInteractive(bool isInteractive)
    {
        if (playButton != null)
        {
            playButton.interactable = isInteractive;
        }
        // TODO: If not using a Button, manage interaction detection here
    }

    // Method to set the visual highlight state
    public void SetHighlight(bool isHighlighted)
    {
        // TODO: Implement visual highlighting (e.g., activate/deactivate a highlight indicator GameObject)
        // if (highlightIndicator != null)
        // {
        //     highlightIndicator.SetActive(isHighlighted);
        // }
        Debug.Log($"Highlight for {cardData?.cardName} set to: {isHighlighted}"); // Log for testing
    }

    // Method to store a reference to the GameManager
    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
    }

    // Method called when the card UI (or its button) is clicked
    public void OnCardClicked()
    {
        if (gameManager != null && cardData != null)
        {
            Debug.Log($"Card UI Clicked: {cardData.cardName}, Type: {cardData.cardType}");
            // Call the PlayCard method in the GameManager, passing the card data
            gameManager.PlayCard(cardData);
        }
        else
        {
            Debug.LogWarning("CardDisplayUI clicked but GameManager or cardData is not set.");
        }
    }

    // If implementing IPointerClickHandler instead of Button:
    /*
    public void OnPointerClick(PointerEventData eventData)
    {
        OnCardClicked(); // Call your click handling logic
    }
    */
}