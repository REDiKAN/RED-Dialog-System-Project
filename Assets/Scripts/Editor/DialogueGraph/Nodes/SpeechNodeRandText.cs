using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;

public class SpeechNodeRandText : BaseNode
{
    public CharacterData Speaker;
    private ObjectField speakerField;
    private VisualElement variantsContainer;
    public List<SpeechVariantElement> variantElements = new List<SpeechVariantElement>();

    private Label totalWeightLabel;

    public class SpeechVariantElement
    {
        public SpeechVariant Data;
        public VisualElement Container;
        public Label PreviewLabel;
        public FloatField WeightField;
        public Button RemoveButton;
        public Slider WeightSlider;
        public VisualElement controlsContainer;
    }

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Speech Rand (Text)";

        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        speakerField = new ObjectField("Speaker") { objectType = typeof(CharacterData) };
        speakerField.RegisterValueChangedCallback(evt => Speaker = evt.newValue as CharacterData);
        mainContainer.Add(speakerField);

        // Заголовок для секции вариантов
        var variantsHeader = new VisualElement();
        variantsHeader.style.flexDirection = FlexDirection.Row;
        variantsHeader.style.justifyContent = Justify.SpaceBetween;
        variantsHeader.style.marginBottom = 10;

        var variantsTitle = new Label("Variants (Weighted Random)");
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

        // Устанавливаем фиксированную ширину узла
        SetPosition(new Rect(position, new Vector2(300, 200)));
    }

    private void AddVariant()
    {
        var variant = new SpeechVariant { Text = "", WeightPercent = 10f };
        CreateVariantUI(variant);
        UpdateTotalWeight();
    }

    private void CreateVariantUI(SpeechVariant variant)
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

        // Верхняя строка: превью текста и кнопки
        var topRow = new VisualElement();
        topRow.style.flexDirection = FlexDirection.Row;
        topRow.style.justifyContent = Justify.SpaceBetween;
        topRow.style.alignItems = Align.FlexStart;
        topRow.style.marginBottom = 8;

        // Превью текста (занимает всё доступное пространство)
        var previewContainer = new VisualElement();
        previewContainer.style.flexGrow = 1;
        previewContainer.style.marginRight = 10;
        previewContainer.style.maxWidth = 200; // Ограничиваем ширину текста

        var previewLabel = new Label(string.IsNullOrEmpty(variant.Text) ? "(empty)" : variant.Text)
        {
            style = {
                whiteSpace = WhiteSpace.Normal,
                fontSize = 11,
                unityTextAlign = TextAnchor.UpperLeft,
                overflow = Overflow.Visible
            }
        };

        previewContainer.Add(previewLabel);

        // Контейнер для кнопок (редактировать и удалить)
        var buttonsContainer = new VisualElement();
        buttonsContainer.style.flexDirection = FlexDirection.Row;
        buttonsContainer.style.alignItems = Align.Center;

        // Кнопка редактирования текста
        var editButton = new Button(() => OpenTextEditor(variant, previewLabel))
        {
            text = "✎",
            style = {
                width = 24,
                height = 20,
                fontSize = 10,
                marginRight = 5
            }
        };

        // Кнопка удаления
        var removeButton = new Button(() =>
        {
            variantsContainer.Remove(container);
            variantElements.RemoveAll(v => v.Container == container);
            UpdateTotalWeight();
        })
        {
            text = "×",
            style = {
                width = 20,
                height = 20,
                fontSize = 12
            }
        };

        buttonsContainer.Add(editButton);
        buttonsContainer.Add(removeButton);

        topRow.Add(previewContainer);
        topRow.Add(buttonsContainer);

        // Нижняя строка: управление весом
        var bottomRow = new VisualElement();
        bottomRow.style.flexDirection = FlexDirection.Row;
        bottomRow.style.alignItems = Align.Center;
        bottomRow.style.justifyContent = Justify.FlexStart;

        var weightLabel = new Label("Weight:");
        weightLabel.style.marginRight = 5;
        weightLabel.style.minWidth = 50;
        weightLabel.style.unityTextAlign = TextAnchor.MiddleLeft;

        // Слайдер для веса
        var weightSlider = new Slider(0, 100);
        weightSlider.style.flexGrow = 1;
        weightSlider.style.marginRight = 10;
        weightSlider.value = variant.WeightPercent;

        // Поле для точного ввода веса
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

        variantElements.Add(new SpeechVariantElement
        {
            Data = variant,
            Container = container,
            PreviewLabel = previewLabel,
            WeightField = weightField,
            WeightSlider = weightSlider,
            RemoveButton = removeButton,
            controlsContainer = bottomRow
        });
    }

    private void OpenTextEditor(SpeechVariant variant, Label previewLabel)
    {
        var graphView = GetFirstAncestorOfType<DialogueGraphView>();
        if (graphView == null) return;

        graphView.OpenTextEditor(variant.Text, GUID, newText =>
        {
            variant.Text = newText;
            previewLabel.text = string.IsNullOrEmpty(newText) ? "(empty)" : newText;
        });
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

    public List<SpeechVariant> GetVariants()
    {
        return variantElements.ConvertAll(v => v.Data);
    }

    public void SetSpeaker(CharacterData speaker)
    {
        Speaker = speaker;
        if (speakerField != null)
            speakerField.SetValueWithoutNotify(speaker);
    }

    public void LoadVariants(List<SpeechVariant> variants)
    {
        // Очистка перед загрузкой
        variantsContainer.Clear();
        variantElements.Clear();

        foreach (var v in variants)
        {
            var variant = new SpeechVariant { Text = v.Text, WeightPercent = v.WeightPercent };
            CreateVariantUI(variant);
        }
        UpdateTotalWeight();
    }

    [System.Serializable]
    private class SpeechNodeRandTextSerializedData
    {
        public string SpeakerGuid;
        public List<SpeechVariantSerialized> Variants = new List<SpeechVariantSerialized>();
    }

    [System.Serializable]
    private class SpeechVariantSerialized
    {
        public string Text;
        public float WeightPercent;
    }

    public override string SerializeNodeData()
    {
        string speakerGuid = string.Empty;
        if (Speaker != null)
        {
            speakerGuid = AssetDatabaseHelper.GetAssetGuid(Speaker);
        }

        var data = new SpeechNodeRandTextSerializedData
        {
            SpeakerGuid = speakerGuid
        };

        // Сериализация вариантов
        foreach (var element in variantElements)
        {
            data.Variants.Add(new SpeechVariantSerialized
            {
                Text = element.Data.Text,
                WeightPercent = element.Data.WeightPercent
            });
        }

        return JsonUtility.ToJson(data);
    }

    public override void DeserializeNodeData(string jsonData)
    {
        var data = JsonUtility.FromJson<SpeechNodeRandTextSerializedData>(jsonData);

        // Загрузка спикера
        if (!string.IsNullOrEmpty(data.SpeakerGuid))
        {
            Speaker = AssetDatabaseHelper.LoadAssetFromGuid<CharacterData>(data.SpeakerGuid);
            if (speakerField != null)
            {
                speakerField.SetValueWithoutNotify(Speaker);
            }
        }

        // Загрузка вариантов
        var variants = new List<SpeechVariant>();
        foreach (var variantData in data.Variants)
        {
            variants.Add(new SpeechVariant
            {
                Text = variantData.Text,
                WeightPercent = variantData.WeightPercent
            });
        }

        LoadVariants(variants);
    }
}