using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System;
using System.Collections;
using DialogueSystem;

/// <summary>
/// ��������� ����������� �������� � �������� �������
/// </summary>
public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ChatPanel chatPanel;
    [SerializeField] private OptionPanel optionPanel;

    [Header("Dialogue Settings")]
    [SerializeField] private float messageDelay = 0.5f; // �������� ����� �����������

    [SerializeField] private DialogueContainer currentDialogue;
    private object currentNode; // ����������: BaseNodeData -> object (������ 1,9)
    private Dictionary<string, int> intVariables = new Dictionary<string, int>();
    private Dictionary<string, string> stringVariables = new Dictionary<string, string>();
    private List<object> visitedNodes = new List<object>(); // ����������: BaseNodeData -> object

    private void Start()
    {
        // ������������� �� ������� ������ �����
        if (optionPanel != null)
        {
            optionPanel.onOptionSelected += HandleOptionSelection;
        }
        else
        {
            Debug.LogError("OptionPanel not assigned in DialogueManager");
        }

        if (chatPanel == null)
        {
            Debug.LogError("ChatPanel not assigned in DialogueManager");
        }

        StartDialogue(currentDialogue);
    }
    /// <summary>
    /// ��������� ������ �� ���������� ����������
    /// </summary>
    /// <param name="dialogueContainer">��������� ������� ��� �������</param>
    public void StartDialogue(DialogueContainer dialogueContainer)
    {
        currentDialogue = dialogueContainer;
        ResetVariables();
        visitedNodes.Clear();

        // ������������� ���������� �� Exposed Properties
        foreach (var prop in currentDialogue.IntExposedProperties)
        {
            intVariables[prop.PropertyName] = prop.IntValue;
        }

        foreach (var prop in currentDialogue.StringExposedProperties)
        {
            stringVariables[prop.PropertyName] = prop.StringValue;
        }

        // �������� ������ � EntryNode
        currentNode = currentDialogue.EntryNodeData;
        ProcessNextNode();
    }

    /// <summary>
    /// ���������� ��� ���������� ������� � ��������� �� ���������
    /// </summary>
    private void ResetVariables()
    {
        intVariables.Clear();
        stringVariables.Clear();
    }

    private void ProcessNextNode()
    {
        if (currentNode == null)
            return;

        // ��������� ������� ���� � ������ ����������
        if (!visitedNodes.Contains(currentNode))
        {
            visitedNodes.Add(currentNode);
        }

        switch (currentNode)
        {
            case EntryNodeData entryNode:
                ProcessEntryNode(entryNode);
                break;
            case SpeechNodeData speechNode:
                ProcessSpeechNode(speechNode);
                break;
            case OptionNodeData optionNode:
                ProcessOptionNode(optionNode);
                break;
            case IntConditionNodeData intCondition:
                ProcessIntCondition(intCondition);
                break;
            case StringConditionNodeData stringCondition:
                ProcessStringCondition(stringCondition);
                break;
            case ModifyIntNodeData modifyNode:
                ProcessModifyIntNode(modifyNode);
                break;
            case EndNodeData endNode:
                ProcessEndNode(endNode);
                break;
            case SpeechNodeImageData speechImageNode:
                ProcessSpeechImageNode(speechImageNode);
                break;
            case OptionNodeImageData optionImageNode:
                ProcessOptionImageNode(optionImageNode);
                break;
            default:
                Debug.LogWarning($"����������� ��� ����: {currentNode.GetType().Name}");
                currentNode = null;
                break;
        }
    }

    /// <summary>
    /// ������������ EntryNodeData
    /// </summary>
    /// <param name="entryNode">������ ���������� ����</param>
    private void ProcessEntryNode(EntryNodeData entryNode)
    {
        // ������� ��������� ���� ����� EntryNode
        var nextLink = currentDialogue.NodeLinks
            .FirstOrDefault(l => l.BaseNodeGuid == entryNode.Guid);

        if (nextLink != null)
        {
            currentNode = GetNodeByGuid(nextLink.TargetNodeGuid);
            ProcessNextNode();
        }
        else
        {
            // ���� ��� ���������� ����, ��������� ������
            currentNode = null;
        }
    }

    /// <summary>
    /// ������������ SpeechNodeData
    /// </summary>
    /// <param name="speechNode">������ ���� ����</param>
    private void ProcessSpeechNode(SpeechNodeData speechNode)
    {
        // �������� ��������� �� CharacterManager
        CharacterData speaker = GetCharacterByGuid(speechNode.SpeakerGuid);

        // ������� ��������� ��� ����
        var message = new Message
        {
            Type = MessageType.NPC,
            Text = speechNode.DialogueText,
            Image = null, // ��� ��������� ����� ����������� �� ������������
            Audio = AssetLoader.LoadAudioClip(speechNode.AudioClipGuid), // ����������: AssetLoader
            Sender = speaker
        };

        // ��������� ��������� � ���
        chatPanel.AddMessage(message);

        // ������������ �����, ���� ��� ����
        if (message.Audio != null)
        {
            StartCoroutine(PlayAudioAfterDelay(message.Audio, messageDelay));
        }

        // ������� ��������� ����
        var nextLink = currentDialogue.NodeLinks
            .FirstOrDefault(l => l.BaseNodeGuid == speechNode.Guid);

        if (nextLink != null)
        {
            currentNode = GetNodeByGuid(nextLink.TargetNodeGuid);
            ProcessNextNode();
        }
        else
        {
            currentNode = null;
        }
    }

    /// <summary>
    /// ������������ SpeechNodeImageData
    /// </summary>
    /// <param name="speechImageNode">������ ���� ����������� ����</param>
    private void ProcessSpeechImageNode(SpeechNodeImageData speechImageNode)
    {
        // �������� ��������� �� CharacterManager
        CharacterData speaker = GetCharacterByGuid(speechImageNode.SpeakerGuid);

        // ������� ��������� ��� ����
        var message = new Message
        {
            Type = MessageType.NPC,
            Text = null, // ��� ����������� ����� �� ������������
            Image = AssetLoader.LoadSprite(speechImageNode.ImageSpriteGuid), // ����������: AssetLoader
            Audio = null,
            Sender = speaker
        };

        // ��������� ��������� � ���
        chatPanel.AddMessage(message);

        // ������� ��������� ����
        var nextLink = currentDialogue.NodeLinks
            .FirstOrDefault(l => l.BaseNodeGuid == speechImageNode.Guid);

        if (nextLink != null)
        {
            currentNode = GetNodeByGuid(nextLink.TargetNodeGuid);
            ProcessNextNode();
        }
        else
        {
            currentNode = null;
        }
    }

    /// <summary>
    /// ������������ OptionNodeData
    /// </summary>
    private void ProcessOptionNode(OptionNodeData optionNode)
    {
        // ������� ������ ��������� ������
        var options = new List<Option>();

        // ������� ��� ����� �� ����� ����
        var optionLinks = currentDialogue.NodeLinks
            .Where(l => l.BaseNodeGuid == optionNode.Guid)
            .ToList();

        foreach (var link in optionLinks)
        {
            var targetNode = GetNodeByGuid(link.TargetNodeGuid);
            string optionText = "������� ������";

            if (targetNode is OptionNodeData optionTarget)
            {
                optionText = !string.IsNullOrEmpty(optionTarget.ResponseText) ?
                    optionTarget.ResponseText : "������� ������";
            }
            else if (targetNode is OptionNodeImageData)
            {
                optionText = "�����������";
            }

            options.Add(new Option
            {
                Text = optionText,
                NextNodeGuid = link.TargetNodeGuid
            });
        }

        // ���������� �������� ������
        if (options.Count > 0 && optionPanel != null)
        {
            optionPanel.ShowOptions(options);
            currentNode = optionNode;
        }
        else
        {
            Debug.LogWarning($"No options found for OptionNode {optionNode.Guid}");

            // �������� ����� ��������� ����
            var nextLink = currentDialogue.NodeLinks.FirstOrDefault(l => l.BaseNodeGuid == optionNode.Guid);
            if (nextLink != null)
            {
                currentNode = GetNodeByGuid(nextLink.TargetNodeGuid);
                ProcessNextNode();
            }
            else
            {
                currentNode = null;
            }
        }
    }

    /// <summary>
    /// ������������ OptionNodeImageData
    /// </summary>
    /// <param name="optionImageNode">������ ���� ����������� ��������� ������</param>
    private void ProcessOptionImageNode(OptionNodeImageData optionImageNode)
    {
        // �������� �������� ������� �� ����
        var options = new List<Option>();

        // �������� ��������� OptionNodeData ��� ���� �������
        var optionLinks = currentDialogue.NodeLinks
            .Where(l => l.BaseNodeGuid == optionImageNode.Guid)
            .ToList();

        foreach (var link in optionLinks)
        {
            var targetNode = GetNodeByGuid(link.TargetNodeGuid);
            if (targetNode is OptionNodeData optionTarget)
            {
                options.Add(new Option
                {
                    Text = optionTarget.ResponseText,
                    NextNodeGuid = link.TargetNodeGuid
                });
            }
            else if (targetNode is OptionNodeImageData optionImageTarget)
            {
                // ��� ����������� ��������� ������
                options.Add(new Option
                {
                    Text = "�����������", // ����������: OptionNodeImageData �� ����� ResponseText
                    NextNodeGuid = link.TargetNodeGuid
                });
            }
        }

        // ���������� ������ ��������� �������
        optionPanel.ShowOptions(options);

        // ������������� ������� ���� ��� OptionNode ��� ����������� ��������� ������
        currentNode = optionImageNode;
    }

    /// <summary>
    /// ������������ IntConditionNodeData
    /// </summary>
    /// <param name="intCondition">������ ��������� �������</param>
    private void ProcessIntCondition(IntConditionNodeData intCondition)
    {
        // ��������� �������
        bool conditionResult = ConditionHandler.EvaluateIntCondition(
            intCondition, intVariables);

        // ���������� ��������� ���� � ����������� �� ����������
        var nextLinks = currentDialogue.NodeLinks
            .Where(l => l.BaseNodeGuid == intCondition.Guid)
            .ToList();

        // ������� ���������� �����
        foreach (var link in nextLinks)
        {
            if (link.PortName == "True" && conditionResult)
            {
                currentNode = GetNodeByGuid(link.TargetNodeGuid);
                ProcessNextNode();
                return;
            }
            else if (link.PortName == "False" && !conditionResult)
            {
                currentNode = GetNodeByGuid(link.TargetNodeGuid);
                ProcessNextNode();
                return;
            }
        }

        // ���� ���������� ����� �� ������, ��������� ������
        Debug.LogWarning($"�� ������ ���������� ����� ��� ������� IntConditionNode {intCondition.Guid}");
        currentNode = null;
    }

    /// <summary>
    /// ������������ StringConditionNodeData
    /// </summary>
    /// <param name="stringCondition">������ ���������� �������</param>
    private void ProcessStringCondition(StringConditionNodeData stringCondition)
    {
        // ��������� �������
        bool conditionResult = ConditionHandler.EvaluateStringCondition(
            stringCondition, stringVariables);

        // ���������� ��������� ���� � ����������� �� ����������
        var nextLinks = currentDialogue.NodeLinks
            .Where(l => l.BaseNodeGuid == stringCondition.Guid)
            .ToList();

        // ������� ���������� �����
        foreach (var link in nextLinks)
        {
            if (link.PortName == "True" && conditionResult)
            {
                currentNode = GetNodeByGuid(link.TargetNodeGuid);
                ProcessNextNode();
                return;
            }
            else if (link.PortName == "False" && !conditionResult)
            {
                currentNode = GetNodeByGuid(link.TargetNodeGuid);
                ProcessNextNode();
                return;
            }
        }

        // ���� ���������� ����� �� ������, ��������� ������
        Debug.LogWarning($"�� ������ ���������� ����� ��� ������� StringConditionNode {stringCondition.Guid}");
        currentNode = null;
    }

    /// <summary>
    /// ������������ ModifyIntNodeData
    /// </summary>
    /// <param name="modifyNode">������ ������������ �����</param>
    private void ProcessModifyIntNode(ModifyIntNodeData modifyNode)
    {
        // ��������� ������� ����������
        if (!intVariables.ContainsKey(modifyNode.SelectedProperty))
        {
            Debug.LogError($"���������� {modifyNode.SelectedProperty} �� ������� � intVariables");
            currentNode = null;
            return;
        }

        // ��������� ��������
        switch (modifyNode.Operator)
        {
            case OperatorType.Set:
                intVariables[modifyNode.SelectedProperty] = modifyNode.Value;
                break;
            case OperatorType.Add:
                intVariables[modifyNode.SelectedProperty] += modifyNode.Value;
                break;
            case OperatorType.Subtract:
                intVariables[modifyNode.SelectedProperty] -= modifyNode.Value;
                break;
            case OperatorType.Multiply:
                intVariables[modifyNode.SelectedProperty] *= modifyNode.Value;
                break;
            case OperatorType.Divide:
                if (modifyNode.Value != 0)
                    intVariables[modifyNode.SelectedProperty] /= modifyNode.Value;
                else
                    Debug.LogWarning("������� �� ���� � ModifyIntNode");
                break;
            case OperatorType.Increment:
                intVariables[modifyNode.SelectedProperty]++;
                break;
            case OperatorType.Decrement:
                intVariables[modifyNode.SelectedProperty]--;
                break;
        }

        // ������� ��������� ����
        var nextLink = currentDialogue.NodeLinks
            .FirstOrDefault(l => l.BaseNodeGuid == modifyNode.Guid);

        if (nextLink != null)
        {
            currentNode = GetNodeByGuid(nextLink.TargetNodeGuid);
            ProcessNextNode();
        }
        else
        {
            currentNode = null;
        }
    }

    /// <summary>
    /// ������������ EndNodeData
    /// </summary>
    /// <param name="endNode">������ ��������� ����</param>
    private void ProcessEndNode(EndNodeData endNode)
    {
        // ��������� ��������� ��������� � ���������� �������
        var message = new Message
        {
            Type = MessageType.System,
            Text = "������ ��������"
        };
        chatPanel.AddMessage(message);

        // ���� ������ ��������� ������, ��������� ���
        if (!string.IsNullOrEmpty(endNode.NextDialogueName))
        {
            DialogueContainer nextDialogue = Resources.Load<DialogueContainer>(endNode.NextDialogueName);
            if (nextDialogue != null)
            {
                StartDialogue(nextDialogue);
            }
            else
            {
                Debug.LogError($"������ {endNode.NextDialogueName} �� ������ � ��������");
            }
        }
        else
        {
            // ��������� ������� ������
            currentNode = null;
        }
    }

    /// <summary>
    /// ������������ ����� �������� ������
    /// </summary>
    public void HandleOptionSelection(string nextNodeGuid)
    {
        // ��������� ��������� �� ������ � ���
        var selectedOption = currentDialogue.NodeLinks
            .FirstOrDefault(l => l.TargetNodeGuid == nextNodeGuid);

        if (selectedOption != null)
        {
            // ������� ����� ���������� ��������
            string optionText = "�����: ";
            var optionNode = GetNodeByGuid(selectedOption.BaseNodeGuid) as OptionNodeData;
            if (optionNode != null && !string.IsNullOrEmpty(optionNode.ResponseText))
            {
                optionText += optionNode.ResponseText;
            }
            else
            {
                optionText += "������� ������";
            }

            var message = new Message
            {
                Type = MessageType.Player,
                Text = optionText
            };
            chatPanel.AddMessage(message);
        }

        // ��������� � ���������� ����
        currentNode = GetNodeByGuid(nextNodeGuid);
        ProcessNextNode();
    }
    /// <summary>
    /// �������� ���� �� ��� GUID
    /// </summary>
    /// <param name="guid">GUID ����</param>
    /// <returns>BaseNodeData ��� null</returns>
    private object GetNodeByGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid))
            return null;

        // ���� � ���� ����� �����
        var speechNode = currentDialogue.SpeechNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (speechNode != null) return speechNode;

        var speechImageNode = currentDialogue.SpeechNodeImageDatas.FirstOrDefault(n => n.Guid == guid);
        if (speechImageNode != null) return speechImageNode;

        var optionNode = currentDialogue.OptionNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (optionNode != null) return optionNode;

        var optionImageNode = currentDialogue.OptionNodeImageDatas.FirstOrDefault(n => n.Guid == guid);
        if (optionImageNode != null) return optionImageNode;

        var intConditionNode = currentDialogue.IntConditionNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (intConditionNode != null) return intConditionNode;

        var stringConditionNode = currentDialogue.StringConditionNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (stringConditionNode != null) return stringConditionNode;

        var modifyIntNode = currentDialogue.ModifyIntNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (modifyIntNode != null) return modifyIntNode;

        var endNode = currentDialogue.EndNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (endNode != null) return endNode;

        return currentDialogue.EntryNodeData;
    }

    /// <summary>
    /// ������������� ����� � ��������� ����� ����������� ������
    /// </summary>
    /// <param name="audioClip">����� ���� ��� ���������������</param>
    /// <param name="delay">�������� � ��������</param>
    /// <returns>�������� ��� ��������������� �����</returns>
    private IEnumerator PlayAudioAfterDelay(AudioClip audioClip, float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioSource.PlayClipAtPoint(audioClip, Camera.main.transform.position);
    }

    /// <summary>
    /// ���������� ������� ������
    /// </summary>
    public void ResetDialogue()
    {
        currentDialogue = null;
        currentNode = null;
        visitedNodes.Clear();
        ResetVariables();
    }

    /// <summary>
    /// �������� ��������� �� GUID (�������� ��� runtime)
    /// </summary>
    /// <param name="guid">GUID ���������</param>
    /// <returns>CharacterData ��� null</returns>
    private CharacterData GetCharacterByGuid(string guid)
    {
        // � runtime ��� AssetDatabase, ������� ���������� ��������
        // � �������� ������� ����� ����������� �������� �� Resources
        return null;
    }

    /// <summary>
    /// ��������������� ����� ��� �������� ������� � runtime
    /// </summary>
    public static class AssetLoader
    {
        /// <summary>
        /// ��������� ����� ���� �� GUID (�������� ��� runtime)
        /// </summary>
        /// <param name="guid">GUID �����</param>
        /// <returns>AudioClip ��� null</returns>
        public static AudioClip LoadAudioClip(string guid)
        {
            // � runtime ��� AssetDatabase, ������� ���������� ��������
            // � �������� ������� ����� ����������� �������� �� Resources
            return null;
        }

        /// <summary>
        /// ��������� ������ �� GUID (�������� ��� runtime)
        /// </summary>
        /// <param name="guid">GUID �������</param>
        /// <returns>Sprite ��� null</returns>
        public static Sprite LoadSprite(string guid)
        {
            // � runtime ��� AssetDatabase, ������� ���������� ��������
            // � �������� ������� ����� ����������� �������� �� Resources
            return null;
        }
    }
}