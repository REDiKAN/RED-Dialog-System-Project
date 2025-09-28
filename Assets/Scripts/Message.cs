using UnityEngine;

/// <summary>
/// Модель данных для сообщения в чате
/// </summary>
[System.Serializable]
public class Message
{
    /// <summary>
    /// Тип сообщения: NPC, Игрок или Системное
    /// </summary>
    public SenderType Type;

    /// <summary>
    /// Текст сообщения (для текстовых сообщений)
    /// </summary>
    public string Text;

    /// <summary>
    /// Изображение для сообщения (спрайт)
    /// </summary>
    public Sprite Image;

    /// <summary>
    /// Аудио для сообщения ( AudioClip )
    /// </summary>
    public AudioClip Audio;

    /// <summary>
    /// Персонаж, отправивший сообщение (для NPC)
    /// </summary>
    public CharacterData Sender;
}

/// <summary>
/// Типы сообщений в чате
/// </summary>
public enum SenderType
{
    NPC,      // Сообщение от NPC
    Player,   // Сообщение от игрока
    System    // Системное сообщение (например, "Диалог завершён")
}

public enum MessageType
{
    System,
    Speech, SpeechText, SpeechImage, SpeechAudio,
    OptionText, OptionImage, OptionAudio,
    Event, Log
}

