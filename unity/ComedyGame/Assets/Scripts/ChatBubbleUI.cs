using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatBubbleUI : MonoBehaviour
{
    [Header("Bubble Prefabs")]
    public GameObject aiBubblePrefab;
    public GameObject userBubblePrefab;

    [Header("Chat Content")]
    public Transform contentRoot;
    public ScrollRect chatScrollRect;

    [Header("AI Avatar Sprites")]
    public Sprite adaAvatarSprite;
    public Sprite bazAvatarSprite;

    public void AddAIMessage(string message, string characterId)
    {
        AddMessage(aiBubblePrefab, message, true, characterId);
    }

    public void AddUserMessage(string message)
    {
        AddMessage(userBubblePrefab, message, false, "");
    }

    private void AddMessage(GameObject prefab, string message, bool isAI, string characterId)
{
    if (prefab == null || contentRoot == null) return;

    GameObject bubble = Instantiate(prefab, contentRoot);

    TMP_Text bubbleText = bubble.GetComponentInChildren<TMP_Text>();
    if (bubbleText != null)
        bubbleText.text = message;

    if (isAI)
    {
        Image[] images = bubble.GetComponentsInChildren<Image>(true);

        foreach (Image img in images)
        {
            if (img.gameObject.name == "AIAvatar")
            {
                img.sprite = GetAvatarSpriteFromCharacterId(characterId);
                break;
            }
        }
    }

    Canvas.ForceUpdateCanvases();

    if (chatScrollRect != null)
        chatScrollRect.verticalNormalizedPosition = 0f;
}

    private Sprite GetAvatarSpriteFromCharacterId(string characterId)
{
    return characterId == "ch_baz" ? bazAvatarSprite : adaAvatarSprite;
}

    public void ClearChat()
    {
        if (contentRoot == null) return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }
    }
}