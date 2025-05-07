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
    // TODO: Add a UI element to display the cards played for the current dish
    // TODO: Add a TextMeshProUGUI or other UI element to show the current cooking selection prompt (e.g., "Select a Tool")
    // TODO: Add a "Cancel Technique" button that is active during Technique selection

    // TODO: Add UI elements for the Game End screen (gameEndPanel)
    public TextMeshProUGUI finalScoreText; // To display the final score on the game end panel
    public TextMeshProUGUI highScoreText; // To display the high score on the game end panel
    // TODO: Add Buttons for "Play Again" and "Back to Menu" on gameEndPanel
    // TODO: Add a "Finish Cooking" button that is active during CookingPhase

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

    // List to store cards played during the current cooking phase
    private List<CardSO> playedCardsThisRound = new List<CardSO>();

    // --- New: Variables for Technique Combination Selection ---
    private CardSO selectedTechnique = null;
    private CardSO selectedTool = null;
    private CardSO selectedIngredient = null;

    // Enum to track the current sub-state during Technique combination
    private enum CookingSelectionState
    {
        None,                   // Not in a selection process
        WaitingForTool,         // Technique selected, waiting for Tool
        WaitingForIngredient    // Technique and Tool selected, waiting for Ingredient
    }
    private CookingSelectionState currentCookingSelectionState = CookingSelectionState.None;
    // --- End New Variables ---


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
        // TODO: Link "Finish Cooking" Button (in gameUIPanel, active during CookingPhase) to Call FinishCooking()
        // TODO: Link a "Cancel Technique" button (in gameUIPanel, active during selection) to Call CancelTechniqueSelection()
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

        // Reset technique selection state when changing states (except within CookingPhase)
        if (state != GameState.CookingPhase)
        {
            CancelTechniqueSelection(); // Reset selection variables and state
        }


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
                playedCardsThisRound.Clear(); // Clear played cards from previous game
                CancelTechniqueSelection(); // Ensure selection state is reset at the start of a game

                gameRecipes = SelectRandomRecipes(totalRounds);

                // Reset decks (optional: uncomment if you want decks to start fresh each new game)
                // if (deck1 != null) deck1.Initialize("Ingredient/Spice Deck", new List<CardSO>(initialDeck1Cards));
                // if (deck2 != null) deck2.Initialize("Tool/Technique Deck", new List<CardSO>(initialDeck2Cards));

                // Activate the main game UI panel
                if (gameUIPanel != null) gameUIPanel.SetActive(true);
                UpdateHandUI();

                // Immediately transition to the drawing phase for the first round.
                ChangeState(GameState.DrawingPhase);
                break;

            case GameState.DrawingPhase:
                Debug.Log($"--- Starting Round {currentRound + 1}/{totalRounds} - Drawing Phase ---");
                if (gameUIPanel != null) gameUIPanel.SetActive(true);
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);

                currentDiscardsThisRound = 0;

                DisplayRecipes(); // Display current and next recipes

                // Update game status UI
                if (currentRoundText != null) currentRoundText.text = $"Round: {currentRound + 1}/{totalRounds}";
                if (currentScoreText != null) currentScoreText.text = $"Score: {totalScore}";
                if (discardsLeftText != null) discardsLeftText.text = $"Discards Left: {maxDiscardsPerRound - currentDiscardsThisRound}";
                // TODO: Update other game status UI elements

                // Add locked card to hand if it exists
                if (lockedCard != null)
                {
                    hand.Add(lockedCard);
                    lockedCard = null;
                    UpdateHandUI();
                    ShowPopupMessage($"Locked card added to hand.", true);
                }

                UpdateHandUI(); // Refresh hand display

                // TODO: Ensure Deck Draw buttons are interactable during this phase.
                // Ensure cards in hand are NOT interactive for playing during this phase.
                SetHandCardInteractivity(false); // Cards in hand cannot be played yet
                // TODO: Hide CookingPhase specific UI like "Finish Cooking" button
                // TODO: Hide Technique selection UI elements (prompt, cancel button).
                break;

            case GameState.CookingPhase:
                Debug.Log("Entered Cooking Phase State");
                if (gameUIPanel != null) gameUIPanel.SetActive(true);
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);

                playedCardsThisRound.Clear(); // Clear played cards list for this round
                CancelTechniqueSelection(); // Reset selection state when entering cooking

                UpdateHandUI(); // Refresh hand UI. Cards in hand should now be interactive (controlled by SetHandCardInteractivity below).

                // TODO: Ensure Deck Draw buttons and Draw Popup UI are NOT interactable.
                // TODO: Ensure "Finish Cooking" button IS interactable.
                // TODO: Hide Technique selection UI elements initially (prompt, cancel button).

                // Set initial interactivity for cards in hand (playable directly unless a technique is in progress)
                SetHandCardInteractivity(true);
                // Initial highlight state (no cards highlighted when starting cooking)
                HighlightSelectableCards(); // This will call SetHighlight(false) on all cards
                break;

            case GameState.ScoringPhase:
                Debug.Log("Entered Scoring Phase State");
                if (gameUIPanel != null) gameUIPanel.SetActive(true);
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);

                SetHandCardInteractivity(false); // Cards in hand cannot be played during scoring
                CancelTechniqueSelection(); // Reset selection state

                // TODO: Implement scoring logic using playedCardsThisRound and gameRecipes[currentRound].
                // TODO: Display score feedback.

                ChangeState(GameState.RoundEnd);
                break;

            case GameState.RoundEnd:
                Debug.Log("Entered Round End State");
                if (gameUIPanel != null) gameUIPanel.SetActive(true);
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);

                SetHandCardInteractivity(false); // Cards in hand cannot be played during round end
                CancelTechniqueSelection(); // Reset selection state

                // TODO: Handle player choice to lock a card.
                // After potential card locking (or decision not to):
                currentRound++;
                currentDiscardsThisRound = 0;
                // TODO: Update game status UI (round number).

                if (currentRound < totalRounds)
                {
                    Debug.Log($"Moving to Round {currentRound + 1}.");
                    ChangeState(GameState.DrawingPhase);
                }
                else
                {
                    Debug.Log("All rounds completed. Ending game.");
                    ChangeState(GameState.GameEnd);
                }
                break;

            case GameState.GameEnd:
                Debug.Log("Entered Game End State. Final Score: " + totalScore);
                if (gameEndPanel != null) gameEndPanel.SetActive(true);
                if (gameUIPanel != null) gameUIPanel.SetActive(false);

                SetHandCardInteractivity(false); // Cards in hand cannot be played during game end
                CancelTechniqueSelection(); // Reset selection state

                if (finalScoreText != null) finalScoreText.text = "Final Score: " + totalScore.ToString();

                int highScore = GetHighScore();
                if (totalScore > highScore)
                {
                    highScore = totalScore;
                    SaveHighScore(highScore);
                    Debug.Log("New High Score!");
                    // TODO: Show new high score indication.
                }
                if (highScoreText != null) highScoreText.text = "High Score: " + highScore.ToString();

                // TODO: Ensure Game End buttons are interactable.
                // TODO: Ensure all other UI elements are not interactable.
                break;
        }
    }

    // --- State Transition Methods ---

    public void StartGame()
    {
        ChangeState(GameState.GameSetup);
    }

    public void RestartGame()
    {
        ChangeState(GameState.GameSetup);
    }

    public void ReturnToMainMenu()
    {
        ChangeState(GameState.MainMenu);
    }


    // --- Gameplay Action Methods ---

    public void DrawCardFromDeck(Deck deckToDrawFrom)
    {
        if (currentState != GameState.DrawingPhase) { Debug.LogWarning("Attempted to draw card outside of Drawing Phase."); ShowPopupMessage("Can only draw cards during the Drawing Phase!", true); return; }
        if (hand.Count >= handLimit) { ShowPopupMessage("Hand is full! Drawing Phase ends.", true); /* Optional: ChangeState(GameState.CookingPhase); */ return; }

        currentlyDrawnCard = deckToDrawFrom.DrawCard();

        if (currentlyDrawnCard != null) { ShowDrawPopup(currentlyDrawnCard); }
        else { Debug.LogError($"Error: Could not draw a card from {deckToDrawFrom.name}."); ShowPopupMessage($"Error drawing card from {deckToDrawFrom.name}.", true); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; }
    }

    void ShowDrawPopup(CardSO card)
    {
        if (currentState != GameState.DrawingPhase) { Debug.LogWarning("Attempted to show draw popup outside of Drawing Phase."); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; return; }

        if (drawPopupPanel != null) drawPopupPanel.SetActive(true);

        if (popupCardArtwork != null) popupCardArtwork.sprite = card.artwork;
        if (popupCardName != null) popupCardName.text = card.cardName;

        string combinedText = card.description;
        if (!string.IsNullOrEmpty(card.description) && card.GetStatsDictionary().Count > 0) { combinedText += "\n\nStats:\n"; }
        foreach (var stat in card.GetStatsDictionary()) { combinedText += $"{stat.Key}: {stat.Value}\n"; }
        if (popupCardDescription != null) popupCardDescription.text = combinedText;

        bool canCombine = false;
        if (card.cardType == CardSO.CardType.Spice) { if (hand.Any(c => c.cardName == card.cardName)) { canCombine = true; } }
        if (combineButton != null) combineButton.gameObject.SetActive(canCombine);

        if (popupMessageText != null) popupMessageText.text = "Choose an action:";
        if (keepButton != null) keepButton.interactable = true;
        if (discardButton != null) discardButton.interactable = (currentDiscardsThisRound < maxDiscardsPerRound);
    }

    void KeepDrawnCard()
    {
        if (currentState != GameState.DrawingPhase || currentlyDrawnCard == null || hand.Count >= handLimit) { if (hand.Count >= handLimit) ShowPopupMessage("Hand is already full! Cannot keep card.", false); Debug.LogWarning("KeepDrawnCard called in invalid state or conditions."); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; return; }

        hand.Add(currentlyDrawnCard);
        UpdateHandUI();

        if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
        currentlyDrawnCard = null;

        if (hand.Count >= handLimit) { ShowPopupMessage("Hand is full! Drawing Phase ends.", true); ChangeState(GameState.CookingPhase); }
    }

    void DiscardDrawnCard(bool autoDiscard = false)
    {
        if ((currentState != GameState.DrawingPhase && !autoDiscard) || currentlyDrawnCard == null) { Debug.LogWarning("DiscardDrawnCard called outside of Drawing Phase or with no card drawn."); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; return; }

        if (currentDiscardsThisRound < maxDiscardsPerRound || autoDiscard)
        {
            Debug.Log($"Discarded: {currentlyDrawnCard.cardName}");
            if (!autoDiscard) { currentDiscardsThisRound++; if (discardsLeftText != null) discardsLeftText.text = $"Discards Left: {maxDiscardsPerRound - currentDiscardsThisRound}"; Debug.Log($"Discards left this round: {maxDiscardsPerRound - currentDiscardsThisRound}"); if (discardButton != null) discardButton.interactable = (currentDiscardsThisRound < maxDiscardsPerRound); }
            if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
            currentlyDrawnCard = null;
        }
        else { ShowPopupMessage($"No discards left this round ({currentDiscardsThisRound}/{maxDiscardsPerRound})!", false); }
    }

    void CombineDrawnCard()
    {
        if (currentState != GameState.DrawingPhase || currentlyDrawnCard == null || currentlyDrawnCard.cardType != CardSO.CardType.Spice) { Debug.LogWarning($"CombineDrawnCard called in invalid state ({currentState}), no card drawn, or card is not a Spice."); ShowPopupMessage("Combination not possible.", false); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; return; }

        CardSO cardInHandToCombine = hand.FirstOrDefault(c => c.cardName == currentlyDrawnCard.cardName);

        if (cardInHandToCombine != null)
        {
            Debug.Log($"Attempting to combine drawn {currentlyDrawnCard.cardName} with {cardInHandToCombine.cardName} from hand.");

            if (spiceStatMapping.TryGetValue(currentlyDrawnCard.cardName, out string statName))
            {
                float drawnStatValue = currentlyDrawnCard.GetStat(statName);
                if (cardInHandToCombine.GetStatsDictionary().ContainsKey(statName))
                {
                    cardInHandToCombine.AddToStat(statName, drawnStatValue); // Requires AddToStat in CardSO.cs
                    Debug.Log($"Combined {currentlyDrawnCard.cardName} stats onto {cardInHandToCombine.cardName}. New '{statName}' value: {cardInHandToCombine.GetStat(statName)}");

                    UpdateHandUI();
                    if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
                    currentlyDrawnCard = null;

                    ShowPopupMessage($"Combined {currentlyDrawnCard.cardName}! {statName} increased on the card in hand.", true);

                    if (hand.Count >= handLimit) { ShowPopupMessage("Hand is full! Drawing Phase ends.", true); ChangeState(GameState.CookingPhase); }
                }
                else { Debug.LogWarning($"Card in hand ({cardInHandToCombine.cardName}) does not have stat '{statName}'. Cannot combine."); ShowPopupMessage($"Cannot combine: Card in hand does not have the required stat.", false); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; }
            }
            else { Debug.LogWarning($"Spice '{currentlyDrawnCard.cardName}' has no defined stat mapping."); ShowPopupMessage($"Cannot combine: '{currentlyDrawnCard.cardName}' does not affect a known stat type.", false); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; }
        }
        else { Debug.LogError("CombineDrawnCard called but no matching card found in hand."); ShowPopupMessage("No matching card in hand to combine!", false); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; }
    }

    // --- Cooking Phase Methods ---

    // New: Method to handle a card being played from the hand.
    // This method is called by the CardDisplayUI script when a card in hand is clicked.
    public void PlayCard(CardSO cardToPlay)
    {
        // Only allow playing cards during the CookingPhase.
        if (currentState != GameState.CookingPhase)
        {
            Debug.LogWarning($"Attempted to play card '{cardToPlay.cardName}' outside of Cooking Phase. Current State: {currentState}");
            ShowPopupMessage("You can only play cards during the Cooking Phase!", true);
            return;
        }
        // Ensure the card clicked is actually in the player's hand.
        if (!hand.Contains(cardToPlay))
        {
            Debug.LogWarning($"Attempted to play card '{cardToPlay.cardName}' not found in hand.");
            ShowPopupMessage("Card not found in hand!", false);
            return;
        }

        // --- Handle Card Playing based on Current Cooking Selection State ---
        switch (currentCookingSelectionState)
        {
            case CookingSelectionState.None:
                // No technique selection in progress, handle direct card plays (Ingredient, Spice, Technique)
                switch (cardToPlay.cardType)
                {
                    case CardSO.CardType.Ingredient:
                    case CardSO.CardType.Spice:
                        // Ingredient and Spice cards are played directly to the dish.
                        Debug.Log($"Playing {cardToPlay.cardType}: {cardToPlay.cardName}");
                        hand.Remove(cardToPlay); // Remove from hand
                        playedCardsThisRound.Add(cardToPlay); // Add to played cards
                        ShowPopupMessage($"Played {cardToPlay.cardName}.", true);
                        UpdateHandUI(); // Update hand display
                                        // TODO: Update played cards UI display (add visual representation of cardToPlay)
                        break;

                    case CardSO.CardType.Tool:
                        // Tool cards cannot be played directly. They are selected after a Technique.
                        Debug.Log($"Attempted to play Tool directly: {cardToPlay.cardName}");
                        ShowPopupMessage("You need to select a Technique first to use a Tool.", true);
                        // Tool remains in hand.
                        break;

                    case CardSO.CardType.Technique:
                        // Technique cards initiate a combination process.
                        Debug.Log($"Initiating Technique: {cardToPlay.cardName}. Select a Tool.");
                        selectedTechnique = cardToPlay; // Store the selected Technique
                        currentCookingSelectionState = CookingSelectionState.WaitingForTool; // Change state
                        ShowPopupMessage($"Selected {cardToPlay.cardName}. Now select a Tool from your hand.", false);
                        // TODO: Update UI prompt text (e.g., show "Select a Tool").
                        // TODO: Make a "Cancel Technique" button visible.
                        SetHandCardInteractivity(false); // Temporarily disable playing other cards directly
                        HighlightSelectableCards(); // Highlight valid Tools
                                                    // Technique remains in hand for now, will be consumed later.
                        break;

                    default:
                        Debug.LogWarning($"Attempted to play card with unhandled type: {cardToPlay.cardType}");
                        ShowPopupMessage("Cannot play this type of card directly.", false);
                        break;
                }
                break; // End case CookingSelectionState.None

            case CookingSelectionState.WaitingForTool:
                // A Technique has been selected, waiting for the player to click a Tool card.
                if (cardToPlay.cardType == CardSO.CardType.Tool)
                {
                    Debug.Log($"Selected Tool: {cardToPlay.cardName}");
                    selectedTool = cardToPlay; // Store the selected Tool
                    currentCookingSelectionState = CookingSelectionState.WaitingForIngredient; // Change state
                    ShowPopupMessage($"Selected Tool: {cardToPlay.cardName}. Now select an Ingredient from your hand.", false);
                    // TODO: Update UI prompt text (e.g., show "Select an Ingredient").
                    HighlightSelectableCards(); // Highlight valid Ingredients
                                                // Tool remains in hand for now.
                }
                else
                {
                    Debug.LogWarning($"Attempted to select non-Tool card ({cardToPlay.cardName}, Type: {cardToPlay.cardType}) when waiting for Tool.");
                    ShowPopupMessage("You must select a Tool card.", false);
                    // Remain in WaitingForTool state.
                }
                break; // End case CookingSelectionState.WaitingForTool

            case CookingSelectionState.WaitingForIngredient:
                // A Technique and Tool have been selected, waiting for the player to click an Ingredient card.
                if (cardToPlay.cardType == CardSO.CardType.Ingredient)
                {
                    Debug.Log($"Selected Ingredient: {cardToPlay.cardName}");
                    selectedIngredient = cardToPlay; // Store the selected Ingredient
                                                     // Have all three pieces? Complete the combination!
                    CompleteTechniqueCombination(); // Call the method to process the combination
                                                    // State will reset to None inside CompleteTechniqueCombination.
                }
                else
                {
                    Debug.LogWarning($"Attempted to select non-Ingredient card ({cardToPlay.cardName}, Type: {cardToPlay.cardType}) when waiting for Ingredient.");
                    ShowPopupMessage("You must select an Ingredient card.", false);
                    // Remain in WaitingForIngredient state.
                }
                break; // End case CookingSelectionState.WaitingForIngredient
        }
    }

    // New: Method to complete the Technique + Tool + Ingredient combination
    void CompleteTechniqueCombination()
    {
        // Ensure all three cards were selected and are still in the player's hand.
        if (selectedTechnique != null && selectedTool != null && selectedIngredient != null &&
            hand.Contains(selectedTechnique) && hand.Contains(selectedTool) && hand.Contains(selectedIngredient))
        {
            Debug.Log($"Completing Technique Combination: {selectedTechnique.cardName} with {selectedTool.cardName} and {selectedIngredient.cardName}");

            // --- Process the Combination ---
            // TODO: Implement the actual effect of the Technique on the Ingredient (e.g., apply stats, change type?).
            // This might involve modifying the Ingredient card's stats or creating a new combined card representation.
            // For now, we just consume the three cards.

            // Remove all three cards from the hand.
            hand.Remove(selectedTechnique);
            hand.Remove(selectedTool);
            hand.Remove(selectedIngredient);

            // Add all three cards to the list of cards played for this round.
            // You might choose to only add the resulting modified Ingredient, or all three for scoring purposes.
            // Let's add all three for now.
            playedCardsThisRound.Add(selectedTechnique);
            playedCardsThisRound.Add(selectedTool);
            playedCardsThisRound.Add(selectedIngredient);

            ShowPopupMessage($"Used {selectedTechnique.cardName} with {selectedTool.cardName} on {selectedIngredient.cardName}!", true);

            // --- Reset Selection State ---
            selectedTechnique = null;
            selectedTool = null;
            selectedIngredient = null;
            currentCookingSelectionState = CookingSelectionState.None; // Reset the state

            // --- UI Updates ---
            UpdateHandUI(); // Update the hand display to show the removed cards
                            // TODO: Update UI to show the newly played cards for the dish.
                            // TODO: Hide Technique selection UI elements (prompt, cancel button).
            SetHandCardInteractivity(true); // Re-enable playing other cards directly (now that selection is done)
            HighlightSelectableCards(); // Clear highlights
        }
        else
        {
            Debug.LogWarning("Attempted to complete Technique combination but cards were missing or not in hand. Cancelling selection.");
            ShowPopupMessage("Error during combination. Please try again.", true);
            // If something went wrong, cancel the whole process.
            CancelTechniqueSelection();
        }
    }

    // New: Method to cancel the current Technique selection process
    public void CancelTechniqueSelection()
    {
        if (currentCookingSelectionState != CookingSelectionState.None)
        {
            Debug.Log("Technique selection cancelled by player or error.");
            selectedTechnique = null;
            selectedTool = null;
            selectedIngredient = null;
            currentCookingSelectionState = CookingSelectionState.None; // Reset the state
            ShowPopupMessage("Technique selection cancelled.", true);
            UpdateHandUI(); // Refresh hand UI (removes highlights)
            // TODO: Hide Technique selection UI elements (prompt, cancel button).
            SetHandCardInteractivity(true); // Re-enable playing other cards directly
            HighlightSelectableCards(); // Clear highlights
        }
        // If state is already None, do nothing.
    }

    // TODO: Implement a method to handle the "Finish Cooking" action.
    // This would likely be called by a UI button available during the CookingPhase.
    public void FinishCooking()
    {
        if (currentState == GameState.CookingPhase && currentCookingSelectionState == CookingSelectionState.None) // Only allow finishing if no technique selection is in progress
        {
            Debug.Log("Finishing Cooking for the round.");
            // TODO: Gather the cards the player used to form the dish from the "played cards" list (playedCardsThisRound).
            // TODO: Trigger the scoring logic (comparing played cards/stats to the current recipe (gameRecipes[currentRound])).
            ChangeState(GameState.ScoringPhase); // Transition to Scoring Phase
        }
        else if (currentState == GameState.CookingPhase && currentCookingSelectionState != CookingSelectionState.None)
        {
            Debug.LogWarning("Attempted to finish cooking while a technique selection is in progress.");
            ShowPopupMessage("Finish your technique selection first, or cancel it.", true);
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
                    // Cards in hand are typically playable only during the CookingPhase,
                    // and interactivity is further managed by SetHandCardInteractivity.
                    // displayScript.SetInteractive(currentState == GameState.CookingPhase); // Old logic
                    displayScript.SetInteractive(true); // Interactivity is managed externally now

                    // Pass a reference to this GameManager to the CardDisplayUI script.
                    // This allows the CardDisplayUI script to call methods back on the GameManager
                    // when the card is interacted with (e.g., clicked to play).
                    displayScript.SetGameManager(this);

                    // TODO: Set highlight based on currentCookingSelectionState and card type
                    // HighlightSelectableCards() will call SetHighlight after this method finishes.
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

        // After updating the UI GameObjects, apply the correct interactivity and highlighting.
        SetHandCardInteractivity(currentState == GameState.CookingPhase && currentCookingSelectionState == CookingSelectionState.None); // Can only play directly if in cooking and no selection
        HighlightSelectableCards(); // Update highlights based on current selection state
    }

    // New: Controls the overall interactivity of cards in the hand GameObjects.
    // This enables/disables the Button or interaction component on the CardDisplayUI.
    void SetHandCardInteractivity(bool isInteractive)
    {
        if (handPanel == null) return;

        CardDisplayUI[] cardUIs = handPanel.GetComponentsInChildren<CardDisplayUI>();
        foreach (var cardUI in cardUIs)
        {
            // This sets the base interactivity. HighlightSelectableCards will override
            // this for specific cards during selection states.
            cardUI.SetInteractive(isInteractive);
        }
    }

    // New: Highlights cards in hand that are currently selectable based on the cooking selection state.
    // Also sets interactivity for selectable cards if in a selection state.
    void HighlightSelectableCards()
    {
        if (handPanel == null) return;

        CardDisplayUI[] cardUIs = handPanel.GetComponentsInChildren<CardDisplayUI>();
        foreach (var cardUI in cardUIs)
        {
            bool isSelectable = false;
            CardSO cardSO = cardUI.GetCardData(); // Get the CardSO data for this UI element

            if (cardSO != null)
            {
                switch (currentCookingSelectionState)
                {
                    case CookingSelectionState.None:
                        // In None state, highlight is off for selection.
                        isSelectable = false;
                        // Cards can be interactive if in CookingPhase. SetHandCardInteractivity handles base interactivity.
                        break;
                    case CookingSelectionState.WaitingForTool:
                        // Highlight Tool cards, and make them interactive (override base interactivity).
                        if (cardSO.cardType == CardSO.CardType.Tool)
                        {
                            isSelectable = true;
                            cardUI.SetInteractive(true); // Make selectable tools interactive
                        }
                        else
                        {
                            // Non-tool cards are not selectable or interactive during WaitingForTool.
                            cardUI.SetInteractive(false);
                        }
                        break;
                    case CookingSelectionState.WaitingForIngredient:
                        // Highlight Ingredient cards, and make them interactive.
                        if (cardSO.cardType == CardSO.CardType.Ingredient)
                        {
                            isSelectable = true;
                            cardUI.SetInteractive(true); // Make selectable ingredients interactive
                        }
                        else
                        {
                            // Non-ingredient cards are not selectable or interactive during WaitingForIngredient.
                            cardUI.SetInteractive(false);
                        }
                        break;
                }
            }
            // Set the visual highlight state for the card UI.
            cardUI.SetHighlight(isSelectable);
        }
        // TODO: Update UI prompt text based on currentCookingSelectionState (e.g., "Select a Tool", "Select an Ingredient").
        // TODO: Control visibility/interactability of "Finish Cooking" and "Cancel Technique" buttons based on state.
    }


    void DisplayRecipes()
    {
        // ... (existing DisplayRecipes code) ...
    }

    void ShowPopupMessage(string message, bool autoHide)
    {
        // ... (existing ShowPopupMessage code) ...
    }

    // TODO: Implement a Coroutine to hide the popup message after a delay.
    // ...

    // --- Recipe Selection ---
    private List<RecipeSO> SelectRandomRecipes(int count)
    {
        // ... (existing SelectRandomRecipes code) ...
        List<RecipeSO> recipes = new List<RecipeSO>();
        if (allAvailableRecipes == null || allAvailableRecipes.Count == 0) { Debug.LogError("No available recipes found!"); return recipes; }
        System.Random rand = new System.Random(); List<RecipeSO> availableForSelection = new List<RecipeSO>(allAvailableRecipes);
        for (int i = 0; i < count; i++) { if (availableForSelection.Count == 0) { Debug.LogWarning($"Could only select {i} recipes, needed {count}."); break; } int randomIndex = rand.Next(availableForSelection.Count); RecipeSO selectedRecipe = availableForSelection[randomIndex]; recipes.Add(selectedRecipe); availableForSelection.RemoveAt(randomIndex); }
        Debug.Log($"Selected {recipes.Count} recipes.");
        return recipes;
    }


    // --- High Score Handling ---
    private int GetHighScore() { return PlayerPrefs.GetInt(HighScoreKey, 0); }
    private void SaveHighScore(int score) { PlayerPrefs.SetInt(HighScoreKey, score); PlayerPrefs.Save(); }
}