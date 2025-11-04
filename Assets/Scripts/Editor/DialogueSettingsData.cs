using UnityEngine;

[CreateAssetMenu(fileName = "DialogueSettings", menuName = "Dialogue System/Settings Data")]
public class DialogueSettingsData : ScriptableObject
{
    public GeneralSettings General = new GeneralSettings();
    public UISettings UI = new UISettings();
    public AudioSettings Audio = new AudioSettings();

    [System.Serializable]
    public class GeneralSettings
    {
        public string DefaultMessageDelay = "0.5";
        public bool AutoScrollEnabled = true;
        public int MaxMessageHistory = 50;
        public bool EnableQuickNodeCreationOnDragDrop = true;
    }

    [System.Serializable]
    public class UISettings
    {
        public Color BackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        public bool ShowTimestamps = false;
        public string FontName = "Default";

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