using UnityEngine;


public static class DialogueValidator
{
    public static void ValidateSpeechNodes(DialogueContainer container)
    {
        foreach (var nodeData in container.SpeechNodeDatas)
        {
            if (string.IsNullOrEmpty(nodeData.SpeakerGuid) ||
                !AssetDatabaseHelper.IsValidGuid(nodeData.SpeakerGuid))
            {
                Debug.LogError($"SpeechNode (GUID: {nodeData.Guid}) has no speaker assigned! Text: \"{nodeData.DialogueText}\"");
            }
        }
    }
}