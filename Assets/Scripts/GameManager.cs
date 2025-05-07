// GameManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System.Collections; // Added for Coroutines
using UnityEngine.EventSystems; // Added for potential event handling in CardDisplayUI (explained below)
using UnityEngine.SceneManagement; // Required for SceneManager.LoadScene

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        MainMenu,
        GameSetup,
        DrawingPhase,
        CookingPhase,
        ScoringPhase,
        RoundEnd,
        GameEnd
    }

    [Header("Game State")]
    private GameState currentState;

    [Header("Card Definitions")]
    public List<CardSO> allCardDefinitions;

    [Header("Decks")]
    public Deck deck1;
    public Deck deck2;

    [Header("Starting Deck Contents (Assign CardSO assets)")]
    public List<CardSO> initialDeck1Cards;
    public List<CardSO> initialDeck2Cards;

    [Header("Recipe Definitions")]
    public List<RecipeSO> allAvailableRecipes;

    [Header("UI References")]
    public GameObject mainMenuPanel;
    public GameObject gameUIPanel;
    public GameObject gameEndPanel;

    public GameObject handPanel;
    public GameObject playedCardsPanel;

    public GameObject cardUIPrefab;

    public GameObject drawPopupPanel;
    public Image popupCardArtwork;
    public TextMeshProUGUI popupCardName;
    public TextMeshProUGUI popupCardDescription;
    public Button keepButton;
    public Button discardButton;
    public Button combineButton;
    public TextMeshProUGUI popupMessageText; // Keep this for the draw popup specifically if needed, or remove if Announcements handles all popups

    public TextMeshProUGUI currentRecipeNameText;
    public TextMeshProUGUI currentRecipeRequirementsAndStatsText;

    public TextMeshProUGUI nextRecipeNameText;
    public TextMeshProUGUI nextRecipeRequirementsAndStatsText;

    public TextMeshProUGUI currentRoundText;
    public TextMeshProUGUI currentScoreText;
    public TextMeshProUGUI discardsLeftText;

    public GameObject scoringResultsPanel;
    public TextMeshProUGUI roundScoreText;
    public TextMeshProUGUI scoreBreakdownText;
    public Button continueButton;

    public Button cancelTechniqueButton;

    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI highScoreText;
    // Removed Play Again Button reference
    // public Button playAgainButton;
    public Button backToMenuButton;

    public Button finishCookingButton;

    [Header("Game Settings")]
    public int handLimit = 6;
    public int maxDiscardsPerRound = 5;
    private int currentDiscardsThisRound = 0;
    private int totalScore = 0;

    private List<RecipeSO> gameRecipes;
    private int currentRound = 0;
    public int totalRounds = 5;

    private List<CardSO> hand = new List<CardSO>();
    private CardSO currentlyDrawnCard;
    private CardSO lockedCard = null;

    private List<CardSO> playedCardsThisRound = new List<CardSO>();

    private CardSO selectedTechnique = null;
    private CardSO selectedTool = null;
    private CardSO selectedIngredient = null;

    private enum CookingSelectionState
    {
        None,
        WaitingForTool,
        WaitingForIngredient
    }
    private CookingSelectionState currentCookingSelectionState = CookingSelectionState.None;

    private const string HighScoreKey = "HighScore";

    private Dictionary<string, string> spiceStatMapping = new Dictionary<string, string>()
    {
        {"Salt", "Saltiness"},
        {"Honey", "Sweetness"},
        {"Garlic Spice", "Umaminess"},
        {"Pepper", "Spiciness"}
    };

    private const int IngredientAmountScore = 0;
    private const int IngredientTypeScore = 15;
    private const int SpiceAmountScore = 0;
    private const int SpiceTypeScore = 15;
    private const int ToolAmountScore = 0;
    private const int ToolTypeScore = 10;
    private const int TechniqueAmountScore = 0;
    private const int TechniqueTypeScore = 10;
    private const int PerfectDishBonus = 25;
    private const float StatMatchTolerance = 0.1f;

    // --- Sound Effect Variables ---
    [Header("Sound Effects")]
    public AudioClip deckClickSound;
    public AudioClip buttonClickSound;
    public AudioClip newHighScoreSound;
    public AudioClip finishCookingSound;
    // Sound for clicking a card and mouse over a card should be handled in CardDisplayUI.cs
    // public AudioClip cardClickSound; // Declare in CardDisplayUI
    // public AudioClip cardMouseOverSound; // Declare in CardDisplayUI

    private AudioSource audioSource;
    // --- End Sound Effect Variables ---


    void Awake()
    {
        // --- Sound Effect Setup ---
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource component not found on GameManager GameObject. Sound effects will not play.");
        }
        // --- End Sound Effect Setup ---


        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
        if (scoringResultsPanel != null) scoringResultsPanel.SetActive(false);
        if (cancelTechniqueButton != null) cancelTechniqueButton.gameObject.SetActive(false);

        // Decks are initialized here on Awake, they will be re-initialized on game restart
        if (deck1 != null) deck1.Initialize("Ingredient/Spice Deck", new List<CardSO>(initialDeck1Cards));
        if (deck2 != null) deck2.Initialize("Tool/Technique Deck", new List<CardSO>(initialDeck2Cards));

        Debug.Log("Spice Stat Mapping Initialized.");

        // Button click sounds can be added here or within the called methods
        if (keepButton != null) keepButton.onClick.AddListener(KeepDrawnCard);
        if (discardButton != null) discardButton.onClick.AddListener(() => DiscardDrawnCard(false));
        if (combineButton != null) combineButton.onClick.AddListener(CombineDrawnCard);

        if (finishCookingButton != null) finishCookingButton.onClick.AddListener(FinishCooking);
        if (continueButton != null) continueButton.onClick.AddListener(ProceedFromRoundEnd);
        if (cancelTechniqueButton != null) cancelTechniqueButton.onClick.AddListener(CancelTechniqueSelection);
        // Removed Play Again Button listener
        // if (playAgainButton != null) playAgainButton.onClick.AddListener(RestartGame);
        if (backToMenuButton != null) backToMenuButton.onClick.AddListener(ReturnToMainMenu);

        ChangeState(GameState.MainMenu);
    }

    void Update()
    {
        // ... (existing Update method) ...
    }

    void ChangeState(GameState newState)
    {
        if (currentState == newState)
        {
            Debug.LogWarning($"Attempted to change to state {newState}, but already in that state.");
            return;
        }

        Debug.Log($"Changing state from {currentState} to {newState}");
        currentState = newState;
        OnStateEnter(newState);
    }

    void OnStateEnter(GameState state)
    {
        // Deactivate all major panels initially
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
        if (scoringResultsPanel != null) scoringResultsPanel.SetActive(false);

        // Always cancel technique selection on state change for safety
        CancelTechniqueSelection();

        // Ensure key UI text elements are active when gameUIPanel is active
        bool gameUIPanelActive = false;
        if (gameUIPanel != null)
        {
            gameUIPanelActive = true; // Assume it will be active in game states
            if (state != GameState.MainMenu && state != GameState.GameEnd)
            {
                gameUIPanel.SetActive(true);
                if (!gameUIPanel.activeSelf) Debug.LogWarning("gameUIPanel failed to activate.");
            }
        }
        else Debug.LogWarning("gameUIPanel is not assigned.");


        // Explicitly activate parent GameObjects of key TextMeshProUGUI elements if they are assigned
        if (gameUIPanelActive)
        {
            if (currentRecipeNameText != null && currentRecipeNameText.gameObject.transform.parent != null)
            {
                currentRecipeNameText.gameObject.transform.parent.gameObject.SetActive(true);
                if (!currentRecipeNameText.gameObject.transform.parent.gameObject.activeSelf) Debug.LogWarning("currentRecipeNameText parent failed to activate.");
            }
            else if (currentRecipeNameText == null) Debug.LogWarning("currentRecipeNameText is not assigned.");

            if (currentRecipeRequirementsAndStatsText != null && currentRecipeRequirementsAndStatsText.gameObject.transform.parent != null)
            {
                currentRecipeRequirementsAndStatsText.gameObject.transform.parent.gameObject.SetActive(true);
                if (!currentRecipeRequirementsAndStatsText.gameObject.transform.parent.gameObject.activeSelf) Debug.LogWarning("currentRecipeRequirementsAndStatsText parent failed to activate.");
            }
            else if (currentRecipeRequirementsAndStatsText == null) Debug.LogWarning("currentRecipeRequirementsAndStatsText is not assigned.");

            if (nextRecipeNameText != null && nextRecipeNameText.gameObject.transform.parent != null)
            {
                nextRecipeNameText.gameObject.transform.parent.gameObject.SetActive(true);
                if (!nextRecipeNameText.gameObject.transform.parent.gameObject.activeSelf) Debug.LogWarning("nextRecipeNameText parent failed to activate.");
            }
            else if (nextRecipeNameText == null) Debug.LogWarning("nextRecipeNameText is not assigned.");

            if (nextRecipeRequirementsAndStatsText != null && nextRecipeRequirementsAndStatsText.gameObject.transform.parent != null)
            {
                nextRecipeRequirementsAndStatsText.gameObject.transform.parent.gameObject.SetActive(true);
                if (!nextRecipeRequirementsAndStatsText.gameObject.transform.parent.gameObject.activeSelf) Debug.LogWarning("nextRecipeRequirementsAndStatsText parent failed to activate.");
            }
            else if (nextRecipeRequirementsAndStatsText == null) Debug.LogWarning("nextRecipeRequirementsAndStatsText is not assigned.");

            if (currentRoundText != null && currentRoundText.gameObject.transform.parent != null)
            {
                currentRoundText.gameObject.transform.parent.gameObject.SetActive(true);
                if (!currentRoundText.gameObject.transform.parent.gameObject.activeSelf) Debug.LogWarning("currentRoundText parent failed to activate.");
            }
            else if (currentRoundText == null) Debug.LogWarning("currentRoundText is not assigned.");

            if (currentScoreText != null && currentScoreText.gameObject.transform.parent != null)
            {
                currentScoreText.gameObject.transform.parent.gameObject.SetActive(true);
                if (!currentScoreText.gameObject.transform.parent.gameObject.activeSelf) Debug.LogWarning("currentScoreText parent failed to activate.");
            }
            else if (currentScoreText == null) Debug.LogWarning("currentScoreText is not assigned.");

            if (discardsLeftText != null && discardsLeftText.gameObject.transform.parent != null)
            {
                discardsLeftText.gameObject.transform.parent.gameObject.SetActive(true);
                if (!discardsLeftText.gameObject.transform.parent.gameObject.activeSelf) Debug.LogWarning("discardsLeftText parent failed to activate.");
            }
            else if (discardsLeftText == null) Debug.LogWarning("discardsLeftText is not assigned.");

        }


        switch (state)
        {
            case GameState.MainMenu:
                Debug.Log("Entered Main Menu State");
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                // Play Main Menu music or ambient sound if applicable
                break;

            case GameState.GameSetup:
                Debug.Log("Entered Game Setup State");
                // Core game state variables are now reset in RestartGame or StartGame before this state is entered.

                // Select new recipes for the game
                Debug.Log($"GameSetup: Starting recipe selection. allAvailableRecipes count: {(allAvailableRecipes != null ? allAvailableRecipes.Count : 0)}");
                gameRecipes = SelectRandomRecipes(totalRounds);

                if (gameRecipes != null)
                {
                    Debug.Log($"GameSetup: SelectRandomRecipes finished. gameRecipes count: {gameRecipes.Count}");
                    // Ensure recipes are displayed immediately after selection
                    DisplayRecipes();
                }
                else
                {
                    Debug.LogError("GameSetup: gameRecipes is null after SelectRandomRecipes! Cannot display recipes.");
                    // Clear recipe UI if recipes are null
                    if (currentRecipeNameText != null) currentRecipeNameText.text = "Error: No Recipes Available"; else Debug.LogWarning("currentRecipeNameText is not assigned.");
                    if (currentRecipeRequirementsAndStatsText != null) currentRecipeRequirementsAndStatsText.text = ""; else Debug.LogWarning("currentRecipeRequirementsAndStatsText is not assigned.");
                    if (nextRecipeNameText != null) nextRecipeNameText.text = "Next Dish: N/A"; else Debug.LogWarning("nextRecipeNameText is not assigned.");
                    if (nextRecipeRequirementsAndStatsText != null) nextRecipeRequirementsAndStatsText.text = ""; else Debug.LogWarning("nextRecipeRequirementsAndStatsText is not assigned.");
                }

                // Update UI elements based on initial state
                UpdateHandUI(); // Hand should be empty initially, except for locked card if carried over
                UpdatePlayedCardsUI(); // Played cards should be empty
                UpdateGameUITexts(); // Update score, round, discards texts

                // Transition to the Drawing Phase
                ChangeState(GameState.DrawingPhase);
                break;

            case GameState.DrawingPhase:
                Debug.Log($"--- Starting Round {currentRound + 1}/{totalRounds} - Drawing Phase ---");

                if (drawPopupPanel != null) drawPopupPanel.SetActive(false); // Ensure draw popup is hidden

                currentDiscardsThisRound = 0; // Reset discards for the new round

                // Display current and next recipes (should be set in GameSetup or previous RoundEnd)
                Debug.Log($"DrawingPhase: Calling DisplayRecipes. gameRecipes count before check: {(gameRecipes != null ? gameRecipes.Count : 0)}");
                DisplayRecipes(); // Ensure recipes are displayed at the start of the drawing phase

                // Update UI text for round, score, and discards
                UpdateGameUITexts();

                // Handle locked card carried over from previous round
                if (lockedCard != null)
                {
                    hand.Add(lockedCard);
                    Debug.Log($"DrawingPhase: Locked card '{lockedCard.cardName}' added to hand.");
                    lockedCard = null; // Clear locked card reference after adding to hand
                    UpdateHandUI(); // Update hand UI to show the added locked card
                    ShowPopupMessage($"Locked card added to hand.", true);
                }
                else
                {
                    UpdateHandUI(); // Update hand UI even if no locked card
                }


                UpdatePlayedCardsUI(); // Ensure played cards UI is updated (should be empty)

                // Set interactivity and highlight for cards in hand
                SetHandCardInteractivity(false); // Cards in hand are not interactive during drawing phase (only decks are clickable)
                if (finishCookingButton != null) finishCookingButton.gameObject.SetActive(false); // Finish cooking button hidden during drawing phase
                HighlightSelectableCards(); // Update highlights based on current state

                // Optionally draw the first card automatically at the start of the Drawing Phase
                // DrawCardFromDeck(deck1); // Example: Draw from deck1 automatically
                break;

            case GameState.CookingPhase:
                Debug.Log("Entered Cooking Phase State");

                if (drawPopupPanel != null) drawPopupPanel.SetActive(false); // Ensure draw popup is hidden

                playedCardsThisRound.Clear(); // Clear played cards from the previous round
                UpdatePlayedCardsUI(); // Update played cards UI (should be empty)
                CancelTechniqueSelection(); // Ensure no technique selection is active

                UpdateHandUI(); // Update hand UI
                UpdateGameUITexts(); // Update score, round, discards texts


                if (finishCookingButton != null) finishCookingButton.gameObject.SetActive(true); // Show finish cooking button

                SetHandCardInteractivity(true); // Cards in hand are interactive during cooking phase
                HighlightSelectableCards(); // Update highlights based on current state
                break;

            case GameState.ScoringPhase:
                Debug.Log("Entered Scoring Phase State");

                if (drawPopupPanel != null) drawPopupPanel.SetActive(false); // Ensure draw popup is hidden
                if (scoringResultsPanel != null) scoringResultsPanel.SetActive(true); // Show scoring results panel

                SetHandCardInteractivity(false); // Hand cards not interactive during scoring
                CancelTechniqueSelection(); // Ensure no technique selection is active
                UpdatePlayedCardsUI(); // Ensure played cards UI is updated

                int roundScore = CalculateRoundScore();
                totalScore += roundScore;
                Debug.Log($"Round {currentRound + 1} Score: {roundScore}. Total Score: {totalScore}");

                // Update score texts
                if (roundScoreText != null) roundScoreText.text = $"Round {currentRound + 1} Score: {roundScore}"; else Debug.LogWarning("roundScoreText is not assigned.");
                UpdateGameUITexts(); // Update score, round, discards texts


                if (continueButton != null) continueButton.gameObject.SetActive(true); // Show continue button

                break;

            case GameState.RoundEnd:
                Debug.Log("Entered Round End State");

                if (drawPopupPanel != null) drawPopupPanel.SetActive(false); // Ensure draw popup is hidden

                SetHandCardInteractivity(false); // Hand cards not interactive at round end
                CancelTechniqueSelection(); // Ensure no technique selection is active
                UpdatePlayedCardsUI(); // Ensure played cards UI is updated
                UpdateGameUITexts(); // Update score, round, discards texts


                // The transition to the next round or GameEnd happens from ProceedFromRoundEnd
                break;

            case GameState.GameEnd:
                Debug.Log("Entered Game End State. Final Score: " + totalScore);
                // Activate Game End panel and deactivate others
                if (gameEndPanel != null) gameEndPanel.SetActive(true); else Debug.LogWarning("gameEndPanel is not assigned.");
                if (gameUIPanel != null) gameUIPanel.SetActive(false);
                if (scoringResultsPanel != null) scoringResultsPanel.SetActive(false);
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);


                SetHandCardInteractivity(false); // Hand cards not interactive at game end
                CancelTechniqueSelection(); // Ensure no technique selection is active
                UpdatePlayedCardsUI(); // Ensure played cards UI is updated

                // Display final score and high score
                if (finalScoreText != null) finalScoreText.text = "Final Score: " + totalScore.ToString(); else Debug.LogWarning("finalScoreText is not assigned.");

                int highScore = GetHighScore();
                if (totalScore > highScore)
                {
                    highScore = totalScore;
                    SaveHighScore(highScore);
                    Debug.Log("New High Score!");
                    // --- New High Score Sound ---
                    if (audioSource != null && newHighScoreSound != null)
                    {
                        audioSource.PlayOneShot(newHighScoreSound);
                    }
                    // --- End New High Score Sound ---
                }
                if (highScoreText != null) highScoreText.text = "High Score: " + highScore.ToString(); else Debug.LogWarning("highScoreText is not assigned.");

                // Removed Play Again Button activation/deactivation
                // if (playAgainButton != null)
                // {
                //     playAgainButton.gameObject.SetActive(true);
                //     playAgainButton.interactable = true;
                // }
                // else Debug.LogWarning("playAgainButton is not assigned.");

                if (backToMenuButton != null)
                {
                    backToMenuButton.gameObject.SetActive(true);
                    backToMenuButton.interactable = true;
                }
                else Debug.LogWarning("backToMenuButton is not assigned.");

                // Hide game-specific UI elements
                // These are now handled by the explicit activation/deactivation logic at the start of OnStateEnter
                // if (continueButton != null) continueButton.gameObject.SetActive(false); else Debug.LogWarning("continueButton is not assigned.");
                // if (finishCookingButton != null) finishCookingButton.gameObject.SetActive(false); else Debug.LogWarning("finishCookingButton is not assigned.");
                // if (currentRoundText != null) currentRoundText.gameObject.SetActive(false); else Debug.LogWarning("currentRoundText is not assigned.");
                // if (currentScoreText != null) currentScoreText.gameObject.SetActive(false); else Debug.LogWarning("currentScoreText is not assigned.");
                // if (discardsLeftText != null) discardsLeftText.gameObject.SetActive(false); else Debug.LogWarning("discardsLeftText is not assigned.");
                // if (currentRecipeNameText != null) currentRecipeNameText.gameObject.SetActive(false); else Debug.LogWarning("currentRecipeNameText is not assigned.");
                // if (currentRecipeRequirementsAndStatsText != null) currentRecipeRequirementsAndStatsText.gameObject.SetActive(false); else Debug.LogWarning("currentRecipeRequirementsAndStatsText is not assigned.");
                // if (nextRecipeNameText != null) nextRecipeNameText.gameObject.SetActive(false); else Debug.LogWarning("nextRecipeNameText is not assigned.");
                // if (nextRecipeRequirementsAndStatsText != null) nextRecipeRequirementsAndStatsText.gameObject.SetActive(false); else Debug.LogWarning("nextRecipeRequirementsAndStatsText is not assigned.");
                // if (playedCardsPanel != null) playedCardsPanel.gameObject.SetActive(false); else Debug.LogWarning("playedCardsPanel is not assigned.");


                break;
        }
    }

    public void StartGame()
    {
        // --- Button Click Sound ---
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        // --- End Button Click Sound ---
        Debug.Log("Starting Game...");

        // Perform necessary resets before entering GameSetup
        currentRound = 0;
        totalScore = 0;
        hand.Clear();
        lockedCard = null;
        currentDiscardsThisRound = 0;
        playedCardsThisRound.Clear();
        gameRecipes = null; // Explicitly clear the old recipe list

        // Re-initialize decks with their initial contents
        if (deck1 != null) deck1.Initialize("Ingredient/Spice Deck", new List<CardSO>(initialDeck1Cards)); else Debug.LogWarning("Deck 1 is not assigned.");
        if (deck2 != null) deck2.Initialize("Tool/Technique Deck", new List<CardSO>(initialDeck2Cards)); else Debug.LogWarning("Deck 2 is not assigned.");

        // Transition to GameSetup state
        ChangeState(GameState.GameSetup);
    }

    // Modified RestartGame method to reload the current scene
    public void RestartGame()
    {
        // --- Button Click Sound ---
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        // --- End Button Click Sound ---

        Debug.Log("Restarting Game by reloading scene...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        // --- Button Click Sound ---
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        // --- End Button Click Sound ---
        Debug.Log("Returning to Main Menu...");

        // Explicitly reset core game state variables
        currentRound = 0;
        totalScore = 0;
        hand.Clear();
        lockedCard = null;
        currentDiscardsThisRound = 0;
        playedCardsThisRound.Clear();
        gameRecipes = null; // Explicitly clear the old recipe list

        // Re-initialize decks with their initial contents
        if (deck1 != null) deck1.Initialize("Ingredient/Spice Deck", new List<CardSO>(initialDeck1Cards)); else Debug.LogWarning("Deck 1 is not assigned for return to menu.");
        if (deck2 != null) deck2.Initialize("Tool/Technique Deck", new List<CardSO>(initialDeck2Cards)); else Debug.LogWarning("Deck 2 is not assigned for return to menu.");

        // Reset UI elements to their initial state or hide them
        if (gameEndPanel != null) gameEndPanel.SetActive(false); // Ensure game end is off
        if (scoringResultsPanel != null) scoringResultsPanel.SetActive(false); // Ensure scoring results is off
        if (drawPopupPanel != null) drawPopupPanel.SetActive(false); // Ensure draw popup is off
        if (cancelTechniqueButton != null) cancelTechniqueButton.gameObject.SetActive(false); // Hide cancel button
        if (finishCookingButton != null) finishCookingButton.gameObject.SetActive(false); // Hide finish cooking button
        if (continueButton != null) continueButton.gameObject.SetActive(false); // Hide continue button
        if (gameUIPanel != null) gameUIPanel.SetActive(false); // Ensure game UI is off

        // Clear existing UI displays by updating with empty lists
        UpdateHandUI(); // Clears hand panel visually
        UpdatePlayedCardsUI(); // Clears played cards panel visually

        // Reset text elements to default or placeholder values
        if (currentRoundText != null) currentRoundText.text = "Round: 0/0"; else Debug.LogWarning("currentRoundText is not assigned for return to menu.");
        if (currentScoreText != null) currentScoreText.text = "Score: 0"; else Debug.LogWarning("currentScoreText is not assigned for return to menu.");
        if (discardsLeftText != null) discardsLeftText.text = $"Discards Left: {maxDiscardsPerRound}"; else Debug.LogWarning("discardsLeftText is not assigned for return to menu."); // Reset discards text
        if (currentRecipeNameText != null) currentRecipeNameText.text = "Current Dish: "; else Debug.LogWarning("currentRecipeNameText is not assigned for return to menu.");
        if (currentRecipeRequirementsAndStatsText != null) currentRecipeRequirementsAndStatsText.text = ""; else Debug.LogWarning("currentRecipeRequirementsAndStatsText is not assigned for return to menu.");
        if (nextRecipeNameText != null) nextRecipeNameText.text = "Next Dish: "; else Debug.LogWarning("nextRecipeNameText is not assigned for return to menu.");
        if (nextRecipeRequirementsAndStatsText != null) nextRecipeRequirementsAndStatsText.text = ""; else Debug.LogWarning("nextRecipeRequirementsAndStatsText is not assigned for return to menu.");
        if (roundScoreText != null) roundScoreText.text = "Round Score: 0"; else Debug.LogWarning("roundScoreText is not assigned for return to menu.");
        if (scoreBreakdownText != null) scoreBreakdownText.text = ""; else Debug.LogWarning("scoreBreakdownText is not assigned for return to menu.");
        if (finalScoreText != null) finalScoreText.text = "Final Score: 0"; else Debug.LogWarning("finalScoreText is not assigned for return to menu.");


        // Transition to MainMenu state
        ChangeState(GameState.MainMenu);
    }

    public void DrawCardFromDeck(Deck deckToDrawFrom)
    {
        // Added check to prevent drawing if the draw popup is active
        if (drawPopupPanel != null && drawPopupPanel.activeSelf)
        {
            Debug.Log("Draw popup is active. Cannot draw a new card yet.");
            ShowPopupMessage("Please choose an action for the current card first.", true); // Use the existing popup message for feedback
            return;
        }

        // --- Deck Click Sound ---
        if (audioSource != null && deckClickSound != null)
        {
            audioSource.PlayOneShot(deckClickSound);
        }
        // --- End Deck Click Sound ---

        if (currentState != GameState.DrawingPhase) { Debug.LogWarning("Attempted to draw card outside of Drawing Phase."); ShowPopupMessage("Can only draw cards during the Drawing Phase!", true); return; }
        if (hand.Count >= handLimit) { ShowPopupMessage("Hand is full! Drawing Phase ends.", true); return; }

        currentlyDrawnCard = deckToDrawFrom.DrawCard();

        if (currentlyDrawnCard != null) { ShowDrawPopup(currentlyDrawnCard); }
        else { Debug.LogError($"Error: Could not draw a card from {deckToDrawFrom.name}."); ShowPopupMessage($"Error drawing card from {deckToDrawFrom.name}.", true); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; }
    }

    void ShowDrawPopup(CardSO card)
    {
        if (currentState != GameState.DrawingPhase) { Debug.LogWarning("Attempted to show draw popup outside of Drawing Phase."); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; return; }

        if (drawPopupPanel != null) drawPopupPanel.SetActive(true); else Debug.LogWarning("drawPopupPanel is not assigned.");

        if (popupCardArtwork != null) popupCardArtwork.sprite = card.artwork; else Debug.LogWarning("popupCardArtwork is not assigned.");
        if (popupCardName != null) popupCardName.text = card.cardName; else Debug.LogWarning("popupCardName is not assigned.");

        string combinedText = card.description;
        if (!string.IsNullOrEmpty(card.description) && card.GetStatsDictionary().Count > 0) { combinedText += "\n\nStats:\n"; }
        foreach (var stat in card.GetStatsDictionary()) { combinedText += $"{stat.Key}: {stat.Value}\n"; }
        if (popupCardDescription != null) popupCardDescription.text = combinedText; else Debug.LogWarning("popupCardDescription is not assigned.");

        // Check if combine button text needs to be set/reset here
        if (combineButton != null && combineButton.GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            combineButton.GetComponentInChildren<TextMeshProUGUI>().text = "Combine"; // Ensure text is set
        }
        else if (combineButton == null) Debug.LogWarning("combineButton is not assigned.");
        else if (combineButton.GetComponentInChildren<TextMeshProUGUI>() == null) Debug.LogWarning("TextMeshProUGUI not found on combineButton children.");


        bool canCombine = false;
        if (card.cardType == CardSO.CardType.Spice) { if (hand.Any(c => c.cardType == CardSO.CardType.Spice && c.cardName == card.cardName)) { canCombine = true; } } // Added check for card type Spice
        if (combineButton != null) combineButton.gameObject.SetActive(canCombine);

        if (popupMessageText != null) popupMessageText.text = "Choose an action:"; else Debug.LogWarning("popupMessageText is not assigned.");
        if (keepButton != null) keepButton.interactable = true; else Debug.LogWarning("keepButton is not assigned.");

        // --- FIX: Set Discard Button Text with Count ---
        if (discardButton != null && discardButton.GetComponentInChildren<TextMeshProUGUI>() != null)
        {
            discardButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Discard ({maxDiscardsPerRound - currentDiscardsThisRound})"; // Set text with count
        }
        else if (discardButton == null) Debug.LogWarning("discardButton is not assigned.");
        else if (discardButton.GetComponentInChildren<TextMeshProUGUI>() == null) Debug.LogWarning("TextMeshProUGUI not found on discardButton children.");
        // --- END FIX ---

        if (discardButton != null) discardButton.interactable = (currentDiscardsThisRound < maxDiscardsPerRound);
    }

    void KeepDrawnCard()
    {
        // --- Button Click Sound ---
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        // --- End Button Click Sound ---

        if (currentState != GameState.DrawingPhase || currentlyDrawnCard == null || hand.Count >= handLimit) { if (hand.Count >= handLimit) ShowPopupMessage("Hand is already full! Cannot keep card.", false); Debug.LogWarning("KeepDrawnCard called in invalid state or conditions."); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; return; }

        hand.Add(currentlyDrawnCard);
        UpdateHandUI();

        if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
        currentlyDrawnCard = null;

        if (hand.Count >= handLimit) { ShowPopupMessage("Hand is full! Drawing Phase ends.", true); ChangeState(GameState.CookingPhase); }
    }

    void DiscardDrawnCard(bool autoDiscard = false)
    {
        // --- Button Click Sound (only if not auto-discard) ---
        if (!autoDiscard && audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        // --- End Button Click Sound ---

        if ((currentState != GameState.DrawingPhase && !autoDiscard) || currentlyDrawnCard == null) { Debug.LogWarning("DiscardDrawnCard called outside of Drawing Phase or with no card drawn."); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; return; }

        if (currentDiscardsThisRound < maxDiscardsPerRound || autoDiscard)
        {
            Debug.Log($"Discarded: {currentlyDrawnCard.cardName}");
            if (!autoDiscard) { currentDiscardsThisRound++; UpdateGameUITexts(); Debug.Log($"Discards left this round: {maxDiscardsPerRound - currentDiscardsThisRound}"); if (discardButton != null) discardButton.interactable = (currentDiscardsThisRound < maxDiscardsPerRound); }
            if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
            currentlyDrawnCard = null;
        }
        else { ShowPopupMessage($"No discards left this round ({currentDiscardsThisRound}/{maxDiscardsPerRound})!", false); }
    }

    void CombineDrawnCard()
    {
        // --- Button Click Sound ---
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        // --- End Button Click Sound ---

        if (currentState != GameState.DrawingPhase || currentlyDrawnCard == null || currentlyDrawnCard.cardType != CardSO.CardType.Spice) { Debug.LogWarning($"CombineDrawnCard called in invalid state ({currentState}), no card drawn, or card is not a Spice."); ShowPopupMessage("Combination not possible.", false); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; return; }

        CardSO cardInHandToCombine = hand.FirstOrDefault(c => c.cardType == CardSO.CardType.Spice && c.cardName == currentlyDrawnCard.cardName); // Added card type check

        if (cardInHandToCombine != null)
        {
            Debug.Log($"Attempting to combine drawn {currentlyDrawnCard.cardName} with {cardInHandToCombine.cardName} from hand.");

            if (spiceStatMapping.TryGetValue(currentlyDrawnCard.cardName, out string statName))
            {
                float drawnStatValue = currentlyDrawnCard.GetStat(statName);
                if (cardInHandToCombine.GetStatsDictionary().ContainsKey(statName))
                {
                    cardInHandToCombine.AddToStat(statName, drawnStatValue);
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

    // This method is likely called by the CardDisplayUI script when a card in hand is clicked
    public void PlayCard(CardSO cardToPlay)
    {
        // --- Card Click Sound (Best handled in CardDisplayUI.cs) ---
        // You would likely add a call to audioSource.PlayOneShot(cardClickSound);
        // in the OnPointerDown or OnClick handler within your CardDisplayUI.cs script.
        // --- End Card Click Sound ---


        if (currentState != GameState.CookingPhase)
        {
            Debug.LogWarning($"Attempted to play card '{cardToPlay.cardName}' outside of Cooking Phase. Current State: {currentState}");
            ShowPopupMessage("You can only play cards during the Cooking Phase!", true);
            return;
        }
        if (!hand.Contains(cardToPlay))
        {
            Debug.LogWarning($"Attempted to play card '{cardToPlay.cardName}' not found in hand.");
            ShowPopupMessage("Card not found in hand!", false);
            return;
        }

        switch (currentCookingSelectionState)
        {
            case CookingSelectionState.None:
                switch (cardToPlay.cardType)
                {
                    case CardSO.CardType.Ingredient:
                    case CardSO.CardType.Spice:
                        Debug.Log($"Playing {cardToPlay.cardType}: {cardToPlay.cardName}");
                        hand.Remove(cardToPlay);
                        playedCardsThisRound.Add(cardToPlay);
                        ShowPopupMessage($"Played {cardToPlay.cardName}.", true);
                        UpdateHandUI();
                        UpdatePlayedCardsUI();
                        break;

                    case CardSO.CardType.Tool:
                        Debug.Log($"Attempted to play Tool directly: {cardToPlay.cardName}");
                        ShowPopupMessage("You need to select a Technique first to use a Tool.", true);
                        break;

                    case CardSO.CardType.Technique:
                        if (hand.Any(c => c.cardType == CardSO.CardType.Tool))
                        {
                            Debug.Log($"Initiating Technique: {cardToPlay.cardName}. Select a Tool.");
                            selectedTechnique = cardToPlay;
                            currentCookingSelectionState = CookingSelectionState.WaitingForTool;
                            ShowPopupMessage($"Selected {cardToPlay.cardName}. Now select a Tool from your hand.", false);
                            HighlightSelectableCards();
                        }
                        else
                        {
                            Debug.LogWarning($"Attempted to play Technique '{cardToPlay.cardName}' but no Tool cards available in hand.");
                            ShowPopupMessage($"You need a Tool card in your hand to use {cardToPlay.cardName}.", true);
                        }
                        break;

                    default:
                        Debug.LogWarning($"Attempted to play card with unhandled type: {cardToPlay.cardType}");
                        ShowPopupMessage("Cannot play this type of card directly.", false);
                        break;
                }
                break;

            case CookingSelectionState.WaitingForTool:
                if (cardToPlay.cardType == CardSO.CardType.Tool)
                {
                    if (hand.Any(c => c.cardType == CardSO.CardType.Ingredient))
                    {
                        Debug.Log($"Selected Tool: {cardToPlay.cardName}");
                        selectedTool = cardToPlay;
                        currentCookingSelectionState = CookingSelectionState.WaitingForIngredient;
                        ShowPopupMessage($"Selected Tool: {cardToPlay.cardName}. Now select an Ingredient from your hand.", false);
                        HighlightSelectableCards();
                    }
                    else
                    {
                        Debug.LogWarning($"Attempted to select Tool '{cardToPlay.cardName}' but no Ingredient cards available in hand.");
                        ShowPopupMessage($"You need an Ingredient card in your hand to use {selectedTechnique.cardName} with {cardToPlay.cardName}.", true);
                    }
                }
                else
                {
                    Debug.LogWarning($"Attempted to select non-Tool card ({cardToPlay.cardName}, Type: {cardToPlay.cardType}) when waiting for Tool.");
                    ShowPopupMessage("You must select a Tool card.", false);
                }
                break;

            case CookingSelectionState.WaitingForIngredient:
                if (cardToPlay.cardType == CardSO.CardType.Ingredient)
                {
                    Debug.Log($"Selected Ingredient: {cardToPlay.cardName}");
                    selectedIngredient = cardToPlay;
                    CompleteTechniqueCombination();
                }
                else
                {
                    Debug.LogWarning($"Attempted to select non-Ingredient card ({cardToPlay.cardName}, Type: {cardToPlay.cardType}) when waiting for Ingredient.");
                    ShowPopupMessage("You must select an Ingredient card.", false);
                }
                break;
        }
    }

    void CompleteTechniqueCombination()
    {
        if (selectedTechnique != null && selectedTool != null && selectedIngredient != null &&
            hand.Contains(selectedTechnique) && hand.Contains(selectedTool) && hand.Contains(selectedIngredient))
        {
            Debug.Log($"Completing Technique Combination: {selectedTechnique.cardName} with {selectedTool.cardName} and {selectedIngredient.cardName}");

            hand.Remove(selectedTechnique);
            hand.Remove(selectedTool);
            hand.Remove(selectedIngredient);

            playedCardsThisRound.Add(selectedTechnique);
            playedCardsThisRound.Add(selectedTool);
            playedCardsThisRound.Add(selectedIngredient);

            ShowPopupMessage($"Used {selectedTechnique.cardName} with {selectedTool.cardName} on {selectedIngredient.cardName}!", true);

            selectedTechnique = null;
            selectedTool = null;
            selectedIngredient = null;
            currentCookingSelectionState = CookingSelectionState.None;

            UpdateHandUI();
            UpdatePlayedCardsUI();
            SetHandCardInteractivity(true);
            HighlightSelectableCards();

            // Optional: Play a success sound for the combination
            // if (audioSource != null && combinationSuccessSound != null)
            // {
            //     audioSource.PlayOneShot(combinationSuccessSound);
            // }
        }
        else
        {
            Debug.LogWarning("Attempted to complete Technique combination but cards were missing or not in hand. Cancelling selection.");
            ShowPopupMessage("Error during combination. Please try again.", true);
            CancelTechniqueSelection();
            // Optional: Play an error sound for failed combination
            // if (audioSource != null && combinationFailSound != null)
            // {
            //     audioSource.PlayOneShot(combinationFailSound);
            // }
        }
    }

    public void CancelTechniqueSelection()
    {
        // --- Button Click Sound ---
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        // --- End Button Click Sound ---

        if (currentCookingSelectionState != CookingSelectionState.None)
        {
            Debug.Log("Technique selection cancelled by player or error.");
            selectedTechnique = null;
            selectedTool = null;
            selectedIngredient = null;
            currentCookingSelectionState = CookingSelectionState.None;
            ShowPopupMessage("Technique selection cancelled.", true);
            UpdateHandUI();
            SetHandCardInteractivity(true);
            HighlightSelectableCards();
        }
    }

    public void FinishCooking()
    {
        if (currentState == GameState.CookingPhase && currentCookingSelectionState == CookingSelectionState.None)
        {
            Debug.Log("Finishing Cooking for the round.");
            // --- Finish Cooking Sound ---
            if (audioSource != null && finishCookingSound != null)
            {
                audioSource.PlayOneShot(finishCookingSound);
            }
            // --- End Finish Cooking Sound ---
            ChangeState(GameState.ScoringPhase);
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


    int CalculateRoundScore()
    {
        int roundScore = 0;
        string breakdownText = "Score Breakdown:\n";

        if (gameRecipes == null || gameRecipes.Count <= currentRound)
        {
            Debug.LogError($"Cannot calculate score: Current recipe for round {currentRound} is missing.");
            breakdownText += "Error: No recipe found for scoring.";
            if (scoreBreakdownText != null) scoreBreakdownText.text = breakdownText; else Debug.LogWarning("scoreBreakdownText is not assigned.");
            return 0;
        }

        RecipeSO currentRecipe = gameRecipes[currentRound];

        var playedIngredients = playedCardsThisRound.Where(c => c.cardType == CardSO.CardType.Ingredient).ToList();
        var playedSpices = playedCardsThisRound.Where(c => c.cardType == CardSO.CardType.Spice).ToList();
        var playedTools = playedCardsThisRound.Where(c => c.cardType == CardSO.CardType.Tool).ToList();
        var playedTechniques = playedCardsThisRound.Where(c => c.cardType == CardSO.CardType.Technique).ToList();

        if (currentRecipe.requiredIngredients != null && playedIngredients.Count == currentRecipe.requiredIngredients.Count)
        {
            roundScore += IngredientAmountScore;
        }
        int requiredSpiceCount = GetRequiredSpiceCount(currentRecipe);
        if (playedSpices.Count == requiredSpiceCount)
        {
            roundScore += SpiceAmountScore;
        }
        if (currentRecipe.requiredTools != null && playedTools.Count == currentRecipe.requiredTools.Count)
        {
            roundScore += ToolAmountScore;
        }
        if (currentRecipe.requiredTechniques != null && playedTechniques.Count > 0 && currentRecipe.requiredTechniques.Count > 0 && playedTechniques.Count == currentRecipe.requiredTechniques.Count)
        {
            roundScore += TechniqueAmountScore;
        }
        else if (currentRecipe.requiredTechniques != null && currentRecipe.requiredTechniques.Count == 0 && playedTechniques.Count == 0)
        {
            roundScore += TechniqueAmountScore;
        }


        int ingredientTypeMatches = CountMatchingRequiredCards(currentRecipe.requiredIngredients, playedIngredients);
        int ingredientScore = ingredientTypeMatches * IngredientTypeScore;
        roundScore += ingredientScore;

        // Note: The original code had a potential issue here by trying to get CardSO.CardType.Spice from requiredIngredients.
        // Spices are a type of Ingredient. The logic below correctly checks played Spices against the required ingredients that *are* spices.
        var requiredSpiceCards = currentRecipe.requiredIngredients?.Where(c => c.cardType == CardSO.CardType.Spice).ToList();
        int spiceTypeMatches = CountMatchingRequiredCards(requiredSpiceCards, playedSpices);
        int spiceScore = spiceTypeMatches * SpiceTypeScore;
        roundScore += spiceScore;


        int toolTypeMatches = CountMatchingRequiredCards(currentRecipe.requiredTools, playedTools);
        int toolScore = toolTypeMatches * ToolTypeScore;
        roundScore += toolScore;

        int techniqueTypeMatches = CountMatchingRequiredCards(currentRecipe.requiredTechniques, playedTechniques);
        int techniqueScore = techniqueTypeMatches * TechniqueTypeScore;
        roundScore += techniqueScore;

        bool perfectDishAchieved = false;
        if (currentRecipe.targetStats != null && currentRecipe.targetStats.Count > 0)
        {
            Dictionary<string, float> combinedStats = GetCombinedPlayedStats(playedCardsThisRound);

            bool perfectMatch = true;

            foreach (var targetStat in currentRecipe.targetStats)
            {
                if (combinedStats.TryGetValue(targetStat.name, out float playedValue))
                {
                    float difference = Mathf.Abs(playedValue - targetStat.targetValue);
                    if (difference > StatMatchTolerance)
                    {
                        perfectMatch = false;
                    }
                }
                else
                {
                    perfectMatch = false;
                }
            }

            if (perfectMatch)
            {
                // Also check if there are any *extra* stats played that aren't in the target recipe
                foreach (var playedStat in combinedStats)
                {
                    if (!currentRecipe.targetStats.Any(ts => ts.name == playedStat.Key))
                    {
                        perfectMatch = false;
                        break;
                    }
                }
            }


            if (perfectMatch)
            {
                roundScore += PerfectDishBonus;
                perfectDishAchieved = true;
                Debug.Log("Achieved Perfect Dish!");
                // Optional: Play a perfect dish bonus sound
                // if (audioSource != null && perfectDishSound != null)
                // {
                //     audioSource.PlayOneShot(perfectDishSound);
                // }
            }
            else
            {
                Debug.Log($"Dish not perfect (stats mismatch or extra stats).");
            }
        }

        breakdownText += $"Ingredient Score: +{ingredientScore}\n";
        breakdownText += $"Spice Score: +{spiceScore}\n";
        breakdownText += $"Tool Score: +{toolScore}\n";
        breakdownText += $"Technique Score: +{techniqueScore}\n";

        if (perfectDishAchieved)
        {
            breakdownText += $"Perfect Dish Bonus: +{PerfectDishBonus}\n";
        }
        else if (currentRecipe.targetStats != null && currentRecipe.targetStats.Count > 0)
        {
            breakdownText += "Perfect Dish Bonus: +0\n";
        }
        else
        {
            breakdownText += "Perfect Dish Bonus: N/A\n";
        }

        if (scoreBreakdownText != null) scoreBreakdownText.text = breakdownText; else Debug.LogWarning("scoreBreakdownText is not assigned.");


        return roundScore;
    }

    int CountMatchingRequiredCards(List<CardSO> requiredCards, List<CardSO> playedCards)
    {
        if (requiredCards == null || playedCards == null) return 0;

        int matches = 0;
        List<CardSO> playedCopy = new List<CardSO>(playedCards);
        List<CardSO> requiredCopy = new List<CardSO>(requiredCards);

        foreach (var requiredCard in requiredCopy)
        {
            CardSO matchingPlayedCard = playedCopy.FirstOrDefault(pc => pc.cardName == requiredCard.cardName);
            if (matchingPlayedCard != null)
            {
                matches++;
                playedCopy.Remove(matchingPlayedCard);
            }
        }
        return matches;
    }

    int GetRequiredSpiceCount(RecipeSO recipe)
    {
        if (recipe == null || recipe.requiredIngredients == null) return 0;
        return recipe.requiredIngredients.Count(c => c.cardType == CardSO.CardType.Spice);
    }

    Dictionary<string, float> GetCombinedPlayedStats(List<CardSO> playedCards)
    {
        Dictionary<string, float> combinedStats = new Dictionary<string, float>();
        if (playedCards == null) return combinedStats;

        foreach (var card in playedCards)
        {
            var cardStats = card.GetStatsDictionary();
            if (cardStats != null)
            {
                foreach (var stat in cardStats)
                {
                    if (combinedStats.ContainsKey(stat.Key))
                    {
                        combinedStats[stat.Key] += stat.Value;
                    }
                    else
                    {
                        combinedStats[stat.Key] = stat.Value;
                    }
                }
            }
        }
        return combinedStats;
    }


    public void ProceedFromRoundEnd()
    {
        // --- Button Click Sound ---
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
        // --- End Button Click Sound ---

        if (currentState == GameState.ScoringPhase || currentState == GameState.RoundEnd)
        {
            if (currentRound < totalRounds - 1)
            {
                // Logic for carrying over locked card and preparing for the next round
                if (lockedCard != null)
                {
                    // Ensure the locked card is removed from the hand if it was somehow still there
                    CardSO lockedCardInHand = hand.FirstOrDefault(c => c == lockedCard);
                    if (lockedCardInHand != null)
                    {
                        hand.Remove(lockedCardInHand);
                    }
                    // Add the locked card to the hand for the next round
                    hand.Add(lockedCard);
                    Debug.Log($"Locked card '{lockedCard.cardName}' carried over to Round {currentRound + 2}."); // Log for next round
                }
                else
                {
                    // If no locked card, clear the hand for the new round
                    hand.Clear();
                }

                playedCardsThisRound.Clear(); // Clear played cards for the new round
                UpdatePlayedCardsUI(); // Update played cards UI (should be empty)
                currentDiscardsThisRound = 0; // Reset discards for the new round

                UpdateHandUI(); // Update hand UI to show the potentially new hand (with locked card or empty)
                currentRound++; // Increment round counter

                // Display recipes for the *new* current round
                DisplayRecipes();

                // Update UI texts for the new round
                UpdateGameUITexts();


                ChangeState(GameState.DrawingPhase); // Transition to Drawing Phase for the next round
            }
            else
            {
                Debug.Log("All rounds completed. Ending game.");
                ChangeState(GameState.GameEnd); // Transition to Game End state
            }
            // Hide scoring results and continue button after proceeding
            if (continueButton != null) continueButton.gameObject.SetActive(false); else Debug.LogWarning("continueButton is not assigned.");
            if (scoringResultsPanel != null) scoringResultsPanel.gameObject.SetActive(false); else Debug.LogWarning("scoringResultsPanel is not assigned.");
        }
    }

    void UpdateHandUI()
    {
        if (handPanel != null)
        {
            // Destroy all existing card UI objects in the hand panel
            foreach (Transform child in handPanel.transform)
            {
                Destroy(child.gameObject);
            }

            // Instantiate new card UI objects for each card in the hand list
            foreach (var cardSO in hand)
            {
                GameObject cardUIObject = Instantiate(cardUIPrefab, handPanel.transform);

                CardDisplayUI displayScript = cardUIObject.GetComponent<CardDisplayUI>();
                if (displayScript != null)
                {
                    displayScript.SetCard(cardSO);
                    displayScript.SetGameManager(this);
                    // --- Pass AudioSource to CardDisplayUI ---
                    // You will need a public AudioSource variable in CardDisplayUI.cs
                    // and a method or property to set it.
                    // displayScript.SetAudioSource(audioSource);
                    // --- End Pass AudioSource ---
                    // --- Pass AudioClips for card sounds to CardDisplayUI ---
                    // You will need public AudioClip variables in CardDisplayUI.cs
                    // for click and mouse over sounds and a method or property to set them.
                    // displayScript.SetCardClickSound(cardClickSound); // Need to add cardClickSound variable here if handled in GM
                    // displayScript.SetCardMouseOverSound(cardMouseOverSound); // Need to add cardMouseOverSound variable here if handled in GM
                    // A better approach is often to have CardDisplayUI request the sounds
                    // from a central SoundManager or the GameManager itself.
                    // --- End Pass AudioClips ---
                }
                else
                {
                    Debug.LogError("CardDisplayUI script not found on cardUIPrefab. Card UI will not display correctly or be interactive.");
                    // Fallback to displaying just the card name if CardDisplayUI is missing
                    TextMeshProUGUI cardText = cardUIObject.GetComponentInChildren<TextMeshProUGUI>();
                    if (cardText != null) cardText.text = cardSO.cardName;
                    Image cardImage = cardUIObject.GetComponentInChildren<Image>();
                    if (cardImage != null && cardSO.artwork != null) cardImage.sprite = cardSO.artwork;
                }
            }
        }
        else
        {
            Debug.LogWarning("Hand Panel UI GameObject is not assigned in GameManager.");
        }

        // Update discards left text (this was previously incorrectly updating hand size)
        // This is now handled in UpdateGameUITexts()
        // if (discardsLeftText != null)
        // {
        //     discardsLeftText.text = $"Discards Left: {maxDiscardsPerRound - currentDiscardsThisRound}";
        // } else Debug.LogWarning("discardsLeftText is not assigned for hand UI update.");


        HighlightSelectableCards(); // Update highlights after updating the hand UI
    }

    void UpdatePlayedCardsUI()
    {
        if (playedCardsPanel != null)
        {
            // Activate the panel only if there are played cards or during scoring/round end
            playedCardsPanel.gameObject.SetActive(playedCardsThisRound.Count > 0 || currentState == GameState.ScoringPhase || currentState == GameState.RoundEnd);

            // Destroy all existing card UI objects in the played cards panel
            foreach (Transform child in playedCardsPanel.transform)
            {
                Destroy(child.gameObject);
            }

            // Instantiate new card UI objects for each card in the playedCardsThisRound list
            foreach (var cardSO in playedCardsThisRound)
            {
                GameObject cardUIObject = Instantiate(cardUIPrefab, playedCardsPanel.transform);

                CardDisplayUI displayScript = cardUIObject.GetComponent<CardDisplayUI>();
                if (displayScript != null)
                {
                    displayScript.SetCard(cardSO);
                    displayScript.SetInteractive(false); // Played cards are not interactive
                    displayScript.SetHighlight(false); // Played cards are not highlighted
                }
                else
                {
                    Debug.LogError("CardDisplayUI script not found on cardUIPrefab for played cards. Card UI will not display correctly.");
                    // Fallback to displaying just the card name if CardDisplayUI is missing
                    TextMeshProUGUI cardText = cardUIObject.GetComponentInChildren<TextMeshProUGUI>();
                    if (cardText != null) cardText.text = cardSO.cardName;
                    Image cardImage = cardUIObject.GetComponentInChildren<Image>();
                    if (cardImage != null && cardSO.artwork != null) cardImage.sprite = cardSO.artwork;
                }
            }
        }
        else
        {
            Debug.LogWarning("Played Cards Panel UI GameObject is not assigned in GameManager.");
        }
    }


    void SetHandCardInteractivity(bool isInteractive)
    {
        if (handPanel == null)
        {
            Debug.LogWarning("handPanel is not assigned. Cannot set hand card interactivity.");
            return;
        }

        CardDisplayUI[] cardUIs = handPanel.GetComponentsInChildren<CardDisplayUI>();
        foreach (var cardUI in cardUIs)
        {
            cardUI.SetInteractive(isInteractive);
        }
    }

    void HighlightSelectableCards()
    {
        if (handPanel == null)
        {
            Debug.LogWarning("handPanel is not assigned. Cannot highlight cards.");
            return;
        }


        CardDisplayUI[] cardUIs = handPanel.GetComponentsInChildren<CardDisplayUI>();
        foreach (var cardUI in cardUIs)
        {
            bool isSelectable = false;
            CardSO cardSO = cardUI.GetCardData();

            if (cardSO != null)
            {
                switch (currentCookingSelectionState)
                {
                    case CookingSelectionState.None:
                        // In None state during Cooking Phase, all playable cards (Ingredient, Spice, Technique with Tool) are selectable
                        if (currentState == GameState.CookingPhase)
                        {
                            if (cardSO.cardType == CardSO.CardType.Ingredient || cardSO.cardType == CardSO.CardType.Spice)
                            {
                                isSelectable = true;
                            }
                            else if (cardSO.cardType == CardSO.CardType.Technique && hand.Any(c => c.cardType == CardSO.CardType.Tool))
                            {
                                isSelectable = true;
                            }
                        }
                        cardUI.SetInteractive(isSelectable); // Only interactive if selectable
                        break;

                    case CookingSelectionState.WaitingForTool:
                        // In WaitingForTool state, only Tool cards are selectable
                        if (cardSO.cardType == CardSO.CardType.Tool)
                        {
                            isSelectable = true;
                            cardUI.SetInteractive(true); // Tools are interactive when waiting for them
                        }
                        else
                        {
                            isSelectable = false;
                            cardUI.SetInteractive(false); // Other cards are not interactive
                        }
                        break;

                    case CookingSelectionState.WaitingForIngredient:
                        // In WaitingForIngredient state, only Ingredient cards are selectable
                        if (cardSO.cardType == CardSO.CardType.Ingredient)
                        {
                            isSelectable = true;
                            cardUI.SetInteractive(true); // Ingredients are interactive when waiting for them
                        }
                        else
                        {
                            isSelectable = false;
                            cardUI.SetInteractive(false); // Other cards are not interactive
                        }
                        break;
                }
            }
            cardUI.SetHighlight(isSelectable); // Apply highlight based on selectability
        }

        // Update button states based on current state and selection state
        if (finishCookingButton != null)
        {
            finishCookingButton.interactable = (currentState == GameState.CookingPhase && currentCookingSelectionState == CookingSelectionState.None);
            finishCookingButton.gameObject.SetActive(currentState == GameState.CookingPhase); // Only show during Cooking Phase
        }
        else Debug.LogWarning("finishCookingButton is not assigned.");

        if (cancelTechniqueButton != null)
        {
            cancelTechniqueButton.gameObject.SetActive(currentCookingSelectionState != CookingSelectionState.None); // Show only when a technique selection is in progress
            cancelTechniqueButton.interactable = (currentCookingSelectionState != CookingSelectionState.None); // Interactive only when visible
        }
        else Debug.LogWarning("cancelTechniqueButton is not assigned.");
    }

    // New method to update the main game UI texts
    void UpdateGameUITexts()
    {
        if (currentRoundText != null) currentRoundText.text = $"Round: {currentRound + 1}/{totalRounds}"; else Debug.LogWarning("currentRoundText is not assigned for UpdateGameUITexts.");
        if (currentScoreText != null) currentScoreText.text = $"Score: {totalScore}"; else Debug.LogWarning("currentScoreText is not assigned for UpdateGameUITexts.");
        if (discardsLeftText != null) discardsLeftText.text = $"Discards Left: {maxDiscardsPerRound - currentDiscardsThisRound}"; else Debug.LogWarning("discardsLeftText is not assigned for UpdateGameUITexts.");
    }


    void DisplayRecipes()
    {
        Debug.Log("DisplayRecipes method entered.");

        // Check if gameRecipes list is valid and has enough recipes for the current round
        if (gameRecipes == null || gameRecipes.Count <= currentRound)
        {
            Debug.LogError($"Cannot display current recipe for round {currentRound}. gameRecipes list is null or has only {gameRecipes?.Count ?? 0} recipes. Returning from DisplayRecipes.");
            // Clear recipe UI if recipes are missing
            if (currentRecipeNameText != null) currentRecipeNameText.text = "Error: No Recipe"; else Debug.LogWarning("currentRecipeNameText is not assigned in DisplayRecipes.");
            if (currentRecipeRequirementsAndStatsText != null) currentRecipeRequirementsAndStatsText.text = ""; else Debug.LogWarning("currentRecipeRequirementsAndStatsText is not assigned in DisplayRecipes.");
            if (nextRecipeNameText != null) nextRecipeNameText.text = "Next Dish: N/A"; else Debug.LogWarning("nextRecipeNameText is not assigned in DisplayRecipes.");
            if (nextRecipeRequirementsAndStatsText != null) nextRecipeRequirementsAndStatsText.text = ""; else Debug.LogWarning("nextRecipeRequirementsAndStatsText is not assigned in DisplayRecipes.");
            return; // Exit the method if recipes are not available
        }

        // Display Current Recipe
        RecipeSO currentRecipe = gameRecipes[currentRound];
        if (currentRecipeNameText != null) currentRecipeNameText.text = currentRecipe.recipeName; else Debug.LogWarning("currentRecipeNameText is not assigned in DisplayRecipes.");

        string currentReqAndStatsText = "";

        if (currentRecipe.requiredIngredients != null && currentRecipe.requiredIngredients.Count > 0)
        {
            currentReqAndStatsText += "Ingredients:\n";
            foreach (var card in currentRecipe.requiredIngredients) currentReqAndStatsText += $"- {card.cardName}\n";
        }

        if (currentRecipe.requiredTechniques != null && currentRecipe.requiredTechniques.Count > 0)
        {
            if (currentReqAndStatsText.Length > 0 && !currentReqAndStatsText.EndsWith("\n")) currentReqAndStatsText += "\n";
            currentReqAndStatsText += "Techniques:\n";
            foreach (var card in currentRecipe.requiredTechniques) currentReqAndStatsText += $"- {card.cardName}\n";
        }

        if (currentRecipe.targetStats != null && currentRecipe.targetStats.Count > 0)
        {
            if (currentReqAndStatsText.Length > 0 && !currentReqAndStatsText.EndsWith("\n")) currentReqAndStatsText += "\n";
            currentReqAndStatsText += "Stats:\n";
            foreach (var stat in currentRecipe.targetStats) currentReqAndStatsText += $"- {stat.name}: {stat.targetValue}\n";
        }

        if (currentRecipeRequirementsAndStatsText != null)
        {
            currentRecipeRequirementsAndStatsText.text = currentReqAndStatsText;
        }
        else
        {
            Debug.LogWarning("currentRecipeRequirementsAndStatsText TextMeshProUGUI is not assigned in GameManager.");
        }

        Debug.Log($"Successfully displayed current recipe: {gameRecipes[currentRound].recipeName}");

        // Display Next Recipe (if available)
        if (gameRecipes != null && currentRound + 1 < gameRecipes.Count)
        {
            RecipeSO nextRecipe = gameRecipes[currentRound + 1];

            if (nextRecipeNameText != null) nextRecipeNameText.text = nextRecipe.recipeName; else Debug.LogWarning("nextRecipeNameText is not assigned in DisplayRecipes (next recipe).");

            string nextReqAndStatsText = "";

            if (nextRecipe.requiredIngredients != null && nextRecipe.requiredIngredients.Count > 0)
            {
                nextReqAndStatsText += "Ingredients:\n";
                foreach (var card in nextRecipe.requiredIngredients) nextReqAndStatsText += $"- {card.cardName}\n";
            }

            if (nextRecipe.requiredTechniques != null && nextRecipe.requiredTechniques.Count > 0)
            {
                if (nextReqAndStatsText.Length > 0 && !nextReqAndStatsText.EndsWith("\n")) nextReqAndStatsText += "\n";
                nextReqAndStatsText += "Techniques:\n";
                foreach (var card in nextRecipe.requiredTechniques) nextReqAndStatsText += $"- {card.cardName}\n";
            }


            if (nextRecipe.targetStats != null && nextRecipe.targetStats.Count > 0)
            {
                if (nextReqAndStatsText.Length > 0 && !nextReqAndStatsText.EndsWith("\n")) nextReqAndStatsText += "\n";
                nextReqAndStatsText += "Stats:\n";
                foreach (var stat in nextRecipe.targetStats) nextReqAndStatsText += $"- {stat.name}: {stat.targetValue}\n";
            }

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
            // Clear next recipe UI if no next recipe is available
            if (nextRecipeNameText != null) nextRecipeNameText.text = "Next Dish: N/A"; else Debug.LogWarning("nextRecipeNameText is not assigned (no next recipe).");
            if (nextRecipeRequirementsAndStatsText != null) nextRecipeRequirementsAndStatsText.text = ""; else Debug.LogWarning("nextRecipeRequirementsAndStatsText is not assigned (no next recipe).");
            Debug.Log("No next recipe to display.");
        }
    }

    void ShowPopupMessage(string message, bool autoHide)
    {
        // This method currently only logs and sets text.
        // You might want to implement a proper UI popup system here.
        if (popupMessageText != null) popupMessageText.text = message; else Debug.LogWarning("popupMessageText is not assigned.");
        Debug.Log($"[Game Message] {message}");

        // If autoHide is true, you might want to start a Coroutine to hide the message after a delay.
        // Example (requires adding `using System.Collections;` and a reference to the popup panel):
        // if (autoHide && drawPopupPanel != null) // Assuming popupMessageText is on drawPopupPanel
        // {
        //     StopCoroutine("HidePopupMessage"); // Stop any existing hide coroutine
        //     StartCoroutine("HidePopupMessage", 3f); // Hide after 3 seconds
        // }
    }

    // Example Coroutine to hide the popup message (needs to be implemented)
    // IEnumerator HidePopupMessage(float delay)
    // {
    //     yield return new WaitForSeconds(delay);
    //     if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
    //     if (popupMessageText != null) popupMessageText.text = ""; // Clear the text as well
    // }


    private List<RecipeSO> SelectRandomRecipes(int count)
    {
        List<RecipeSO> recipes = new List<RecipeSO>();
        Debug.Log("SelectRandomRecipes method entered.");

        if (allAvailableRecipes == null || allAvailableRecipes.Count == 0)
        {
            Debug.LogError("No available recipes found in the 'allAvailableRecipes' list! Cannot select recipes. Returning empty list.");
            return recipes; // Return empty list if no recipes are available
        }

        Debug.Log($"Selecting {count} recipes from {allAvailableRecipes.Count} available.");

        System.Random rand = new System.Random();
        // Create a copy of the available recipes list to select from
        List<RecipeSO> availableForSelection = new List<RecipeSO>(allAvailableRecipes);

        for (int i = 0; i < count; i++)
        {
            // If we run out of unique recipes before selecting 'count', stop
            if (availableForSelection.Count == 0)
            {
                Debug.LogWarning($"Could only select {i} unique recipes, needed {count}. Not enough unique recipes available.");
                break;
            }
            int randomIndex = rand.Next(availableForSelection.Count);
            RecipeSO selectedRecipe = availableForSelection[randomIndex];
            recipes.Add(selectedRecipe);
            availableForSelection.RemoveAt(randomIndex); // Remove selected recipe to ensure uniqueness
            Debug.Log($"Selected recipe {i + 1}: {selectedRecipe.recipeName}");
        }

        Debug.Log($"SelectRandomRecipes finished. Returning {recipes.Count} recipes.");
        return recipes; // Return the list of selected recipes
    }

    private int GetHighScore() { return PlayerPrefs.GetInt(HighScoreKey, 0); }
    private void SaveHighScore(int score) { PlayerPrefs.SetInt(HighScoreKey, score); PlayerPrefs.Save(); }
}
