using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System;

/// <summary>
/// ������� ����� ��� ���� ����� ����������� �����
/// �������� ����� ������ � �������� ��� ���� �����
/// </summary>
public abstract class BaseNode : Node
{
    public string GUID { get; set; } // ���������� ������������� ����
    public bool EntryPoint { get; set; } = false; // �������� �� ���� ������ �����

    /// <summary>
    /// ������������� ���� � ��������� ��������
    /// </summary>
    public virtual void Initialize(Vector2 position)
    {
        GUID = Guid.NewGuid().ToString();
        SetPosition(new Rect(position, new Vector2(200, 150)));
    }
}
