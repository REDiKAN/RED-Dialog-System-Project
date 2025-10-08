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
/// Управляет выполнением диалогов в реальном времени
/// </summary>
public class DialogueManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ChatPanel chatPanel;
    [SerializeField] private OptionPanel optionPanel;

    [Header("Dialogue Settings")]
    [SerializeField] private float messageDelay = 0.5f; // Задержка между сообщениями

    [SerializeField] private DialogueContainer currentDialogue;
    private object currentNode; // Исправлено: BaseNodeData -> object (ошибки 1,9)
    private Dictionary<string, int> intVariables = new Dictionary<string, int>();
    private Dictionary<string, string> stringVariables = new Dictionary<string, string>();
    private List<object> visitedNodes = new List<object>(); // Исправлено: BaseNodeData -> object

    private void Start()
    {
        // Подписываемся на событие выбора опции
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
    /// Запускает диалог по указанному контейнеру
    /// </summary>
    /// <param name="dialogueContainer">Контейнер диалога для запуска</param>
    public void StartDialogue(DialogueContainer dialogueContainer)
    {
        currentDialogue = dialogueContainer;
        ResetVariables();
        visitedNodes.Clear();

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
        ProcessNextNode();
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
            default:
                Debug.LogWarning($"Неизвестный тип узла: {currentNode?.GetType().Name}");
                currentNode = null;
                break;
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
        CharacterData speaker = GetCharacterByGuid(speechNode.SpeakerGuid);
        var message = new Message
        {
            Type = SenderType.NPC,
            Text = speechNode.DialogueText,
            Image = null,
            Audio = AssetLoader.LoadAudioClip(speechNode.AudioClipGuid),
            Sender = speaker
        };
        chatPanel.AddMessage(message, MessageType.Speech);

        if (message.Audio != null)
        {
            StartCoroutine(PlayAudioAfterDelay(message.Audio, messageDelay));
        }

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
            // Режим выбора опций — как раньше
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
        CharacterData speaker = GetCharacterByGuid(speechImageNode.SpeakerGuid);
        var message = new Message
        {
            Type = SenderType.NPC,
            Text = null,
            Image = AssetLoader.LoadSprite(speechImageNode.ImageSpriteGuid),
            Audio = null,
            Sender = speaker
        };
        chatPanel.AddMessage(message, MessageType.SpeechImage);

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

    private IEnumerator DelayedGoToNode(string nextNodeGuid)
    {
        yield return new WaitForSeconds(messageDelay);
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
                    Text = "Изображение", // Исправлено: OptionNodeImageData не имеет ResponseText
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
        chatPanel.AddMessage(message, MessageType.System);

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
            }
        }
        else
        {
            // Завершаем текущий диалог
            currentNode = null;
        }
    }

    /// <summary>
    /// Обработка выбора опции игроком с задержкой перед следующим сообщением
    /// </summary>
    public void HandleOptionSelection(string nextNodeGuid)
    {
        // Сразу скрываем панель
        if (optionPanel != null)
            optionPanel.Hide();

        // Получаем текст опции через обратную связь
        var linkToNext = currentDialogue.NodeLinks
            .FirstOrDefault(l => l.TargetNodeGuid == nextNodeGuid);

        if (linkToNext != null)
        {
            var optionNode = GetNodeByGuid(linkToNext.BaseNodeGuid);
            string optionText = "Изображение";

            if (optionNode is OptionNodeData opt)
                optionText = !string.IsNullOrEmpty(opt.ResponseText) ? opt.ResponseText : "Неизвестный вариант";

            var message = new Message
            {
                Type = SenderType.Player,
                Text = optionText // ← без префикса!
            };
            chatPanel.AddMessage(message, MessageType.OptionText);
        }

        // Устанавливаем следующий узел и продолжаем
        currentNode = GetNodeByGuid(nextNodeGuid);
        ProcessNextNode(); // ← сразу, без задержки
    }

    /// <summary>
    /// Задержка перед обработкой следующего узла (для плавности диалога)
    /// </summary>
    private IEnumerator DelayedProcessNextNode()
    {
        yield return new WaitForSeconds(messageDelay);
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

        // Ищем в всех типах узлов
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
        currentDialogue = null;
        currentNode = null;
        visitedNodes.Clear();
        ResetVariables();
    }

    /// <summary>
    /// Получает персонажа по GUID (заглушка для runtime)
    /// </summary>
    /// <param name="guid">GUID персонажа</param>
    /// <returns>CharacterData или null</returns>
    private CharacterData GetCharacterByGuid(string guid)
    {
        // В runtime нет AssetDatabase, поэтому используем заглушку
        // В реальной системе нужно реализовать загрузку из Resources
        return null;
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
}