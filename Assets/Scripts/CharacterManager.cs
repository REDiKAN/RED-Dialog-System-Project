using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    private static CharacterManager _instance;
    public static CharacterManager Instance => _instance;

    private Dictionary<string, CharacterData> characterCache = new Dictionary<string, CharacterData>();

    private void Awake()
    {
        if (_instance != null)
            Destroy(gameObject);
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllCharacters();
        }
    }

    /// <summary>
    /// ��������� ���� ���������� �� Resources/Characters
    /// </summary>
    private void LoadAllCharacters()
    {
        var characters = Resources.LoadAll<CharacterData>("Characters");
        foreach (var character in characters)
            characterCache[character.name] = character;
    }

    /// <summary>
    /// �������� ��������� �� �����
    /// </summary>
    /// <param name="characterName">��� ���������</param>
    /// <returns>CharacterData ��� null</returns>
    public CharacterData GetCharacter(string characterName)
    {
        if (characterCache.TryGetValue(characterName, out var character))
            return character;

        Debug.LogError($"�������� {characterName} �� ������ � Resources/Characters");
        return null;
    }
}
