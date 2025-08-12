using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.UIElements;

public class GraphSaveUnility
{
    private DialogueGrapView targetGrapView;
    private DialogueContainer containerCache;

    private List<Edge> Edges => targetGrapView.edges.ToList();
    private List<DialogueNode> Nodes => targetGrapView.nodes.ToList().Cast<DialogueNode>().ToList();
    public static GraphSaveUnility GetInstance(DialogueGrapView targetGrapView)
    {
        return new GraphSaveUnility
        {
            targetGrapView = targetGrapView
        };
    }

    public void SaveGraph(string fileName)
    {
        var dialogueContainer = ScriptableObject.CreateInstance<DialogueContainer>();

        if (!SaveNodes(dialogueContainer)) return;

        SaveExposedProperties(dialogueContainer);

        if (!AssetDatabase.IsValidFolder($"Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        AssetDatabase.CreateAsset(dialogueContainer, $"Assets/Resources/{fileName}.asset");
        AssetDatabase.SaveAssets();
    }


    private bool SaveNodes(DialogueContainer dialogueContainer)
    {
        if (!Edges.Any()) return false;

        var connectedPorts = Edges.Where(x => x.input.node != null).ToArray();
        for (int i = 0; i < connectedPorts.Length; i++)
        {
            var outputNode = connectedPorts[i].output.node as DialogueNode;
            var inputNode = connectedPorts[i].input.node as DialogueNode;

            dialogueContainer.NodeLinks.Add(new NodeLinkData
            {
                BaseNodeGuid = outputNode.GUID,
                PortName = connectedPorts[i].output.portName,
                TargetNodeGuid = inputNode.GUID
            });
        }

        foreach (var dialogueNode in Nodes.Where(node => !node.EmtryPoint))
        {
            dialogueContainer.DialogueNodeDatas.Add(new DialogueNodeData
            {
                Guid = dialogueNode.GUID,
                DialogueText = dialogueNode.DialogueText,
                Position = dialogueNode.GetPosition().position
            });
        }

        return true;
    }
    private void SaveExposedProperties(DialogueContainer dialogueContainer)
    {
        dialogueContainer.ExposedProperties.AddRange(targetGrapView.ExposedProperties);
    }
    public void LoadGraph(string fileName)
    {
        containerCache = Resources.Load<DialogueContainer>(fileName);

        if (containerCache == null)
        {
            EditorUtility.DisplayDialog("File Not Found", "Target dialogue graph file does not exists", "OK");
            return;
        }

        ClearGraph();
        CreateNodes();
        ConnectNodes();
        CreateExposedProperties();
    }

    private void CreateExposedProperties()
    {
        targetGrapView.ClearBlackBoardAndExposedProperties();

        foreach (var exposedProperty in containerCache.ExposedProperties)
        {
            targetGrapView.AddproperToBlackBoard(exposedProperty);
        }
    }

    private void ConnectNodes()
    {
        for (int i = 0; i < Nodes.Count; i++)
        {
            var connection = containerCache.NodeLinks.Where(x => x.BaseNodeGuid == Nodes[i].GUID).ToList();

            for (int j = 0; j < connection.Count; j++)
            {
                var targetNodeGuid = connection[j].TargetNodeGuid;
                var targetNode = Nodes.First(x => x.GUID == targetNodeGuid);

                LinkNode(Nodes[i].outputContainer[j].Q<Port>(), (Port) targetNode.inputContainer[0]);

                targetNode.SetPosition(new Rect(
                    containerCache.DialogueNodeDatas.First(x => x.Guid == targetNodeGuid).Position,
                    targetGrapView.defaultNodeSize
                    ));
            }
        }
    }

    private void LinkNode(Port output, Port input)
    {
        var tempEdge = new Edge 
        {
            output = output,
            input = input
        };

        tempEdge?.input.Connect(tempEdge);
        tempEdge?.output.Connect(tempEdge);

        targetGrapView.Add(tempEdge);


    }

    private void CreateNodes()
    {
        foreach (var nodeData in containerCache.DialogueNodeDatas)
        {
            var tempNode = targetGrapView.CreateDialogueNode(nodeData.DialogueText, Vector2.zero);
            tempNode.GUID = nodeData.Guid;
            targetGrapView.AddElement(tempNode);

            var nodePorts = containerCache.NodeLinks.Where(x => x.BaseNodeGuid == nodeData.Guid).ToList();
            nodePorts.ForEach(x => targetGrapView.AddChoicePort(tempNode, x.PortName));
        }
    }

    private void ClearGraph()
    {
        Nodes.Find(x => x.EmtryPoint).GUID = containerCache.NodeLinks[0].BaseNodeGuid;

        foreach (var node in Nodes)
        {
            if (node.EmtryPoint) continue;

            Edges.Where(x => x.input.node == node).ToList()
                .ForEach(edge => targetGrapView.RemoveElement(edge));

            targetGrapView.RemoveElement(node);
        }
    }
}
