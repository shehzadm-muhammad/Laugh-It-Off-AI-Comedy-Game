using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EndSceneSummaryController : MonoBehaviour
{
    [Header("UI References")]
    public Image portraitImage;
    public TMP_Text characterNameText;
    public TMP_Text sceneNameText;

    public TMP_Text summaryHeaderText;
    public TMP_Text laughStyleText;
    public TMP_Text sceneCompletedText;
    public TMP_Text progressText;
    public TMP_Text turnsPlayedText;
    public TMP_Text bestLineLabelText;
    public TMP_Text bestLineText;
    public TMP_Text ratingPromptText;
    public TMP_Text footerQuoteText;

    [Header("Character Sprites")]
    public Sprite adaSprite;
    public Sprite bazSprite;

    private void Start()
    {
        PopulateSummaryScreen();
    }

    private void PopulateSummaryScreen()
    {
        UpdateCharacter();
        UpdateSceneName();
        UpdateSummaryBreakdown();
        UpdateFooter();
    }

    private void UpdateCharacter()
    {
        string characterId = GameState.SelectedCharacter;

        if (characterNameText != null)
            characterNameText.text = (characterId == "ch_baz") ? "Baz" : "Ada";

        if (portraitImage != null)
        {
            if (characterId == "ch_baz" && bazSprite != null)
                portraitImage.sprite = bazSprite;
            else if (adaSprite != null)
                portraitImage.sprite = adaSprite;
        }
    }

    private void UpdateSceneName()
    {
        if (sceneNameText == null) return;
        sceneNameText.text = GetSceneDisplayName(GameState.SelectedScene);
    }

    private void UpdateSummaryBreakdown()
    {
        string characterId = GameState.SelectedCharacter;
        string summary = GameState.LastSummary ?? "";
        int turns = GameState.LastTurnCount;

        if (summaryHeaderText != null)
            summaryHeaderText.text = "Great job! Here’s how you did...";

        if (laughStyleText != null)
        {
            laughStyleText.text = (characterId == "ch_baz")
                ? "Laugh Style: Chaotic & Playful"
                : "Laugh Style: Supportive & Witty";
        }

        if (sceneCompletedText != null)
            sceneCompletedText.text = "Scene: " + GetSceneDisplayName(GameState.SelectedScene);

        if (progressText != null)
            progressText.text = "Progress: " + GetProgressLine(summary, characterId);

        if (turnsPlayedText != null)
            turnsPlayedText.text = "Turns: " + Mathf.Max(1, turns);

        if (bestLineLabelText != null)
            bestLineLabelText.text = "Best Line:";

        if (bestLineText != null)
        {
            bestLineText.text = !string.IsNullOrWhiteSpace(GameState.LastBestLine)
                ? GameState.LastBestLine
                : GetBestLine(characterId);
        }

        if (ratingPromptText != null)
            ratingPromptText.text = "Rate this session";
    }

    private void UpdateFooter()
    {
        if (footerQuoteText == null) return;

        if (!string.IsNullOrEmpty(GameState.LastFunnyLine))
            footerQuoteText.text = GameState.LastFunnyLine;
        else
            footerQuoteText.text = "A little laughter goes a long way.";
    }

    private string GetSceneDisplayName(string sceneId)
    {
        switch (sceneId)
        {
            case "sc_exam_panic": return "Exam Panic";
            case "sc_shift_gone_wrong": return "Shift Gone Wrong";
            case "sc_fridge_marathon": return "Fridge Marathon";
            case "sc_wedding_prep_chaos": return "Wedding Prep Chaos";
            case "sc_comedy_kitchen_disaster": return "Comedy Kitchen Disaster";
            default: return "Scene Complete";
        }
    }

    private string GetProgressLine(string summary, string characterId)
    {
        string lower = summary.ToLower();

        if (lower.Contains("improved"))
            return "Your session ended on a better note";

        if (lower.Contains("steady"))
            return "Steady progress still counts";

        if (characterId == "ch_baz")
            return "You got a light reset and a breather";

        return "You gave yourself a small reset";
    }

    private string GetBestLine(string characterId)
    {
        if (characterId == "ch_baz")
            return "\"Filed under dramatic but solvable.\"";

        return "\"One difficult moment does not define you.\"";
    }

    public void SetRatingHilarious()
    {
        GameState.LastRating = "Hilarious";
    }

    public void SetRatingPrettyFunny()
    {
        GameState.LastRating = "Pretty Funny";
    }

    public void SetRatingItWasOkay()
    {
        GameState.LastRating = "It Was Okay";
    }

    public void SetRatingMeh()
    {
        GameState.LastRating = "Meh";
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene("ComedyGame");
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}