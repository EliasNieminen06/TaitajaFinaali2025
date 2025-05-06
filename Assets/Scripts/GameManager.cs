// GameManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq; // Required for LINQ operations like .Where() and .ToList()

public class GameManager : MonoBehaviour
{
    [Header("Card Definitions")]
    // Drag ALL your created CardSO assets into this list in the Inspector
    public List<CardSO> allCardDefinitions;

    [Header("Decks")]
    public Deck deck1; // Assign Deck GameObject here
    public Deck deck2; // Assign Deck GameObject here

    // Optional: Lists to assign starting cards for each deck in Inspector
    [Header("Starting Deck Contents (Assign CardSO assets)")]
    public List<CardSO> initialDeck1Cards;
    public List<CardSO> initialDeck2Cards;


    [Header("UI References")]
    public GameObject handPanel; // UI Panel that holds card representations
    public GameObject cardUIPrefab; // Prefab for displaying a card in hand/popup (Needs CardUI.cs)
    public GameObject drawPopupPanel; // The panel that pops up
    public Image popupCardArtwork; // Image for the card in the popup
    public TextMeshProUGUI popupCardName; // Text for name in popup
    public TextMeshProUGUI popupCardDescription; // Text for description in popup (Update to show stats?)
    public Button keepButton;
    public Button discardButton;
    public Button combineButton;
    public TextMeshProUGUI popupMessageText; // Optional: for messages like "Hand Full"

    [Header("Game Settings")]
    public int handLimit = 6;

    // Internal state
    private List<CardSO> hand = new List<CardSO>(); // Hand now holds CardSO references
    private CardSO currentlyDrawnCard; // Holds the currently drawn CardSO

    void Awake()
    {
        // Initialize Decks using the lists populated in the Inspector
        deck1.Initialize("Deck 1", new List<CardSO>(initialDeck1Cards)); // Create a copy of the list
        deck2.Initialize("Deck 2", new List<CardSO>(initialDeck2Cards)); // Create a copy of the list

        // Hide the popup initially
        drawPopupPanel.SetActive(false);

        // --- Link UI Buttons ---
        // You still need to link the Deck Draw Buttons in the Inspector
        // Example Inspector setup:
        // Deck1 Draw Button -> OnClick() -> GameManager GameObject -> GameManager script -> DrawCardFromDeck (select Deck1)
        // Deck2 Draw Button -> OnClick() -> GameManager GameObject -> GameManager script -> DrawCardFromDeck (select Deck2)

        keepButton.onClick.AddListener(KeepDrawnCard);
        discardButton.onClick.AddListener(DiscardDrawnCard);
        combineButton.onClick.AddListener(CombineDrawnCard);

        UpdateHandUI();
    }

    // Helper to get cards by type (Optional now that you define decks directly)
    // You could still use this if you wanted decks to be dynamically built
    // List<CardSO> GetCardsByType(params CardSO.CardType[] types)
    // {
    //     List<CardSO> deckList = new List<CardSO>();
    //     foreach (var cardSO in allCardDefinitions)
    //     {
    //         foreach (var type in types)
    //         {
    //             if (cardSO.cardType == type)
    //             {
    //                 deckList.Add(cardSO); // Add reference to the SO
    //                 break;
    //             }
    //         }
    //     }
    //      // Add duplicates if needed
    //     // ... logic ...
    //     return deckList;
    // }


    // Call this from your UI Buttons for drawing
    public void DrawCardFromDeck(Deck deckToDrawFrom)
    {
        if (hand.Count >= handLimit)
        {
            ShowPopupMessage("Hand is full!", true); // Show message briefly
            return;
        }

        currentlyDrawnCard = deckToDrawFrom.DrawCard();

        if (currentlyDrawnCard != null)
        {
            ShowDrawPopup(currentlyDrawnCard);
        }
        else
        {
            ShowPopupMessage($"{deckToDrawFrom.name} is empty!", true);
        }
    }

    void ShowDrawPopup(CardSO card)
    {
        popupCardArtwork.sprite = card.artwork;
        popupCardName.text = card.cardName;
        // Display Description and Stats
        popupCardDescription.text = card.description + "\n\n";
        foreach (var stat in card.GetStatsDictionary())
        {
            popupCardDescription.text += $"{stat.Key}: {stat.Value}\n";
        }


        // Check for combination possibility
        bool canCombine = false;
        if (card.cardType == CardSO.CardType.Spice)
        {
            // Check if the hand contains any card with the *same name* as the drawn Spice
            foreach (var handCard in hand)
            {
                if (handCard.cardName == card.cardName)
                {
                    canCombine = true;
                    break;
                }
            }
        }
        combineButton.gameObject.SetActive(canCombine); // Show/hide combine button

        popupMessageText.text = "Choose an action:"; // Reset message
        drawPopupPanel.SetActive(true);
    }

    void KeepDrawnCard()
    {
        if (currentlyDrawnCard != null && hand.Count < handLimit)
        {
            hand.Add(currentlyDrawnCard); // Add the CardSO reference to hand
            UpdateHandUI();
            drawPopupPanel.SetActive(false);
            currentlyDrawnCard = null; // Clear the drawn card reference
        }
        else if (hand.Count >= handLimit)
        {
            ShowPopupMessage("Hand is already full!", false);
        }
        else // currentlyDrawnCard is null
        {
            // Should not happen if popup is active, but good failsafe
            Debug.LogError("KeepDrawnCard called but no card was drawn.");
            drawPopupPanel.SetActive(false); // Hide popup anyway
        }
    }

    void DiscardDrawnCard()
    {
        if (currentlyDrawnCard != null)
        {
            Debug.Log($"Discarded: {currentlyDrawnCard.cardName}");
            // CardSO is simply lost in this basic setup.
            // You could add it to a discard pile deck if needed.
            drawPopupPanel.SetActive(false);
            currentlyDrawnCard = null; // Clear the drawn card reference
        }
        else
        {
            Debug.LogError("DiscardDrawnCard called but no card was drawn.");
            drawPopupPanel.SetActive(false); // Hide popup anyway
        }
    }

    void CombineDrawnCard()
    {
        if (currentlyDrawnCard != null && currentlyDrawnCard.cardType == CardSO.CardType.Spice)
        {
            CardSO cardInHandToCombine = null;
            // Find the first card in hand with the same name as the drawn Spice
            foreach (var handCard in hand)
            {
                if (handCard.cardName == currentlyDrawnCard.cardName)
                {
                    cardInHandToCombine = handCard;
                    break;
                }
            }

            if (cardInHandToCombine != null)
            {
                Debug.Log($"Combined {currentlyDrawnCard.cardName} with {cardInHandToCombine.cardName} from hand.");

                // --- Combination Effect (Example: Remove both) ---
                // Remove the CardSO reference from hand
                hand.Remove(cardInHandToCombine);
                // currentlyDrawnCard (a CardSO reference) doesn't get added to hand, it's just consumed

                UpdateHandUI();
                drawPopupPanel.SetActive(false);
                currentlyDrawnCard = null; // Clear the drawn card reference

                // --- TODO: Add actual combination logic here ---
                // e.g., Create a new combined card and add it to hand/deck
                // e.g., Apply a temporary or permanent buff/effect
            }
            else
            {
                // This shouldn't happen if the button was active, but failsafe
                ShowPopupMessage("No matching card in hand to combine!", false);
            }
        }
        else
        {
            ShowPopupMessage("Combination not possible.", false);
        }
    }


    void UpdateHandUI()
    {
        // Clear existing UI cards
        foreach (Transform child in handPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Instantiate new UI cards based on the hand list
        foreach (var cardSO in hand) // Iterate through CardSO references
        {
            GameObject cardUIObject = Instantiate(cardUIPrefab, handPanel.transform);
            // You need a script on your cardUIPrefab (e.g., CardDisplayUI.cs)
            // that takes a CardSO and sets its UI elements.

            // Example (assuming cardUIPrefab has a TextMeshProUGUI child)
            TextMeshProUGUI cardText = cardUIObject.GetComponentInChildren<TextMeshProUGUI>();
            if (cardText != null) cardText.text = cardSO.cardName;

            // Example (assuming cardUIPrefab has an Image child)
            Image cardImage = cardUIObject.GetComponentInChildren<Image>();
            if (cardImage != null && cardSO.artwork != null) cardImage.sprite = cardSO.artwork;

            // --- TODO: Implement a proper CardDisplayUI.cs script ---
            // This script should take a CardSO and display its name, artwork, description, and stats.
            // CardDisplayUI displayScript = cardUIObject.GetComponent<CardDisplayUI>();
            // if(displayScript != null) displayScript.SetCard(cardSO);
        }
    }

    void ShowPopupMessage(string message, bool autoHide)
    {
        popupMessageText.text = message;
        // Implement auto-hide using a Coroutine if autoHide is true
        Debug.Log(message); // Also log for debugging
    }
}