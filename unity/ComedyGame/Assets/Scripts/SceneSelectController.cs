using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class SceneSelectController : MonoBehaviour
{
    [Header("Scene Navigation")]
    public string nextSceneName = "ComedyGame";

    [Header("UI References")]
    public TMP_Text selectedText;

    public Image examImage;
    public Image shiftImage;
    public Image fridgeImage;
    public Image weddingImage;
    public Image kitchenImage;

    [Header("Card Roots")]
    public RectTransform examCardRoot;
    public RectTransform shiftCardRoot;
    public RectTransform fridgeCardRoot;
    public RectTransform weddingCardRoot;
    public RectTransform kitchenCardRoot;

    [Header("Card Scale")]
    public Vector3 selectedScale = new Vector3(1.08f, 1.08f, 1f);
    public Vector3 unselectedScale = new Vector3(1f, 1f, 1f);

    private void Start()
    {
        if (string.IsNullOrEmpty(GameState.SelectedScene))
        {
            GameState.SelectedScene = "";
        }

        UpdateSelectedText();
        UpdateButtonHighlights();
    }

    public void SelectExam()
    {
        GameState.SelectedScene = "sc_exam_panic";
        UpdateSelectedText();
        UpdateButtonHighlights();
    }

    public void SelectShift()
    {
        GameState.SelectedScene = "sc_shift_gone_wrong";
        UpdateSelectedText();
        UpdateButtonHighlights();
    }

    public void SelectFridge()
    {
        GameState.SelectedScene = "sc_fridge_marathon";
        UpdateSelectedText();
        UpdateButtonHighlights();
    }

    public void SelectWedding()
    {
        GameState.SelectedScene = "sc_wedding_prep_chaos";
        UpdateSelectedText();
        UpdateButtonHighlights();
    }

    public void SelectKitchen()
    {
        GameState.SelectedScene = "sc_comedy_kitchen_disaster";
        UpdateSelectedText();
        UpdateButtonHighlights();
    }

    public void ContinueToGame()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    public void GoBack()
    {
        SceneManager.LoadScene("CharacterSelect");
    }

    private void UpdateSelectedText()
    {
        if (selectedText == null) return;

        switch (GameState.SelectedScene)
        {
            case "sc_exam_panic":
                selectedText.text = "Selected: Exam Panic";
                break;
            case "sc_shift_gone_wrong":
                selectedText.text = "Selected: Shift Gone Wrong";
                break;
            case "sc_fridge_marathon":
                selectedText.text = "Selected: Fridge Marathon";
                break;
            case "sc_wedding_prep_chaos":
                selectedText.text = "Selected: Wedding Prep Chaos";
                break;
            case "sc_comedy_kitchen_disaster":
                selectedText.text = "Selected: Kitchen Disaster";
                break;
            default:
                selectedText.text = "Choose a scene";
                break;
        }
    }

    private void UpdateButtonHighlights()
    {
        // Keep all artwork clean
        if (examImage != null) examImage.color = Color.white;
        if (shiftImage != null) shiftImage.color = Color.white;
        if (fridgeImage != null) fridgeImage.color = Color.white;
        if (weddingImage != null) weddingImage.color = Color.white;
        if (kitchenImage != null) kitchenImage.color = Color.white;

        // Reset all scales
        if (examCardRoot != null) examCardRoot.localScale = unselectedScale;
        if (shiftCardRoot != null) shiftCardRoot.localScale = unselectedScale;
        if (fridgeCardRoot != null) fridgeCardRoot.localScale = unselectedScale;
        if (weddingCardRoot != null) weddingCardRoot.localScale = unselectedScale;
        if (kitchenCardRoot != null) kitchenCardRoot.localScale = unselectedScale;

        // Apply selected scale
        switch (GameState.SelectedScene)
        {
            case "sc_exam_panic":
                if (examCardRoot != null) examCardRoot.localScale = selectedScale;
                break;
            case "sc_shift_gone_wrong":
                if (shiftCardRoot != null) shiftCardRoot.localScale = selectedScale;
                break;
            case "sc_fridge_marathon":
                if (fridgeCardRoot != null) fridgeCardRoot.localScale = selectedScale;
                break;
            case "sc_wedding_prep_chaos":
                if (weddingCardRoot != null) weddingCardRoot.localScale = selectedScale;
                break;
            case "sc_comedy_kitchen_disaster":
                if (kitchenCardRoot != null) kitchenCardRoot.localScale = selectedScale;
                break;
        }
    }
}