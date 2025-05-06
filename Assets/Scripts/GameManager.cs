// GameManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq; // Required for LINQ operations like .FirstOrDefault(), .Any()

public class GameManager : MonoBehaviour
{
    // Define the different states of the game within the single scene
    public enum GameState
    {
        MainMenu,       // Player is in the main menu
        GameSetup,      // Setting up a new game instance
        DrawingPhase,   // Player draws cards
        CookingPhase,   // Player uses cards to cook
        ScoringPhase,   // Evaluate the current dish/round
        RoundEnd,       // Prepare for the next round or game end
        GameEnd         // Game is over, show final score/highscore
    }

    [Header("Game State")]
    private GameState currentState;

    [Header("Card Definitions")]
    // Drag ALL your created CardSO assets into this list in the Inspector
    public List<CardSO> allCardDefinitions;

    [Header("Decks")]
    public Deck deck1; // Assign Deck GameObject here (Ingredients/Spices)
    public Deck deck2; // Assign Deck GameObject here (Tools/Techniques)

    [Header("Starting Deck Contents (Assign CardSO assets)")]
    // Cards to initially populate each deck's infinite pool when the game starts
    public List<CardSO> initialDeck1Cards; // Initial cards for Deck 1's pool
    public List<CardSO> initialDeck2Cards; // Initial cards for Deck 2's pool

    [Header("Recipe Definitions")]
    // Drag ALL your created RecipeSO assets into this list in the Inspector
    public List<RecipeSO> allAvailableRecipes;

    [Header("UI References")]
    // UI panels for different states in the same scene
    public GameObject mainMenuPanel;        // Assign your Main Menu UI panel
    public GameObject gameUIPanel;          // Assign your main game UI panel (hand, decks, recipes, etc.)
    public GameObject gameEndPanel;         // Assign your Game End/Score UI panel

    public GameObject handPanel;            // UI Panel that holds card representations of the player's hand
    public GameObject cardUIPrefab;         // Prefab for displaying a card in UI (should have CardDisplayUI.cs attached)

    public GameObject drawPopupPanel;       // The panel that pops up when drawing a card
    public Image popupCardArtwork;          // Image for the card in the draw popup
    public TextMeshProUGUI popupCardName;  // Text for name in draw popup
    public TextMeshProUGUI popupCardDescription; // Text for description + stats in draw popup
    public Button keepButton;               // Button to keep drawn card
    public Button discardButton;            // Button to discard drawn card
    public Button combineButton;            // Button to combine drawn card (for spices)
    public TextMeshProUGUI popupMessageText; // Optional: for messages in the draw popup or general game messages

    // UI elements to display the CURRENT recipe's info in gameUIPanel
    public TextMeshProUGUI currentRecipeNameText; // To display the name of the current recipe
    // This single TextMeshProUGUI will display the requirements (Ingredients, Techniques) and target stats for the CURRENT recipe
    public TextMeshProUGUI currentRecipeRequirementsAndStatsText;

    // UI elements to display the NEXT recipe's info in gameUIPanel
    public TextMeshProUGUI nextRecipeNameText; // To display the name of the next recipe
    // This single TextMeshProUGUI will display the requirements (Ingredients, Techniques) and target stats for the NEXT recipe
    public TextMeshProUGUI nextRecipeRequirementsAndStatsText;

    // TODO: Add UI elements to display game progress/status in gameUIPanel
    public TextMeshProUGUI currentRoundText;     // e.g., "Round 1/5"
    public TextMeshProUGUI currentScoreText;     // e.g., "Score: 150"
    public TextMeshProUGUI discardsLeftText;     // e.g., "Discards Left: 3"
    // TODO: Add UI elements related to locking a card (e.g., a prompt, visual indicator)

    // TODO: Add UI elements for the Game End screen (gameEndPanel)
    public TextMeshProUGUI finalScoreText; // To display the final score on the game end panel
    public TextMeshProUGUI highScoreText; // To display the high score on the game end panel
    // TODO: Add Buttons for "Play Again" and "Back to Menu" on gameEndPanel

    [Header("Game Settings")]
    public int handLimit = 6; // Maximum number of cards a player can hold
    public int maxDiscardsPerRound = 5; // Max discards allowed per round
    private int currentDiscardsThisRound = 0; // Counter for discards used in the current round
    private int totalScore = 0; // Player's cumulative score across all rounds

    // Recipe/Round Management
    private List<RecipeSO> gameRecipes; // The 5 random recipes for the current game instance
    private int currentRound = 0; // Index of the current round (0-based)
    public int totalRounds = 5; // Total number of rounds in a game

    // Internal game state variables
    private List<CardSO> hand = new List<CardSO>(); // List of CardSO references currently in the player's hand
    private CardSO currentlyDrawnCard; // Holds the CardSO reference of the card currently in the draw popup
    private CardSO lockedCard = null; // Stores the CardSO reference of the card locked for the next round

    // High Score storage key
    private const string HighScoreKey = "HighScore"; // Key for PlayerPrefs to store high score

    // Map Spice card names to the stat names they affect for combination logic
    // You MUST populate this dictionary with the exact 'cardName' strings from your Spice CardSO assets
    // and the exact 'name' strings from the corresponding StatEntry within those CardSO assets.
    private Dictionary<string, string> spiceStatMapping = new Dictionary<string, string>()
    {
        // Example entries based on your design document:
        {"Salt", "Saltiness"},
        {"Honey", "Sweetness"},
        {"Garlic Spice", "Umaminess"}, // Example, ensure "Garlic Spice" matches your CardSO name
        {"Pepper", "Spiciness"} // Example, ensure "Pepper" matches your CardSO name and "Spiciness" matches the StatEntry name
        // Add all your spice card name to stat name mappings here.
    };


    void Awake()
    {
        // --- Initial UI Setup ---
        // Hide all gameplay and game end UI initially, show only Main Menu.
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (drawPopupPanel != null) drawPopupPanel.SetActive(false); // Ensure draw popup is hidden initially

        // --- Deck Initialization ---
        // Initialize the decks with the cards assigned in the Inspector lists.
        // This uses the infinite deck logic you implemented in Deck.cs.
        if (deck1 != null) deck1.Initialize("Ingredient/Spice Deck", new List<CardSO>(initialDeck1Cards));
        if (deck2 != null) deck2.Initialize("Tool/Technique Deck", new List<CardSO>(initialDeck2Cards));

        // Initialize the spice stat mapping dictionary
        Debug.Log("Spice Stat Mapping Initialized.");

        // --- Link Draw Popup UI Buttons ---
        // These buttons are part of the drawPopupPanel.
        // Ensure these buttons are assigned in the Inspector.
        if (keepButton != null) keepButton.onClick.AddListener(KeepDrawnCard);
        // Apply the fix for the discard button listener using a lambda to pass 'false'
        if (discardButton != null) discardButton.onClick.AddListener(() => DiscardDrawnCard(false));
        if (combineButton != null) combineButton.onClick.AddListener(CombineDrawnCard);

        // --- TODO: Link other UI Buttons in the Inspector ---
        // Link Main Menu Start Button (in mainMenuPanel) to Call StartGame()
        // Link Game End Screen "Play Again" Button (in gameEndPanel) to Call RestartGame()
        // Link Game End Screen "Back to Menu" Button (in gameEndPanel) to Call ReturnToMainMenu()
        // Link "Finish Cooking" Button (in gameUIPanel, active during CookingPhase) to Call FinishCooking()
        // TODO: Link any buttons or interactions related to locking a card during RoundEnd.

        // Start the game in the Main Menu state when the scene loads.
        ChangeState(GameState.MainMenu);
    }

    // Update is called once per frame
    void Update()
    {
        // You can add state-dependent logic here if needed,
        // but often state transitions and actions are triggered by user input (button clicks, card drags)
        // or game events rather than continuous Update checks.
    }

    // Method to change the current game state
    void ChangeState(GameState newState)
    {
        if (currentState == newState)
        {
            Debug.LogWarning($"Attempted to change to state {newState}, but already in that state.");
            return; // Avoid changing to the same state
        }

        Debug.Log($"Changing state from {currentState} to {newState}");
        currentState = newState;
        OnStateEnter(newState); // Call the state entry logic
    }

    // Actions to perform when entering a new state
    void OnStateEnter(GameState state)
    {
        // --- Manage UI panel visibility based on the current state ---
        // Hide all panels first, then activate the relevant one(s).
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (drawPopupPanel != null) drawPopupPanel.SetActive(false); // Draw popup is managed separately

        switch (state)
        {
            case GameState.MainMenu:
                Debug.Log("Entered Main Menu State");
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                // TODO: Add any specific Main Menu setup (e.g., loading settings, displaying initial high score).
                break;

            case GameState.GameSetup:
                Debug.Log("Entered Game Setup State");
                // This state runs once at the beginning of a new game session.
                // Reset all game variables for a fresh start.
                currentRound = 0;
                totalScore = 0;
                hand.Clear(); // Clear hand from any previous game
                lockedCard = null; // Ensure no card is locked from a previous game
                currentDiscardsThisRound = 0; // Reset discards

                // Select the random recipes for this game instance using the RecipeSOs.
                gameRecipes = SelectRandomRecipes(totalRounds);

                // Reset decks (optional: uncomment if you want decks to start fresh each new game)
                // if (deck1 != null) deck1.Initialize("Ingredient/Spice Deck", new List<CardSO>(initialDeck1Cards));
                // if (deck2 != null) deck2.Initialize("Tool/Technique Deck", new List<CardSO>(initialDeck2Cards));

                // Activate the main game UI panel
                if (gameUIPanel != null) gameUIPanel.SetActive(true);
                UpdateHandUI(); // Clear the displayed hand UI

                // Immediately transition to the drawing phase for the first round.
                ChangeState(GameState.DrawingPhase);
                break;

            case GameState.DrawingPhase:
                Debug.Log($"--- Starting Round {currentRound + 1}/{totalRounds} - Drawing Phase ---");
                if (gameUIPanel != null) gameUIPanel.SetActive(true); // Ensure game UI is active
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false); // Ensure draw popup is hidden when entering

                currentDiscardsThisRound = 0; // Reset discards for the new round

                // Display the current AND next recipe's information for this round.
                DisplayRecipes();

                // Update game status UI
                if (currentRoundText != null) currentRoundText.text = $"Round: {currentRound + 1}/{totalRounds}";
                if (currentScoreText != null) currentScoreText.text = $"Score: {totalScore}";
                if (discardsLeftText != null) discardsLeftText.text = $"Discard ({maxDiscardsPerRound - currentDiscardsThisRound})";

                // If there's a locked card from the previous round, add it to the hand now.
                if (lockedCard != null)
                {
                    hand.Add(lockedCard);
                    lockedCard = null; // Card is now in hand, clear the locked slot
                    UpdateHandUI(); // Update UI to show the added locked card
                    ShowPopupMessage($"Locked card added to hand.", true); // Inform player
                }

                UpdateHandUI(); // Refresh hand display (important if a locked card was added or hand was cleared)

                // TODO: Ensure Deck Draw buttons are interactable during this phase.
                // TODO: Ensure cards in hand are NOT interactable for playing during this phase.
                break;

            case GameState.CookingPhase:
                Debug.Log("Entered Cooking Phase State");
                if (gameUIPanel != null) gameUIPanel.SetActive(true); // Ensure game UI is active
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false); // Hide draw popup

                UpdateHandUI(); // Refresh hand UI. Cards in hand should now be interactive for playing.

                // TODO: Enable card playing mechanics in the UI (e.g., clicking on hand cards via CardDisplayUI).
                // TODO: Provide a way for the player to signify they are done cooking (e.g., activate a "Finish Cooking" button).
                // TODO: Ensure Deck Draw buttons and Draw Popup related UI are NOT interactable.
                break;

            case GameState.ScoringPhase:
                Debug.Log("Entered Scoring Phase State");
                if (gameUIPanel != null) gameUIPanel.SetActive(true); // Ensure game UI is active
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false); // Hide draw popup

                // TODO: Implement the scoring logic for the current round.
                // 1. Access the cards the player played for the dish (you need a list to store these in CookingPhase).
                // 2. Compare the played cards and their combined stats to the requirements and target stats of the current recipe (gameRecipes[currentRound]).
                // 3. Calculate the score gained for this round.
                // 4. Add the round score to totalScore.
                // 5. Display score feedback for the round in the game UI (e.g., how well they matched the recipe, points gained, updated total score).

                // Transition to RoundEnd after scoring is done (you might add a delay or a "Continue" button here).
                ChangeState(GameState.RoundEnd);
                break;

            case GameState.RoundEnd:
                Debug.Log("Entered Round End State");
                if (gameUIPanel != null) gameUIPanel.SetActive(true); // Ensure game UI is active
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false); // Hide draw popup

                // TODO: Handle the player's choice to lock one card from their *remaining* hand for the next round.
                // This typically involves a UI prompt or interaction where the player clicks a card in their hand.
                // You need to implement a mechanism to allow this selection.
                // If a card is selected to be locked:
                // - Store the CardSO reference in the 'lockedCard' variable.
                // - Remove the card from the 'hand' list.
                // - Update the hand UI (`UpdateHandUI()`).
                // - Show a confirmation message (e.g., using ShowPopupMessage).
                // - TODO: Disable further card locking options for this round after one is selected.

                currentRound++; // Increment round counter for the next round
                currentDiscardsThisRound = 0; // Reset discards for the new round

                // Check if the game has completed all rounds
                if (currentRound < totalRounds)
                {
                    // If there are more rounds, transition to the next round's drawing phase.
                    Debug.Log($"Moving to Round {currentRound + 1}.");
                    ChangeState(GameState.DrawingPhase);
                }
                else
                {
                    // If all rounds are done, the game is over.
                    Debug.Log("All rounds completed. Ending game.");
                    ChangeState(GameState.GameEnd);
                }
                break;

            case GameState.GameEnd:
                Debug.Log("Entered Game End State. Final Score: " + totalScore);
                // Activate the Game End UI panel.
                if (gameEndPanel != null) gameEndPanel.SetActive(true);
                if (gameUIPanel != null) gameUIPanel.SetActive(false); // Hide game UI

                // Display the final score on the game end panel.
                if (finalScoreText != null) finalScoreText.text = "Final Score: " + totalScore.ToString();

                // Handle Highscore logic: Check if current score is a new high score and save if it is.
                int highScore = GetHighScore(); // Get existing high score from PlayerPrefs
                if (totalScore > highScore)
                {
                    highScore = totalScore;
                    SaveHighScore(highScore); // Save the new high score to PlayerPrefs
                    Debug.Log("New High Score!");
                    // TODO: Show a visual indication or message for achieving a new high score.
                }
                // Display the high score on the game end panel.
                if (highScoreText != null) highScoreText.text = "High Score: " + highScore.ToString();

                // TODO: Ensure "Play Again" and "Back to Menu" buttons on the Game End panel are interactable.
                // TODO: Ensure all other UI elements from the game scene are not interactable or hidden.
                break;
        }
    }

    // --- State Transition Methods (Called by UI Buttons) ---

    // Called by a button in the Main Menu UI to start a new game.
    public void StartGame()
    {
        ChangeState(GameState.GameSetup); // Transition to the Game Setup state
    }

    // Called by a button in the Game End UI to start the game over.
    public void RestartGame()
    {
        ChangeState(GameState.GameSetup); // Transition back to Game Setup to reset and start a new game
    }

    // Called by a button (e.g., in Game End or a Pause Menu) to return to the main menu state.
    public void ReturnToMainMenu()
    {
        ChangeState(GameState.MainMenu); // Transition to the Main Menu state
    }


    // --- Gameplay Action Methods (Called by UI Button Clicks) ---

    // Called by buttons associated with decks (e.g., "Draw Ingredient/Spice Card")
    // Ensure these buttons are only interactable during the DrawingPhase.
    public void DrawCardFromDeck(Deck deckToDrawFrom)
    {
        // Allow drawing only during the DrawingPhase and if the hand is not yet full.
        if (currentState != GameState.DrawingPhase)
        {
            Debug.LogWarning($"Attempted to draw card outside of Drawing Phase. Current State: {currentState}");
            ShowPopupMessage("Can only draw cards during the Drawing Phase!", true);
            return;
        }

        if (hand.Count >= handLimit)
        {
            // If hand is full, inform the player and do not draw a card.
            ShowPopupMessage("Hand is full! Drawing Phase ends.", true);
            // Optional: Automatically transition to CookingPhase if hand is full upon attempting to draw.
            // ChangeState(GameState.CookingPhase);
            return; // Do not draw if hand is full
        }

        // Draw a card from the specified deck using the infinite deck logic.
        currentlyDrawnCard = deckToDrawFrom.DrawCard();

        if (currentlyDrawnCard != null)
        {
            // If a card was successfully drawn, show the draw popup panel with the card's details.
            ShowDrawPopup(currentlyDrawnCard);
        }
        else
        {
            // This should ideally not happen with an infinite pool unless the initial pool was empty.
            Debug.LogError($"Error: Could not draw a card from {deckToDrawFrom.name}. Pool might be empty or not initialized correctly.");
            ShowPopupMessage($"Error drawing card from {deckToDrawFrom.name}.", true);
            // In case of error, hide the popup and clear the drawn card reference.
            if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
            currentlyDrawnCard = null;
        }
    }

    // Displays the details of the drawn card in the draw popup UI.
    // Also sets up the interactability of the popup's buttons.
    void ShowDrawPopup(CardSO card)
    {
        // Only show popup if the state is DrawingPhase.
        if (currentState != GameState.DrawingPhase)
        {
            Debug.LogWarning("Attempted to show draw popup outside of Drawing Phase.");
            if (drawPopupPanel != null) drawPopupPanel.SetActive(false); // Hide if somehow active
            currentlyDrawnCard = null; // Clear the drawn card as it can't be acted upon
            return;
        }

        // Activate the draw popup panel.
        if (drawPopupPanel != null) drawPopupPanel.SetActive(true);

        // Update UI elements in the popup with information from the drawn card.
        if (popupCardArtwork != null) popupCardArtwork.sprite = card.artwork;
        if (popupCardName != null) popupCardName.text = card.cardName;

        // Combine Description and Stats into a single string for the popup's description text element.
        string combinedText = card.description;
        if (!string.IsNullOrEmpty(card.description) && card.GetStatsDictionary().Count > 0)
        {
            combinedText += "\n\nStats:\n"; // Add a separator and label for stats
        }
        foreach (var stat in card.GetStatsDictionary())
        {
            combinedText += $"{stat.Key}: {stat.Value}\n";
        }
        if (popupCardDescription != null) popupCardDescription.text = combinedText;

        // Determine if the "Combine" button should be active for the drawn card:
        // Active only if the drawn card is a Spice AND there is at least one card already in hand
        // with the exact same name.
        bool canCombine = false;
        if (card.cardType == CardSO.CardType.Spice)
        {
            // Check if any card in the player's current hand has the same name as the drawn spice card.
            if (hand.Any(c => c.cardName == card.cardName))
            {
                canCombine = true;
            }
        }
        // Set the "Combine" button's active state based on whether combination is possible.
        if (combineButton != null) combineButton.gameObject.SetActive(canCombine);

        // Reset the popup message text
        if (popupMessageText != null) popupMessageText.text = "Choose an action:";

        // Ensure the "Keep" button is interactable.
        if (keepButton != null) keepButton.interactable = true;
        // Disable the "Discard" button if the player has used all discards for this round.
        if (discardButton != null) discardButton.interactable = (currentDiscardsThisRound < maxDiscardsPerRound);
    }

    // Action taken when the player clicks "Keep" in the draw popup.
    void KeepDrawnCard()
    {
        // Allow keeping only if in the DrawingPhase, a card is currently drawn in the popup,
        // and the player's hand is not yet full.
        if (currentState != GameState.DrawingPhase || currentlyDrawnCard == null || hand.Count >= handLimit)
        {
            if (hand.Count >= handLimit) ShowPopupMessage("Hand is already full! Cannot keep card.", false);
            Debug.LogWarning("KeepDrawnCard called in invalid state or conditions.");
            // Close the popup if conditions are not met
            if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
            currentlyDrawnCard = null; // Clear drawn card reference as it wasn't kept
            return;
        }

        // Add the currently drawn card's CardSO reference to the player's hand list.
        hand.Add(currentlyDrawnCard); // Add the CardSO reference to the hand list
        UpdateHandUI(); // Update the visual display of the hand to show the new card

        // Hide the draw popup and clear the reference to the card that was just kept.
        if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
        currentlyDrawnCard = null; // Clear the drawn card reference

        // Check if the hand is now full after keeping the card.
        if (hand.Count >= handLimit)
        {
            // If hand is full, the Drawing Phase for this round ends.
            ShowPopupMessage("Hand is full! Drawing Phase ends.", true);
            // Automatically transition to the CookingPhase.
            ChangeState(GameState.CookingPhase);
        }
        // If hand is not full, the player remains in the DrawingPhase and can draw again.
    }

    // Action taken when the player clicks the "Discard" button in the draw popup.
    // autoDiscard parameter is used internally if a card needs to be discarded without player choice (e.g., hand full on draw attempt).
    void DiscardDrawnCard(bool autoDiscard = false)
    {
        // Allow discarding only if in the DrawingPhase (unless it's an auto-discard scenario)
        // AND if a card is currently drawn in the popup.
        if ((currentState != GameState.DrawingPhase && !autoDiscard) || currentlyDrawnCard == null)
        {
            Debug.LogWarning("DiscardDrawnCard called outside of Drawing Phase or with no card drawn.");
            if (drawPopupPanel != null) drawPopupPanel.SetActive(false); // Close popup
            currentlyDrawnCard = null; // Clear drawn card
            return;
        }

        // Check if the player has discards left for this round, unless it's an automatic discard.
        if (currentDiscardsThisRound < maxDiscardsPerRound || autoDiscard)
        {
            Debug.Log($"Discarded: {currentlyDrawnCard.cardName}");
            // In this basic setup, the discarded CardSO reference is just removed from the flow.
            // If you needed a discard pile for gameplay mechanics, you would add it to a discard Deck here.

            // Increment the discard counter for the round if it was a player-initiated discard.
            if (!autoDiscard)
            {
                currentDiscardsThisRound++;
                // TODO: Update the UI element showing the number of discards left for the round.
                if (discardsLeftText != null) discardsLeftText.text = $"Discards Left: {maxDiscardsPerRound - currentDiscardsThisRound}";
                Debug.Log($"Discards left this round: {maxDiscardsPerRound - currentDiscardsThisRound}");
                // Update the discard button's interactability in case discards are now maxed.
                if (discardButton != null) discardButton.interactable = (currentDiscardsThisRound < maxDiscardsPerRound);
            }

            // Hide the draw popup and clear the reference to the card that was just discarded.
            if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
            currentlyDrawnCard = null; // Clear the drawn card reference

            // If it was a player-initiated discard, they remain in the DrawingPhase and can draw again.
            // If autoDiscard was true (e.g., hand was full on draw attempt), the state does not change either.
        }
        else
        {
            // Player tried to discard but has no discards left this round.
            ShowPopupMessage($"No discards left this round ({currentDiscardsThisRound}/{maxDiscardsPerRound})!", false);
            // The discard button should already be disabled in the popup UI if discards are maxed.
        }
    }

    // Action taken when the player clicks the "Combine" button in the draw popup.
    // This is currently implemented for combining matching Spice cards to increase stats.
    // Ensure this button is only interactable/active when a combine is possible.
    void CombineDrawnCard()
    {
        // Allow combining only if in the DrawingPhase, a card is currently drawn,
        // the drawn card is a Spice, AND there is at least one card already in hand
        // with the exact same name to combine with.
        if (currentState != GameState.DrawingPhase || currentlyDrawnCard == null || currentlyDrawnCard.cardType != CardSO.CardType.Spice)
        {
            Debug.LogWarning($"CombineDrawnCard called in invalid state ({currentState}), no card drawn, or card is not a Spice ({currentlyDrawnCard?.cardType}).");
            ShowPopupMessage("Combination not possible.", false);
            if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
            currentlyDrawnCard = null;
            return;
        }

        // Find the first card in hand with the exact same name as the drawn Spice.
        // This is the card that the drawn spice will be combined with.
        CardSO cardInHandToCombine = hand.FirstOrDefault(c => c.cardName == currentlyDrawnCard.cardName);

        // This check should ideally not fail if the combine button's active state is managed correctly,
        // but we include it for safety.
        if (cardInHandToCombine != null)
        {
            Debug.Log($"Attempting to combine drawn {currentlyDrawnCard.cardName} with {cardInHandToCombine.cardName} from hand.");

            // --- Combination Logic: Combine Stats ---
            // Find the name of the stat that this type of spice affects using the mapping dictionary.
            if (spiceStatMapping.TryGetValue(currentlyDrawnCard.cardName, out string statName))
            {
                // Get the value of the relevant stat from the drawn card.
                float drawnStatValue = currentlyDrawnCard.GetStat(statName);

                // Check if the card in hand actually has this stat before trying to add.
                // (It should if it's the same spice type, but this is a good check).
                if (cardInHandToCombine.GetStatsDictionary().ContainsKey(statName))
                {
                    // Add the drawn card's stat value to the corresponding stat on the card in hand.
                    // This calls the AddToStat method you must implement in your CardSO.cs.
                    cardInHandToCombine.AddToStat(statName, drawnStatValue);

                    Debug.Log($"Combined {currentlyDrawnCard.cardName} stats onto {cardInHandToCombine.cardName}. " +
                              $"New '{statName}' value on card in hand: {cardInHandToCombine.GetStat(statName)}");

                    // After successfully combining stats, the drawn card is consumed.
                    // The card in hand remains and its stat is increased.
                    // We do NOT remove cardInHandToCombine from the hand here.

                    // --- UI Update ---
                    // Update the visual display of the hand. Your CardDisplayUI script must
                    // be able to read and show the updated stats from the CardSO instance.
                    UpdateHandUI();

                    // Hide the draw popup since the action is complete.
                    if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
                    currentlyDrawnCard = null; // Clear the reference to the drawn card.

                    ShowPopupMessage($"Combined {currentlyDrawnCard.cardName}! {statName} increased on the card in hand.", true);

                    // Check if hand is now full after combining (less likely as no card was added, but good habit)
                    if (hand.Count >= handLimit)
                    {
                        ShowPopupMessage("Hand is full! Drawing Phase ends.", true);
                        // Optional: Automatically transition to CookingPhase if hand is full
                        // ChangeState(GameState.CookingPhase);
                    }
                }
                else
                {
                    // The matching card in hand somehow doesn't have the expected stat.
                    Debug.LogWarning($"Card in hand ({cardInHandToCombine.cardName}) does not have stat '{statName}'. Cannot combine.");
                    ShowPopupMessage($"Cannot combine: Card in hand does not have the required stat.", false);
                    // Decide what happens in this edge case - perhaps discard the drawn card?
                    if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
                    currentlyDrawnCard = null;
                }
            }
            else
            {
                // The drawn spice card's name is not in the spiceStatMapping dictionary.
                Debug.LogWarning($"Spice '{currentlyDrawnCard.cardName}' has no defined stat mapping in spiceStatMapping dictionary.");
                ShowPopupMessage($"Cannot combine: '{currentlyDrawnCard.cardName}' does not affect a known stat type.", false);
                // Decide what happens when a spice doesn't have a mapping - discard, return to deck?
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
                currentlyDrawnCard = null;
            }
        }
        else
        {
            // This should ideally not happen if the combine button's active state is managed correctly,
            // but it's a failsafe.
            Debug.LogError("CombineDrawnCard called but no matching card found in hand. Combine button should not have been active.");
            ShowPopupMessage("No matching card in hand to combine!", false);
            if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
            currentlyDrawnCard = null;
        }
    }

    // --- Cooking Phase Methods ---

    // TODO: Implement methods for playing cards from hand during CookingPhase.
    // This method would be called by the CardDisplayUI script when a card in hand is clicked.
    // public void PlayCard(CardSO cardToPlay)
    // {
    //    // 1. Check if the current state is CookingPhase.
    //    if (currentState != GameState.CookingPhase)
    //    {
    //        Debug.LogWarning("Attempted to play card outside of Cooking Phase.");
    //        ShowPopupMessage("You can only play cards during the Cooking Phase!", true);
    //        return;
    //    }
    //    // 2. Check if the cardToPlay is actually in the 'hand' list.
    //    if (!hand.Contains(cardToPlay))
    //    {
    //         Debug.LogWarning($"Attempted to play card '{cardToPlay.cardName}' not found in hand.");
    //         ShowPopupMessage("Card not found in hand!", false);
    //         return;
    //    }
    //    // 3. Remove the card from the 'hand' list.
    //    hand.Remove(cardToPlay);
    //    // 4. Add the card to a temporary list representing the "played cards" for the current dish.
    //    // TODO: You need a List<CardSO> playedCardsThisRound; variable in GameManager.
    //    // playedCardsThisRound.Add(cardToPlay);
    //
    //    Debug.Log($"Played card: {cardToPlay.cardName}");
    //
    //    // 5. Update the hand UI to reflect the removed card.
    //    UpdateHandUI();
    //    // 6. TODO: Potentially update a separate UI area to show the cards played for the dish.
    // }

    // TODO: Implement a method to handle the "Finish Cooking" action.
    // This would likely be called by a UI button available during the CookingPhase.
    public void FinishCooking()
    {
        if (currentState == GameState.CookingPhase)
        {
            Debug.Log("Finishing Cooking for the round.");
            // TODO: Gather the cards the player used to form the dish from the "played cards" list (e.g., playedCardsThisRound).
            // TODO: Trigger the scoring logic (comparing played cards/stats to the current recipe (gameRecipes[currentRound])).
            ChangeState(GameState.ScoringPhase); // Transition to Scoring Phase
        }
        else
        {
            Debug.LogWarning("Attempted to finish cooking outside of Cooking Phase.");
            ShowPopupMessage("Can only finish cooking during the Cooking Phase!", true);
        }
    }


    // --- Round End Methods ---

    // TODO: Implement a method to handle locking a card during RoundEnd.
    // This method would be called by a UI interaction (e.g., clicking a card in hand after scoring).
    // public void LockCardForNextRound(CardSO cardToLock)
    // {
    //    // Ensure the state is RoundEnd, the card is in hand, and no card is already locked for the next round.
    //    if (currentState == GameState.RoundEnd && hand.Contains(cardToLock) && lockedCard == null)
    //    {
    //         lockedCard = cardToLock; // Store the card to be carried over to the next round
    //         hand.Remove(cardToLock); // Remove it from the current hand
    //         UpdateHandUI(); // Update the hand UI to show the card has been removed
    //         ShowPopupMessage($"Locked {cardToLock.cardName} for the next round.", true);
    //         // TODO: Disable further card locking options for this round after one is selected.
    //         // The state will transition to the next round's DrawingPhase or GameEnd automatically
    //         // after a delay or UI confirmation, based on the logic in OnStateEnter(GameState.RoundEnd).
    //    }
    //     else
    //    {
    //         Debug.LogWarning("Attempted to lock card in invalid state or conditions.");
    //         // Show a message to the player if locking is not possible.
    //         ShowPopupMessage("Cannot lock card at this time.", false);
    //    }
    // }


    // --- UI Update Methods ---

    // Updates the visual display of the player's hand in the UI panel.
    void UpdateHandUI()
    {
        // Clear existing UI card GameObjects from the hand panel's hierarchy.
        if (handPanel != null)
        {
            foreach (Transform child in handPanel.transform)
            {
                Destroy(child.gameObject);
            }

            // Instantiate new UI card GameObjects for each CardSO currently in the player's hand.
            foreach (var cardSO in hand) // Iterate through CardSO references in the hand list
            {
                // Instantiate a new Card UI prefab as a child of the hand panel.
                GameObject cardUIObject = Instantiate(cardUIPrefab, handPanel.transform);

                // Get the CardDisplayUI script component from the instantiated object.
                // This script is responsible for setting the visual details and handling interaction for a single card UI.
                CardDisplayUI displayScript = cardUIObject.GetComponent<CardDisplayUI>();
                if (displayScript != null)
                {
                    // Pass the CardSO data to the CardDisplayUI script so it can update its UI elements.
                    displayScript.SetCard(cardSO);

                    // Set the interactivity of the card UI based on the current game state.
                    // Cards in hand are typically playable only during the CookingPhase.
                    displayScript.SetInteractive(currentState == GameState.CookingPhase);

                    // Pass a reference to this GameManager to the CardDisplayUI script.
                    // This allows the CardDisplayUI script to call methods back on the GameManager
                    // when the card is interacted with (e.g., clicked to play).
                    displayScript.SetGameManager(this);
                }
                else
                {
                    // Fallback if the CardDisplayUI script is not attached to the cardUIPrefab.
                    Debug.LogError("CardDisplayUI script not found on cardUIPrefab. Card UI will not display correctly or be interactive.");
                    // Basic fallback to show name and artwork if the dedicated script is missing.
                    TextMeshProUGUI cardText = cardUIObject.GetComponentInChildren<TextMeshProUGUI>();
                    if (cardText != null) cardText.text = cardSO.cardName;
                    Image cardImage = cardUIObject.GetComponentInChildren<Image>();
                    if (cardImage != null && cardSO.artwork != null) cardImage.sprite = cardSO.artwork;
                }
            }
        }
        // TODO: Update a separate UI element (e.g., a TextMeshProUGUI) to show the current hand size (hand.Count) vs the handLimit.
        if (discardsLeftText != null) // Placeholder, replace with dedicated hand size text
        {
            // handSizeText.text = $"Hand: {hand.Count}/{handLimit}";
        }
    }

    // Displays the information of the current AND next recipes in the designated UI elements.
    void DisplayRecipes()
    {
        // --- Display Current Recipe ---
        // Ensure there are recipes selected for this game and the current round index is valid.
        if (gameRecipes == null || gameRecipes.Count <= currentRound)
        {
            Debug.LogError($"Cannot display current recipe for round {currentRound}. gameRecipes list is null or has only {gameRecipes?.Count ?? 0} recipes.");
            // Clear current recipe UI elements if no recipe is available for the current round.
            if (currentRecipeNameText != null) currentRecipeNameText.text = "Error: No Recipe";
            if (currentRecipeRequirementsAndStatsText != null) currentRecipeRequirementsAndStatsText.text = "";
        }
        else
        {
            RecipeSO currentRecipe = gameRecipes[currentRound];
            // Update the Current Recipe Name UI element.
            if (currentRecipeNameText != null) currentRecipeNameText.text = currentRecipe.recipeName; // Just the name, no "Current Dish:" prefix

            // Combine Current Recipe Requirements and Target Stats into a single string.
            string currentReqAndStatsText = "";

            // Display Required Ingredients
            if (currentRecipe.requiredIngredients != null && currentRecipe.requiredIngredients.Count > 0)
            {
                currentReqAndStatsText += "Ingredients:\n";
                foreach (var card in currentRecipe.requiredIngredients) currentReqAndStatsText += $"- {card.cardName}\n";
            }

            // Display Required Techniques
            if (currentRecipe.requiredTechniques != null && currentRecipe.requiredTechniques.Count > 0)
            {
                // Add a new line before Techniques if Ingredients were listed
                if (currentReqAndStatsText.Length > 0 && !currentReqAndStatsText.EndsWith("\n")) currentReqAndStatsText += "\n";
                currentReqAndStatsText += "Techniques:\n";
                foreach (var card in currentRecipe.requiredTechniques) currentReqAndStatsText += $"- {card.cardName}\n";
            }

            // Display Target Stats
            if (currentRecipe.targetStats != null && currentRecipe.targetStats.Count > 0)
            {
                // Add a new line before Stats if Ingredients or Techniques were listed
                if (currentReqAndStatsText.Length > 0 && !currentReqAndStatsText.EndsWith("\n")) currentReqAndStatsText += "\n";
                currentReqAndStatsText += "Stats:\n";
                foreach (var stat in currentRecipe.targetStats) currentReqAndStatsText += $"- {stat.name}: {stat.targetValue}\n";
            }

            // Assign the combined current recipe text to the designated UI element.
            if (currentRecipeRequirementsAndStatsText != null)
            {
                currentRecipeRequirementsAndStatsText.text = currentReqAndStatsText;
            }
            else
            {
                Debug.LogWarning("currentRecipeRequirementsAndStatsText TextMeshProUGUI is not assigned in GameManager.");
            }

            Debug.Log($"Displayed Current Recipe for Round {currentRound + 1}: {currentRecipe.recipeName}");
        }


        // --- Display Next Recipe ---
        // Check if there is a next recipe available (i.e., not in the last round).
        if (gameRecipes != null && currentRound + 1 < gameRecipes.Count)
        {
            RecipeSO nextRecipe = gameRecipes[currentRound + 1];

            // Update the Next Recipe Name UI element.
            if (nextRecipeNameText != null) nextRecipeNameText.text = nextRecipe.recipeName; // Just the name

            // Combine Next Recipe Requirements and Target Stats into a single string.
            string nextReqAndStatsText = "";

            // Display Required Ingredients for the next recipe
            if (nextRecipe.requiredIngredients != null && nextRecipe.requiredIngredients.Count > 0)
            {
                nextReqAndStatsText += "Ingredients:\n";
                foreach (var card in nextRecipe.requiredIngredients) nextReqAndStatsText += $"- {card.cardName}\n";
            }

            // Display Required Techniques for the next recipe
            if (nextRecipe.requiredTechniques != null && nextRecipe.requiredTechniques.Count > 0)
            {
                if (nextReqAndStatsText.Length > 0 && !nextReqAndStatsText.EndsWith("\n")) nextReqAndStatsText += "\n";
                nextReqAndStatsText += "Techniques:\n";
                foreach (var card in nextRecipe.requiredTechniques) nextReqAndStatsText += $"- {card.cardName}\n";
            }

            // Display Next Recipe Target Stats
            if (nextRecipe.targetStats != null && nextRecipe.targetStats.Count > 0)
            {
                if (nextReqAndStatsText.Length > 0 && !nextReqAndStatsText.EndsWith("\n")) nextReqAndStatsText += "\n";
                nextReqAndStatsText += "Stats:\n";
                foreach (var stat in nextRecipe.targetStats) nextReqAndStatsText += $"- {stat.name}: {stat.targetValue}\n";
            }

            // Assign the combined next recipe text to the designated UI element.
            if (nextRecipeRequirementsAndStatsText != null)
            {
                nextRecipeRequirementsAndStatsText.text = nextReqAndStatsText;
            }
            else
            {
                Debug.LogWarning("nextRecipeRequirementsAndStatsText TextMeshProUGUI is not assigned in GameManager.");
            }

            Debug.Log($"Displayed Next Recipe for Round {currentRound + 2}: {nextRecipe.recipeName}");

        }
        else
        {
            // If there is no next recipe (i.e., in the last round), clear the next recipe UI elements or set placeholder.
            if (nextRecipeNameText != null) nextRecipeNameText.text = "Next Dish: N/A"; // Indicate no next dish
            if (nextRecipeRequirementsAndStatsText != null) nextRecipeRequirementsAndStatsText.text = "";
            Debug.Log("No next recipe to display.");
        }
    }

    // Displays a message in a designated UI text element (e.g., popupMessageText or a separate message area).
    void ShowPopupMessage(string message, bool autoHide)
    {
        // Use your popupMessageText UI element to display messages.
        if (popupMessageText != null) popupMessageText.text = message;
        Debug.Log($"[Game Message] {message}"); // Also log messages for debugging

        // TODO: Implement auto-hide for the message using a Coroutine if autoHide is true.
        // Example: if (autoHide) StartCoroutine(HideMessageAfterDelay(3f));
    }

    // TODO: Implement a Coroutine to hide the popup message after a delay.
    // IEnumerator HideMessageAfterDelay(float delay) { yield return new WaitForSeconds(delay); if(popupMessageText != null) popupMessageText.text = ""; }


    // --- Recipe Selection ---

    // Selects a list of random recipes for a new game instance from the 'allAvailableRecipes' list.
    // Called during the GameSetup state.
    private List<RecipeSO> SelectRandomRecipes(int count)
    {
        List<RecipeSO> recipes = new List<RecipeSO>();
        // Ensure there are recipes available to select from.
        if (allAvailableRecipes == null || allAvailableRecipes.Count == 0)
        {
            Debug.LogError("No available recipes found in the 'allAvailableRecipes' list! Cannot select recipes.");
            return recipes; // Return an empty list
        }

        System.Random rand = new System.Random();
        // Create a temporary list to select from, to avoid selecting the same recipe multiple times if you want unique recipes per game.
        List<RecipeSO> availableForSelection = new List<RecipeSO>(allAvailableRecipes);

        // Select 'count' number of random recipes.
        for (int i = 0; i < count; i++)
        {
            // Stop if we've run out of unique recipes before reaching the desired count.
            if (availableForSelection.Count == 0)
            {
                Debug.LogWarning($"Could only select {i} unique recipes, needed {count}. Not enough unique recipes available.");
                break;
            }
            // Select a random index from the available recipes.
            int randomIndex = rand.Next(availableForSelection.Count);
            RecipeSO selectedRecipe = availableForSelection[randomIndex];
            recipes.Add(selectedRecipe); // Add the selected recipe to the game's list.
            availableForSelection.RemoveAt(randomIndex); // Remove the selected recipe from the temporary list (for unique selection).
        }

        Debug.Log($"Selected {recipes.Count} recipes for the game instance.");
        return recipes; // Return the list of selected RecipeSOs for this game.
    }


    // --- High Score Handling ---

    // Helper method to get the high score from Unity's PlayerPrefs.
    private int GetHighScore()
    {
        // PlayerPrefs.GetInt(key, defaultValue) returns the integer value associated with the key,
        // or the defaultValue (0 in this case) if the key doesn't exist.
        return PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    // Helper method to save the high score to Unity's PlayerPrefs.
    private void SaveHighScore(int score)
    {
        PlayerPrefs.SetInt(HighScoreKey, score);
        PlayerPrefs.Save(); // Save the changes to disk.
    }

    // Note: Since we are in a single scene, there's no explicit method here to load a new scene for the menu.
    // Transitions happen by changing the state and managing UI panel visibility.
}