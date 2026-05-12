using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class EndSceneController : MonoBehaviour
{
    [Header("References")]
    public ApiClient apiClient;          // we will drag GameManager here
    public TMP_Text summaryText;         // optional: where to show summary from backend
    public GameObject ratingsPanel;
    public GameObject inputPanel;
    [Header("Ratings (selected values)")]
    public int funny = 3;
    public int helpful = 3;
    public int safe = 3;

    private const string EndSceneUrl = "http://127.0.0.1:8000/end_scene";

    [System.Serializable]
    private class EndSceneRequest
    {
        public string session_id;
        public string character_id;
        public string scene_id;
        public int mood_before;
        public int mood_after;
        public int rating_funny;
        public int rating_helpful;
        public int rating_safe;
        public int turns;
    }

    [System.Serializable]
    private class EndSceneResponse
    {
        public string summary;
    }

    // Called by rating buttons
    public void SetFunny(int v) { funny = v; }
    public void SetHelpful(int v) { helpful = v; }
    public void SetSafe(int v) { safe = v; }

    // Called by End Scene button
    public void OnEndSceneClicked()
{
    if (apiClient == null)
    {
        if (summaryText != null) summaryText.text = "Missing ApiClient reference.";
        return;
    }

    if (ratingsPanel != null)
    {
        ratingsPanel.SetActive(true);
    }

    if (inputPanel != null)
    {
        inputPanel.SetActive(false);
    }
}
    public void OnSubmitRatingsClicked()
{
    if (apiClient == null)
    {
        if (summaryText != null) summaryText.text = "Missing ApiClient reference.";
        return;
    }

    if (ratingsPanel != null)
    {
        ratingsPanel.SetActive(false);
    }

    if (inputPanel != null)
    {
        inputPanel.SetActive(true);
    }

    StartCoroutine(SendEndScene());
}

    private IEnumerator SendEndScene()
    {
        // Pull values from ApiClient (we'll add 3 small getters next)
        var payload = new EndSceneRequest
        {
            session_id = apiClient.GetSessionId(),
            character_id = apiClient.GetSelectedCharacterIdPublic(),
            scene_id = apiClient.GetSelectedSceneIdPublic(),
            mood_before = apiClient.GetMoodBefore(),
            mood_after = apiClient.GetMoodNow(),
            rating_funny = funny,
            rating_helpful = helpful,
            rating_safe = safe,
            turns = apiClient.GetTurnCount()
        };

        string json = JsonUtility.ToJson(payload);

        using (var req = new UnityWebRequest(EndSceneUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                if (summaryText != null)
                    summaryText.text = $"EndScene API error: {req.error}";
                yield break;
            }

            var res = JsonUtility.FromJson<EndSceneResponse>(req.downloadHandler.text);
            if (summaryText != null)
                summaryText.text = res.summary;
        }
    }
}