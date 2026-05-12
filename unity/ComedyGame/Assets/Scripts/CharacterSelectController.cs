using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectController : MonoBehaviour
{
    [Header("Scene Navigation")]
    public string nextSceneName = "SceneSelect";

    [Header("UI References")]
    public TMP_Text characterInfoText;

    public Image adaNamePlate;
    public Image bazNamePlate;

    public RectTransform adaCardRoot;
    public RectTransform bazCardRoot;

    [Header("Name Plate Colours")]
    public Color adaSelectedPlate = new Color32(242, 143, 165, 255);
    public Color adaUnselectedPlate = new Color32(242, 143, 165, 170);

    public Color bazSelectedPlate = new Color32(105, 201, 255, 255);
    public Color bazUnselectedPlate = new Color32(105, 201, 255, 170);

    [Header("Card Scale")]
    public Vector3 selectedScale = new Vector3(1.05f, 1.05f, 1f);
    public Vector3 unselectedScale = new Vector3(1f, 1f, 1f);

    private void Start()
    {
        if (string.IsNullOrEmpty(GameState.SelectedCharacter))
        {
            GameState.SelectedCharacter = "ch_ada";
        }

        UpdateUI();
    }

    public void SelectAda()
    {
        GameState.SelectedCharacter = "ch_ada";
        UpdateUI();
    }

    public void SelectBaz()
    {
        GameState.SelectedCharacter = "ch_baz";
        UpdateUI();
    }

    public void ContinueToGame()
    {
        SceneManager.LoadScene(nextSceneName);
    }

    public void GoBack()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateUI()
    {
        bool adaSelected = GameState.SelectedCharacter == "ch_ada";
        bool bazSelected = GameState.SelectedCharacter == "ch_baz";

        if (characterInfoText != null)
        {
            if (adaSelected)
            {
                characterInfoText.text =
                    "Ada • Supportive witty friend — calm humour and reassurance";
            }
            else if (bazSelected)
            {
                characterInfoText.text =
                    "Baz • Chaotic comedy bot — playful energy and quick jokes";
            }
        }

        if (adaNamePlate != null)
            adaNamePlate.color = adaSelected ? adaSelectedPlate : adaUnselectedPlate;

        if (bazNamePlate != null)
            bazNamePlate.color = bazSelected ? bazSelectedPlate : bazUnselectedPlate;

        if (adaCardRoot != null)
            adaCardRoot.localScale = adaSelected ? selectedScale : unselectedScale;

        if (bazCardRoot != null)
            bazCardRoot.localScale = bazSelected ? selectedScale : unselectedScale;
    }
}