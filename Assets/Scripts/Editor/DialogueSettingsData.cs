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

        // AUTO-SAVE: ƒобавл€ем новую настройку дл€ автоматического сохранени€ при закрытии Unity
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
}