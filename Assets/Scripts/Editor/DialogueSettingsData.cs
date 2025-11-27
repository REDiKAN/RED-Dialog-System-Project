using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueSettings", menuName = "Dialogue System/Settings Data")]
public class DialogueSettingsData : ScriptableObject
{

    public bool EnableHotkeyUndoRedo = false; // Отключаем по умолчанию
    public GeneralSettings General = new GeneralSettings();
    public UISettings UI = new UISettings();
    [System.Serializable]
    public class GeneralSettings
    {
        public string DefaultMessageDelay = "0.5";
        public bool AutoScrollEnabled = true;
        public bool EnableQuickNodeCreationOnDragDrop = true;
        public bool EnableHotkeyUndoRedo = true;
        public bool AutoSaveOnUnityClose = true;

        public bool enableAutoSaveLocation = true;
        public string autoSaveFolderPath = "";
    }
    [System.Serializable]
    public class UISettings
    {
        public bool UseCustomBackgroundColor = false;
        public Color CustomBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    }
    [System.Serializable]
    public class AudioSettings
    {
        public float MasterVolume = 1f;
        public bool MuteOnPause = true;
        public string AudioMixerPath = "Audio/Mixer";
    }

    // New field for favorite node types
    [SerializeField] private List<string> _favoriteNodeTypes = new List<string>();
    public List<string> FavoriteNodeTypes
    {
        get
        {
            // Initialize with defaults if empty
            if (_favoriteNodeTypes == null || _favoriteNodeTypes.Count == 0)
            {
                _favoriteNodeTypes = new List<string> {
                    "SpeechNodeText",
                    "OptionNodeText",
                    "IntConditionNode",
                    "EndNode"
                };
            }
            return _favoriteNodeTypes;
        }
        set { _favoriteNodeTypes = value; }
    }

    private void OnEnable()
    {
        // Ensure default values are set when the object is enabled
        if (_favoriteNodeTypes == null)
        {
            _favoriteNodeTypes = new List<string>();
        }

        if (_favoriteNodeTypes.Count == 0)
        {
            _favoriteNodeTypes.AddRange(new List<string> {
                "SpeechNodeText",
                "OptionNodeText",
                "IntConditionNode",
                "EndNode"
            });
        }

        ValidateAutoSaveFolder();
    }

    /// <summary>
    /// Валидация пути к папке автосохранения
    /// </summary>
    public void ValidateAutoSaveFolder()
    {
        if (General.enableAutoSaveLocation && !string.IsNullOrEmpty(General.autoSaveFolderPath))
        {
            if (!IsValidSavePath(General.autoSaveFolderPath))
            {
                General.enableAutoSaveLocation = false;
                Debug.LogWarning($"Auto-save folder is not accessible: {General.autoSaveFolderPath}");
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }
    }

    /// <summary>
    /// Проверяет, существует ли папка и находится ли она внутри Assets
    /// </summary>
    public bool IsValidSavePath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return false;

        string fullPath = GetFullPath(relativePath);
        return !string.IsNullOrEmpty(fullPath) && System.IO.Directory.Exists(fullPath);
    }

    /// <summary>
    /// Возвращает полный путь к папке из относительного пути
    /// </summary>
    public string GetFullPath(string relativePath = null)
    {
        if (relativePath == null)
            relativePath = General.autoSaveFolderPath;

        if (string.IsNullOrEmpty(relativePath))
            return null;

        // Проверяем, что путь находится внутри Assets
        if (relativePath.Contains("..") || relativePath.StartsWith("/") || relativePath.StartsWith("\\"))
            return null;

        return System.IO.Path.Combine(Application.dataPath, relativePath);
    }

    /// <summary>
    /// Устанавливает путь к папке автосохранения с валидацией
    /// </summary>
    public void SetAutoSaveFolderPath(string fullPath)
    {
        // Проверяем, что путь находится внутри Assets
        if (!string.IsNullOrEmpty(fullPath) && fullPath.StartsWith(Application.dataPath))
        {
            // Преобразуем в относительный путь от Assets
            string relativePath = fullPath.Substring(Application.dataPath.Length + 1);
            // Нормализуем путь (заменяем обратные слеши)
            relativePath = relativePath.Replace("\\", "/");

            // Проверяем, что папка существует
            if (System.IO.Directory.Exists(fullPath))
            {
                General.autoSaveFolderPath = relativePath;
                General.enableAutoSaveLocation = true;
                return;
            }
        }

        // Если путь недействителен
        General.enableAutoSaveLocation = false;
        Debug.LogWarning("Invalid auto-save folder path. Must be inside Assets folder.");
    }
}