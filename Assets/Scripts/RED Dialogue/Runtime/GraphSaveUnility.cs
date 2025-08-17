using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// ������� ��� ���������� � �������� ���������� ������
/// </summary>
public class GraphSaveUtility
{
    //#region Properties

    //private GraphView targetGraphView;
    //private DialogueContainer containerCache;
    //private List<Edge> Edges => targetGraphView.edges.ToList();
    //private List<Node> Nodes => targetGraphView.nodes.ToList().Cast<Node>().ToList();

    //#endregion

    //#region Initialization
    //public static GraphSaveUtility GetInstance(GraphView targetGraphView)
    //{
    //    return new GraphSaveUtility { targetGraphView = targetGraphView };
    //}
    //#endregion

    //#region Saving
    ///// <summary>
    ///// ���������� ����� � ����
    ///// </summary>
    //public void SaveGraph(string fileName)
    //{
    //    if (!Edges.Any())
    //    {
    //        EditorUtility.DisplayDialog("Error", "No edges to save!", "OK");
    //        return;
    //    }

    //    var dialogueContainer = ScriptableObject.CreateInstance<DialogueContainer>();
    //    SaveNodes(dialogueContainer);
    //    SaveExposedProperties(dialogueContainer);

    //    // ������� ����� Resources ���� �� ����������
    //    if (!AssetDatabase.IsValidFolder("Assets/Resources"))
    //        AssetDatabase.CreateFolder("Assets", "Resources");

    //    AssetDatabase.CreateAsset(dialogueContainer, $"Assets/Resources/{fileName}.asset");
    //    AssetDatabase.SaveAssets();
    //}

    ///// <summary>
    ///// ���������� ����� � ������
    ///// </summary>
    //private void SaveNodes(DialogueContainer dialogueContainer)
    //{
    //    // ��������� ����� ����� ������
    //    var connectedPorts = Edges.Where(x => x.input.node != null).ToArray();
    //    foreach (var edge in connectedPorts)
    //    {
    //        var outputNode = edge.output.node as DialogueNode;
    //        var inputNode = edge.input.node as DialogueNode;

    //        dialogueContainer.NodeLinks.Add(new NodeLinkData
    //        {
    //            BaseNodeGuid = outputNode.GUID,
    //            PortName = edge.output.portName,
    //            TargetNodeGuid = inputNode.GUID
    //        });
    //    }

    //    // ��������� ������ �����
    //    foreach (var node in Nodes.Where(node => !node.EntryPoint))
    //    {
    //        dialogueContainer.DialogueNodeDatas.Add(new DialogueNodeData
    //        {
    //            Guid = node.GUID,
    //            DialogueText = node.DialogueText,
    //            Position = node.GetPosition().position
    //        });
    //    }
    //}

    ///// <summary>
    ///// ���������� ������� ������ �����
    ///// </summary>
    //private void SaveExposedProperties(DialogueContainer dialogueContainer)
    //{
    //    dialogueContainer.ExposedProperties.Clear();
    //    dialogueContainer.ExposedProperties.AddRange(targetGraphView.ExposedProperties);
    //}
    //#endregion

    //#region Loading
    ///// <summary>
    ///// �������� ����� �� �����
    ///// </summary>
    //public void LoadGraph(string fileName)
    //{
    //    containerCache = Resources.Load<DialogueContainer>(fileName);
    //    if (containerCache == null)
    //    {
    //        EditorUtility.DisplayDialog("File Not Found", "Target dialogue graph file does not exist", "OK");
    //        return;
    //    }

    //    ClearGraph();
    //    CreateNodes();
    //    ConnectNodes();
    //    CreateExposedProperties();
    //}

    ///// <summary>
    ///// �������� ����� �� ����������� ������
    ///// </summary>
    //private void CreateNodes()
    //{
    //    foreach (var nodeData in containerCache.DialogueNodeDatas)
    //    {
    //        var tempNode = targetGraphView.CreateDialogueNode(nodeData.DialogueText, Vector2.zero);
    //        tempNode.GUID = nodeData.Guid;
    //        targetGraphView.AddElement(tempNode);

    //        var nodePorts = containerCache.NodeLinks.Where(x => x.BaseNodeGuid == nodeData.Guid).ToList();
    //        nodePorts.ForEach(x => targetGraphView.AddChoicePort(tempNode, x.PortName));
    //    }
    //}

    ///// <summary>
    ///// �������������� ������ ����� ������
    ///// </summary>
    //private void ConnectNodes()
    //{
    //    foreach (var node in Nodes)
    //    {
    //        var connections = containerCache.NodeLinks.Where(x => x.BaseNodeGuid == node.GUID).ToList();
    //        for (int j = 0; j < connections.Count; j++)
    //        {
    //            var targetNodeGuid = connections[j].TargetNodeGuid;
    //            var targetNode = Nodes.First(x => x.GUID == targetNodeGuid);

    //            LinkNodes(node.outputContainer[j].Q<Port>(), (Port)targetNode.inputContainer[0]);
    //            targetNode.SetPosition(new Rect(
    //                containerCache.DialogueNodeDatas.First(x => x.Guid == targetNodeGuid).Position,
    //                targetGraphView.defaultNodeSize
    //            ));
    //        }
    //    }
    //}

    ///// <summary>
    ///// ���������� ���� ������
    ///// </summary>
    //private void LinkNodes(Port output, Port input)
    //{
    //    var tempEdge = new Edge { output = output, input = input };
    //    tempEdge.input.Connect(tempEdge);
    //    tempEdge.output.Connect(tempEdge);
    //    targetGraphView.Add(tempEdge);
    //}

    ///// <summary>
    ///// �������������� ������� ������ �����
    ///// </summary>
    //private void CreateExposedProperties()
    //{
    //    targetGraphView.ClearBlackBoardAndExposedProperties();
    //    foreach (var exposedProperty in containerCache.ExposedProperties)
    //    {
    //        targetGraphView.AddPropertyToBlackBoard(exposedProperty);
    //    }
    //}

    ///// <summary>
    ///// ������� �������� ����� ����� ���������
    ///// </summary>
    //private void ClearGraph()
    //{
    //    // ��������� GUID ��������� �����
    //    Nodes.Find(x => x.EntryPoint).GUID = containerCache.NodeLinks[0].BaseNodeGuid;

    //    // ������� ��� ���� ����� ����������
    //    foreach (var node in Nodes.Where(node => !node.EntryPoint))
    //    {
    //        // ������� ��������� �����
    //        var edgesToRemove = Edges.Where(x => x.input.node == node).ToList();
    //        foreach (var edge in edgesToRemove)
    //        {
    //            targetGraphView.RemoveElement(edge);
    //        }

    //        targetGraphView.RemoveElement(node);
    //    }
    //}
    //#endregion
}
