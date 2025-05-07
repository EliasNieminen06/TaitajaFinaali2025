// GameManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq; // Required for LINQ operations like .FirstOrDefault(), .Any(), .Where(), .Sum(), .GroupBy()

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

    // Game Status UI
    public TextMeshProUGUI currentRoundText;     // e.g., "Round 1/5"
    public TextMeshProUGUI currentScoreText;     // e.g., "Score: 150"
    public TextMeshProUGUI discardsLeftText;     // e.g., "Discards Left: 3"
    // TODO: Add UI elements related to locking a card (e.g., a prompt, visual indicator)
    // TODO: Add a UI element to display the cards played for the current dish
    // TODO: Add a TextMeshProUGUI or other UI element to show the current cooking selection prompt (e.g., "Select a Tool")
    // TODO: Add a "Cancel Technique" button that is active during Technique selection

    // --- New: UI for Scoring Feedback ---
    public GameObject scoringResultsPanel; // Panel to show score breakdown for the round
    public TextMeshProUGUI roundScoreText; // Text to display points gained this round
    public TextMeshProUGUI scoreBreakdownText; // Text to show how points were awarded (e.g., Ingredients: +15)
    public Button continueButton; // Button to proceed from Scoring/RoundEnd
    // --- End New UI ---


    // TODO: Add UI elements for the Game End screen (gameEndPanel)
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI highScoreText;
    // TODO: Add Buttons for "Play Again" and "Back to Menu" on gameEndPanel

    // New: Button to signal end of cooking phase
    public Button finishCookingButton; // Assign your "bell" button here

    [Header("Game Settings")]
    public int handLimit = 6; // Maximum number of cards a player can hold
    public int maxDiscardsPerRound = 5; // Max discards allowed per round
    private int currentDiscardsThisRound = 0; // Counter for discards used in the current round
    private int totalScore = 0; // Player's cumulative score across all rounds

    // Recipe/Round Management
    private List<RecipeSO> gameRecipes;
    private int currentRound = 0; // Index of the current round (0-based)
    public int totalRounds = 5;

    // Internal game state variables
    private List<CardSO> hand = new List<CardSO>(); // List of CardSO references currently in the player's hand
    private CardSO currentlyDrawnCard; // Holds the CardSO reference of the card currently in the draw popup
    private CardSO lockedCard = null; // Stores the CardSO reference of the card locked for the next round

    // List to store cards played during the current cooking phase
    private List<CardSO> playedCardsThisRound = new List<CardSO>();

    // Variables for Technique Combination Selection
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

    // High Score storage key
    private const string HighScoreKey = "HighScore";

    // Map Spice card names to stat names
    private Dictionary<string, string> spiceStatMapping = new Dictionary<string, string>()
    {
        {"Salt", "Saltiness"},
        {"Honey", "Sweetness"},
        {"Garlic Spice", "Umaminess"},
        {"Pepper", "Spiciness"}
        // Add all your spice card name to stat name mappings here.
    };

    // --- New: Scoring Constants ---
    private const int IngredientAmountScore = 5;
    private const int IngredientTypeScore = 15;
    private const int SpiceAmountScore = 5;
    private const int SpiceTypeScore = 15;
    private const int ToolAmountScore = 5;
    private const int ToolTypeScore = 10;
    private const int TechniqueAmountScore = 5;
    private const int TechniqueTypeScore = 10;
    private const int PerfectDishBonus = 30;
    private const float StatMatchTolerance = 0.1f; // Tolerance for stat matching for perfect dish bonus
    // --- End Scoring Constants ---


    void Awake()
    {
        // --- Initial UI Setup ---
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
        // New: Hide scoring panel initially
        if (scoringResultsPanel != null) scoringResultsPanel.SetActive(false);


        // --- Deck Initialization ---
        if (deck1 != null) deck1.Initialize("Ingredient/Spice Deck", new List<CardSO>(initialDeck1Cards));
        if (deck2 != null) deck2.Initialize("Tool/Technique Deck", new List<CardSO>(initialDeck2Cards));

        Debug.Log("Spice Stat Mapping Initialized.");

        // --- Link Draw Popup UI Buttons ---
        if (keepButton != null) keepButton.onClick.AddListener(KeepDrawnCard);
        if (discardButton != null) discardButton.onClick.AddListener(() => DiscardDrawnCard(false));
        if (combineButton != null) combineButton.onClick.AddListener(CombineDrawnCard);

        // --- Link Other UI Buttons ---
        // TODO: Link Main Menu Start Button to Call StartGame()
        // TODO: Link Game End Screen "Play Again" Button to Call RestartGame()
        // TODO: Link Game End Screen "Back to Menu" Button to Call ReturnToMainMenu()
        // New: Link Finish Cooking Button
        if (finishCookingButton != null) finishCookingButton.onClick.AddListener(FinishCooking);
        // New: Link Continue Button (used after scoring/round end)
        if (continueButton != null) continueButton.onClick.AddListener(ProceedFromRoundEnd);
        // TODO: Link a "Cancel Technique" button to Call CancelTechniqueSelection()
        // TODO: Link any buttons or interactions related to locking a card during RoundEnd.

        ChangeState(GameState.MainMenu);
    }

    // Update is called once per frame
    void Update()
    {
        // ... (existing Update method) ...
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
        if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
        // New: Hide scoring panel when entering most states
        if (scoringResultsPanel != null) scoringResultsPanel.SetActive(false);


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
                currentRound = 0;
                totalScore = 0;
                hand.Clear();
                lockedCard = null;
                currentDiscardsThisRound = 0;
                playedCardsThisRound.Clear(); // Clear played cards from previous game
                CancelTechniqueSelection(); // Ensure selection state is reset at the start of a game

                Debug.Log($"GameSetup: Starting recipe selection. allAvailableRecipes count: {(allAvailableRecipes != null ? allAvailableRecipes.Count : 0)}");

                // Select the random recipes for this game instance using the RecipeSOs.
                gameRecipes = SelectRandomRecipes(totalRounds);

                // Debug.Log: Check if gameRecipes was populated
                if (gameRecipes != null)
                {
                    Debug.Log($"GameSetup: SelectRandomRecipes finished. gameRecipes count: {gameRecipes.Count}");
                }
                else
                {
                    Debug.LogError("GameSetup: gameRecipes is null after SelectRandomRecipes!");
                }


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

                // Debug.Log: Check if DisplayRecipes is called and gameRecipes content
                Debug.Log($"DrawingPhase: Calling DisplayRecipes. gameRecipes count before check: {(gameRecipes != null ? gameRecipes.Count : 0)}");

                // Display the current AND next recipe's information for this round.
                DisplayRecipes();

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
                                                 // Hide CookingPhase specific UI like "Finish Cooking" button
                if (finishCookingButton != null) finishCookingButton.gameObject.SetActive(false);
                // TODO: Hide Technique selection UI elements (prompt, cancel button).
                HighlightSelectableCards(); // Ensure no highlights are active
                break;

            case GameState.CookingPhase:
                Debug.Log("Entered Cooking Phase State");
                if (gameUIPanel != null) gameUIPanel.SetActive(true);
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);

                playedCardsThisRound.Clear(); // Clear played cards list for this round
                CancelTechniqueSelection(); // Reset selection state when entering cooking

                UpdateHandUI(); // Refresh hand UI. Cards in hand should now be interactive (controlled by SetHandCardInteractivity below).

                // TODO: Ensure Deck Draw buttons and Draw Popup UI are NOT interactable.
                // Ensure "Finish Cooking" button IS interactable.
                if (finishCookingButton != null) finishCookingButton.gameObject.SetActive(true);
                // TODO: Hide Technique selection UI elements initially (prompt, cancel button).

                // Set initial interactivity for cards in hand (playable directly unless a technique is in progress)
                SetHandCardInteractivity(true);
                // Initial highlight state (no cards highlighted when starting cooking)
                HighlightSelectableCards(); // This will call SetHighlight(false) on all cards
                break;

            case GameState.ScoringPhase:
                Debug.Log("Entered Scoring Phase State");
                if (gameUIPanel != null) gameUIPanel.SetActive(true); // Game UI might remain active to show played cards/score
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
                if (scoringResultsPanel != null) scoringResultsPanel.SetActive(true); // Show scoring results panel

                SetHandCardInteractivity(false); // Cards in hand cannot be played during scoring
                CancelTechniqueSelection(); // Reset selection state

                // --- Perform Scoring for the Round ---
                int roundScore = CalculateRoundScore();
                totalScore += roundScore;
                Debug.Log($"Round {currentRound + 1} Score: {roundScore}. Total Score: {totalScore}");

                // --- Display Scoring Results UI ---
                if (roundScoreText != null) roundScoreText.text = $"Round {currentRound + 1} Score: {roundScore}";
                // scoreBreakdownText is populated within CalculateRoundScore

                // Update total score display
                if (currentScoreText != null) currentScoreText.text = $"Score: {totalScore}";


                // Transition to RoundEnd after a delay or when a "Continue" button is clicked.
                if (continueButton != null) continueButton.gameObject.SetActive(true); // Make continue button visible

                break;

            case GameState.RoundEnd:
                Debug.Log("Entered Round End State");
                if (gameUIPanel != null) gameUIPanel.SetActive(true); // Game UI might remain active
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
                // Scoring results panel might still be visible or transition away

                SetHandCardInteractivity(false); // Cards in hand cannot be played during round end
                CancelTechniqueSelection(); // Reset selection state

                // --- Prepare for Next Round or Game End ---
                // Round increment, cleanup, and transition logic are now primarily in ProceedFromRoundEnd

                // The continue button (made visible in ScoringPhase) will now handle the transition from RoundEnd.

                break;

            case GameState.GameEnd:
                Debug.Log("Entered Game End State. Final Score: " + totalScore);
                if (gameEndPanel != null) gameEndPanel.SetActive(true);
                if (gameUIPanel != null) gameUIPanel.SetActive(false);
                if (scoringResultsPanel != null) scoringResultsPanel.SetActive(false); // Hide scoring results

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
                if (continueButton != null) continueButton.gameObject.SetActive(false); // Hide continue button
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

    // Method to handle a card being played from the hand.
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

    // Called by a UI button available during the CookingPhase (your "bell" button).
    public void FinishCooking()
    {
        if (currentState == GameState.CookingPhase && currentCookingSelectionState == CookingSelectionState.None) // Only allow finishing if no technique selection is in progress
        {
            Debug.Log("Finishing Cooking for the round.");
            // TODO: Gather the cards the player used to form the dish from the "played cards" list (playedCardsThisRound).
            // This is already done as cards are added to playedCardsThisRound when played.
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


    // --- Scoring Phase Methods ---

    // New: Calculates the score for the current round based on played cards and the recipe.
    int CalculateRoundScore()
    {
        int roundScore = 0;
        string breakdownText = "Score Breakdown:\n"; // To build the text for scoreBreakdownText UI

        if (gameRecipes == null || gameRecipes.Count <= currentRound)
        {
            Debug.LogError($"Cannot calculate score: Current recipe for round {currentRound} is missing.");
            breakdownText += "Error: No recipe found for scoring.";
            if (scoreBreakdownText != null) scoreBreakdownText.text = breakdownText;
            return 0; // Cannot score without a recipe
        }

        RecipeSO currentRecipe = gameRecipes[currentRound];

        // --- Score Calculation Logic ---

        // 1. Count played cards by type
        var playedIngredients = playedCardsThisRound.Where(c => c.cardType == CardSO.CardType.Ingredient).ToList();
        var playedSpices = playedCardsThisRound.Where(c => c.cardType == CardSO.CardType.Spice).ToList();
        var playedTools = playedCardsThisRound.Where(c => c.cardType == CardSO.CardType.Tool).ToList();
        var playedTechniques = playedCardsThisRound.Where(c => c.cardType == CardSO.CardType.Technique).ToList();

        // 2. Score based on AMOUNT of each type
        if (currentRecipe.requiredIngredients != null && playedIngredients.Count == currentRecipe.requiredIngredients.Count) // Added null check
        {
            roundScore += IngredientAmountScore;
            breakdownText += $"Ingredients Amount ({playedIngredients.Count}/{currentRecipe.requiredIngredients.Count}): +{IngredientAmountScore}\n";
        }
        else if (currentRecipe.requiredIngredients != null) // Provide feedback even if count doesn't match
        {
            breakdownText += $"Ingredients Amount ({playedIngredients.Count}/{currentRecipe.requiredIngredients.Count}): +0\n";
        }
        else // Handle case where recipe has no required ingredients listed
        {
            breakdownText += $"Ingredients Amount ({playedIngredients.Count}/0): +0\n";
        }


        int requiredSpiceCount = GetRequiredSpiceCount(currentRecipe);
        if (playedSpices.Count == requiredSpiceCount)
        {
            roundScore += SpiceAmountScore;
            breakdownText += $"Spices Amount ({playedSpices.Count}/{requiredSpiceCount}): +{SpiceAmountScore}\n";
        }
        else
        {
            breakdownText += $"Spices Amount ({playedSpices.Count}/{requiredSpiceCount}): +0\n";
        }

        if (currentRecipe.requiredTools != null && playedTools.Count == currentRecipe.requiredTools.Count) // Added null check
        {
            roundScore += ToolAmountScore;
            breakdownText += $"Tools Amount ({playedTools.Count}/{currentRecipe.requiredTools.Count}): +{ToolAmountScore}\n";
        }
        else if (currentRecipe.requiredTools != null)
        {
            breakdownText += $"Tools Amount ({playedTools.Count}/{currentRecipe.requiredTools.Count}): +0\n";
        }
        else
        {
            breakdownText += $"Tools Amount ({playedTools.Count}/0): +0\n";
        }

        if (currentRecipe.requiredTechniques != null && playedTechniques.Count == currentRecipe.requiredTechniques.Count) // Added null check
        {
            roundScore += TechniqueAmountScore;
            breakdownText += $"Techniques Amount ({playedTechniques.Count}/{currentRecipe.requiredTechniques.Count}): +{TechniqueAmountScore}\n";
        }
        else if (currentRecipe.requiredTechniques != null)
        {
            breakdownText += $"Techniques Amount ({playedTechniques.Count}/{currentRecipe.requiredTechniques.Count}): +0\n";
        }
        else
        {
            breakdownText += $"Techniques Amount ({playedTechniques.Count}/0): +0\n";
        }


        // 3. Score based on RIGHT TYPE of each required card
        int ingredientTypeMatches = CountMatchingRequiredCards(currentRecipe.requiredIngredients, playedIngredients);
        roundScore += ingredientTypeMatches * IngredientTypeScore;
        breakdownText += $"Right Ingredients ({ingredientTypeMatches}/{(currentRecipe.requiredIngredients != null ? currentRecipe.requiredIngredients.Count : 0)}): +{ingredientTypeMatches * IngredientTypeScore}\n";


        // Spices: Assuming RecipeSO lists required spices if "Right Spices" is scored this way.
        // If your RecipeSO *does not* have a requiredSpices list, you might score this differently (e.g., just using any spice).
        // Adjust this part based on your RecipeSO structure.
        // For now, assuming spices *are* in requiredIngredients list for type matching
        int spiceTypeMatches = CountMatchingRequiredCards(currentRecipe.requiredIngredients.Where(c => c.cardType == CardSO.CardType.Spice).ToList(), playedSpices);
        roundScore += spiceTypeMatches * SpiceTypeScore;
        breakdownText += $"Right Spices ({spiceTypeMatches}/{requiredSpiceCount}): +{spiceTypeMatches * SpiceTypeScore}\n";


        int toolTypeMatches = CountMatchingRequiredCards(currentRecipe.requiredTools, playedTools);
        roundScore += toolTypeMatches * ToolTypeScore;
        breakdownText += $"Right Tools ({toolTypeMatches}/{(currentRecipe.requiredTools != null ? currentRecipe.requiredTools.Count : 0)}): +{toolTypeMatches * ToolTypeScore}\n";

        int techniqueTypeMatches = CountMatchingRequiredCards(currentRecipe.requiredTechniques, playedTechniques);
        roundScore += techniqueTypeMatches * TechniqueTypeScore;
        breakdownText += $"Right Techniques ({techniqueTypeMatches}/{(currentRecipe.requiredTechniques != null ? currentRecipe.requiredTechniques.Count : 0)}): +{techniqueTypeMatches * TechniqueTypeScore}\n";


        // 4. Perfect Dish Bonus (based on combined stats matching target stats)
        if (currentRecipe.targetStats != null && currentRecipe.targetStats.Count > 0)
        {
            // Calculate the combined stats of the played dish
            Dictionary<string, float> combinedStats = GetCombinedPlayedStats(playedCardsThisRound);

            // Compare combined stats to target stats
            bool perfectMatch = true;
            // float totalStatDifference = 0f; // If implementing partial score based on difference

            // Check if every required target stat is present and close in the combined stats
            foreach (var targetStat in currentRecipe.targetStats)
            {
                if (combinedStats.TryGetValue(targetStat.name, out float playedValue))
                {
                    // Calculate the absolute difference for this stat
                    float difference = Mathf.Abs(playedValue - targetStat.targetValue);
                    // totalStatDifference += difference; // Accumulate total difference

                    // Check if this specific stat is within tolerance for a "perfect" match
                    if (difference > StatMatchTolerance)
                    {
                        perfectMatch = false; // If any stat is outside tolerance, it's not a perfect match
                    }
                }
                else
                {
                    // If a required target stat is completely missing from played cards, it's not perfect.
                    perfectMatch = false;
                    // totalStatDifference += targetStat.targetValue; // Add the target value as a penalty for missing stat
                }
            }

            // Check for extra stats in the played dish not required by the recipe
            // This makes the "Perfect Dish" more strict. Remove this loop if extra stats are okay.
            foreach (var playedStat in combinedStats)
            {
                if (!currentRecipe.targetStats.Any(ts => ts.name == playedStat.Key))
                {
                    // Found a played stat that is not in the target stats
                    perfectMatch = false;
                    // Optional: Add a penalty for extra stats
                    // totalStatDifference += playedStat.Value;
                }
            }


            // Award Perfect Dish bonus if all stats are within tolerance (and potentially no extra stats)
            if (perfectMatch)
            {
                roundScore += PerfectDishBonus;
                breakdownText += $"Perfect Dish Bonus: +{PerfectDishBonus}\n";
                Debug.Log("Achieved Perfect Dish!");
            }
            else
            {
                // TODO: Implement partial scoring based on totalStatDifference if desired,
                // instead of just an all-or-nothing bonus.
                // For now, just indicate it wasn't a perfect match.
                breakdownText += "Perfect Dish Bonus: +0 (Stats did not match perfectly)\n";
                // Debug.Log($"Dish not perfect. Total stat difference: {totalStatDifference}");
            }
        }
        else
        {
            // If the recipe has no target stats, perhaps the Perfect Dish bonus isn't applicable,
            // or it's automatically awarded? Based on your Scoring.txt, it seems tied to stats.
            breakdownText += "Perfect Dish Bonus: N/A (Recipe has no target stats)\n";
        }


        // Update the score breakdown UI text
        if (scoreBreakdownText != null) scoreBreakdownText.text = breakdownText;


        return roundScore;
    }

    // Helper method to count how many required cards of a specific list match the played cards of that type.
    int CountMatchingRequiredCards(List<CardSO> requiredCards, List<CardSO> playedCards)
    {
        if (requiredCards == null || playedCards == null) return 0; // Handle null lists

        int matches = 0;
        // We use copies of the lists so we can remove matched cards and handle duplicates correctly.
        List<CardSO> playedCopy = new List<CardSO>(playedCards);
        List<CardSO> requiredCopy = new List<CardSO>(requiredCards);

        foreach (var requiredCard in requiredCopy)
        {
            // Find if there is a matching card in the played copy
            CardSO matchingPlayedCard = playedCopy.FirstOrDefault(pc => pc.cardName == requiredCard.cardName);
            if (matchingPlayedCard != null)
            {
                matches++;
                playedCopy.Remove(matchingPlayedCard); // Remove the matched played card so it can't match another requirement
            }
        }
        return matches;
    }

    // Helper method to get the total count of required Spice cards (assuming they are listed in RecipeSO)
    // If Spice requirements are different (e.g., just "any spice"), adjust this.
    int GetRequiredSpiceCount(RecipeSO recipe)
    {
        if (recipe == null || recipe.requiredIngredients == null) return 0; // Handle nulls
                                                                            // Assuming spices are listed in requiredIngredients and marked as Spice type:
        return recipe.requiredIngredients.Count(c => c.cardType == CardSO.CardType.Spice);
        // If they are in a separate list in RecipeSO:
        // return recipe.requiredSpices.Count; // You'd need a requiredSpices list in RecipeSO
    }

    // Helper method to get the combined stats from a list of played cards.
    Dictionary<string, float> GetCombinedPlayedStats(List<CardSO> playedCards)
    {
        Dictionary<string, float> combinedStats = new Dictionary<string, float>();
        if (playedCards == null) return combinedStats; // Return empty if list is null

        foreach (var card in playedCards)
        {
            // Note: If a Technique modifies an Ingredient's stats, ensure the Ingredient
            // card instance in playedCardsThisRound reflects those modifications before this is called.
            // Currently, the Technique just adds the original Ingredient/Tool/Technique CardSOs.
            // You'll need to update CompleteTechniqueCombination to handle stat modifications.

            var cardStats = card.GetStatsDictionary();
            if (cardStats != null) // Check if the card has stats
            {
                foreach (var stat in cardStats)
                {
                    if (combinedStats.ContainsKey(stat.Key))
                    {
                        combinedStats[stat.Key] += stat.Value; // Add to existing stat
                    }
                    else
                    {
                        combinedStats[stat.Key] = stat.Value; // Add new stat
                    }
                }
            }
        }
        return combinedStats;
    }


    // --- Round End Methods ---

    // Method called by the Continue button after Scoring/RoundEnd
    public void ProceedFromRoundEnd()
    {
        if (currentState == GameState.ScoringPhase || currentState == GameState.RoundEnd)
        {
            // Check for game end *before* transitioning
            if (currentRound < totalRounds)
            {
                // --- Round End Cleanup ---
                // Clear the hand (except for the locked card) and played cards list for the next round.
                if (lockedCard != null)
                {
                    // Remove the locked card instance from the hand list before clearing the rest.
                    // This ensures the correct instance is preserved if there are duplicates.
                    CardSO lockedCardInHand = hand.FirstOrDefault(c => c == lockedCard); // Find the exact instance
                    if (lockedCardInHand != null)
                    {
                        hand.Remove(lockedCardInHand);
                    }
                }
                hand.Clear(); // Clear remaining cards in hand
                if (lockedCard != null)
                {
                    hand.Add(lockedCard); // Add the locked card back to the hand for the next round
                    Debug.Log($"Locked card '{lockedCard.cardName}' carried over to Round {currentRound + 1}.");
                    // Note: If locking UI involves moving the card UI, UpdateHandUI will rebuild it.
                }
                playedCardsThisRound.Clear(); // Clear cards played in the just-finished round
                currentDiscardsThisRound = 0; // Reset discards for the new round


                UpdateHandUI(); // Refresh hand display to show only the locked card (if any)
                currentRound++; // Increment round counter HERE after cleanup

                DisplayRecipes(); // Update recipe display for the next round using the INCREMENTED currentRound
                                  // Update other game status UI
                if (currentRoundText != null) currentRoundText.text = $"Round: {currentRound + 1}/{totalRounds}"; // Update round text here
                if (discardsLeftText != null) discardsLeftText.text = $"Discards Left: {maxDiscardsPerRound - currentDiscardsThisRound}"; // Update discards text here


                // Transition to the next round's drawing phase.
                ChangeState(GameState.DrawingPhase);
            }
            else
            {
                // If all rounds are done, the game is over.
                Debug.Log("All rounds completed. Ending game.");
                ChangeState(GameState.GameEnd);
            }
            // Hide the continue button after it's clicked
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            // Hide scoring results panel when proceeding
            if (scoringResultsPanel != null) scoringResultsPanel.SetActive(false);
        }
    }


    // TODO: Implement a method to handle locking a card during RoundEnd.
    // This method would be called by a UI interaction (e.g., clicking a card in hand after scoring).
    // public void LockCardForNextRound(CardSO cardToLock)
    // {
    //    // Ensure the state is RoundEnd, the card is in hand, and no card is already locked for the next round.
    //    if (currentState == GameState.RoundEnd && hand.Contains(cardToLock) && lockedCard == null)
    //    {
    //         lockedCard = cardToLock; // Store the card to be carried over to the next round
    //         // Don't remove from hand immediately if the locking UI keeps it visible until proceeding.
    //         // The cleanup logic in ProceedFromRoundEnd will handle clearing the hand and re-adding the locked card.
    //         ShowPopupMessage($"Selected {cardToLock.cardName} to lock for the next round.", true);
    //         // TODO: Visually indicate the selected locked card in hand.
    //         // TODO: Disable further card locking options for this round.
    //    }
    //     else if (currentState == GameState.RoundEnd && lockedCard != null)
    //    {
    //         ShowPopupMessage("You can only lock one card per round.", true);
    //    }
    //     else if (currentState == GameState.RoundEnd && !hand.Contains(cardToLock))
    //    {
    //         Debug.LogWarning($"Attempted to lock card '{cardToLock.cardName}' not found in hand.");
    //         ShowPopupMessage("Card not found in hand!", false);
    //    }
    //     else
    //    {
    //         Debug.LogWarning("Attempted to lock card outside of Round End state.");
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

                    // Pass a reference to this GameManager to the CardDisplayUI script.
                    // This allows the CardDisplayUI script to call methods back on the GameManager
                    // when the card is interacted with (e.g., clicked to play).
                    displayScript.SetGameManager(this);

                    // Interactivity and highlighting are managed by HighlightSelectableCards and SetHandCardInteractivity.
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
        HighlightSelectableCards(); // This method now handles both highlighting AND interactivity for playable cards
    }

    // Controls the overall interactivity of cards in the hand GameObjects.
    // This enables/disables the Button or interaction component on the CardDisplayUI.
    // NOTE: This method is now mainly called by HighlightSelectableCards
    // to selectively enable interactive cards during selection states.
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

    // Highlights cards in hand that are currently selectable based on the cooking selection state.
    // Also sets interactivity for selectable cards if in a selection state, and disables others.
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
                        // In None state:
                        // - No cards are "selectable" for combination highlighting.
                        isSelectable = false;
                        // - Interactivity depends only on whether it's the Cooking Phase.
                        cardUI.SetInteractive(currentState == GameState.CookingPhase);
                        break;

                    case CookingSelectionState.WaitingForTool:
                        // In WaitingForTool state:
                        // - Tool cards are selectable and interactive.
                        // - Other cards are not selectable and NOT interactive.
                        if (cardSO.cardType == CardSO.CardType.Tool)
                        {
                            isSelectable = true;
                            cardUI.SetInteractive(true); // Make selectable tools interactive
                        }
                        else
                        {
                            isSelectable = false;
                            cardUI.SetInteractive(false); // Disable other cards
                        }
                        break;

                    case CookingSelectionState.WaitingForIngredient:
                        // In WaitingForIngredient state:
                        // - Ingredient cards are selectable and interactive.
                        // - Other cards are not selectable and NOT interactive.
                        if (cardSO.cardType == CardSO.CardType.Ingredient)
                        {
                            isSelectable = true;
                            cardUI.SetInteractive(true); // Make selectable ingredients interactive
                        }
                        else
                        {
                            isSelectable = false;
                            cardUI.SetInteractive(false); // Disable other cards
                        }
                        break;
                }
            }
            // Set the visual highlight state for the card UI.
            cardUI.SetHighlight(isSelectable);
        }
        // TODO: Update UI prompt text based on currentCookingSelectionState (e.g., "Select a Tool", "Select an Ingredient").
        // TODO: Control visibility/interactability of "Finish Cooking" and "Cancel Technique" buttons based on state.
        // Example: "Finish Cooking" button active ONLY if currentState == CookingPhase AND currentCookingSelectionState == None
        // Example: "Cancel Technique" button active ONLY if currentCookingSelectionState != None
        if (finishCookingButton != null)
        {
            finishCookingButton.interactable = (currentState == GameState.CookingPhase && currentCookingSelectionState == CookingSelectionState.None);
        }
        // TODO: Implement logic for Cancel button
    }


    // Displays the information of the current AND next recipes in the designated UI elements.
    void DisplayRecipes()
    {
        // Debug.Log: Indicate the method was entered
        Debug.Log("DisplayRecipes method entered.");

        // --- Display Current Recipe ---
        // Ensure there are recipes selected for this game and the current round index is valid.
        if (gameRecipes == null || gameRecipes.Count <= currentRound)
        {
            Debug.LogError($"Cannot display current recipe for round {currentRound}. gameRecipes list is null or has only {gameRecipes?.Count ?? 0} recipes. Returning from DisplayRecipes.");
            // Clear current recipe UI elements if no recipe is available for the current round.
            if (currentRecipeNameText != null) currentRecipeNameText.text = "Error: No Recipe";
            if (currentRecipeRequirementsAndStatsText != null) currentRecipeRequirementsAndStatsText.text = "";
            // Also clear next recipe UI if the current one is missing
            if (nextRecipeNameText != null) nextRecipeNameText.text = "Next Dish: N/A";
            if (nextRecipeRequirementsAndStatsText != null) nextRecipeRequirementsAndStatsText.text = "";
            return; // Early exit point
        }

        RecipeSO currentRecipe = gameRecipes[currentRound];
        // Update the Current Recipe Name UI element.
        if (currentRecipeNameText != null) currentRecipeNameText.text = currentRecipe.recipeName; // Just the name

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
            foreach (var card in currentRecipe.requiredTechniques) currentReqAndStatsText += $"- {card.cardName}\n"; // Corrected line
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

        Debug.Log($"Successfully displayed current recipe: {gameRecipes[currentRound].recipeName}");


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
                foreach (var card in nextRecipe.requiredTechniques) nextReqAndStatsText += $"- {card.cardName}\n"; // Corrected line
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

            Debug.Log($"Successfully displayed next recipe: {gameRecipes[currentRound + 1].recipeName}");

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
        // Debug.Log: Indicate the method was entered
        Debug.Log("SelectRandomRecipes method entered.");

        // Ensure there are recipes available to select from.
        if (allAvailableRecipes == null || allAvailableRecipes.Count == 0)
        {
            Debug.LogError("No available recipes found in the 'allAvailableRecipes' list! Cannot select recipes. Returning empty list."); // More descriptive error
            return recipes; // Return an empty list
        }

        // Debug.Log: Show how many recipes are available to select from
        Debug.Log($"Selecting {count} recipes from {allAvailableRecipes.Count} available.");


        System.Random rand = new System.Random();
        List<RecipeSO> availableForSelection = new List<RecipeSO>(allAvailableRecipes);

        for (int i = 0; i < count; i++)
        {
            if (availableForSelection.Count == 0)
            {
                Debug.LogWarning($"Could only select {i} unique recipes, needed {count}. Not enough unique recipes available.");
                break;
            }
            int randomIndex = rand.Next(availableForSelection.Count);
            RecipeSO selectedRecipe = availableForSelection[randomIndex];
            recipes.Add(selectedRecipe);
            availableForSelection.RemoveAt(randomIndex);
            Debug.Log($"Selected recipe {i + 1}: {selectedRecipe.recipeName}"); // Debug.Log for each selected recipe
        }

        Debug.Log($"SelectRandomRecipes finished. Returning {recipes.Count} recipes."); // Debug.Log
        return recipes;
    }


    // --- High Score Handling ---
    private int GetHighScore() { return PlayerPrefs.GetInt(HighScoreKey, 0); }
    private void SaveHighScore(int score) { PlayerPrefs.SetInt(HighScoreKey, score); PlayerPrefs.Save(); }
}