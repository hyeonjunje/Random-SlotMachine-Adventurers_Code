using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RandomCheatKeyAdventures.BootStrap
{
    public enum ESceneBootstrapOrder
    {
        Managers = -1000,
        Systems = -900,
        Creator = -800,
        UI = -700,
        Views = -500,
        Test = 0,
        ETC = -10,
    }

    [Serializable]
    public class Entry
    {
        [field: SerializeField] public GameObject Prefab { get; private set; }            // 프리팹 참조
        [field: SerializeField] public bool InstantiateInactive { get; private set; }     // 처음엔 비활성화로 할지
        [field: SerializeField] public ESceneBootstrapOrder Order { get; private set; }   // 생성 순서(의존성 대비하여)
        [field: SerializeField] public int OrderIndex { get; private set; }               // 같은 order일 경우 해당 인덱스로 순서 조정
    }

    [CreateAssetMenu(fileName = "SO_BootstrapData", menuName = "Scriptable Objects/SO_BootstrapData")]
    public class SO_BootstrapData : ScriptableObject
    {
        [SerializeField] private List<Entry> _managers = new List<Entry>();
        [SerializeField] private List<Entry> _systems = new List<Entry>();
        [SerializeField] private List<Entry> _creators = new List<Entry>();
        [SerializeField] private List<Entry> _uis = new List<Entry>();
        [SerializeField] private List<Entry> _views = new List<Entry>();
        [SerializeField] private List<Entry> _test = new List<Entry>();
        [SerializeField] private List<Entry> _etc = new List<Entry>();

        public IReadOnlyList<Entry> GetEntries()
        {
            List<Entry> result = new List<Entry>();
            result.AddRange(_managers.OrderBy(entry => entry.OrderIndex));
            result.AddRange(_creators.OrderBy(entry => entry.OrderIndex));
            result.AddRange(_systems.OrderBy(entry => entry.OrderIndex));
            result.AddRange(_uis.OrderBy(entry => entry.OrderIndex));
            result.AddRange(_views.OrderBy(entry => entry.OrderIndex));
            result.AddRange(_test.OrderBy(entry => entry.OrderIndex));
            result.AddRange(_etc.OrderBy(entry => entry.OrderIndex));

            return result;
        }
    }
}
