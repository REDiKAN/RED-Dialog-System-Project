using UnityEngine;
using TMPro;

public class OptionTextMessage : MonoBehaviour, IMessageObject
{
    [Header("Message Data")]
    [SerializeField] private TMP_Text textMessage;
    public void InitializationContent(Message contentMessage)
    {
        if (!string.IsNullOrEmpty(contentMessage.Text))
            textMessage.text = contentMessage.Text;
        else textMessage.text = "";

        SetCharacterAvatar(contentMessage.Sender);
        SetCharacterName(contentMessage.Sender);
    }

    public void SetCharacterAvatar(CharacterData characte)
    {
        
    }

    public void SetCharacterName(CharacterData character)
    {
        
    }
}
