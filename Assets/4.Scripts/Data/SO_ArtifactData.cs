using SerializeReferenceEditor;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_ArtifactData", menuName = "Scriptable Objects/SO_ArtifactData")]
public class SO_ArtifactData : ScriptableObject
{
    [field: SerializeField] public EArtifactId ID { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }
    [field: SerializeField][TextArea] public string Description { get; private set; }
    [field: SerializeField] public int Price { get; private set; }
    [field: SerializeField] public EArtifactPool Pools { get; private set; }
    [field: SerializeField] public EPlayerJob OwnerJob { get; private set; }

    [SerializeReference, SR] public List<ArtifactTrigger> Logics = new List<ArtifactTrigger> ();

}
