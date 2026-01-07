using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Collections;
using DialogueSystem;

/// <summary>
/// Управляет выполнением диалогов в реальном времени
/// </summary>
public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ChatPanel chatPanel;
    [SerializeField] private OptionPanel optionPanel;
    [SerializeField] private Button _continueButton;

    [Header("Dialogue Settings")]
    [SerializeField] private float messageDelay = 0.5f;
    [SerializeField] private DialogueContainer currentDialogue;

    private object currentNode;
    private Dictionary<string, int> intVariables = new Dictionary<string, int>();
    private Dictionary<string, string> stringVariables = new Dictionary<string, string>();
    private List<object> visitedNodes = new List<object>();
    private readonly System.Random random = new System.Random();
    [SerializeField] private TimerDisplayController _timerDisplayController;

    private Dictionary<string, bool> _originalButtonPressStates = new Dictionary<string, bool>();

    // Для работы режима ожидания нажатия кнопки
    private SpeechNodeData _pendingSpeechNode;
    private SpeechNodeImageData _pendingSpeechImageNode;
    private SpeechRandNodeData _pendingSpeechRandNode;

    [Header("Chat Handlers")]
    [SerializeField] private List<ChatConfiguration> chatConfigurations = new List<ChatConfiguration>();
    private int currentChatIndex = 0;

    [Header("Events")]
    [SerializeField] private UnityEvent startDialogueEvent;
    [SerializeField] private UnityEvent endDialogueEvent;

    [System.Serializable]
    public class ChatConfiguration
    {
        public ChatHandler chatHandler;
    }
    private void Awake()
    {
        // Гарантируем создание CharacterManager при запуске
        if (CharacterManager.Instance == null)
        {
            GameObject managerObject = GameObject.Find("CharacterManager");
            if (managerObject == null)
            {
                managerObject = new GameObject("CharacterManager");
                managerObject.AddComponent<CharacterManager>();
            }
            else
            {
                managerObject.AddComponent<CharacterManager>();
            }
            DontDestroyOnLoad(managerObject);
        }
    }

    private void Start()
    {
        // Проверяем, что хотя бы один ChatHandler настроен
        if (chatConfigurations.Count == 0 || chatConfigurations[0].chatHandler == null)
        {
            Debug.LogError("No ChatHandlers configured in DialogueManager! Please assign at least one ChatHandler.");
            return;
        }

        // Проверяем корректность настроек для всех ChatHandlers
        for (int i = 0; i < chatConfigurations.Count; i++)
        {
            var config = chatConfigurations[i];
            if (config.chatHandler == null)
            {
                Debug.LogWarning($"Chat configuration at index {i} has no ChatHandler assigned!");
                continue;
            }

            config.chatHandler.ValidateSetup();

            // Активируем только первый чат
            config.chatHandler.SetActive(i == 0);
        }

        // Подписываемся на событие выбора опции только для активного чата
        if (chatConfigurations[0].chatHandler?.optionPanel != null)
        {
            optionPanel = chatConfigurations[0].chatHandler.optionPanel;
            optionPanel.onOptionSelected += HandleOptionSelection;
        }
        else
        {
            Debug.LogError("OptionPanel not assigned in first ChatHandler");
        }

        // Инициализация компонентов через ChatHandler
        chatPanel = chatConfigurations[0].chatHandler?.chatPanel;
        if (chatPanel == null)
        {
            Debug.LogError("ChatPanel not assigned in first ChatHandler");
        }

        _timerDisplayController = chatConfigurations[0].chatHandler?.timerDisplayController;

        // Настройка кнопки продолжения
        if (_continueButton != null)
        {
            _continueButton.onClick.AddListener(OnContinueButtonPressed);
            _continueButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Continue button not assigned in DialogueManager. Button press feature will not work.");
        }

        StartDialogue(currentDialogue);
    }

    /// <summary>
    /// Запускает диалог по указанному контейнеру
    /// </summary>
    /// <param name="dialogueContainer">Контейнер диалога для запуска</param>
    private void StartDialogue(DialogueContainer dialogueContainer)
    {
        currentDialogue = dialogueContainer;
        ResetVariables();
        visitedNodes.Clear();
        // Сохраняем исходные состояния всех персонажей перед запуском диалога
        SaveOriginalCharacterStates();

        // Инициализация переменных из Exposed Properties
        foreach (var prop in currentDialogue.IntExposedProperties)
        {
            intVariables[prop.PropertyName] = prop.IntValue;
        }
        foreach (var prop in currentDialogue.StringExposedProperties)
        {
            stringVariables[prop.PropertyName] = prop.StringValue;
        }

        // Начинаем диалог с EntryNode
        currentNode = currentDialogue.EntryNodeData;

        // ВЫЗОВ СОБЫТИЯ ЗАПУСКА ДИАЛОГА
        startDialogueEvent?.Invoke();

        ProcessNextNode();
    }
    private void SwitchChat(int targetChatIndex)
    {
        if (targetChatIndex < 0 || targetChatIndex >= chatConfigurations.Count)
        {
            Debug.LogError($"Invalid chat index: {targetChatIndex}. Available range: 0-{chatConfigurations.Count - 1}");
            return;
        }

        // Отключаем текущий чат
        if (currentChatIndex >= 0 && currentChatIndex < chatConfigurations.Count)
        {
            var currentConfig = chatConfigurations[currentChatIndex];
            if (currentConfig.chatHandler != null)
            {
                currentConfig.chatHandler.SetActive(false);

                // Отписываемся от событий предыдущего чата
                if (currentConfig.chatHandler.optionPanel != null)
                {
                    currentConfig.chatHandler.optionPanel.onOptionSelected -= HandleOptionSelection;
                }
            }
        }

        // Включаем новый чат
        currentChatIndex = targetChatIndex;
        var newConfig = chatConfigurations[currentChatIndex];

        if (newConfig.chatHandler == null)
        {
            Debug.LogError($"Chat configuration at index {targetChatIndex} has no ChatHandler assigned!");
            return;
        }

        if (!newConfig.chatHandler.ValidateSetup())
        {
            Debug.LogWarning($"ChatHandler at index {targetChatIndex} has invalid setup but will continue.");
        }

        newConfig.chatHandler.SetActive(true);

        // Переподписываемся на события нового чата
        if (newConfig.chatHandler.optionPanel != null)
        {
            // Сначала отписываемся для предотвращения дублирования
            newConfig.chatHandler.optionPanel.onOptionSelected -= HandleOptionSelection;
            newConfig.chatHandler.optionPanel.onOptionSelected += HandleOptionSelection;
        }

        // Обновляем локальные ссылки
        chatPanel = newConfig.chatHandler.chatPanel;
        optionPanel = newConfig.chatHandler.optionPanel;
        _timerDisplayController = newConfig.chatHandler.timerDisplayController;

        Debug.Log($"Switched to chat configuration index: {currentChatIndex}, handler: {newConfig.chatHandler?.name}");
    }

    private void ProcessChangeChatIconNode(ChangeChatIconNodeData nodeData)
    {
        // Получаем активный ChatHandler
        if (currentChatIndex >= 0 && currentChatIndex < chatConfigurations.Count)
        {
            var chatConfig = chatConfigurations[currentChatIndex];
            if (chatConfig != null && chatConfig.chatHandler != null)
            {
                var chatHandler = chatConfig.chatHandler;
                if (chatHandler.iconChatPanel != null)
                {
                    // Загружаем спрайт из Resources
                    if (!string.IsNullOrEmpty(nodeData.IconSpritePath))
                    {
                        // Убираем возможные расширения в пути
                        string cleanPath = nodeData.IconSpritePath;
                        if (cleanPath.EndsWith(".sprite"))
                            cleanPath = cleanPath.Substring(0, cleanPath.Length - 7);

                        var sprite = Resources.Load<Sprite>(cleanPath);
                        if (sprite != null)
                        {
                            chatHandler.iconChatPanel.sprite = sprite;
                        }
                        else
                        {
                            Debug.LogWarning($"Sprite not found in Resources for path: {cleanPath}. " +
                                $"Make sure the sprite is in Assets/Resources folder with correct path.");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("iconChatPanel is null in active ChatHandler");
                }
            }
            else
            {
                Debug.LogWarning("Chat configuration or ChatHandler is null");
            }
        }
        else
        {
            Debug.LogWarning($"Invalid chat index: {currentChatIndex}");
        }

        // Переходим к следующему узлу
        GoToNextNode(nodeData.Guid);
    }

    private void ProcessChangeChatNameNode(ChangeChatNameNodeData nodeData)
    {
        // Получаем активный ChatHandler
        if (currentChatIndex >= 0 && currentChatIndex < chatConfigurations.Count)
        {
            var chatConfig = chatConfigurations[currentChatIndex];
            if (chatConfig != null && chatConfig.chatHandler != null)
            {
                var chatHandler = chatConfig.chatHandler;
                if (chatHandler.nameChatPanel != null)
                {
                    // Устанавливаем новое название
                    chatHandler.nameChatPanel.text = nodeData.NewChatName;
                }
                else
                {
                    Debug.LogWarning("nameChatPanel is null in active ChatHandler");
                }
            }
            else
            {
                Debug.LogWarning("Chat configuration or ChatHandler is null");
            }
        }
        else
        {
            Debug.LogWarning($"Invalid chat index: {currentChatIndex}");
        }

        // Переходим к следующему узлу
        GoToNextNode(nodeData.Guid);
    }

    private void SaveOriginalCharacterStates()
    {
        _originalButtonPressStates.Clear();

        // Получаем всех персонажей из CharacterManager
        if (CharacterManager.Instance != null)
        {
            var allCharacters = CharacterManager.Instance.GetAllCharacters();
            foreach (var character in allCharacters)
            {
                if (!_originalButtonPressStates.ContainsKey(character.name))
                {
                    _originalButtonPressStates[character.name] = character.RequireButtonPressForMessages;
                }
            }
        }
    }

    /// <summary>
    /// Сбрасывает все переменные диалога к значениям по умолчанию
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

        if (!visitedNodes.Contains(currentNode))
            visitedNodes.Add(currentNode);

        switch (currentNode)
        {
            case EntryNodeData entryNode:
                ProcessEntryNode(entryNode);
                break;
            case SpeechNodeData speechNode:
                ProcessSpeechNode(speechNode);
                break;
            case SpeechNodeImageData speechImageNode:
                ProcessSpeechImageNode(speechImageNode);
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
            case OptionNodeData optionNode:
                ProcessOptionNode(optionNode);
                break;
            case OptionNodeImageData optionImageNode:
                ProcessOptionImageNode(optionImageNode);
                break;
            case EventNodeData eventNode:
                eventNode.Event.Invoke();
                var nextLink = currentDialogue.NodeLinks.FirstOrDefault(l => l.BaseNodeGuid == eventNode.Guid);
                if (nextLink != null)
                {
                    currentNode = GetNodeByGuid(nextLink.TargetNodeGuid);
                    ProcessNextNode();
                }
                else
                    currentNode = null;
                break;
            case CharacterIntConditionNodeData charIntCondition:
                ProcessCharacterIntCondition(charIntCondition);
                break;
            case CharacterModifyIntNodeData charModifyInt:
                ProcessCharacterModifyInt(charModifyInt);
                break;
            case DebugLogNodeData debugLog:
                Debug.Log(debugLog.MessageText);
                GoToNextNode(debugLog.Guid);
                break;
            case DebugWarningNodeData debugWarn:
                Debug.LogWarning(debugWarn.MessageText);
                GoToNextNode(debugWarn.Guid);
                break;
            case DebugErrorNodeData debugErr:
                Debug.LogError(debugErr.MessageText);
                GoToNextNode(debugErr.Guid);
                break;
            case SpeechRandNodeData speechRandNode:
                ProcessSpeechRandNode(speechRandNode);
                break;
            case RandomBranchNodeData randomBranchNode:
                ProcessRandomBranchNode(randomBranchNode);
                break;
            case TimerNodeData timerNode:
                ProcessTimerNode(timerNode);
                break;
            case PauseNodeData pauseNode:
                ProcessPauseNode(pauseNode);
                return;
            case WireNodeData wireNode:
                GoToNextNode(wireNode.Guid);
                break;
            case CharacterButtonPressNodeData characterButtonPressNode:
                ProcessCharacterButtonPressNode(characterButtonPressNode);
                break;
            case ChatSwitchNodeData chatSwitchNode:
                SwitchChat(chatSwitchNode.TargetChatIndex);
                GoToNextNode(chatSwitchNode.Guid);
                break;
            case ChangeChatIconNodeData changeChatIconNode:
                ProcessChangeChatIconNode(changeChatIconNode);
                break;

            case ChangeChatNameNodeData changeChatNameNode:
                ProcessChangeChatNameNode(changeChatNameNode);
                break;
            default:
                Debug.LogWarning($"Неизвестный тип узла: {currentNode?.GetType().Name}");
                currentNode = null;
                break;
        }
    }

    private void ProcessPauseNode(PauseNodeData pauseNode)
    {
        if (_timerDisplayController == null)
        {
            Debug.LogError("TimerDisplayController is not assigned in DialogueManager. Pause timer will not be displayed.");
            // fallback: просто ждём без отображения
            StartCoroutine(DelayedGoToNextNode(pauseNode));
            return;
        }

        var nextLink = currentDialogue.NodeLinks
            .FirstOrDefault(l => l.BaseNodeGuid == pauseNode.Guid);
        string nextGuid = nextLink?.TargetNodeGuid;

        void OnPauseTimeout()
        {
            if (!string.IsNullOrEmpty(nextGuid))
            {
                currentNode = GetNodeByGuid(nextGuid);
                ProcessNextNode();
            }
            else
            {
                currentNode = null;
            }
        }

        _timerDisplayController.StartTimer(pauseNode.DurationSeconds, OnPauseTimeout);
    }

    private void ProcessCharacterButtonPressNode(CharacterButtonPressNodeData nodeData)
    {
        var character = CharacterManager.Instance.GetCharacter(nodeData.CharacterName);
        if (character == null)
        {
            Debug.LogError($"Character '{nodeData.CharacterName}' not found for button press node '{nodeData.Guid}'.");
            GoToNextNode(nodeData.Guid);
            return;
        }

        // Если мы впервые меняем этого персонажа в текущем диалоге, сохраняем исходное состояние
        if (!_originalButtonPressStates.ContainsKey(nodeData.CharacterName))
        {
            _originalButtonPressStates[nodeData.CharacterName] = character.RequireButtonPressForMessages;
        }

        character.RequireButtonPressForMessages = nodeData.RequireButtonPress;
        Debug.Log($"Changed RequireButtonPressForMessages for character '{nodeData.CharacterName}' to {nodeData.RequireButtonPress}");
        GoToNextNode(nodeData.Guid);
    }

    private IEnumerator DelayedGoToNextNode(PauseNodeData pauseNode)
    {
        yield return new WaitForSeconds(pauseNode.DurationSeconds);
        var nextLink = currentDialogue.NodeLinks
            .FirstOrDefault(l => l.BaseNodeGuid == pauseNode.Guid);
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

    private void ProcessTimerNode(TimerNodeData timerNode)
    {
        var outgoingLinks = currentDialogue.NodeLinks
            .Where(l => l.BaseNodeGuid == timerNode.Guid)
            .ToList();

        var optionLinks = outgoingLinks
            .Where(link => link.PortName == "Options")
            .Select(link => GetNodeByGuid(link.TargetNodeGuid))
            .Where(node => node is OptionNodeData or OptionNodeImageData)
            .ToList();

        string timeoutTargetGuid = null;
        var timeoutLink = outgoingLinks.FirstOrDefault(l => l.PortName == "Timeout");
        if (timeoutLink != null)
            timeoutTargetGuid = timeoutLink.TargetNodeGuid;

        if (optionLinks.Count > 0 && optionPanel != null)
        {
            var options = new List<Option>();
            foreach (var optNode in optionLinks)
            {
                string text = "Изображение";
                if (optNode is OptionNodeData opt)
                    text = !string.IsNullOrEmpty(opt.ResponseText) ? opt.ResponseText : "Неизвестный вариант";

                // Ищем связь ИЗ опции к следующему узлу
                var nextLink = currentDialogue.NodeLinks
                    .FirstOrDefault(l => l.BaseNodeGuid == ((BaseNodeData)optNode).Guid);
                if (nextLink != null)
                {
                    options.Add(new Option
                    {
                        Text = text,
                        NextNodeGuid = nextLink.TargetNodeGuid
                    });
                }
            }
            if (options.Count > 0)
            {
                optionPanel.ShowOptions(options);
            }
        }

        void OnTimeout()
        {
            if (optionPanel != null && optionPanel.gameObject.activeSelf)
                optionPanel.Hide();

            if (!string.IsNullOrEmpty(timeoutTargetGuid))
            {
                currentNode = GetNodeByGuid(timeoutTargetGuid);
                ProcessNextNode();
            }
            else
            {
                currentNode = null;
            }
        }

        _timerDisplayController?.StartTimer(timerNode.DurationSeconds, OnTimeout);
    }

    private void ProcessRandomBranchNode(RandomBranchNodeData randomBranchNode)
    {
        if (randomBranchNode.Variants.Count == 0)
        {
            Debug.LogError($"RandomBranchNode {randomBranchNode.Guid} has no variants");
            currentNode = null;
            return;
        }

        // Взвешенный случайный выбор
        float totalWeight = 0f;
        foreach (var variant in randomBranchNode.Variants)
            totalWeight += variant.WeightPercent;

        string selectedPort = "";
        if (totalWeight <= 0f)
        {
            // fallback: первый вариант
            selectedPort = randomBranchNode.Variants[0].PortName;
        }
        else
        {
            float pick = (float)random.NextDouble() * totalWeight;
            float current = 0f;
            foreach (var variant in randomBranchNode.Variants)
            {
                current += variant.WeightPercent;
                if (pick <= current)
                {
                    selectedPort = variant.PortName;
                    break;
                }
            }
        }

        // Находим связь по выбранному порту
        var outgoingLinks = currentDialogue.NodeLinks
            .Where(l => l.BaseNodeGuid == randomBranchNode.Guid && l.PortName == selectedPort)
            .ToList();

        if (outgoingLinks.Count > 0)
        {
            if (outgoingLinks.Count > 1)
            {
                Debug.LogWarning($"RandomBranchNode {randomBranchNode.Guid} has multiple connections for port {selectedPort}. Using first.");
            }
            currentNode = GetNodeByGuid(outgoingLinks.First().TargetNodeGuid);
            ProcessNextNode();
        }
        else
        {
            Debug.LogWarning($"RandomBranchNode {randomBranchNode.Guid} has no connection for selected port {selectedPort}");
            currentNode = null;
        }
    }

    private void ProcessSpeechRandNode(SpeechRandNodeData speechRandNode)
    {
        CharacterData speaker = null;
        if (!string.IsNullOrEmpty(speechRandNode.SpeakerName))
            speaker = CharacterManager.Instance?.GetCharacter(speechRandNode.SpeakerName);

        string selectedText = "";
        if (speechRandNode.Variants.Count > 0)
        {
            // Взвешенный случайный выбор
            float totalWeight = 0f;
            foreach (var v in speechRandNode.Variants)
                totalWeight += v.WeightPercent;

            if (totalWeight <= 0f)
            {
                // fallback: первый
                selectedText = speechRandNode.Variants[0].Text;
            }
            else
            {
                float pick = (float)random.NextDouble() * totalWeight;
                float current = 0f;
                foreach (var v in speechRandNode.Variants)
                {
                    current += v.WeightPercent;
                    if (pick <= current)
                    {
                        selectedText = v.Text;
                        break;
                    }
                }
            }
        }

        // Если список пуст — selectedText остаётся ""
        var message = new Message
        {
            Type = SenderType.NPC,
            Text = selectedText,
            Image = null,
            Audio = null,
            Sender = speaker
        };

        chatPanel.AddMessage(message, MessageTypeDialogue.Speech);

        // Если персонаж требует нажатия кнопки для продолжения
        if (speaker != null && speaker.RequireButtonPressForMessages)
        {
            _pendingSpeechRandNode = speechRandNode;
            ShowContinueButton();
            return; // Останавливаем выполнение до нажатия кнопки
        }

        // Обработка исходящих связей
        var outgoingLinks = currentDialogue.NodeLinks
            .Where(l => l.BaseNodeGuid == speechRandNode.Guid)
            .ToList();

        var optionLinks = outgoingLinks.Where(link =>
        {
            var target = GetNodeByGuid(link.TargetNodeGuid);
            return target is OptionNodeData || target is OptionNodeImageData;
        }).ToList();

        if (optionLinks.Any())
        {
            var options = new List<Option>();
            foreach (var linkToOption in optionLinks)
            {
                var optionNode = GetNodeByGuid(linkToOption.TargetNodeGuid);
                if (optionNode == null) continue;

                string text = "Изображение";
                if (optionNode is OptionNodeData opt)
                    text = !string.IsNullOrEmpty(opt.ResponseText) ? opt.ResponseText : "Неизвестный вариант";

                var nextLinkAfterOption = currentDialogue.NodeLinks
                    .FirstOrDefault(l => l.BaseNodeGuid == ((BaseNodeData)optionNode).Guid);

                if (nextLinkAfterOption != null)
                {
                    options.Add(new Option
                    {
                        Text = text,
                        NextNodeGuid = nextLinkAfterOption.TargetNodeGuid
                    });
                }
            }

            if (options.Count > 0 && optionPanel != null)
            {
                optionPanel.ShowOptions(options);
            }
            else
            {
                Debug.LogError("Нет валидных вариантов для отображения!");
                currentNode = null;
            }
            return;
        }

        var speechLinks = outgoingLinks.Where(link =>
        {
            var target = GetNodeByGuid(link.TargetNodeGuid);
            return target is SpeechNodeData || target is SpeechNodeImageData || target is SpeechRandNodeData;
        }).ToList();

        if (speechLinks.Any())
        {
            if (speechLinks.Count > 1)
            {
                Debug.LogWarning($"SpeechRandNode {speechRandNode.Guid} имеет несколько исходящих Speech-связей. Будет использована первая.");
            }
            string nextGuid = speechLinks.First().TargetNodeGuid;
            StartCoroutine(DelayedGoToNode(nextGuid));
            return;
        }

        var nextLinearLink = outgoingLinks.FirstOrDefault();
        if (nextLinearLink != null)
        {
            currentNode = GetNodeByGuid(nextLinearLink.TargetNodeGuid);
            ProcessNextNode();
        }
        else
        {
            currentNode = null;
        }
    }

    /// <summary>
    /// Обрабатывает EntryNodeData
    /// </summary>
    /// <param name="entryNode">Данные начального узла</param>
    private void ProcessEntryNode(EntryNodeData entryNode)
    {
        // Находим следующий узел после EntryNode
        var nextLink = currentDialogue.NodeLinks
            .FirstOrDefault(l => l.BaseNodeGuid == entryNode.Guid);
        if (nextLink != null)
        {
            currentNode = GetNodeByGuid(nextLink.TargetNodeGuid);
            ProcessNextNode();
        }
        else
        {
            // Если нет следующего узла, завершаем диалог
            currentNode = null;
        }
    }

    /// <summary>
    /// Обрабатывает SpeechNodeData
    /// </summary>
    /// <param name="speechNode">Данные узла речи</param>
    private void ProcessSpeechNode(SpeechNodeData speechNode)
    {
        CharacterData speaker = GetCharacterByName(speechNode.SpeakerName);
        if (speaker == null)
        {
            Debug.LogError($"SpeechNode '{speechNode.Guid}' has no valid speaker. Assign a CharacterData in the graph editor.");
            return;
        }

        var message = new Message
        {
            Type = SenderType.NPC,
            Text = speechNode.DialogueText,
            Image = null,
            Audio = AssetLoader.LoadAudioClip(speechNode.AudioClipGuid),
            Sender = speaker
        };

        chatPanel.AddMessage(message, MessageTypeDialogue.Speech);

        // Если персонаж требует нажатия кнопки для продолжения
        if (speaker.RequireButtonPressForMessages)
        {
            _pendingSpeechNode = speechNode;
            ShowContinueButton();
            return; // Останавливаем выполнение до нажатия кнопки
        }

        // Исходная логика продолжения с задержкой
        if (message.Audio != null)
            StartCoroutine(PlayAudioAfterDelay(message.Audio, messageDelay));

        ProcessSpeechNodeContinuation(speechNode);
    }

    private void ProcessSpeechNodeContinuation(SpeechNodeData speechNode)
    {
        var outgoingLinks = currentDialogue.NodeLinks
            .Where(l => l.BaseNodeGuid == speechNode.Guid)
            .ToList();

        // Проверяем: есть ли исходящие связи к OptionNode?
        var optionLinks = outgoingLinks.Where(link =>
        {
            var target = GetNodeByGuid(link.TargetNodeGuid);
            return target is OptionNodeData || target is OptionNodeImageData;
        }).ToList();

        if (optionLinks.Any())
        {
            // Режим выбора опций
            var options = new List<Option>();
            foreach (var linkToOption in optionLinks)
            {
                var optionNode = GetNodeByGuid(linkToOption.TargetNodeGuid);
                if (optionNode == null) continue;

                string text = "Изображение";
                if (optionNode is OptionNodeData opt)
                    text = !string.IsNullOrEmpty(opt.ResponseText) ? opt.ResponseText : "Неизвестный вариант";

                var nextLinkAfterOption = currentDialogue.NodeLinks
                    .FirstOrDefault(l => l.BaseNodeGuid == ((BaseNodeData)optionNode).Guid);

                if (nextLinkAfterOption != null)
                {
                    options.Add(new Option
                    {
                        Text = text,
                        NextNodeGuid = nextLinkAfterOption.TargetNodeGuid
                    });
                }
            }

            if (options.Count > 0 && optionPanel != null)
            {
                optionPanel.ShowOptions(options);
            }
            else
            {
                Debug.LogError("Нет валидных вариантов для отображения!");
                currentNode = null;
            }
            return;
        }

        // Нет Option — проверяем: есть ли Speech-связи?
        var speechLinks = outgoingLinks.Where(link =>
        {
            var target = GetNodeByGuid(link.TargetNodeGuid);
            return target is SpeechNodeData || target is SpeechNodeImageData;
        }).ToList();

        if (speechLinks.Any())
        {
            // Цепочка Speech → Speech: запускаем следующее сообщение с задержкой
            if (speechLinks.Count > 1)
            {
                Debug.LogWarning($"SpeechNode {speechNode.Guid} имеет несколько исходящих Speech-связей. Будет использована первая.");
            }
            string nextGuid = speechLinks.First().TargetNodeGuid;
            StartCoroutine(DelayedGoToNode(nextGuid));
            return;
        }

        // Нет ни Option, ни Speech — линейный переход (к Condition, Modify, End и т.д.)
        var nextLinearLink = outgoingLinks.FirstOrDefault();
        if (nextLinearLink != null)
        {
            currentNode = GetNodeByGuid(nextLinearLink.TargetNodeGuid);
            ProcessNextNode();
        }
        else
        {
            currentNode = null;
        }
    }

    /// <summary>
    /// Обрабатывает SpeechNodeImageData
    /// </summary>
    /// <param name="speechImageNode">Данные узла изображения речи</param>
    private void ProcessSpeechImageNode(SpeechNodeImageData speechImageNode)
    {
        CharacterData speaker = GetCharacterByName(speechImageNode.SpeakerName);
        if (speaker == null)
        {
            Debug.LogError($"SpeechImageNode '{speechImageNode.Guid}' has no valid speaker...");
            return;
        }

        var message = new Message
        {
            Type = SenderType.NPC,
            Text = null,
            Image = !string.IsNullOrEmpty(speechImageNode.ImageSpritePath) ?
                Resources.Load<Sprite>(speechImageNode.ImageSpritePath) : null,
            Audio = null,
            Sender = speaker
        };

        chatPanel.AddMessage(message, MessageTypeDialogue.SpeechImage);

        // Если персонаж требует нажатия кнопки для продолжения
        if (speaker.RequireButtonPressForMessages)
        {
            _pendingSpeechImageNode = speechImageNode;
            ShowContinueButton();
            return; // Останавливаем выполнение до нажатия кнопки
        }

        // Исходная логика продолжения с задержкой
        ProcessSpeechImageNodeContinuation(speechImageNode);
    }

    private void ProcessSpeechImageNodeContinuation(SpeechNodeImageData speechImageNode)
    {
        var outgoingLinks = currentDialogue.NodeLinks
            .Where(l => l.BaseNodeGuid == speechImageNode.Guid)
            .ToList();

        // Проверяем: есть ли исходящие связи к OptionNode?
        var optionLinks = outgoingLinks.Where(link =>
        {
            var target = GetNodeByGuid(link.TargetNodeGuid);
            return target is OptionNodeData || target is OptionNodeImageData;
        }).ToList();

        if (optionLinks.Any())
        {
            var options = new List<Option>();
            foreach (var linkToOption in optionLinks)
            {
                var optionNode = GetNodeByGuid(linkToOption.TargetNodeGuid);
                if (optionNode == null) continue;

                string text = "Изображение";
                if (optionNode is OptionNodeData opt)
                    text = !string.IsNullOrEmpty(opt.ResponseText) ? opt.ResponseText : "Неизвестный вариант";

                var nextLinkAfterOption = currentDialogue.NodeLinks
                    .FirstOrDefault(l => l.BaseNodeGuid == ((BaseNodeData)optionNode).Guid);

                if (nextLinkAfterOption != null)
                {
                    options.Add(new Option
                    {
                        Text = text,
                        NextNodeGuid = nextLinkAfterOption.TargetNodeGuid
                    });
                }
            }

            if (options.Count > 0 && optionPanel != null)
            {
                optionPanel.ShowOptions(options);
            }
            else
            {
                Debug.LogError("Нет валидных вариантов для отображения!");
                currentNode = null;
            }
            return;
        }

        // Нет Option — проверяем Speech-связи
        var speechLinks = outgoingLinks.Where(link =>
        {
            var target = GetNodeByGuid(link.TargetNodeGuid);
            return target is SpeechNodeData || target is SpeechNodeImageData;
        }).ToList();

        if (speechLinks.Any())
        {
            if (speechLinks.Count > 1)
            {
                Debug.LogWarning($"SpeechImageNode {speechImageNode.Guid} имеет несколько исходящих Speech-связей. Будет использована первая.");
            }
            string nextGuid = speechLinks.First().TargetNodeGuid;
            StartCoroutine(DelayedGoToNode(nextGuid));
            return;
        }

        // Линейный переход
        var nextLinearLink = outgoingLinks.FirstOrDefault();
        if (nextLinearLink != null)
        {
            currentNode = GetNodeByGuid(nextLinearLink.TargetNodeGuid);
            ProcessNextNode();
        }
        else
        {
            currentNode = null;
        }
    }

    private void ShowContinueButton()
    {
        if (_continueButton != null)
        {
            _continueButton.gameObject.SetActive(true);
        }
    }

    private void HideContinueButton()
    {
        if (_continueButton != null)
        {
            _continueButton.gameObject.SetActive(false);
        }
    }

    private void OnContinueButtonPressed()
    {
        HideContinueButton();

        // Продолжаем диалог в зависимости от типа ожидаемого узла
        if (_pendingSpeechNode != null)
        {
            ProcessSpeechNodeContinuation(_pendingSpeechNode);
            _pendingSpeechNode = null;
        }
        else if (_pendingSpeechImageNode != null)
        {
            ProcessSpeechImageNodeContinuation(_pendingSpeechImageNode);
            _pendingSpeechImageNode = null;
        }
        else if (_pendingSpeechRandNode != null)
        {
            ProcessSpeechRandNodeContinuation(_pendingSpeechRandNode);
            _pendingSpeechRandNode = null;
        }
    }

    private void ProcessSpeechRandNodeContinuation(SpeechRandNodeData speechRandNode)
    {
        var outgoingLinks = currentDialogue.NodeLinks
            .Where(l => l.BaseNodeGuid == speechRandNode.Guid)
            .ToList();

        var optionLinks = outgoingLinks.Where(link =>
        {
            var target = GetNodeByGuid(link.TargetNodeGuid);
            return target is OptionNodeData || target is OptionNodeImageData;
        }).ToList();

        if (optionLinks.Any())
        {
            var options = new List<Option>();
            foreach (var linkToOption in optionLinks)
            {
                var optionNode = GetNodeByGuid(linkToOption.TargetNodeGuid);
                if (optionNode == null) continue;

                string text = "Изображение";
                if (optionNode is OptionNodeData opt)
                    text = !string.IsNullOrEmpty(opt.ResponseText) ? opt.ResponseText : "Неизвестный вариант";

                var nextLinkAfterOption = currentDialogue.NodeLinks
                    .FirstOrDefault(l => l.BaseNodeGuid == ((BaseNodeData)optionNode).Guid);

                if (nextLinkAfterOption != null)
                {
                    options.Add(new Option
                    {
                        Text = text,
                        NextNodeGuid = nextLinkAfterOption.TargetNodeGuid
                    });
                }
            }

            if (options.Count > 0 && optionPanel != null)
            {
                optionPanel.ShowOptions(options);
            }
            else
            {
                Debug.LogError("Нет валидных вариантов для отображения!");
                currentNode = null;
            }
            return;
        }

        var speechLinks = outgoingLinks.Where(link =>
        {
            var target = GetNodeByGuid(link.TargetNodeGuid);
            return target is SpeechNodeData || target is SpeechNodeImageData || target is SpeechRandNodeData;
        }).ToList();

        if (speechLinks.Any())
        {
            if (speechLinks.Count > 1)
            {
                Debug.LogWarning($"SpeechRandNode {speechRandNode.Guid} имеет несколько исходящих Speech-связей. Будет использована первая.");
            }
            string nextGuid = speechLinks.First().TargetNodeGuid;
            StartCoroutine(DelayedGoToNode(nextGuid));
            return;
        }

        var nextLinearLink = outgoingLinks.FirstOrDefault();
        if (nextLinearLink != null)
        {
            currentNode = GetNodeByGuid(nextLinearLink.TargetNodeGuid);
            ProcessNextNode();
        }
        else
        {
            currentNode = null;
        }
    }

    private IEnumerator DelayedGoToNode(string nextNodeGuid, float delay = -1f)
    {
        if (delay < 0f) delay = messageDelay;
        yield return new WaitForSeconds(delay);
        currentNode = GetNodeByGuid(nextNodeGuid);
        ProcessNextNode();
    }

    /// <summary>
    /// Обрабатывает OptionNodeData
    /// </summary>
    private void ProcessOptionNode(OptionNodeData optionNode)
    {
        // Создаем список вариантов ответа
        var options = new List<Option>();
        // Находим все связи от этого узла
        var optionLinks = currentDialogue.NodeLinks
            .Where(l => l.BaseNodeGuid == optionNode.Guid)
            .ToList();

        foreach (var link in optionLinks)
        {
            var targetNode = GetNodeByGuid(link.TargetNodeGuid);
            string optionText = "Вариант ответа";
            if (targetNode is OptionNodeData optionTarget)
            {
                optionText = !string.IsNullOrEmpty(optionTarget.ResponseText) ?
                    optionTarget.ResponseText : "Вариант ответа";
            }
            else if (targetNode is OptionNodeImageData)
            {
                optionText = "Изображение";
            }
            options.Add(new Option
            {
                Text = optionText,
                NextNodeGuid = link.TargetNodeGuid
            });
        }

        // Показываем варианты ответа
        if (options.Count > 0 && optionPanel != null)
        {
            optionPanel.ShowOptions(options);
            currentNode = optionNode;
        }
        else
        {
            Debug.LogWarning($"No options found for OptionNode {optionNode.Guid}");
            // Пытаемся найти следующий узел
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
    /// Обрабатывает OptionNodeImageData
    /// </summary>
    /// <param name="optionImageNode">Данные узла изображения вариантов ответа</param>
    private void ProcessOptionImageNode(OptionNodeImageData optionImageNode)
    {
        // Получаем варианты ответов из узла
        var options = new List<Option>();
        // Получаем связанные OptionNodeData для всех выходов
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
                // Для изображений вариантов ответа
                options.Add(new Option
                {
                    Text = "Изображение",
                    NextNodeGuid = link.TargetNodeGuid
                });
            }
        }

        // Показываем панель вариантов ответов
        optionPanel.ShowOptions(options);
        // Устанавливаем текущий узел как OptionNode для последующей обработки выбора
        currentNode = optionImageNode;
    }

    /// <summary>
    /// Обрабатывает IntConditionNodeData
    /// </summary>
    /// <param name="intCondition">Данные числового условия</param>
    private void ProcessIntCondition(IntConditionNodeData intCondition)
    {
        // Проверяем условие
        bool conditionResult = ConditionHandler.EvaluateIntCondition(
            intCondition, intVariables);

        // Определяем следующий узел в зависимости от результата
        var nextLinks = currentDialogue.NodeLinks
            .Where(l => l.BaseNodeGuid == intCondition.Guid)
            .ToList();

        // Находим подходящий выход
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

        // Если подходящий выход не найден, завершаем диалог
        Debug.LogWarning($"Не найден подходящий выход для условия IntConditionNode {intCondition.Guid}");
        currentNode = null;
    }

    /// <summary>
    /// Обрабатывает StringConditionNodeData
    /// </summary>
    /// <param name="stringCondition">Данные строкового условия</param>
    private void ProcessStringCondition(StringConditionNodeData stringCondition)
    {
        // Проверяем условие
        bool conditionResult = ConditionHandler.EvaluateStringCondition(
            stringCondition, stringVariables);

        // Определяем следующий узел в зависимости от результата
        var nextLinks = currentDialogue.NodeLinks
            .Where(l => l.BaseNodeGuid == stringCondition.Guid)
            .ToList();

        // Находим подходящий выход
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

        // Если подходящий выход не найден, завершаем диалог
        Debug.LogWarning($"Не найден подходящий выход для условия StringConditionNode {stringCondition.Guid}");
        currentNode = null;
    }

    /// <summary>
    /// Обрабатывает ModifyIntNodeData
    /// </summary>
    /// <param name="modifyNode">Данные модификатора числа</param>
    private void ProcessModifyIntNode(ModifyIntNodeData modifyNode)
    {
        // Проверяем наличие переменной
        if (!intVariables.ContainsKey(modifyNode.SelectedProperty))
        {
            Debug.LogError($"Переменная {modifyNode.SelectedProperty} не найдена в intVariables");
            currentNode = null;
            return;
        }

        // Применяем операцию
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
                    Debug.LogWarning("Деление на ноль в ModifyIntNode");
                break;
            case OperatorType.Increment:
                intVariables[modifyNode.SelectedProperty]++;
                break;
            case OperatorType.Decrement:
                intVariables[modifyNode.SelectedProperty]--;
                break;
        }

        // Находим следующий узел
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
    /// Обрабатывает EndNodeData
    /// </summary>
    /// <param name="endNode">Данные конечного узла</param>
    private void ProcessEndNode(EndNodeData endNode)
    {
        // Добавляем системное сообщение о завершении диалога
        var message = new Message
        {
            Type = SenderType.System,
            Text = "Диалог завершен"
        };
        chatPanel.AddMessage(message, MessageTypeDialogue.System);

        // Если указан следующий диалог, запускаем его
        if (!string.IsNullOrEmpty(endNode.NextDialogueName))
        {
            DialogueContainer nextDialogue = Resources.Load<DialogueContainer>(endNode.NextDialogueName);
            if (nextDialogue != null)
            {
                StartDialogue(nextDialogue);
            }
            else
            {
                Debug.LogError($"Диалог {endNode.NextDialogueName} не найден в ресурсах");
                // Явное завершение текущего диалога при ошибке
                currentNode = null;
                endDialogueEvent?.Invoke();
            }
        }
        else
        {
            // Завершаем текущий диалог
            currentNode = null;

            // ВЫЗОВ СОБЫТИЯ ЗАВЕРШЕНИЯ ДИАЛОГА
            endDialogueEvent?.Invoke();
        }
    }


    /// <summary>
    /// Обработка выбора опции игроком с задержкой перед следующим сообщением
    /// </summary>
    public void HandleOptionSelection(string nextNodeGuid)
    {
        if (optionPanel != null)
            optionPanel.Hide();

        _timerDisplayController?.StopTimer();

        currentNode = GetNodeByGuid(nextNodeGuid);
        ProcessNextNode();
    }

    /// <summary>
    /// Получает узел по его GUID
    /// </summary>
    /// <param name="guid">GUID узла</param>
    /// <returns>BaseNodeData или null</returns>
    private object GetNodeByGuid(string guid)
    {
        if (string.IsNullOrEmpty(guid))
            return null;
        // Ищем в Speech
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
        var charIntConditionNode = currentDialogue.CharacterIntConditionNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (charIntConditionNode != null) return charIntConditionNode;
        var charModifyIntNode = currentDialogue.CharacterModifyIntNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (charModifyIntNode != null) return charModifyIntNode;
        var debugLogNode = currentDialogue.DebugLogNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (debugLogNode != null) return debugLogNode;
        var debugWarnNode = currentDialogue.DebugWarningNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (debugWarnNode != null) return debugWarnNode;
        var debugErrNode = currentDialogue.DebugErrorNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (debugErrNode != null) return debugErrNode;
        // Поддержка SpeechRandNodeData
        var speechRandNode = currentDialogue.SpeechRandNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (speechRandNode != null) return speechRandNode;
        var timerNode = currentDialogue.TimerNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (timerNode != null) return timerNode;
        var pauseNode = currentDialogue.PauseNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (pauseNode != null) return pauseNode;
        var wireNode = currentDialogue.WireNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (wireNode != null) return wireNode;
        var charButtonPressNode = currentDialogue.CharacterButtonPressNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (charButtonPressNode != null) return charButtonPressNode;
        // Добавлено: обработка ChatSwitchNodeData
        var chatSwitchNode = currentDialogue.ChatSwitchNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (chatSwitchNode != null) return chatSwitchNode;

        var changeChatIconNode = currentDialogue.ChangeChatIconNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (changeChatIconNode != null) return changeChatIconNode;

        var changeChatNameNode = currentDialogue.ChangeChatNameNodeDatas.FirstOrDefault(n => n.Guid == guid);
        if (changeChatNameNode != null) return changeChatNameNode;
        // EntryNode ищем ОТДЕЛЬНО и ТОЛЬКО если guid совпадает
        if (currentDialogue.EntryNodeData?.Guid == guid)
            return currentDialogue.EntryNodeData;
        // Если ничего не найдено — возвращаем null
        return null;
    }


    private void GoToNextNode(string currentGuid)
    {
        var nextLink = currentDialogue.NodeLinks.FirstOrDefault(l => l.BaseNodeGuid == currentGuid);
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
    /// Воспроизводит аудио с задержкой после отображения текста
    /// </summary>
    /// <param name="audioClip">Аудио клип для воспроизведения</param>
    /// <param name="delay">Задержка в секундах</param>
    /// <returns>Корутину для воспроизведения аудио</returns>
    private IEnumerator PlayAudioAfterDelay(AudioClip audioClip, float delay)
    {
        yield return new WaitForSeconds(delay);
        AudioSource.PlayClipAtPoint(audioClip, Camera.main.transform.position);
    }

    /// <summary>
    /// Сбрасывает текущий диалог
    /// </summary>
    public void ResetDialogue()
    {
        // Восстанавливаем исходные состояния персонажей
        RestoreOriginalCharacterStates();
        currentDialogue = null;
        currentNode = null;
        visitedNodes.Clear();
        ResetVariables();

        // ВЫЗОВ СОБЫТИЯ ЗАВЕРШЕНИЯ ДИАЛОГА ПРИ СБРОСЕ
        endDialogueEvent?.Invoke();
    }

    private void RestoreOriginalCharacterStates()
    {
        foreach (var kvp in _originalButtonPressStates)
        {
            var character = CharacterManager.Instance.GetCharacter(kvp.Key);
            if (character != null)
            {
                character.RequireButtonPressForMessages = kvp.Value;
            }
        }

        // Очищаем кэш после восстановления
        _originalButtonPressStates.Clear();
    }

    /// <summary>
    /// Получает персонажа по имени из Resources/Characters
    /// </summary>
    private CharacterData GetCharacterByName(string characterName)
    {
        if (string.IsNullOrEmpty(characterName))
            return null;

        // Ищем в кэше CharacterManager
        var character = CharacterManager.Instance?.GetCharacter(characterName);
        if (character != null)
            return character;

        // Fallback: загрузка напрямую из Resources
        character = Resources.Load<CharacterData>($"Characters/{characterName}");
        if (character == null)
            Debug.LogError($"Character '{characterName}' not found in Resources/Characters");
        return character;
    }

    /// <summary>
    /// Вспомогательный класс для загрузки ассетов в runtime
    /// </summary>
    public static class AssetLoader
    {
        /// <summary>
        /// Загружает аудио клип по GUID (заглушка для runtime)
        /// </summary>
        /// <param name="guid">GUID аудио</param>
        /// <returns>AudioClip или null</returns>
        public static AudioClip LoadAudioClip(string guid)
        {
            // В runtime нет AssetDatabase, поэтому используем заглушку
            // В реальной системе нужно реализовать загрузку из Resources
            return null;
        }

        /// <summary>
        /// Загружает спрайт по GUID (заглушка для runtime)
        /// </summary>
        /// <param name="guid">GUID спрайта</param>
        /// <returns>Sprite или null</returns>
        public static Sprite LoadSprite(string guid)
        {
            // В runtime нет AssetDatabase, поэтому используем заглушку
            // В реальной системе нужно реализовать загрузку из Resources
            return null;
        }
    }

    private void ProcessCharacterIntCondition(CharacterIntConditionNodeData condition)
    {
        var character = CharacterManager.Instance.GetCharacter(condition.CharacterName);
        if (character == null)
        {
            Debug.LogError($"Character '{condition.CharacterName}' not found for condition node.");
            currentNode = null;
            return;
        }

        if (!character.TryGetVariable(condition.SelectedVariable, out var variable))
        {
            Debug.LogError($"Variable '{condition.SelectedVariable}' not found on character '{condition.CharacterName}'.");
            currentNode = null;
            return;
        }

        bool result = false;
        switch (condition.Comparison)
        {
            case ComparisonType.Equal: result = variable.Value == condition.CompareValue; break;
            case ComparisonType.NotEqual: result = variable.Value != condition.CompareValue; break;
            case ComparisonType.Greater: result = variable.Value > condition.CompareValue; break;
            case ComparisonType.Less: result = variable.Value < condition.CompareValue; break;
            case ComparisonType.GreaterOrEqual: result = variable.Value >= condition.CompareValue; break;
            case ComparisonType.LessOrEqual: result = variable.Value <= condition.CompareValue; break;
            default: result = false; break;
        }

        var nextLinks = currentDialogue.NodeLinks.Where(l => l.BaseNodeGuid == condition.Guid).ToList();
        foreach (var link in nextLinks)
        {
            if ((link.PortName == "True" && result) || (link.PortName == "False" && !result))
            {
                currentNode = GetNodeByGuid(link.TargetNodeGuid);
                ProcessNextNode();
                return;
            }
        }

        Debug.LogWarning($"No matching output port for CharacterIntConditionNode {condition.Guid}");
        currentNode = null;
    }

    private void ProcessCharacterModifyInt(CharacterModifyIntNodeData modify)
    {
        var character = CharacterManager.Instance.GetCharacter(modify.CharacterName);
        if (character == null)
        {
            Debug.LogError($"Character '{modify.CharacterName}' not found for modify node.");
            currentNode = null;
            return;
        }

        if (!character.TryGetVariable(modify.SelectedVariable, out var variable))
        {
            Debug.LogError($"Variable '{modify.SelectedVariable}' not found on character '{modify.CharacterName}'.");
            currentNode = null;
            return;
        }

        switch (modify.Operator)
        {
            case OperatorType.Set: variable.Value = modify.Value; break;
            case OperatorType.Add: variable.Value += modify.Value; break;
            case OperatorType.Subtract: variable.Value -= modify.Value; break;
            case OperatorType.Multiply: variable.Value *= modify.Value; break;
            case OperatorType.Divide:
                if (modify.Value != 0) variable.Value /= modify.Value;
                else Debug.LogWarning("Division by zero in CharacterModifyIntNode");
                break;
            case OperatorType.Increment: variable.Value++; break;
            case OperatorType.Decrement: variable.Value--; break;
        }

        var nextLink = currentDialogue.NodeLinks.FirstOrDefault(l => l.BaseNodeGuid == modify.Guid);
        if (nextLink != null)
        {
            currentNode = GetNodeByGuid(nextLink.TargetNodeGuid);
            ProcessNextNode();
        }
        else currentNode = null;
    }

    private void OnDestroy()
    {
        // Гарантируем вызов события завершения при уничтожении объекта
        if (currentNode != null)
        {
            endDialogueEvent?.Invoke();
        }
        RestoreOriginalCharacterStates();
    }
}