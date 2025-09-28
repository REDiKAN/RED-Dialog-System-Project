using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class SpeechTextMessage : MonoBehaviour, IMessageObject
{
    [Header("Character Data")]
    [SerializeField] private Image characterAvatar;
    [SerializeField] private TMP_Text characterName;

    [Header("Message Data")]
    [SerializeField] private TMP_Text textMessage;

    public void InitializationContent(Message contentMessage)
    {
        if (!string.IsNullOrEmpty(contentMessage.Text))
            textMessage.text = contentMessage.Text;
        else textMessage.text = "";

        if (characterAvatar != null && contentMessage.Sender == null)
            Debug.LogWarning("Персонаж не указан для NPC сообщения");

        SetCharacterAvatar(contentMessage.Sender);
        SetCharacterName(contentMessage.Sender);
    }

    public void SetCharacterAvatar(CharacterData character)
    {
        if (characterAvatar != null && character != null && character.Icon != null)
            characterAvatar.sprite = character.Icon;
    }

    public void SetCharacterName(CharacterData character)
    {
        if (characterName != null && character != null)
        {
            characterName.text = character.FirstName;
            characterName.color = character.NameColor;
        }
    }
}
