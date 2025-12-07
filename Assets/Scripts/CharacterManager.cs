using System.Collections.Generic;
using System.Linq;
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
    /// Загружает всех персонажей из Resources/Characters
    /// </summary>
    private void LoadAllCharacters()
    {
        var characters = Resources.LoadAll<CharacterData>("Characters");
        foreach (var character in characters)
            characterCache[character.name] = character;
    }

    /// <summary>
    /// Получает персонажа по имени
    /// </summary>
    /// <param name="characterName">Имя персонажа</param>
    /// <returns>CharacterData или null</returns>
    public CharacterData GetCharacter(string characterName)
    {
        if (characterCache.TryGetValue(characterName, out var character))
            return character;

        Debug.LogError($"Персонаж {characterName} не найден в Resources/Characters");
        return null;
    }

    public List<CharacterData> GetAllCharacters()
    {
        return characterCache.Values.ToList();
    }
}
