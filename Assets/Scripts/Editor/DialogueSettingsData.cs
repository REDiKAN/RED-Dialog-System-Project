using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueSettings", menuName = "Dialogue System/Settings Data")]
public class DialogueSettingsData : ScriptableObject
{
    public GeneralSettings General = new GeneralSettings();
    public UISettings UI = new UISettings();
    [System.Serializable]
    public class GeneralSettings
    {
        public string DefaultMessageDelay = "0.5";
        public bool AutoScrollEnabled = true;
        public bool EnableQuickNodeCreationOnDragDrop = true;
        public bool EnableHotkeyUndoRedo = true; //    
        // AUTO-SAVE:         Unity
        public bool AutoSaveOnUnityClose = true;
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
    }
}