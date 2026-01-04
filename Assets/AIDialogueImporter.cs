using DialogueSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueSystem.Editor
{
    public class AIDialogueImporter : EditorWindow
    {
        private string dialogueJson = "";
        private Vector2 scrollPosition;
        private string statusMessage = "";
        private MessageType statusType = MessageType.Info;
        private string templatePath;

        [MenuItem("Dialog System/AI Dialogue Importer")]
        public static void ShowWindow()
        {
            GetWindow<AIDialogueImporter>("AI Dialogue Importer");
        }

        private void OnEnable()
        {
            templatePath = Application.dataPath + "/DialogueSystem/AITemplate/dialog_template.txt";
            if (!File.Exists(templatePath))
            {
                CreateTemplateFile();
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("AI Dialogue Importer", EditorStyles.boldLabel);

            // Инструкции
            EditorGUILayout.HelpBox(
                "1. Create dialogue using AI with the template file\n" +
                "2. Paste the generated JSON below (must be valid format)\n" +
                "3. Click 'Import Dialogue' to create a dialogue asset",
                MessageType.Info
            );

            // Кнопка для открытия шаблона
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✎ Open Template File", GUILayout.Width(150)))
            {
                if (!File.Exists(templatePath))
                {
                    CreateTemplateFile();
                }
                EditorUtility.RevealInFinder(templatePath);
            }
            EditorGUILayout.EndHorizontal();

            // Поле для вставки JSON
            EditorGUILayout.LabelField("Generated Dialogue JSON:", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            dialogueJson = EditorGUILayout.TextArea(dialogueJson, EditorStyles.textArea);
            EditorGUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {
                ClearStatus();
            }

            // Кнопки загрузки/очистки
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load JSON File"))
            {
                string path = EditorUtility.OpenFilePanel("Load Generated Dialogue", Application.dataPath, "json");
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    dialogueJson = File.ReadAllText(path);
                    ShowStatus("JSON file loaded successfully", MessageType.Info);
                }
            }

            if (GUILayout.Button("Clear"))
            {
                dialogueJson = "";
                ClearStatus();
            }
            EditorGUILayout.EndHorizontal();

            // Кнопка импорта
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(dialogueJson));
            if (GUILayout.Button("Import Dialogue", GUILayout.Height(40)))
            {
                ImportDialogue();
            }
            EditorGUI.EndDisabledGroup();

            // Статус сообщение
            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, statusType);
            }
        }

        private void CreateTemplateFile()
        {
            string directory = Path.GetDirectoryName(templatePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string templateContent = GetTemplateContent();
            File.WriteAllText(templatePath, templateContent);
            AssetDatabase.Refresh();
            ShowStatus("Template file created successfully", MessageType.Info);
        }

        private string GetTemplateContent()
        {
            return @"# AI-GENERATED DIALOGUE TEMPLATE FOR UNITY
# Follow these rules precisely when generating dialogue structures

## GLOBAL RULES
1. NEVER use EventNode
2. ALWAYS start with an EntryNode
3. Each node must have a unique GUID in format ""node-type-number""
4. All position coordinates must be in format ""(x,y)"" with x and y as numbers
5. All connections must be properly defined between nodes
6. JSON structure must EXACTLY match the DialogueContainer class structure
7. All string values must be properly escaped for JSON

## AVAILABLE NODE TYPES

### SPEECH NODES
- SpeechNodeText: For text-only dialogue
  Required: Guid, Position, DialogueText, SpeakerName (can be empty), NodeType=""SpeechNodeText""
- SpeechNodeImage: For showing images
  Required: Guid, Position, ImageSpritePath (Resources path), SpeakerName, NodeType=""SpeechNodeImage""
- SpeechNodeRandText: For random dialogue variants
  Required: Guid, Position, SpeakerName, Variants (array of {Text, WeightPercent})

### OPTION NODES
- OptionNodeText: For player text choices
  Required: Guid, Position, ResponseText, NodeType=""OptionNodeText""
- OptionNodeImage: For player image choices
  Required: Guid, Position, ImageSpriteGuid (empty if not used), NodeType=""OptionNodeImage""

### CONDITION NODES
- IntConditionNode: For checking integer variables
  Required: Guid, Position, SelectedProperty, Comparison (Equal/NotEqual/Greater/Less/GreaterOrEqual/LessOrEqual), CompareValue
- StringConditionNode: For checking string variables
  Required: Guid, Position, SelectedProperty, Comparison (Equal/NotEqual/IsNullOrEmpty), CompareValue
- CharacterIntConditionNode: For checking character variables
  Required: Guid, Position, CharacterName, SelectedVariable, Comparison, CompareValue

### ACTION NODES
- ModifyIntNode: For modifying global integer variables
  Required: Guid, Position, SelectedProperty, Operator (Set/Add/Subtract/Multiply/Divide/Increment/Decrement), Value
- CharacterModifyIntNode: For modifying character integer variables
  Required: Guid, Position, CharacterName, SelectedVariable, Operator, Value
- CharacterButtonPressNode: For changing character button press requirements
  Required: Guid, Position, CharacterName, RequireButtonPress (true/false)

### UTILITY NODES
- EndNode: For ending the dialogue
  Required: Guid, Position, NextDialogueName (can be empty)
- NoteNode: For visual notes in the graph
  Required: Guid, Position, NoteText, BackgroundColor (RGBA values)
- TimerNode: For timed options
  Required: Guid, Position, DurationSeconds
- PauseNode: For pausing dialogue flow
  Required: Guid, Position, DurationSeconds
- RandomBranchNode: For random branching
  Required: Guid, Position, Variants (array of {PortName, WeightPercent})
- WireNode: For visual connection routing
  Required: Guid, Position
- ChatSwitchNode: For switching between chat panels
  Required: Guid, Position, TargetChatIndex (integer)
- ChangeChatIconNode: For changing chat panel icon
  Required: Guid, Position, IconSpritePath (Resources path)
- ChangeChatNameNode: For changing chat panel name
  Required: Guid, Position, NewChatName

## EXAMPLE DIALOGUE STRUCTURE
{
  ""EntryNodeData"": {
    ""Guid"": ""entry-001"",
    ""Position"": ""(100,200)""
  },
  ""NodeLinks"": [
    {
      ""BaseNodeGuid"": ""entry-001"",
      ""PortName"": ""Next"",
      ""TargetNodeGuid"": ""speech-001""
    },
    {
      ""BaseNodeGuid"": ""speech-001"",
      ""PortName"": ""Next"",
      ""TargetNodeGuid"": ""option-001""
    },
    {
      ""BaseNodeGuid"": ""option-001"",
      ""PortName"": ""Option1"",
      ""TargetNodeGuid"": ""speech-002""
    },
    {
      ""BaseNodeGuid"": ""option-001"",
      ""PortName"": ""Option2"",
      ""TargetNodeGuid"": ""speech-003""
    },
    {
      ""BaseNodeGuid"": ""speech-002"",
      ""PortName"": ""Next"",
      ""TargetNodeGuid"": ""end-001""
    },
    {
      ""BaseNodeGuid"": ""speech-003"",
      ""PortName"": ""Next"",
      ""TargetNodeGuid"": ""end-001""
    }
  ],
  ""SpeechNodeDatas"": [
    {
      ""Guid"": ""speech-001"",
      ""Position"": ""(200,200)"",
      ""DialogueText"": ""Hello traveler! Welcome to our village. How can I help you today?"",
      ""SpeakerName"": ""Villager"",
      ""NodeType"": ""SpeechNodeText""
    },
    {
      ""Guid"": ""speech-002"",
      ""Position"": ""(400,150)"",
      ""DialogueText"": ""Great! We have many jobs available. Come see me tomorrow morning."",
      ""SpeakerName"": ""Villager"",
      ""NodeType"": ""SpeechNodeText""
    },
    {
      ""Guid"": ""speech-003"",
      ""Position"": ""(400,250)"",
      ""DialogueText"": ""Of course! The inn is just down the street to your left."",
      ""SpeakerName"": ""Villager"",
      ""NodeType"": ""SpeechNodeText""
    }
  ],
  ""OptionNodeDatas"": [
    {
      ""Guid"": ""option-001"",
      ""Position"": ""(300,200)"",
      ""ResponseText"": ""I'm looking for work.\nI just want to rest."",
      ""NodeType"": ""OptionNodeText""
    }
  ],
  ""EndNodeDatas"": [
    {
      ""Guid"": ""end-001"",
      ""Position"": ""(500,200)"",
      ""NextDialogueName"": """"
    }
  ],
  ""IntExposedProperties"": [
    {
      ""PropertyName"": ""PlayerReputation"",
      ""IntValue"": 0,
      ""MinValue"": -100,
      ""MaxValue"": 100
    }
  ],
  ""StringExposedProperties"": [],
  ""BaseCharacterGuid"": """"
}

## OUTPUT INSTRUCTIONS
1. Generate ONLY valid JSON in the EXACT format shown above
2. NEVER include EventNode
3. Each node type must be placed in the correct array in the JSON
4. Position values MUST be in format ""(x,y)""
5. For SpeakerName in speech nodes: use character name or leave empty
6. For ImageSpritePath: use path relative to Resources folder (no file extension)
7. For NoteNode BackgroundColor: use format [R,G,B,A] with values from 0 to 1
8. For NodeLinks PortName:
   - Regular connections: ""Next""
   - Option connections: ""Option1"", ""Option2"", etc.
   - Condition connections: ""True"", ""False""
   - Timer connections: ""Options"", ""Timeout""
   - RandomBranch connections: ""Variant1"", ""Variant2"", etc.
9. DO NOT include any additional fields not mentioned in this template";
        }

        private void ImportDialogue()
        {
            if (string.IsNullOrEmpty(dialogueJson))
            {
                ShowStatus("Please provide dialogue JSON", MessageType.Warning);
                return;
            }

            try
            {
                // Проверка синтаксиса JSON
                if (!IsValidJson(dialogueJson))
                {
                    ShowStatus("Invalid JSON format. Please check syntax and escaping.", MessageType.Error);
                    return;
                }

                // Создание нового диалогового ассета
                DialogueContainer newDialogue = ScriptableObject.CreateInstance<DialogueContainer>();

                // Десериализация JSON в ассет
                JsonUtility.FromJsonOverwrite(dialogueJson, newDialogue);

                // Проверка на наличие запрещенных узлов (EventNode)
                if (newDialogue.EventNodeDatas != null && newDialogue.EventNodeDatas.Count > 0)
                {
                    ShowStatus("Dialogue contains EventNode which is not allowed. Regenerate without EventNode.", MessageType.Error);
                    return;
                }

                AutoArrangeNodes(newDialogue);

                // Автоматическое формирование имени файла
                string defaultName = "AI_Generated_Dialogue";

                int counter = 1;
                string finalName = defaultName;

                while (AssetDatabase.LoadAssetAtPath<DialogueContainer>($"Assets/DialogueSystem/Dialogues/{finalName}.asset") != null)
                {
                    finalName = $"{defaultName}_{counter++}";
                }

                string savePath = $"Assets/DialogueSystem/Dialogues/{finalName}.asset";
                string directory = Path.GetDirectoryName(savePath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Сохранение ассета
                AssetDatabase.CreateAsset(newDialogue, savePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                ShowStatus($"Dialogue imported successfully!\nSaved to: {savePath}", MessageType.Info);

                // Предложение открыть проводник
                if (EditorUtility.DisplayDialog("Import Complete",
                    "Dialogue has been successfully imported. Would you like to reveal it in the Project window?",
                    "Yes", "No"))
                {
                    EditorGUIUtility.PingObject(newDialogue);
                }
            }
            catch (Exception e)
            {
                ShowStatus($"Import failed: {e.Message}", MessageType.Error);
                Debug.LogError($"AI Dialogue Import Error: {e}");
            }
        }

        // Добавьте этот метод в класс AIDialogueImporter
        private void AutoArrangeNodes(DialogueContainer dialogue)
        {
            // Сначала собираем все узлы в словарь для быстрого доступа по GUID
            Dictionary<string, BaseNodeData> allNodes = new Dictionary<string, BaseNodeData>();

            // Добавляем EntryNode
            if (dialogue.EntryNodeData != null)
            {
                allNodes[dialogue.EntryNodeData.Guid] = dialogue.EntryNodeData;
            }

            // Добавляем все типы узлов
            AddNodesToDictionary(dialogue.SpeechNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.SpeechNodeImageDatas, allNodes);
            AddNodesToDictionary(dialogue.SpeechRandNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.OptionNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.OptionNodeImageDatas, allNodes);
            //AddNodesToDictionary(dialogue.IntConditionNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.StringConditionNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.ModifyIntNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.EndNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.EventNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.CharacterIntConditionNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.CharacterModifyIntNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.DebugLogNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.DebugWarningNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.DebugErrorNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.TimerNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.PauseNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.WireNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.CharacterButtonPressNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.ChatSwitchNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.ChangeChatIconNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.ChangeChatNameNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.NoteNodeDatas, allNodes);
            AddNodesToDictionary(dialogue.RandomBranchNodeDatas, allNodes);

            // Определяем начальную позицию и шаг
            float startX = 100;
            float startY = 200;
            float horizontalStep = 250; // Расстояние между узлами по горизонтали
            float verticalStep = 150;  // Расстояние между ответвлениями по вертикали

            // Создаем очередь для обхода узлов в порядке следования
            Queue<string> nodeQueue = new Queue<string>();
            HashSet<string> visitedNodes = new HashSet<string>();

            // Начинаем с EntryNode
            if (dialogue.EntryNodeData != null)
            {
                nodeQueue.Enqueue(dialogue.EntryNodeData.Guid);
                visitedNodes.Add(dialogue.EntryNodeData.Guid);

                // Располагаем EntryNode
                SetNodePosition(dialogue.EntryNodeData, new Vector2(startX, startY));
                startX += horizontalStep;
            }

            // Обходим узлы в порядке следования
            while (nodeQueue.Count > 0)
            {
                string currentNodeGuid = nodeQueue.Dequeue();

                // Находим все связи, исходящие из текущего узла
                var outgoingLinks = dialogue.NodeLinks
                    .Where(link => link.BaseNodeGuid == currentNodeGuid)
                    .ToList();

                // Если есть несколько исходящих связей (развилка), распределяем их вертикально
                if (outgoingLinks.Count > 1)
                {
                    float branchStartY = startY - ((outgoingLinks.Count - 1) * verticalStep / 2);

                    for (int i = 0; i < outgoingLinks.Count; i++)
                    {
                        string targetGuid = outgoingLinks[i].TargetNodeGuid;

                        if (allNodes.TryGetValue(targetGuid, out BaseNodeData targetNode) &&
                            !visitedNodes.Contains(targetGuid))
                        {
                            // Располагаем узел ветки
                            float branchY = branchStartY + (i * verticalStep);
                            SetNodePosition(targetNode, new Vector2(startX, branchY));

                            visitedNodes.Add(targetGuid);
                            nodeQueue.Enqueue(targetGuid);
                        }
                    }

                    startX += horizontalStep;
                }
                // Если одна исходящая связь - просто продолжаем горизонтально
                else if (outgoingLinks.Count == 1)
                {
                    string targetGuid = outgoingLinks[0].TargetNodeGuid;

                    if (allNodes.TryGetValue(targetGuid, out BaseNodeData targetNode) &&
                        !visitedNodes.Contains(targetGuid))
                    {
                        // Располагаем узел
                        SetNodePosition(targetNode, new Vector2(startX, startY));

                        visitedNodes.Add(targetGuid);
                        nodeQueue.Enqueue(targetGuid);
                        startX += horizontalStep;
                    }
                }
            }
        }

        private void AddNodesToDictionary<T>(List<T> nodeList, Dictionary<string, BaseNodeData> dictionary) where T : BaseNodeData
        {
            foreach (var node in nodeList)
            {
                if (!dictionary.ContainsKey(node.Guid))
                {
                    dictionary.Add(node.Guid, node);
                }
            }
        }

        private void SetNodePosition(BaseNodeData node, Vector2 position)
        {
            // Преобразуем Vector2 в строку в формате "(x,y)" как требуется в системе
            node.Position = new Vector2(position.x, position.y);
        }

        private bool IsValidJson(string json)
        {
            try
            {
                // Простая проверка корректности JSON
                var obj = JsonUtility.FromJson<ValidationObject>(json);
                return obj != null;
            }
            catch
            {
                return false;
            }
        }

        // Вспомогательный класс для валидации JSON
        [System.Serializable]
        private class ValidationObject
        {
            public EntryNodeData EntryNodeData;
            public List<NodeLinkData> NodeLinks;
        }

        private void ShowStatus(string message, MessageType type)
        {
            statusMessage = message;
            statusType = type;
            Repaint();
        }

        private void ClearStatus()
        {
            statusMessage = "";
            statusType = MessageType.Info;
        }
    }
}