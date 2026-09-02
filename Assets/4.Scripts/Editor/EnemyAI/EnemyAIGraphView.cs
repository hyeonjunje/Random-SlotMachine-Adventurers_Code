#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyAIGraphView : GraphView
{
    private SO_EnemyAI _targetAI;
    private Dictionary<int, EnemyAINode> _nodeMap = new Dictionary<int, EnemyAINode>();

    public EnemyAIGraphView(SO_EnemyAI enemyAI)
    {
        _targetAI = enemyAI;

        // 기본 조작 설정
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // 배경 그리드
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        // Undo/Redo 단축키 등록
        RegisterCallback<KeyDownEvent>(OnKeyDown);

        // 그래프 구성
        BuildGraph();

        // Edge 연결/해제 이벤트
        graphViewChanged += OnGraphViewChanged;
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        // Ctrl+Z = Undo
        if (evt.ctrlKey && evt.keyCode == KeyCode.Z && !evt.shiftKey)
        {
            Undo.PerformUndo();
            Reload();
            evt.StopPropagation();
        }
        // Ctrl+Y 또는 Ctrl+Shift+Z = Redo
        else if ((evt.ctrlKey && evt.keyCode == KeyCode.Y) ||
                 (evt.ctrlKey && evt.shiftKey && evt.keyCode == KeyCode.Z))
        {
            Undo.PerformRedo();
            Reload();
            evt.StopPropagation();
        }
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();
        ports.ForEach(port =>
        {
            if (startPort != port &&
                startPort.node != port.node &&
                startPort.direction != port.direction)
            {
                compatiblePorts.Add(port);
            }
        });
        return compatiblePorts;
    }

    public void BuildGraph()
    {
        ClearGraph();
        _nodeMap.Clear();

        if (_targetAI == null) return;

        // 노드 생성
        for (int i = 0; i < _targetAI.EnemyActGroup.Count; i++)
        {
            var group = _targetAI.EnemyActGroup[i];
            var node = new EnemyAINode(group, _targetAI, this, i);
            _nodeMap[group.Id] = node;
            AddElement(node);
        }

        // 연결선 생성
        foreach (var group in _targetAI.EnemyActGroup)
        {
            if (!_nodeMap.ContainsKey(group.Id)) continue;
            var sourceNode = _nodeMap[group.Id];

            // 기본 NextId 연결 (Out 출력 포트)
            if (group.NextId != 0 && _nodeMap.ContainsKey(group.NextId))
            {
                var targetNode = _nodeMap[group.NextId];
                var edge = sourceNode.DefaultOutputPort.ConnectTo(targetNode.InputPort);
                AddElement(edge);
            }

            // Transition 연결
            for (int t = 0; t < group.EnemyActTransitions.Count; t++)
            {
                var transition = group.EnemyActTransitions[t];
                if (transition.NextId != 0 && _nodeMap.ContainsKey(transition.NextId))
                {
                    var targetNode = _nodeMap[transition.NextId];
                    if (t < sourceNode.TransitionOutputPorts.Count)
                    {
                        var edge = sourceNode.TransitionOutputPorts[t].ConnectTo(targetNode.InputPort);
                        AddElement(edge);
                    }
                }
            }
        }
    }

    private void ClearGraph()
    {
        var edgesToRemove = edges.ToList();
        foreach (var edge in edgesToRemove)
        {
            RemoveElement(edge);
        }

        var nodesToRemove = nodes.ToList();
        foreach (var node in nodesToRemove)
        {
            RemoveElement(node);
        }
    }

    public void Reload()
    {
        // 리로드 전에 현재 위치 저장
        SaveAllNodePositions();
        BuildGraph();
    }

    public void SaveAllNodePositions()
    {
        if (_targetAI == null) return;

        bool changed = false;
        foreach (var kvp in _nodeMap)
        {
            var node = kvp.Value;
            var group = node.ActGroup;
            var newPos = node.GetPosition().position;
            var newWidth = node.CurrentWidth;

            if (group.NodePosition != newPos)
            {
                SetNodePosition(group, newPos);
                changed = true;
            }
            if (!Mathf.Approximately(group.NodeWidth, newWidth))
            {
                SetNodeWidth(group, newWidth);
                changed = true;
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(_targetAI);
        }
    }

    private void SetNodePosition(EnemyActGroup group, Vector2 position)
    {
        var posField = typeof(EnemyActGroup).GetField("<NodePosition>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (posField != null)
        {
            posField.SetValue(group, position);
        }
    }

    private void SetNodeWidth(EnemyActGroup group, float width)
    {
        var widthField = typeof(EnemyActGroup).GetField("<NodeWidth>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (widthField != null)
        {
            widthField.SetValue(group, width);
        }
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        // 노드 이동 시 위치 저장
        if (graphViewChange.movedElements != null)
        {
            foreach (var element in graphViewChange.movedElements)
            {
                if (element is EnemyAINode node)
                {
                    Undo.RecordObject(_targetAI, "Move Node");
                    SetNodePosition(node.ActGroup, node.GetPosition().position);
                    EditorUtility.SetDirty(_targetAI);
                }
            }
        }

        if (graphViewChange.edgesToCreate != null)
        {
            foreach (var edge in graphViewChange.edgesToCreate)
            {
                var outputNode = edge.output.node as EnemyAINode;
                var inputNode = edge.input.node as EnemyAINode;
                if (outputNode == null || inputNode == null) continue;

                Undo.RecordObject(_targetAI, "Connect Nodes");

                if (edge.output == outputNode.DefaultOutputPort)
                {
                    SetNextId(outputNode.ActGroup, inputNode.ActGroup.Id);
                }
                else
                {
                    int transitionIndex = outputNode.TransitionOutputPorts.IndexOf(edge.output);
                    if (transitionIndex >= 0 && transitionIndex < outputNode.ActGroup.EnemyActTransitions.Count)
                    {
                        SetTransitionNextId(outputNode.ActGroup.EnemyActTransitions[transitionIndex], inputNode.ActGroup.Id);
                    }
                }

                EditorUtility.SetDirty(_targetAI);
            }
        }

        if (graphViewChange.elementsToRemove != null)
        {
            foreach (var element in graphViewChange.elementsToRemove)
            {
                if (element is Edge edge)
                {
                    var outputNode = edge.output.node as EnemyAINode;
                    if (outputNode == null) continue;

                    Undo.RecordObject(_targetAI, "Disconnect Nodes");

                    if (edge.output == outputNode.DefaultOutputPort)
                    {
                        SetNextId(outputNode.ActGroup, 0);
                    }
                    else
                    {
                        int transitionIndex = outputNode.TransitionOutputPorts.IndexOf(edge.output);
                        if (transitionIndex >= 0 && transitionIndex < outputNode.ActGroup.EnemyActTransitions.Count)
                        {
                            SetTransitionNextId(outputNode.ActGroup.EnemyActTransitions[transitionIndex], 0);
                        }
                    }

                    EditorUtility.SetDirty(_targetAI);
                }
                else if (element is EnemyAINode node)
                {
                    Undo.RecordObject(_targetAI, "Delete Node");
                    _targetAI.EnemyActGroup.Remove(node.ActGroup);
                    _nodeMap.Remove(node.ActGroup.Id);
                    EditorUtility.SetDirty(_targetAI);
                }
            }
        }

        return graphViewChange;
    }

    private void SetNextId(EnemyActGroup group, int nextId)
    {
        var field = typeof(EnemyActGroup).GetField("<NextId>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null) field.SetValue(group, nextId);
    }

    private void SetTransitionNextId(EnemyActTransition transition, int nextId)
    {
        var field = typeof(EnemyActTransition).GetField("<NextId>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null) field.SetValue(transition, nextId);
    }
}
#endif
