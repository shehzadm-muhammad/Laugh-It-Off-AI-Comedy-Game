using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChatInputSubmit : MonoBehaviour
{
    public TMP_InputField inputField;
    public ApiClient apiClient;

    private void Update()
    {
        if (inputField == null || apiClient == null)
            return;

        if (!inputField.isFocused)
            return;

        if (Keyboard.current == null)
            return;

        if (Keyboard.current.enterKey.wasPressedThisFrame ||
            Keyboard.current.numpadEnterKey.wasPressedThisFrame)
        {
            apiClient.OnSendClicked();
        }
    }
}