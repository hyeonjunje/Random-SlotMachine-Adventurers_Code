using System.Collections.Generic;
using UnityEngine;
using static SO_AudioData;

[CreateAssetMenu(fileName = "SO_AudioCatalog_SFX", menuName = "Scriptable Objects/Audio/SO_AudioCatalog_SFX")]
public class SO_AudioCatalog_SFX : ScriptableObject
{
    [field: SerializeField] public List<SfxEntry> Sfxs { get; private set; } = new();

}
