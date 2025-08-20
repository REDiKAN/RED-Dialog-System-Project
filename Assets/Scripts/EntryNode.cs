using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ��������� ���� - ����� ����� � ���������� ����
/// �� ����� ���� ������ ��� ���������
/// </summary>
public class EntryNode : BaseNode
{
    /// <summary>
    /// ������������� ���������� ����
    /// </summary>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
        title = "START";
        EntryPoint = true;

        // ������� �������� ����
        var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        outputPort.portName = "Next";
        outputContainer.Add(outputPort);

        // ��������� �������� � �����������
        capabilities &= ~Capabilities.Movable;
        capabilities &= ~Capabilities.Deletable;

        // ��������� ���������� ��������� ����
        RefreshExpandedState();
        RefreshPorts();

        // ��������� ����������� ����� ��� ���������� ����
        styleSheets.Add(Resources.Load<StyleSheet>("DefNode"));
    }
}