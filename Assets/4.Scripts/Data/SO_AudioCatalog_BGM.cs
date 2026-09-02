using System.Collections.Generic;
using UnityEngine;
using static SO_AudioData;

[CreateAssetMenu(fileName = "SO_AudioCatalog_BGM", menuName = "Scriptable Objects/Audio/SO_AudioCatalog_BGM")]

public class SO_AudioCatalog_BGM : ScriptableObject
{
    [field: SerializeField] public List<BgmEntry> Bgms { get; private set; }
}
