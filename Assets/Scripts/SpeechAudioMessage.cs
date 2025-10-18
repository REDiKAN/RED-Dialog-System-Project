// Assets/Scripts/SpeechAudioMessage.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Префаб сообщения с аудио от персонажа (без текста/изображения)
/// </summary>
public class SpeechAudioMessage : MonoBehaviour, IMessageObject
{
    [Header("Character Data")]
    [SerializeField] private Image characterAvatar;
    [SerializeField] private TMP_Text characterName;

    [Header("Message Data")]
    // Аудио не отображается визуально, но может использоваться для триггера воспроизведения
    // Визуальный контент может быть пустым или содержать иконку динамика и т.п.
    [SerializeField] private GameObject audioIndicator; // опционально

    public void InitializationContent(Message contentMessage)
    {
        // Аудио обрабатывается в DialogueManager, здесь только визуал
        if (audioIndicator != null)
        {
            audioIndicator.SetActive(contentMessage.Audio != null);
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
