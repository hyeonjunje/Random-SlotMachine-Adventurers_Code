using UnityEngine;
using RandomCheatKeyAdventures.BootStrap;
using System.Collections.Generic;
using System;

public enum EBootstrapperType
{
    Live,
    Debug,
    Custom1, // 커스텀 1 -> 현준
    Custom2, // 커스텀 2 -> 민우
    Demo,    // 데모 버전
}

[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public class SceneBootstrapper : MonoBehaviour
{
    [SerializeField] private EBootstrapperType _bootstrapperType;

    [SerializeField] private SO_BootstrapData[] _bootstrapData;
    [SerializeField] private SO_ConfigData_InGame[] _configData;
    [SerializeField] private Transform _canvas;

    private SO_BootstrapData _currentBootstrapper;
    private SO_ConfigData_InGame _currentConfig;

    private Dictionary<ESceneBootstrapOrder, Transform> _parents = new Dictionary<ESceneBootstrapOrder, Transform>();
    
    private void Awake()
    {
        int bootstrapperType = (int)_bootstrapperType;
        if (bootstrapperType >= _bootstrapData.Length || bootstrapperType >= _configData.Length)
        {
            Debug.LogError("SceneBootstrapper 컴포넌트에 SO_BootstrapData, SO_ConfigData_InGame 할당이 제대로 되지 않음");
            return;
        }

        _currentBootstrapper = _bootstrapData[bootstrapperType];
        _currentConfig = _configData[bootstrapperType];

        AppConfig.SetConfig(_bootstrapperType, _currentConfig);

        IReadOnlyList<Entry> entries = _currentBootstrapper.GetEntries();

        // 타입별 부모 생성
        MakeParent(entries);

        // 필요한 프리펩들 씬에 생성
        BootstrapScene(entries);
    }

    private void MakeParent(IReadOnlyList<Entry> entries)
    {
        foreach(Entry entry in entries)
        {
            if(_parents.ContainsKey(entry.Order) == false)
            {
                if (entry.Order == ESceneBootstrapOrder.UI)
                {
                    _parents.Add(entry.Order, _canvas);
                }
                else
                {
                    Transform parent = new GameObject(entry.Order.ToString()).transform;
                    parent.SetParent(transform);
                    _parents.Add(entry.Order, parent);
                }
            }
        }
    }

    private void BootstrapScene(IReadOnlyList<Entry> entries)
    {
        if (entries == null)
        {
            return;
        }

        foreach (Entry entry in entries)
        {
            if (entry.Prefab == null)
            {
                Debug.LogError($"SceneBootstrapper missing prefab. Order: {entry.Order}, OrderIndex: {entry.OrderIndex}", this);
                continue;
            }

            GameObject obj = Instantiate(entry.Prefab, _parents[entry.Order]);
            obj.SetActive(!entry.InstantiateInactive);

            if (obj.TryGetComponent<IInitializable>(out IInitializable init))
            {
                init.Initialize();
            }
        }
    }

#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetAllSingletons()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        Type singleTonPersistent = typeof(SingletonPersistent<>);
        Type singletonScene = typeof(SingletonScene<>);

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract)
                {
                    continue;
                }
                var baseType = type.BaseType;

                if (baseType != null && baseType.IsGenericType && baseType.GetGenericTypeDefinition() == singleTonPersistent)
                {
                    var fiInstance = baseType.GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    var fiQuitting = baseType.GetField("_quitting", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    fiInstance?.SetValue(null, null);
                    fiQuitting?.SetValue(null, false);
                }

                if (baseType != null && baseType.IsGenericType && baseType.GetGenericTypeDefinition() == singletonScene)
                {
                    var fiInstance = baseType.GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    var fiQuitting = baseType.GetField("_quitting", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    fiInstance?.SetValue(null, null);
                    fiQuitting?.SetValue(null, false);
                }
            }
        }
    }
#endif
}
