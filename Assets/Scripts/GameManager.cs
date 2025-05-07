// GameManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System.Collections; // Added for Coroutines

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
    public Button playAgainButton;
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


    void Awake()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
        if (scoringResultsPanel != null) scoringResultsPanel.SetActive(false);
        if (cancelTechniqueButton != null) cancelTechniqueButton.gameObject.SetActive(false);

        if (deck1 != null) deck1.Initialize("Ingredient/Spice Deck", new List<CardSO>(initialDeck1Cards));
        if (deck2 != null) deck2.Initialize("Tool/Technique Deck", new List<CardSO>(initialDeck2Cards));

        Debug.Log("Spice Stat Mapping Initialized.");

        if (keepButton != null) keepButton.onClick.AddListener(KeepDrawnCard);
        if (discardButton != null) discardButton.onClick.AddListener(() => DiscardDrawnCard(false));
        if (combineButton != null) combineButton.onClick.AddListener(CombineDrawnCard);

        if (finishCookingButton != null) finishCookingButton.onClick.AddListener(FinishCooking);
        if (continueButton != null) continueButton.onClick.AddListener(ProceedFromRoundEnd);
        if (cancelTechniqueButton != null) cancelTechniqueButton.onClick.AddListener(CancelTechniqueSelection);
        if (playAgainButton != null) playAgainButton.onClick.AddListener(RestartGame);
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
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (gameUIPanel != null) gameUIPanel.SetActive(false);
        if (gameEndPanel != null) gameEndPanel.SetActive(false);
        if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
        if (scoringResultsPanel != null) scoringResultsPanel.SetActive(false);

        CancelTechniqueSelection();

        switch (state)
        {
            case GameState.MainMenu:
                Debug.Log("Entered Main Menu State");
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                break;

            case GameState.GameSetup:
                Debug.Log("Entered Game Setup State");
                currentRound = 0;
                totalScore = 0;
                hand.Clear();
                lockedCard = null;
                currentDiscardsThisRound = 0;
                playedCardsThisRound.Clear();
                UpdatePlayedCardsUI();
                CancelTechniqueSelection();

                Debug.Log($"GameSetup: Starting recipe selection. allAvailableRecipes count: {(allAvailableRecipes != null ? allAvailableRecipes.Count : 0)}");

                gameRecipes = SelectRandomRecipes(totalRounds);

                if (gameRecipes != null)
                {
                    Debug.Log($"GameSetup: SelectRandomRecipes finished. gameRecipes count: {gameRecipes.Count}");
                }
                else
                {
                    Debug.LogError("GameSetup: gameRecipes is null after SelectRandomRecipes!");
                }

                if (gameUIPanel != null) gameUIPanel.SetActive(true);
                UpdateHandUI();
                UpdatePlayedCardsUI();

                ChangeState(GameState.DrawingPhase);
                break;

            case GameState.DrawingPhase:
                Debug.Log($"--- Starting Round {currentRound + 1}/{totalRounds} - Drawing Phase ---");
                if (gameUIPanel != null) gameUIPanel.SetActive(true);
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);

                currentDiscardsThisRound = 0;

                Debug.Log($"DrawingPhase: Calling DisplayRecipes. gameRecipes count before check: {(gameRecipes != null ? gameRecipes.Count : 0)}");

                DisplayRecipes();

                if (currentRoundText != null) currentRoundText.text = $"Round: {currentRound + 1}/{totalRounds}";
                if (currentScoreText != null) currentScoreText.text = $"Score: {totalScore}";
                if (discardsLeftText != null) discardsLeftText.text = $"Discards Left: {maxDiscardsPerRound - currentDiscardsThisRound}";

                if (lockedCard != null)
                {
                    hand.Add(lockedCard);
                    lockedCard = null;
                    UpdateHandUI();
                    Announcements.instance.Announce("Locked card added to hand.", 3f); // Modified
                }

                UpdateHandUI();
                UpdatePlayedCardsUI();

                SetHandCardInteractivity(false);
                if (finishCookingButton != null) finishCookingButton.gameObject.SetActive(false);
                HighlightSelectableCards();
                break;

            case GameState.CookingPhase:
                Debug.Log("Entered Cooking Phase State");
                if (gameUIPanel != null) gameUIPanel.SetActive(true);
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);

                playedCardsThisRound.Clear();
                UpdatePlayedCardsUI();
                CancelTechniqueSelection();

                UpdateHandUI();

                if (finishCookingButton != null) finishCookingButton.gameObject.SetActive(true);

                SetHandCardInteractivity(true);
                HighlightSelectableCards();
                break;

            case GameState.ScoringPhase:
                Debug.Log("Entered Scoring Phase State");
                if (gameUIPanel != null) gameUIPanel.SetActive(true);
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
                if (scoringResultsPanel != null) scoringResultsPanel.SetActive(true);

                SetHandCardInteractivity(false);
                CancelTechniqueSelection();
                UpdatePlayedCardsUI();

                int roundScore = CalculateRoundScore();
                totalScore += roundScore;
                Debug.Log($"Round {currentRound + 1} Score: {roundScore}. Total Score: {totalScore}");

                if (roundScoreText != null) roundScoreText.text = $"Round {currentRound + 1} Score: {roundScore}";

                if (currentScoreText != null) currentScoreText.text = $"Score: {totalScore}";


                if (continueButton != null) continueButton.gameObject.SetActive(true);

                break;

            case GameState.RoundEnd:
                Debug.Log("Entered Round End State");
                if (gameUIPanel != null) gameUIPanel.SetActive(true);
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);

                SetHandCardInteractivity(false);
                CancelTechniqueSelection();
                UpdatePlayedCardsUI();

                break;

            case GameState.GameEnd:
                Debug.Log("Entered Game End State. Final Score: " + totalScore);
                if (gameEndPanel != null) gameEndPanel.SetActive(true);
                if (gameUIPanel != null) gameUIPanel.SetActive(false);
                if (scoringResultsPanel != null) scoringResultsPanel.SetActive(false);
                if (drawPopupPanel != null) drawPopupPanel.SetActive(false);


                SetHandCardInteractivity(false);
                CancelTechniqueSelection();
                UpdatePlayedCardsUI();

                if (finalScoreText != null) finalScoreText.text = "Final Score: " + totalScore.ToString();

                int highScore = GetHighScore();
                if (totalScore > highScore)
                {
                    highScore = totalScore;
                    SaveHighScore(highScore);
                    Debug.Log("New High Score!");
                }
                if (highScoreText != null) highScoreText.text = "High Score: " + highScore.ToString();

                if (playAgainButton != null)
                {
                    playAgainButton.gameObject.SetActive(true);
                    playAgainButton.interactable = true;
                }
                if (backToMenuButton != null)
                {
                    backToMenuButton.gameObject.SetActive(true);
                    backToMenuButton.interactable = true;
                }

                if (continueButton != null) continueButton.gameObject.SetActive(false);
                if (finishCookingButton != null) finishCookingButton.gameObject.SetActive(false);
                if (currentRoundText != null) currentRoundText.gameObject.SetActive(false);
                if (currentScoreText != null) currentScoreText.gameObject.SetActive(false);
                if (discardsLeftText != null) discardsLeftText.gameObject.SetActive(false);
                if (currentRecipeNameText != null) currentRecipeNameText.gameObject.SetActive(false);
                if (currentRecipeRequirementsAndStatsText != null) currentRecipeRequirementsAndStatsText.gameObject.SetActive(false);
                if (nextRecipeNameText != null) nextRecipeNameText.gameObject.SetActive(false);
                if (nextRecipeRequirementsAndStatsText != null) nextRecipeRequirementsAndStatsText.gameObject.SetActive(false);
                if (playedCardsPanel != null) playedCardsPanel.gameObject.SetActive(false);


                break;
        }
    }

    public void StartGame()
    {
        ChangeState(GameState.GameSetup);
    }

    public void RestartGame()
    {
        StartGame();
    }

    public void ReturnToMainMenu()
    {
        currentRound = 0;
        totalScore = 0;
        hand.Clear();
        lockedCard = null;
        currentDiscardsThisRound = 0;
        playedCardsThisRound.Clear();
        UpdatePlayedCardsUI();
        CancelTechniqueSelection();

        ChangeState(GameState.MainMenu);
    }

    public void DrawCardFromDeck(Deck deckToDrawFrom)
    {
        if (currentState != GameState.DrawingPhase) { Debug.LogWarning("Attempted to draw card outside of Drawing Phase."); Announcements.instance.Announce("Can only draw cards during the Drawing Phase!", 3f); return; } // Modified
        if (hand.Count >= handLimit) { Announcements.instance.Announce("Hand is full! Drawing Phase ends.", 3f); return; } // Modified

        currentlyDrawnCard = deckToDrawFrom.DrawCard();

        if (currentlyDrawnCard != null) { ShowDrawPopup(currentlyDrawnCard); }
        else { Debug.LogError($"Error: Could not draw a card from {deckToDrawFrom.name}."); Announcements.instance.Announce($"Error drawing card from {deckToDrawFrom.name}.", 3f); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; } // Modified
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

        // popupMessageText is used for the draw popup itself, not general announcements
        if (popupMessageText != null) popupMessageText.text = "Choose an action:";
        if (keepButton != null) keepButton.interactable = true;
        if (discardButton != null) discardButton.interactable = (currentDiscardsThisRound < maxDiscardsPerRound);
    }

    void KeepDrawnCard()
    {
        if (currentState != GameState.DrawingPhase || currentlyDrawnCard == null || hand.Count >= handLimit) { if (hand.Count >= handLimit) Announcements.instance.Announce("Hand is already full! Cannot keep card.", 3f); Debug.LogWarning("KeepDrawnCard called in invalid state or conditions."); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; return; } // Modified

        hand.Add(currentlyDrawnCard);
        UpdateHandUI();

        if (drawPopupPanel != null) drawPopupPanel.SetActive(false);
        currentlyDrawnCard = null;

        if (hand.Count >= handLimit) { Announcements.instance.Announce("Hand is full! Drawing Phase ends.", 3f); ChangeState(GameState.CookingPhase); } // Modified
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
        else { Announcements.instance.Announce($"No discards left this round ({currentDiscardsThisRound}/{maxDiscardsPerRound})!", 3f); } // Modified
    }

    void CombineDrawnCard()
    {
        if (currentState != GameState.DrawingPhase || currentlyDrawnCard == null || currentlyDrawnCard.cardType != CardSO.CardType.Spice) { Debug.LogWarning($"CombineDrawnCard called in invalid state ({currentState}), no card drawn, or card is not a Spice."); Announcements.instance.Announce("Combination not possible.", 3f); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; return; } // Modified

        CardSO cardInHandToCombine = hand.FirstOrDefault(c => c.cardName == currentlyDrawnCard.cardName);

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

                    Announcements.instance.Announce($"Combined {currentlyDrawnCard.cardName}! {statName} increased on the card in hand.", 3f); // Modified

                    if (hand.Count >= handLimit) { Announcements.instance.Announce("Hand is full! Drawing Phase ends.", 3f); ChangeState(GameState.CookingPhase); } // Modified
                }
                else { Debug.LogWarning($"Card in hand ({cardInHandToCombine.cardName}) does not have stat '{statName}'. Cannot combine."); Announcements.instance.Announce($"Cannot combine: Card in hand does not have the required stat.", 3f); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; } // Modified
            }
            else { Debug.LogWarning($"Spice '{currentlyDrawnCard.cardName}' has no defined stat mapping."); Announcements.instance.Announce($"Cannot combine: '{currentlyDrawnCard.cardName}' does not affect a known stat type.", 3f); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; } // Modified
        }
        else { Debug.LogError("CombineDrawnCard called but no matching card found in hand."); Announcements.instance.Announce("No matching card in hand to combine!", 3f); if (drawPopupPanel != null) drawPopupPanel.SetActive(false); currentlyDrawnCard = null; } // Modified
    }

    public void PlayCard(CardSO cardToPlay)
    {
        if (currentState != GameState.CookingPhase)
        {
            Debug.LogWarning($"Attempted to play card '{cardToPlay.cardName}' outside of Cooking Phase. Current State: {currentState}");
            Announcements.instance.Announce("You can only play cards during the Cooking Phase!", 3f); // Modified
            return;
        }
        if (!hand.Contains(cardToPlay))
        {
            Debug.LogWarning($"Attempted to play card '{cardToPlay.cardName}' not found in hand.");
            Announcements.instance.Announce("Card not found in hand!", 3f); // Modified
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
                        Announcements.instance.Announce($"Played {cardToPlay.cardName}.", 3f); // Modified
                        UpdateHandUI();
                        UpdatePlayedCardsUI();
                        break;

                    case CardSO.CardType.Tool:
                        Debug.Log($"Attempted to play Tool directly: {cardToPlay.cardName}");
                        Announcements.instance.Announce("You need to select a Technique first to use a Tool.", 3f); // Modified
                        break;

                    case CardSO.CardType.Technique:
                        if (hand.Any(c => c.cardType == CardSO.CardType.Tool))
                        {
                            Debug.Log($"Initiating Technique: {cardToPlay.cardName}. Select a Tool.");
                            selectedTechnique = cardToPlay;
                            currentCookingSelectionState = CookingSelectionState.WaitingForTool;
                            Announcements.instance.Announce($"Selected {cardToPlay.cardName}. Now select a Tool from your hand.", 3f); // Modified
                            HighlightSelectableCards();
                        }
                        else
                        {
                            Debug.LogWarning($"Attempted to play Technique '{cardToPlay.cardName}' but no Tool cards available in hand.");
                            Announcements.instance.Announce($"You need a Tool card in your hand to use {cardToPlay.cardName}.", 3f); // Modified
                        }
                        break;

                    default:
                        Debug.LogWarning($"Attempted to play card with unhandled type: {cardToPlay.cardType}");
                        Announcements.instance.Announce("Cannot play this type of card directly.", 3f); // Modified
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
                        Announcements.instance.Announce($"Selected Tool: {cardToPlay.cardName}. Now select an Ingredient from your hand.", 3f); // Modified
                        HighlightSelectableCards();
                    }
                    else
                    {
                        Debug.LogWarning($"Attempted to select Tool '{cardToPlay.cardName}' but no Ingredient cards available in hand.");
                        Announcements.instance.Announce($"You need an Ingredient card in your hand to use {selectedTechnique.cardName} with {cardToPlay.cardName}.", 3f); // Modified
                    }
                }
                else
                {
                    Debug.LogWarning($"Attempted to select non-Tool card ({cardToPlay.cardName}, Type: {cardToPlay.cardType}) when waiting for Tool.");
                    Announcements.instance.Announce("You must select a Tool card.", 3f); // Modified
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
                    Announcements.instance.Announce("You must select an Ingredient card.", 3f); // Modified
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

            Announcements.instance.Announce($"Used {selectedTechnique.cardName} with {selectedTool.cardName} on {selectedIngredient.cardName}!", 3f); // Modified

            selectedTechnique = null;
            selectedTool = null;
            selectedIngredient = null;
            currentCookingSelectionState = CookingSelectionState.None;

            UpdateHandUI();
            UpdatePlayedCardsUI();
            SetHandCardInteractivity(true);
            HighlightSelectableCards();
        }
        else
        {
            Debug.LogWarning("Attempted to complete Technique combination but cards were missing or not in hand. Cancelling selection.");
            Announcements.instance.Announce("Error during combination. Please try again.", 3f); // Modified
            CancelTechniqueSelection();
        }
    }

    public void CancelTechniqueSelection()
    {
        if (currentCookingSelectionState != CookingSelectionState.None)
        {
            Debug.Log("Technique selection cancelled by player or error.");
            selectedTechnique = null;
            selectedTool = null;
            selectedIngredient = null;
            currentCookingSelectionState = CookingSelectionState.None;
            Announcements.instance.Announce("Technique selection cancelled.", 3f); // Modified
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
            ChangeState(GameState.ScoringPhase);
        }
        else if (currentState == GameState.CookingPhase && currentCookingSelectionState != CookingSelectionState.None)
        {
            Debug.LogWarning("Attempted to finish cooking while a technique selection is in progress.");
            Announcements.instance.Announce("Finish your technique selection first, or cancel it.", 3f); // Modified
        }
        else
        {
            Debug.LogWarning("Attempted to finish cooking outside of Cooking Phase.");
            Announcements.instance.Announce("Can only finish cooking during the Cooking Phase!", 3f); // Modified
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
            if (scoreBreakdownText != null) scoreBreakdownText.text = breakdownText;
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
        if (currentRecipe.requiredTechniques != null && playedTechniques.Count == currentRecipe.requiredTechniques.Count)
        {
            roundScore += TechniqueAmountScore;
        }


        int ingredientTypeMatches = CountMatchingRequiredCards(currentRecipe.requiredIngredients, playedIngredients);
        int ingredientScore = ingredientTypeMatches * IngredientTypeScore;
        roundScore += ingredientScore;

        int spiceTypeMatches = CountMatchingRequiredCards(currentRecipe.requiredIngredients.Where(c => c.cardType == CardSO.CardType.Spice).ToList(), playedSpices);
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

        if (scoreBreakdownText != null) scoreBreakdownText.text = breakdownText;


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
        if (currentState == GameState.ScoringPhase || currentState == GameState.RoundEnd)
        {
            if (currentRound < totalRounds - 1)
            {
                if (lockedCard != null)
                {
                    CardSO lockedCardInHand = hand.FirstOrDefault(c => c == lockedCard);
                    if (lockedCardInHand != null)
                    {
                        hand.Remove(lockedCardInHand);
                    }
                }
                hand.Clear();
                if (lockedCard != null)
                {
                    hand.Add(lockedCard);
                    Debug.Log($"Locked card '{lockedCard.cardName}' carried over to Round {currentRound + 1}.");
                }
                playedCardsThisRound.Clear();
                UpdatePlayedCardsUI();
                currentDiscardsThisRound = 0;

                UpdateHandUI();
                currentRound++;

                DisplayRecipes();
                if (currentRoundText != null) currentRoundText.text = $"Round: {currentRound + 1}/{totalRounds}";
                if (discardsLeftText != null) discardsLeftText.text = $"Discards Left: {maxDiscardsPerRound - currentDiscardsThisRound}";


                ChangeState(GameState.DrawingPhase);
            }
            else
            {
                Debug.Log("All rounds completed. Ending game.");
                ChangeState(GameState.GameEnd);
            }
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (scoringResultsPanel != null) scoringResultsPanel.gameObject.SetActive(false);
        }
    }

    void UpdateHandUI()
    {
        if (handPanel != null)
        {
            foreach (Transform child in handPanel.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (var cardSO in hand)
            {
                GameObject cardUIObject = Instantiate(cardUIPrefab, handPanel.transform);

                CardDisplayUI displayScript = cardUIObject.GetComponent<CardDisplayUI>();
                if (displayScript != null)
                {
                    displayScript.SetCard(cardSO);
                    displayScript.SetGameManager(this);
                }
                else
                {
                    Debug.LogError("CardDisplayUI script not found on cardUIPrefab. Card UI will not display correctly or be interactive.");
                    TextMeshProUGUI cardText = cardUIObject.GetComponentInChildren<TextMeshProUGUI>();
                    if (cardText != null) cardText.text = cardSO.cardName;
                    Image cardImage = cardUIObject.GetComponentInChildren<Image>();
                    if (cardImage != null && cardSO.artwork != null) cardImage.sprite = cardSO.artwork;
                }
            }
        }
        if (discardsLeftText != null)
        {
            // handSizeText.text = $"Hand: {hand.Count}/{handLimit}";
        }

        HighlightSelectableCards();
    }

    void UpdatePlayedCardsUI()
    {
        if (playedCardsPanel != null)
        {
            playedCardsPanel.gameObject.SetActive(playedCardsThisRound.Count > 0 || currentState == GameState.ScoringPhase || currentState == GameState.RoundEnd);

            foreach (Transform child in playedCardsPanel.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (var cardSO in playedCardsThisRound)
            {
                GameObject cardUIObject = Instantiate(cardUIPrefab, playedCardsPanel.transform);

                CardDisplayUI displayScript = cardUIObject.GetComponent<CardDisplayUI>();
                if (displayScript != null)
                {
                    displayScript.SetCard(cardSO);
                    displayScript.SetInteractive(false);
                    displayScript.SetHighlight(false);
                }
                else
                {
                    Debug.LogError("CardDisplayUI script not found on cardUIPrefab for played cards. Card UI will not display correctly.");
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
        if (handPanel == null) return;

        CardDisplayUI[] cardUIs = handPanel.GetComponentsInChildren<CardDisplayUI>();
        foreach (var cardUI in cardUIs)
        {
            cardUI.SetInteractive(isInteractive);
        }
    }

    void HighlightSelectableCards()
    {
        if (handPanel == null) return;

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
                        isSelectable = false;
                        cardUI.SetInteractive(currentState == GameState.CookingPhase);
                        break;

                    case CookingSelectionState.WaitingForTool:
                        if (cardSO.cardType == CardSO.CardType.Tool)
                        {
                            isSelectable = true;
                            cardUI.SetInteractive(true);
                        }
                        else
                        {
                            isSelectable = false;
                            cardUI.SetInteractive(false);
                        }
                        break;

                    case CookingSelectionState.WaitingForIngredient:
                        if (cardSO.cardType == CardSO.CardType.Ingredient)
                        {
                            isSelectable = true;
                            cardUI.SetInteractive(true);
                        }
                        else
                        {
                            isSelectable = false;
                            cardUI.SetInteractive(false);
                        }
                        break;
                }
            }
            cardUI.SetHighlight(isSelectable);
        }
        if (finishCookingButton != null)
        {
            finishCookingButton.interactable = (currentState == GameState.CookingPhase && currentCookingSelectionState == CookingSelectionState.None);
            finishCookingButton.gameObject.SetActive(currentState == GameState.CookingPhase);
        }
        if (cancelTechniqueButton != null)
        {
            cancelTechniqueButton.gameObject.SetActive(currentCookingSelectionState != CookingSelectionState.None);
            cancelTechniqueButton.interactable = (currentCookingSelectionState != CookingSelectionState.None);
        }
    }

    void DisplayRecipes()
    {
        Debug.Log("DisplayRecipes method entered.");

        if (gameRecipes == null || gameRecipes.Count <= currentRound)
        {
            Debug.LogError($"Cannot display current recipe for round {currentRound}. gameRecipes list is null or has only {gameRecipes?.Count ?? 0} recipes. Returning from DisplayRecipes.");
            if (currentRecipeNameText != null) currentRecipeNameText.text = "Error: No Recipe";
            if (currentRecipeRequirementsAndStatsText != null) currentRecipeRequirementsAndStatsText.text = "";
            if (nextRecipeNameText != null) nextRecipeNameText.text = "Next Dish: N/A";
            if (nextRecipeRequirementsAndStatsText != null) nextRecipeRequirementsAndStatsText.text = "";
            return;
        }

        RecipeSO currentRecipe = gameRecipes[currentRound];
        if (currentRecipeNameText != null) currentRecipeNameText.text = currentRecipe.recipeName;

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

        if (gameRecipes != null && currentRound + 1 < gameRecipes.Count)
        {
            RecipeSO nextRecipe = gameRecipes[currentRound + 1];

            if (nextRecipeNameText != null) nextRecipeNameText.text = nextRecipe.recipeName;

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
            if (nextRecipeNameText != null) nextRecipeNameText.text = "Next Dish: N/A";
            if (nextRecipeRequirementsAndStatsText != null) nextRecipeRequirementsAndStatsText.text = "";
            Debug.Log("No next recipe to display.");
        }
    }

    // Modified ShowPopupMessage to use Announcements
    void ShowPopupMessage(string message, bool autoHide)
    {
        // This method is now primarily for the draw popup's message text if needed,
        // general game announcements will use the Announcements system.
        // Consider if popupMessageText should still be used or if Announcements should handle all text feedback.
        // For now, keeping it for the draw popup's "Choose an action:" message and using Announcements for others.

        // If you want all messages to go through Announcements:
        // Announcements.instance.Announce(message, autoHide ? 3f : 0f); // Use 3s for auto-hide, 0s for manual close (not implemented in Announcements yet)

        // If you want to keep the draw popup's specific message:
        if (popupMessageText != null && drawPopupPanel != null && drawPopupPanel.activeSelf)
        {
            popupMessageText.text = message;
        }
        Debug.Log($"[Game Message] {message}");
    }

    private List<RecipeSO> SelectRandomRecipes(int count)
    {
        List<RecipeSO> recipes = new List<RecipeSO>();
        Debug.Log("SelectRandomRecipes method entered.");

        if (allAvailableRecipes == null || allAvailableRecipes.Count == 0)
        {
            Debug.LogError("No available recipes found in the 'allAvailableRecipes' list! Cannot select recipes. Returning empty list.");
            return recipes;
        }

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
            Debug.Log($"Selected recipe {i + 1}: {selectedRecipe.recipeName}");
        }

        Debug.Log($"SelectRandomRecipes finished. Returning {recipes.Count} recipes.");
        return recipes;
    }

    private int GetHighScore() { return PlayerPrefs.GetInt(HighScoreKey, 0); }
    private void SaveHighScore(int score) { PlayerPrefs.SetInt(HighScoreKey, score); PlayerPrefs.Save(); }
}