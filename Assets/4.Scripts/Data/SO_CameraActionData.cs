using SerializeReferenceEditor;
using System.Collections;
using UnityEngine;


[CreateAssetMenu(fileName = "SO_CameraActionData", menuName = "Scriptable Objects/SO_CameraActionData")]
public class SO_CameraActionData : ScriptableObject
{
    [field: SerializeReference, SR] public CameraAction CameraAction { get; private set; }
}
