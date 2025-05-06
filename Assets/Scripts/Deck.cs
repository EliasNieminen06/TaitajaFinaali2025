// Deck.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Deck : MonoBehaviour
{
    // Use CardSO references now
    // This list will represent the pool of cards that can be drawn.
    // It will not be emptied as cards are drawn.
    private List<CardSO> cardPool;
    private string deckName;
    private CardSO lastDrawnCard = null; // Store the last drawn card to prevent immediate duplicates
    private System.Random rng = new System.Random(); // Random number generator

    // CardCount might be less meaningful for an infinite deck,
    // but we can return the size of the pool.
    public int CardCount { get { return cardPool != null ? cardPool.Count : 0; } }

    // Initialize with a list of CardSO references
    public void Initialize(string name, List<CardSO> initialCards)
    {
        deckName = name;
        // Store the initial cards as the infinite pool
        cardPool = new List<CardSO>(initialCards);

        if (cardPool.Count == 0)
        {
            Debug.LogWarning($"Deck '{deckName}' initialized with no cards in the pool!");
        }
        else
        {
            Debug.Log($"{deckName} Initialized with {cardPool.Count} cards in the pool.");
        }
        // No initial shuffle needed for this infinite draw logic
    }

    // Shuffle method is less relevant for this infinite draw,
    // but can be kept if you have other uses for a shuffled list.
    // For this infinite deck, we will pick randomly from the pool.
    public void Shuffle()
    {
        Debug.Log($"{deckName} Shuffle called, but not directly used for infinite drawing.");
    }

    // Draw a CardSO reference (Infinite deck with no immediate duplicates)
    public CardSO DrawCard()
    {
        if (cardPool == null || cardPool.Count == 0)
        {
            Debug.LogWarning($"{deckName} has no cards in its pool to draw from!");
            return null;
        }

        // Create a temporary list of cards to draw from for this turn
        List<CardSO> availableCards = new List<CardSO>(cardPool);

        // If there was a last drawn card, remove it from the available cards for this draw
        if (lastDrawnCard != null)
        {
            availableCards.Remove(lastDrawnCard);
        }

        // If after removing the last drawn card, the available pool is empty,
        // it means the card pool only contained one type of card, or only
        // the last drawn card remains as a unique option.
        // In this case, we must allow drawing the last drawn card again.
        if (availableCards.Count == 0)
        {
            availableCards = new List<CardSO>(cardPool); // Reset to include all cards
                                                         // Optionally, add a warning if immediate duplicates are unavoidable
            if (cardPool.Count == 1)
            {
                Debug.LogWarning($"{deckName} only has one card type in its pool. Cannot prevent drawing the same card twice in a row.");
            }
            else
            {
                // This case happens if the initial pool had more than one card,
                // but all other cards were the 'lastDrawnCard'.
                // It's rare but possible with specific card distributions.
                Debug.LogWarning($"Only the last drawn card ({lastDrawnCard.cardName}) is available to draw from {deckName}'s pool.");
            }
        }

        // Select a random card from the available pool
        int randomIndex = rng.Next(availableCards.Count);
        CardSO drawnCard = availableCards[randomIndex];

        // Update the last drawn card for the *next* draw
        lastDrawnCard = drawnCard;

        Debug.Log($"Drew: {drawnCard.cardName} from {deckName}");
        return drawnCard;
    }

    // Add a CardSO reference to the *infinite pool*.
    // This card will be available for future draws.
    public void AddCard(CardSO card)
    {
        if (cardPool == null)
        {
            cardPool = new List<CardSO>();
        }
        // Add the card to the pool.
        cardPool.Add(card);
        Debug.Log($"Added {card.cardName} to {deckName}'s infinite pool.");
    }

    // GetCards now returns the list of all cards in the infinite pool.
    public List<CardSO> GetCards()
    {
        if (cardPool == null) return new List<CardSO>();
        return cardPool.ToList(); // Return a copy of the pool
    }
}