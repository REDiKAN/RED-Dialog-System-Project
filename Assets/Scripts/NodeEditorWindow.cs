using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor;
using System;
using UnityEngine;

public class NodeEditorWindow : VisualElement
{
    private BaseNode targetNode;
    private DialogueGraphView graphView;
    private INodeEditor nodeEditor;
    private Vector2 originalPosition;
    private object copiedProperties;

    public NodeEditorWindow(BaseNode node, DialogueGraphView graphView)
    {
        this.targetNode = node;
        this.graphView = graphView;
        this.originalPosition = node.GetPosition().position;

        style.position = Position.Absolute;
        style.width = 400;
        style.height = 600;
        style.backgroundColor = new Color(0.21f, 0.21f, 0.21f);
        style.borderWidth = 2;
        style.borderColor = Color.gray;
        style.left = (graphView.contentRect.width - 400) / 2;
        style.top = (graphView.contentRect.height - 600) / 2;

        DrawWindow();
    }

    private void DrawWindow()
    {
        // Заголовок
        var header = new Label($"Editing: {targetNode.GetType().Name}");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 16;
        header.style.marginBottom = 10;
        Add(header);

        // Создаем соответствующий редактор для типа ноды
        nodeEditor = targetNode switch
        {
            SpeechNode speechNode => new SpeechNodeEditor(speechNode, graphView),
            OptionNode optionNode => new OptionNodeEditor(optionNode, graphView),
            IntConditionNode intNode => new IntConditionNodeEditor(intNode, graphView),
            StringConditionNode strNode => new StringConditionNodeEditor(strNode, graphView),
            ModifyIntNode modifyNode => new ModifyIntNodeEditor(modifyNode, graphView),
            EndNode endNode => new EndNodeEditor(endNode, graphView),
            _ => new BaseNodeEditor(targetNode, graphView)
        };

        Add(nodeEditor.VisualElement);

        // Кнопки действий
        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.justifyContent = Justify.SpaceBetween;
        buttonContainer.style.marginTop = 10;

        var applyButton = new Button(ApplyChanges) { text = "Apply" };
        var resetButton = new Button(ResetChanges) { text = "Reset" };
        var copyButton = new Button(CopyProperties) { text = "Copy" };
        var pasteButton = new Button(PasteProperties) { text = "Paste" };
        var closeButton = new Button(() => parent.Remove(this)) { text = "Close" };

        buttonContainer.Add(applyButton);
        buttonContainer.Add(resetButton);
        buttonContainer.Add(copyButton);
        buttonContainer.Add(pasteButton);
        buttonContainer.Add(closeButton);

        Add(buttonContainer);
    }

    private void ApplyChanges()
    {
        nodeEditor.ApplyChanges();
        targetNode.SetPosition(new Rect(nodeEditor.Position, targetNode.GetPosition().size));
        graphView.RefreshNodeVisuals(targetNode);
    }

    private void ResetChanges() => nodeEditor.ResetChanges();
    private void CopyProperties() => copiedProperties = nodeEditor.CopyProperties();
    private void PasteProperties() => nodeEditor.PasteProperties(copiedProperties);
}