using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class EventNode : BaseNode
{
    public UnityEvent RuntimeEvent = new UnityEvent();
    private VisualElement _eventContainer;
    private EventNodeHelperSO _helperSO;

    public EventNode()
    {
        this.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
    }

    private void OnDetachedFromPanel(DetachFromPanelEvent evt)
    {
        if (_helperSO != null)
        {
            UnityEngine.Object.DestroyImmediate(_helperSO);
            _helperSO = null;
        }
    }

    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "Event Node";

        // Input port
        var inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        inputPort.portName = "Input";
        inputContainer.Add(inputPort);

        // Output port
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // Создаем временный ScriptableObject для редактирования события
        _helperSO = ScriptableObject.CreateInstance<EventNodeHelperSO>();
        _helperSO.Event = RuntimeEvent;

        _eventContainer = new IMGUIContainer(() =>
        {
            var so = new SerializedObject(_helperSO);
            var prop = so.FindProperty("Event");
            EditorGUILayout.PropertyField(prop, true);
            so.ApplyModifiedProperties();
            // Синхронизируем обратно в RuntimeEvent
            RuntimeEvent = _helperSO.Event;
        });

        mainContainer.Add(_eventContainer);
        RefreshExpandedState();
        RefreshPorts();
    }
}