#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyAINode : Node
{
    public EnemyActGroup ActGroup { get; private set; }
    public Port InputPort { get; private set; }
    public Port DefaultOutputPort { get; private set; }
    public List<Port> TransitionOutputPorts { get; private set; } = new List<Port>();

    private SO_EnemyAI _targetAI;
    private EnemyAIGraphView _graphView;
    private int _groupIndex;
    private SerializedObject _serializedObject;
    private float _currentWidth;

    private string GroupPropertyPath => $"<EnemyActGroup>k__BackingField.Array.data[{_groupIndex}]";

    public float CurrentWidth => _currentWidth;

    public EnemyAINode(EnemyActGroup actGroup, SO_EnemyAI targetAI, EnemyAIGraphView graphView, int groupIndex)
    {
        ActGroup = actGroup;
        _targetAI = targetAI;
        _graphView = graphView;
        _groupIndex = groupIndex;
        _serializedObject = new SerializedObject(targetAI);

        // 저장된 너비 복원 (기본 280)
        _currentWidth = actGroup.NodeWidth > 0 ? actGroup.NodeWidth : 280f;

        // 노드 타이틀
        title = $"Group {actGroup.Id}";
        if (actGroup.IsStart)
        {
            title = $"▶ Group {actGroup.Id} (Start)";
            AddToClassList("start-node");
        }

        // 위치/크기 설정
        SetPosition(new Rect(actGroup.NodePosition, new Vector2(_currentWidth, 200)));
        style.width = _currentWidth;

        // 포트 생성
        CreatePorts();

        // 내용 생성
        CreateBody();

        // 리사이즈 핸들 추가
        var resizer = new NodeResizeHandle(this, (newWidth) =>
        {
            _currentWidth = newWidth;
        });

        RefreshExpandedState();
        RefreshPorts();
    }

    private void CreatePorts()
    {
        InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        InputPort.portName = "In";
        InputPort.portColor = new Color(0.2f, 0.8f, 0.4f);
        inputContainer.Add(InputPort);

        DefaultOutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
        DefaultOutputPort.portName = "Out";
        DefaultOutputPort.portColor = new Color(0.3f, 0.6f, 1.0f);
        outputContainer.Add(DefaultOutputPort);

        for (int i = 0; i < ActGroup.EnemyActTransitions.Count; i++)
        {
            var transition = ActGroup.EnemyActTransitions[i];
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            string conditionName = transition.Condition != null ? transition.Condition.GetType().Name : "None";
            port.portName = $"조건: {conditionName}";
            port.portColor = new Color(1.0f, 0.6f, 0.2f);
            outputContainer.Add(port);
            TransitionOutputPorts.Add(port);
        }
    }

    private void CreateBody()
    {
        var container = new VisualElement();
        container.AddToClassList("node-body");

        // IsStart 토글
        var startToggle = new Toggle("시작 그룹") { value = ActGroup.IsStart };
        startToggle.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_targetAI, "Toggle Start");
            SetField<EnemyActGroup, bool>(ActGroup, "<IsStart>k__BackingField", evt.newValue);
            EditorUtility.SetDirty(_targetAI);
            title = evt.newValue ? $"▶ Group {ActGroup.Id} (Start)" : $"Group {ActGroup.Id}";
            if (evt.newValue) AddToClassList("start-node"); else RemoveFromClassList("start-node");
        });
        container.Add(startToggle);

        // Id 필드
        var idField = new IntegerField("ID") { value = ActGroup.Id };
        idField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_targetAI, "Change ID");
            SetField<EnemyActGroup, int>(ActGroup, "<Id>k__BackingField", evt.newValue);
            title = ActGroup.IsStart ? $"▶ Group {evt.newValue} (Start)" : $"Group {evt.newValue}";
            EditorUtility.SetDirty(_targetAI);
        });
        container.Add(idField);

        // ─── 행동 목록 ───
        var actsHeader = new Label("─── 행동 목록 ───");
        actsHeader.AddToClassList("section-header");
        container.Add(actsHeader);

        if (ActGroup.Acts.Count == 0)
        {
            var emptyLabel = new Label("(행동 없음)");
            emptyLabel.AddToClassList("empty-label");
            container.Add(emptyLabel);
        }
        else
        {
            for (int i = 0; i < ActGroup.Acts.Count; i++)
            {
                var actContainer = CreateActElement(ActGroup.Acts[i], i);
                container.Add(actContainer);
            }
        }

        var addActButton = new Button(() =>
        {
            Undo.RecordObject(_targetAI, "Add Act");
            ActGroup.Acts.Add(new EnemyAct());
            EditorUtility.SetDirty(_targetAI);
            _graphView.Reload();
        }) { text = "+ 행동 추가" };
        addActButton.AddToClassList("add-button");
        container.Add(addActButton);

        // ─── 전이 조건 ───
        var transHeader = new Label("─── 전이 조건 ───");
        transHeader.AddToClassList("section-header");
        container.Add(transHeader);

        if (ActGroup.EnemyActTransitions.Count == 0)
        {
            var emptyLabel = new Label("(전이 없음)");
            emptyLabel.AddToClassList("empty-label");
            container.Add(emptyLabel);
        }
        else
        {
            for (int i = 0; i < ActGroup.EnemyActTransitions.Count; i++)
            {
                var transContainer = CreateTransitionElement(ActGroup.EnemyActTransitions[i], i);
                container.Add(transContainer);
            }
        }

        var addTransButton = new Button(() =>
        {
            Undo.RecordObject(_targetAI, "Add Transition");
            ActGroup.EnemyActTransitions.Add(new EnemyActTransition());
            EditorUtility.SetDirty(_targetAI);
            _graphView.Reload();
        }) { text = "+ 전이 추가" };
        addTransButton.AddToClassList("add-button");
        container.Add(addTransButton);

        extensionContainer.Add(container);
    }

    private VisualElement CreateActElement(EnemyAct act, int actIndex)
    {
        var actContainer = new VisualElement();
        actContainer.AddToClassList("act-container");

        // 헤더
        var header = new VisualElement();
        header.AddToClassList("act-header");

        string actNameStr = string.Empty;
        string actName = string.IsNullOrEmpty(actNameStr) ? $"행동 {actIndex + 1}" : actNameStr;
        var nameLabel = new Label($"🎯 {actName}");
        nameLabel.AddToClassList("act-name");
        header.Add(nameLabel);

        void UpdateNameLabel()
        {
            string name = string.Empty;
            nameLabel.text = $"🎯 {(string.IsNullOrEmpty(name) ? $"행동 {actIndex + 1}" : name)}";
        }

        var deleteButton = new Button(() =>
        {
            Undo.RecordObject(_targetAI, "Delete Act");
            ActGroup.Acts.RemoveAt(actIndex);
            EditorUtility.SetDirty(_targetAI);
            _graphView.Reload();
        }) { text = "✕" };
        deleteButton.AddToClassList("delete-button");
        header.Add(deleteButton);
        actContainer.Add(header);

        // 행동 타입
        var actTypeEnum = new EnumField("행동 타입", act.EnemyActType);
        actTypeEnum.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_targetAI, "Change EnemyActType");
            SetField<EnemyAct, EEnemyActType>(act, "<EnemyActType>k__BackingField", (EEnemyActType)evt.newValue);
            UpdateNameLabel();
            EditorUtility.SetDirty(_targetAI);
        });
        actContainer.Add(actTypeEnum);

        // Value1
        var value1Field = new IntegerField("Value 1") { value = act.Value1 };
        value1Field.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_targetAI, "Change Value 1");
            SetField<EnemyAct, int>(act, "<Value1>k__BackingField", evt.newValue);
            UpdateNameLabel();
            EditorUtility.SetDirty(_targetAI);
        });
        actContainer.Add(value1Field);

        // Value2
        var value2Field = new IntegerField("Value 2") { value = act.Value2 };
        value2Field.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_targetAI, "Change Value 2");
            SetField<EnemyAct, int>(act, "<Value2>k__BackingField", evt.newValue);
            UpdateNameLabel();
            EditorUtility.SetDirty(_targetAI);
        });
        actContainer.Add(value2Field);

        // 확률 (슬라이더 + 숫자 동기화)
        var probRow = new VisualElement();
        probRow.style.flexDirection = FlexDirection.Row;
        probRow.style.alignItems = Align.Center;

        var probLabel = new Label("확률");
        probLabel.style.width = 60;
        probLabel.style.minWidth = 60;
        probRow.Add(probLabel);

        var probSlider = new Slider(0f, 1f) { value = act.Probability };
        probSlider.style.flexGrow = 1;
        probRow.Add(probSlider);

        var probFloat = new FloatField() { value = act.Probability };
        probFloat.style.width = 55;
        probFloat.style.minWidth = 55;
        probRow.Add(probFloat);

        probSlider.RegisterValueChangedCallback(evt =>
        {
            float clamped = Mathf.Clamp01(evt.newValue);
            probFloat.SetValueWithoutNotify(clamped);
            Undo.RecordObject(_targetAI, "Change Probability");
            SetField<EnemyAct, float>(act, "<Probability>k__BackingField", clamped);
            EditorUtility.SetDirty(_targetAI);
        });
        probFloat.RegisterValueChangedCallback(evt =>
        {
            float clamped = Mathf.Clamp01(evt.newValue);
            probSlider.SetValueWithoutNotify(clamped);
            probFloat.SetValueWithoutNotify(clamped);
            Undo.RecordObject(_targetAI, "Change Probability");
            SetField<EnemyAct, float>(act, "<Probability>k__BackingField", clamped);
            EditorUtility.SetDirty(_targetAI);
        });
        actContainer.Add(probRow);

        // 행동 횟수
        var countField = new IntegerField("행동 횟수 (-1=∞)") { value = act.ActCount };
        countField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_targetAI, "Change ActCount");
            SetField<EnemyAct, int>(act, "<ActCount>k__BackingField", evt.newValue);
            EditorUtility.SetDirty(_targetAI);
        });
        actContainer.Add(countField);

        // 반복 제한
        var repeatField = new IntegerField("반복 제한 (-1=∞)") { value = act.RepeatLimit };
        repeatField.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_targetAI, "Change RepeatLimit");
            SetField<EnemyAct, int>(act, "<RepeatLimit>k__BackingField", evt.newValue);
            EditorUtility.SetDirty(_targetAI);
        });
        actContainer.Add(repeatField);

        // 애니메이션 타입
        var animEnum = new EnumField("애니메이션", act.CharacterAnimationType);
        animEnum.RegisterValueChangedCallback(evt =>
        {
            Undo.RecordObject(_targetAI, "Change Animation Type");
            SetField<EnemyAct, ECharacterAnimationType>(act, "<CharacterAnimationType>k__BackingField",
                (ECharacterAnimationType)evt.newValue);
            EditorUtility.SetDirty(_targetAI);
        });
        actContainer.Add(animEnum);

        // Effects (SerializedProperty 인라인 편집)
        var effectsHeader = new Label("▼ Effects");
        effectsHeader.AddToClassList("sub-section-header");
        actContainer.Add(effectsHeader);

        string effectsPath = $"{GroupPropertyPath}.<Acts>k__BackingField.Array.data[{actIndex}].<Effects>k__BackingField";
        _serializedObject.Update();
        var effectsProp = _serializedObject.FindProperty(effectsPath);

        if (effectsProp != null)
        {
            var effectsField = new PropertyField(effectsProp, "");
            effectsField.Bind(_serializedObject);
            effectsField.AddToClassList("serialized-property-field");
            actContainer.Add(effectsField);
        }
        else
        {
            var fallback = new Label("(Effects 경로 못 찾음)");
            fallback.AddToClassList("effects-info");
            actContainer.Add(fallback);
        }

        return actContainer;
    }

    private VisualElement CreateTransitionElement(EnemyActTransition transition, int transIndex)
    {
        var transContainer = new VisualElement();
        transContainer.AddToClassList("transition-container");

        var header = new VisualElement();
        header.AddToClassList("transition-header");

        string conditionName = transition.Condition != null ? transition.Condition.GetType().Name : "조건 없음";
        var condLabel = new Label($"⚡ {conditionName}");
        condLabel.AddToClassList("transition-name");
        header.Add(condLabel);

        var deleteButton = new Button(() =>
        {
            Undo.RecordObject(_targetAI, "Delete Transition");
            ActGroup.EnemyActTransitions.RemoveAt(transIndex);
            EditorUtility.SetDirty(_targetAI);
            _graphView.Reload();
        }) { text = "✕" };
        deleteButton.AddToClassList("delete-button");
        header.Add(deleteButton);
        transContainer.Add(header);

        // Condition (SerializedProperty 인라인 편집)
        var condHeader = new Label("▼ Condition");
        condHeader.AddToClassList("sub-section-header");
        transContainer.Add(condHeader);

        string condPath = $"{GroupPropertyPath}.<EnemyActTransitions>k__BackingField.Array.data[{transIndex}].<Condition>k__BackingField";
        _serializedObject.Update();
        var condProp = _serializedObject.FindProperty(condPath);

        if (condProp != null)
        {
            var condField = new PropertyField(condProp, "");
            condField.Bind(_serializedObject);
            condField.AddToClassList("serialized-property-field");
            transContainer.Add(condField);
        }
        else
        {
            var fallback = new Label("(Condition 경로 못 찾음)");
            fallback.AddToClassList("effects-info");
            transContainer.Add(fallback);
        }

        var targetLabel = new Label($"→ Group {transition.NextId}");
        targetLabel.AddToClassList("transition-target");
        transContainer.Add(targetLabel);

        return transContainer;
    }

    private void SetField<TOwner, TValue>(TOwner owner, string fieldName, TValue value)
    {
        var field = typeof(TOwner).GetField(fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null) field.SetValue(owner, value);
    }
}
#endif
