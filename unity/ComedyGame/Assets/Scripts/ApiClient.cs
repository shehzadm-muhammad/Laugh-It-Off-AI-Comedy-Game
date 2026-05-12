using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class ApiClient : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField userInput;
    public ScrollRect chatScrollRect;

    public Image characterPortrait;
    public TMP_Text characterNameText;
    public Image sceneBackground;

    [Header("Bubble UI")]
    public ChatBubbleUI chatBubbleUI;

    [Header("Optional UI")]
    public Slider moodSlider;

    [Header("Character Sprites")]
    public Sprite adaSprite;
    public Sprite bazSprite;

    [Header("Scene Background Sprites")]
    public Sprite examBgSprite;
    public Sprite shiftBgSprite;
    public Sprite fridgeBgSprite;
    public Sprite weddingBgSprite;
    public Sprite comedyKitchenBgSprite;

    private const string GenerateUrl = "http://127.0.0.1:8000/generate";
    private string sessionId;
    private string lastAIReply = "";
    private bool openingMessageShown = false;

    [System.Serializable]
    private class ChatTurn
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    private class GenerateRequest
    {
        public string session_id;
        public string character_id;
        public string scene_id;
        public int mood;
        public string user_text;
        public List<ChatTurn> history;
    }

    [System.Serializable]
    private class GenerateResponse
    {
        public string reply_text;
        public bool safety_flag;
        public int retry_count;
        public int latency_ms;
    }

    private readonly List<ChatTurn> history = new List<ChatTurn>();
    private int moodBefore = 3;
    private int turnCount = 0;

    private void Start()
    {
        sessionId = System.Guid.NewGuid().ToString();

        if (string.IsNullOrEmpty(GameState.SelectedCharacter))
            GameState.SelectedCharacter = "ch_ada";

        if (string.IsNullOrEmpty(GameState.SelectedScene))
            GameState.SelectedScene = "sc_exam_panic";

        if (moodSlider != null)
        {
            moodSlider.minValue = 1;
            moodSlider.maxValue = 5;
            moodSlider.wholeNumbers = true;
            moodSlider.value = 3;
        }

        moodBefore = GetMood();
        GameState.LastBestLine = "";
        UpdateCharacterPortrait();
        UpdateCharacterName();
        UpdateSceneBackground();

        StartConversation();
    }

    private void StartConversation()
    {
        if (openingMessageShown) return;
        openingMessageShown = true;

        if (chatBubbleUI != null)
            chatBubbleUI.ClearChat();

        string openingLine = GetOpeningLine(GetSelectedCharacterId(), GetSelectedSceneId());
        
        lastAIReply = openingLine;
        GameState.LastBestLine = ExtractBestLine(openingLine);
        
        AddAIMessage(openingLine);
        history.Add(new ChatTurn { role = "assistant", content = openingLine });
    }

    private string GetOpeningLine(string characterId, string sceneId)
    {
        if (characterId == "ch_baz")
        {
            switch (sceneId)
            {
                case "sc_exam_panic":
                    return "Welcome to Exam Emergency Mode. On a scale from slightly doomed to academic supernova, where are we today?";
                case "sc_shift_gone_wrong":
                    return "Alright, chaos report time. How badly did this shift audition for a disaster movie?";
                case "sc_fridge_marathon":
                    return "Welcome, brave athlete. What snack dilemma are we dramatically facing tonight?";
                case "sc_wedding_prep_chaos":
                    return "Wedding chaos detected. What burst into glittery flames first?";
                case "sc_comedy_kitchen_disaster":
                    return "Kitchen incident unit speaking. What got burnt, dropped, exploded, or emotionally overcooked?";
                default:
                    return "Comedy support has arrived. What madness are we dealing with today?";
            }
        }
        else
        {
            switch (sceneId)
            {
                case "sc_exam_panic":
                    return "Hey, exam panic can feel huge, but you do not have to carry it alone. What part is stressing you most right now?";
                case "sc_shift_gone_wrong":
                    return "Hey, rough shifts can really drain you. What happened at work today?";
                case "sc_fridge_marathon":
                    return "Okay, no judgment zone. What food decision is causing tonight's mini crisis?";
                case "sc_wedding_prep_chaos":
                    return "Wedding prep can get overwhelming fast. What is the biggest stress right now?";
                case "sc_comedy_kitchen_disaster":
                    return "Cooking disasters happen to the best of us. Tell me what went wrong.";
                default:
                    return "Hey, I am here. What is stressing you today?";
            }
        }
    }

    private string GetSelectedCharacterId()
    {
        return string.IsNullOrEmpty(GameState.SelectedCharacter) ? "ch_ada" : GameState.SelectedCharacter;
    }

    private string GetSelectedSceneId()
    {
        return string.IsNullOrEmpty(GameState.SelectedScene) ? "sc_exam_panic" : GameState.SelectedScene;
    }

    private int GetMood()
    {
        if (moodSlider == null) return 3;
        return Mathf.RoundToInt(moodSlider.value);
    }

public void OnSendClicked()
{
    string text = userInput != null ? userInput.text.Trim() : "";

    if (string.IsNullOrWhiteSpace(text))
        return;

    AddUserMessage(text);

    history.Add(new ChatTurn { role = "user", content = text });
    turnCount += 1;
    GameState.LastTurnCount = turnCount;

    if (userInput != null)
    {
        userInput.text = "";
        userInput.ActivateInputField();
        userInput.Select();
    }

    StartCoroutine(Send(text));
}

    private IEnumerator Send(string text)
    {
        string characterId = GetSelectedCharacterId();
        string sceneId = GetSelectedSceneId();
        int mood = GetMood();

        var payload = new GenerateRequest
        {
            session_id = sessionId,
            character_id = characterId,
            scene_id = sceneId,
            mood = mood,
            user_text = text,
            history = history
        };

        string json = JsonUtility.ToJson(payload);

        using (var req = new UnityWebRequest(GenerateUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                AddAIMessage($"API error: {req.error}. Is backend running?");
                yield break;
            }

            var res = JsonUtility.FromJson<GenerateResponse>(req.downloadHandler.text);

            string reply = res != null && !string.IsNullOrEmpty(res.reply_text)
                ? res.reply_text
                : "I had a tiny comedy blackout there. Try me again.";
            
            lastAIReply = reply;
            GameState.LastBestLine = ExtractBestLine(reply);
            
            AddAIMessage(reply);
            history.Add(new ChatTurn { role = "assistant", content = reply });
        }
    }

    private void AddAIMessage(string text)
    {
        if (chatBubbleUI != null)
        {
        chatBubbleUI.AddAIMessage(text, GetSelectedCharacterId());
        }
        StartCoroutine(ScrollToBottomNextFrame());
    }

    private void AddUserMessage(string text)
    {
        if (chatBubbleUI != null)
            chatBubbleUI.AddUserMessage(text);

        StartCoroutine(ScrollToBottomNextFrame());
    }

   private IEnumerator ScrollToBottomNextFrame()
{
    yield return null;
    yield return new WaitForEndOfFrame();

    Canvas.ForceUpdateCanvases();

    if (chatScrollRect != null && chatScrollRect.content != null)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(chatScrollRect.content);
        chatScrollRect.verticalNormalizedPosition = 0f;
    }

    Canvas.ForceUpdateCanvases();
}
    private void UpdateCharacterPortrait()
    {
        if (characterPortrait == null) return;

        string characterId = GetSelectedCharacterId();

        if (characterId == "ch_ada" && adaSprite != null)
        {
            characterPortrait.sprite = adaSprite;
        }
        else if (characterId == "ch_baz" && bazSprite != null)
        {
            characterPortrait.sprite = bazSprite;
        }
    }

    private void UpdateCharacterName()
    {
        if (characterNameText == null) return;
        characterNameText.text = GetCharacterDisplayName();
    }

    private string GetCharacterDisplayName()
    {
        return GetSelectedCharacterId() == "ch_baz" ? "Baz" : "Ada";
    }

    private void UpdateSceneBackground()
    {
        if (sceneBackground == null) return;

        string sceneId = GetSelectedSceneId();

        if (sceneId == "sc_exam_panic" && examBgSprite != null)
        {
            sceneBackground.sprite = examBgSprite;
        }
        else if (sceneId == "sc_shift_gone_wrong" && shiftBgSprite != null)
        {
            sceneBackground.sprite = shiftBgSprite;
        }
        else if (sceneId == "sc_fridge_marathon" && fridgeBgSprite != null)
        {
            sceneBackground.sprite = fridgeBgSprite;
        }
        else if (sceneId == "sc_wedding_prep_chaos" && weddingBgSprite != null)
        {
            sceneBackground.sprite = weddingBgSprite;
        }
        else if (sceneId == "sc_comedy_kitchen_disaster" && comedyKitchenBgSprite != null)
        {
            sceneBackground.sprite = comedyKitchenBgSprite;
        }
    }
    private string ExtractBestLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "\"A little laughter goes a long way.\"";

        string cleaned = text.Replace("\n", " ").Trim();

        string[] parts = cleaned.Split(new char[] { '.', '!', '?' }, System.StringSplitOptions.RemoveEmptyEntries);

        string firstSentence = parts.Length > 0 ? parts[0].Trim() : cleaned;

        if (firstSentence.Length > 90)
            firstSentence = firstSentence.Substring(0, 90).Trim();

        return $"\"{firstSentence}\"";
    }

    public string GetSessionId() => sessionId;
    public string GetSelectedCharacterIdPublic() => GetSelectedCharacterId();
    public string GetSelectedSceneIdPublic() => GetSelectedSceneId();
    public int GetMoodBefore() => moodBefore;
    public int GetMoodNow() => GetMood();
    public int GetTurnCount() => turnCount;
}