// Assets/Scripts/SpeechImageMessage.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Компонент для отображения сообщения в виде изображения от персонажа
/// Реализует интерфейс IMessageObject для интеграции с ChatPanel
/// </summary>
public class SpeechImageMessage : MonoBehaviour, IMessageObject
{
    [Header("Character Data")]
    [SerializeField] private Image characterAvatar;
    [SerializeField] private TMP_Text characterName;

    [Header("Message Data")]
    [SerializeField] private Image messageImage;

    public void InitializationContent(Message contentMessage)
    {
        if (messageImage == null)
        {
            Debug.LogError("messageImage is not assigned!");
            return;
        }

        if (contentMessage.Image != null)
        {
            messageImage.sprite = contentMessage.Image;
            messageImage.enabled = true;
        }
        else
        {
            messageImage.enabled = false;
        }

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