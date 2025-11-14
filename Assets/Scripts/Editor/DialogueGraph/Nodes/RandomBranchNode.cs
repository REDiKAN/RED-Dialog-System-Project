// Assets/Scripts/Editor/DialogueGraph/Nodes/RandomBranchNode.cs
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEditor;

public class RandomBranchNode : BaseNode
{
    private VisualElement variantsContainer;
    public List<BranchVariantElement> variantElements = new List<BranchVariantElement>();
    private Label totalWeightLabel;

    public class BranchVariantElement
    {
        public RandomBranchVariant Data;
        public VisualElement Container;
        public Port Port;
        public FloatField WeightField;
        public Slider WeightSlider;
        public Button RemoveButton;
        public TextField NameField;
    }

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Random Branch";

        // Input port
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Header for variants section
        var variantsHeader = new VisualElement();
        variantsHeader.style.flexDirection = FlexDirection.Row;
        variantsHeader.style.justifyContent = Justify.SpaceBetween;
        variantsHeader.style.marginBottom = 10;

        var variantsTitle = new Label("Branch Variants");
        variantsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;

        totalWeightLabel = new Label("Total: 0%");
        totalWeightLabel.style.color = new StyleColor(Color.gray);

        variantsHeader.Add(variantsTitle);
        variantsHeader.Add(totalWeightLabel);
        mainContainer.Add(variantsHeader);

        variantsContainer = new VisualElement();
        mainContainer.Add(variantsContainer);

        var addButton = new Button(AddVariant) { text = "Add Variant" };
        addButton.style.marginTop = 5;
        mainContainer.Add(addButton);

        RefreshExpandedState();
        RefreshPorts();

        UpdateTotalWeight();

        // Set fixed width for the node
        SetPosition(new Rect(position, new Vector2(300, 200)));

        // Add initial variant
        AddVariant();
    }

    private void AddVariant()
    {
        var variant = new RandomBranchVariant
        {
            PortName = $"Variant {variantElements.Count + 1}",
            WeightPercent = 10f
        };
        CreateVariantUI(variant);
        UpdateTotalWeight();
    }

    private void CreateVariantUI(RandomBranchVariant variant)
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.marginBottom = 12;
        container.style.paddingTop = 8;
        container.style.paddingBottom = 8;
        container.style.paddingLeft = 8;
        container.style.paddingRight = 8;
        container.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.2f));
        container.style.borderTopLeftRadius = 4;
        container.style.borderTopRightRadius = 4;
        container.style.borderBottomLeftRadius = 4;
        container.style.borderBottomRightRadius = 4;

        // Top row: Port name and remove button
        var topRow = new VisualElement();
        topRow.style.flexDirection = FlexDirection.Row;
        topRow.style.justifyContent = Justify.SpaceBetween;
        topRow.style.alignItems = Align.Center;
        topRow.style.marginBottom = 8;

        var nameField = new TextField("Name") { value = variant.PortName };
        nameField.style.flexGrow = 1;
        nameField.style.marginRight = 10;
        nameField.RegisterValueChangedCallback(evt =>
        {
            variant.PortName = evt.newValue;
            UpdatePortName(variant);
        });

        var removeButton = new Button(() =>
        {
            // Don't remove if it's the last variant
            if (variantElements.Count <= 1)
            {
                EditorUtility.DisplayDialog("Cannot Remove", "At least one variant must remain.", "OK");
                return;
            }

            RemoveVariant(variant);
        })
        {
            text = "×",
            style = {
                width = 20,
                height = 20,
                fontSize = 12
            }
        };

        topRow.Add(nameField);
        topRow.Add(removeButton);

        // Bottom row: Weight controls
        var bottomRow = new VisualElement();
        bottomRow.style.flexDirection = FlexDirection.Row;
        bottomRow.style.alignItems = Align.Center;
        bottomRow.style.justifyContent = Justify.FlexStart;

        var weightLabel = new Label("Weight:");
        weightLabel.style.marginRight = 5;
        weightLabel.style.minWidth = 50;
        weightLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

        var weightSlider = new Slider(0, 100);
        weightSlider.style.flexGrow = 1;
        weightSlider.style.marginRight = 10;
        weightSlider.value = variant.WeightPercent;

        var weightField = new FloatField() { value = variant.WeightPercent };
        weightField.style.width = 60;

        weightSlider.RegisterValueChangedCallback(evt =>
        {
            var newValue = Mathf.Clamp(evt.newValue, 0, 100);
            variant.WeightPercent = newValue;
            weightField.value = newValue;
            UpdateTotalWeight();
        });

        weightField.RegisterValueChangedCallback(evt =>
        {
            var newValue = Mathf.Clamp(evt.newValue, 0, 100);
            variant.WeightPercent = newValue;
            weightSlider.value = newValue;
            weightField.value = newValue;
            UpdateTotalWeight();
        });

        bottomRow.Add(weightLabel);
        bottomRow.Add(weightSlider);
        bottomRow.Add(weightField);

        container.Add(topRow);
        container.Add(bottomRow);
        variantsContainer.Add(container);

        // Create output port for this variant
        var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        port.portName = variant.PortName;
        outputContainer.Add(port);

        variantElements.Add(new BranchVariantElement
        {
            Data = variant,
            Container = container,
            Port = port,
            WeightField = weightField,
            WeightSlider = weightSlider,
            RemoveButton = removeButton,
            NameField = nameField
        });

        RefreshPorts();
        RefreshExpandedState();
    }

    private void UpdatePortName(RandomBranchVariant variant)
    {
        var element = variantElements.Find(v => v.Data == variant);
        if (element != null && element.Port != null)
        {
            element.Port.portName = variant.PortName;
            RefreshPorts();
        }
    }

    private void RemoveVariant(RandomBranchVariant variant)
    {
        var element = variantElements.Find(v => v.Data == variant);
        if (element != null)
        {
            // Remove port
            if (element.Port != null)
            {
                // Remove connections from this port
                var edgesToRemove = new List<Edge>();
                foreach (var edge in element.Port.connections)
                {
                    edgesToRemove.Add(edge);
                }

                var graphView = GetFirstAncestorOfType<DialogueGraphView>();
                foreach (var edge in edgesToRemove)
                {
                    graphView.RemoveElement(edge);
                }

                outputContainer.Remove(element.Port);
            }

            // Remove UI container
            variantsContainer.Remove(element.Container);
            variantElements.Remove(element);

            RefreshPorts();
            RefreshExpandedState();
            UpdateTotalWeight();
        }
    }

    private void UpdateTotalWeight()
    {
        float total = 0f;
        foreach (var element in variantElements)
        {
            total += element.Data.WeightPercent;
        }

        totalWeightLabel.text = $"Total: {total:F1}%";

        if (Mathf.Approximately(total, 100f))
        {
            totalWeightLabel.style.color = new StyleColor(Color.green);
        }
        else if (total > 100f)
        {
            totalWeightLabel.style.color = new StyleColor(Color.red);
        }
        else
        {
            totalWeightLabel.style.color = new StyleColor(Color.yellow);
        }
    }

    public List<RandomBranchVariant> GetVariants()
    {
        return variantElements.ConvertAll(v => v.Data);
    }

    public void LoadVariants(List<RandomBranchVariant> variants)
    {
        // Clear before loading
        foreach (var element in variantElements.ToArray())
        {
            RemoveVariant(element.Data);
        }

        foreach (var v in variants)
        {
            var variant = new RandomBranchVariant { PortName = v.PortName, WeightPercent = v.WeightPercent };
            CreateVariantUI(variant);
        }
        UpdateTotalWeight();
    }

    [System.Serializable]
    private class RandomBranchNodeSerializedData
    {
        public List<BranchVariantSerialized> Variants = new List<BranchVariantSerialized>();
    }

    [System.Serializable]
    private class BranchVariantSerialized
    {
        public string PortName;
        public float WeightPercent;
    }

    public override string SerializeNodeData()
    {
        var data = new RandomBranchNodeSerializedData();

        foreach (var element in variantElements)
        {
            data.Variants.Add(new BranchVariantSerialized
            {
                PortName = element.Data.PortName,
                WeightPercent = element.Data.WeightPercent
            });
        }

        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<RandomBranchNodeSerializedData>(jsonData);

        // Преобразуем сериализованные данные в варианты
        var variants = new List<RandomBranchVariant>();
        foreach (var variantData in data.Variants)
        {
            variants.Add(new RandomBranchVariant
            {
                PortName = variantData.PortName,
                WeightPercent = variantData.WeightPercent
            });
        }

        LoadVariants(variants);
    }
}