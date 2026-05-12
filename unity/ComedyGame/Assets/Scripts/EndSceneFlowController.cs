using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class EndSceneFlowController : MonoBehaviour
{
    [Header("References")]
    public ApiClient apiClient;

    private const string EndSceneUrl = "http://127.0.0.1:8000/end_scene";

    [System.Serializable]
    private class EndSceneRequest
    {
        public string session_id;
        public string character_id;
        public string scene_id;
        public int mood_after;
        public int turns;
    }

    [System.Serializable]
    private class EndSceneResponse
    {
        public bool success;
        public string session_id;
        public string character_id;
        public string scene_id;
        public string character_name;
        public string scene_name;
        public string summary_text;
        public int mood_after;
        public int turns;
    }

    public void OnEndSceneClicked()
    {
        StartCoroutine(SendEndScene());
    }

    private IEnumerator SendEndScene()
    {
        if (apiClient == null)
        {
            Debug.LogError("EndSceneFlowController: ApiClient is not assigned.");
            yield break;
        }

        var payload = new EndSceneRequest
        {
            session_id = apiClient.GetSessionId(),
            character_id = apiClient.GetSelectedCharacterIdPublic(),
            scene_id = apiClient.GetSelectedSceneIdPublic(),
            mood_after = apiClient.GetMoodNow(),
            turns = apiClient.GetTurnCount()
        };

        string json = JsonUtility.ToJson(payload);
        Debug.Log("END SCENE PAYLOAD: " + json);

        using (var req = new UnityWebRequest(EndSceneUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("End scene API error: " + req.error);
                Debug.LogError("End scene response body: " + req.downloadHandler.text);

                GameState.LastSummary = "Scene ended, but the summary could not be loaded.";
                GameState.LastFunnyLine = "A little laughter goes a long way.";
                SceneManager.LoadScene("EndSceneSummary");
                yield break;
            }

            string responseText = req.downloadHandler.text;
            Debug.Log("END SCENE RESPONSE: " + responseText);

            var res = JsonUtility.FromJson<EndSceneResponse>(responseText);
            
            GameState.LastSessionId = payload.session_id;
            
            GameState.LastSummary = string.IsNullOrEmpty(res.summary_text)
                ? "You completed the scene."
                : res.summary_text;

            GameState.LastFunnyLine = "A little laughter goes a long way.";

            SceneManager.LoadScene("EndSceneSummary");
        }
    }
}