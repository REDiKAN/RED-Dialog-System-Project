using UnityEngine;
/// <summary>
/// ������ ������ ��� ��������� � ����
/// </summary>
[System.Serializable]
public class Message
{
    /// <summary>
    /// ��� ���������: NPC, ����� ��� ���������
    /// </summary>
    public MessageType Type;

    /// <summary>
    /// ����� ��������� (��� ��������� ���������)
    /// </summary>
    public string Text;

    /// <summary>
    /// ����������� ��� ��������� (������)
    /// </summary>
    public Sprite Image;

    /// <summary>
    /// ����� ��� ��������� ( AudioClip )
    /// </summary>
    public AudioClip Audio;

    /// <summary>
    /// ��������, ����������� ��������� (��� NPC)
    /// </summary>
    public CharacterData Sender;
}

/// <summary>
/// ���� ��������� � ����
/// </summary>
public enum MessageType
{
    NPC,      // ��������� �� NPC
    Player,   // ��������� �� ������
    System    // ��������� ��������� (��������, "������ ��������")
}
