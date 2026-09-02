using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_SlotMachineConfig", menuName = "Scriptable Objects/SO_SlotMachineConfig")]
public class SO_SlotMachineConfig : ScriptableObject
{
    public const int HORIZONTAL = 3;
    public const int VERTICAL = 3;

    [field: Header("슬롯머신 애니메이션 수치")]
    [field: Header("슬롯 내려가는거 관련")]
    [field: SerializeField] public float[] SlotSpinDelay { get; private set; } = new float[9];
    [field: SerializeField] public float MoveDuration { get; private set; } = 1f;
    [field: SerializeField] public float SlotMoveSpeed { get; private set; } = 1500f;
    [field: SerializeField] public float SlotRestoreYPos { get; private set; } = -200f;
    [field: SerializeField] public Vector3 SlotSpinScale { get; private set; } = new Vector3(1.1f, 1.1f, 1);
    
    [field: Header("슬롯 멈추는거 관련")]
    [field: SerializeField] public float[] SlotStopDelay { get; private set; } = new float[9];
    [field: SerializeField] public int StopOffset { get; private set; } = 3;
    [field: SerializeField] public float SlotExtraUnderDampingYPos { get; private set; } = -60f;
    [field: SerializeField] public float UnderDampingDuration { get; private set; } = 0.1f;
    [field: SerializeField] public Ease DampingEase { get; private set; } = Ease.OutCubic;
    [field: SerializeField] public float RestoreDuration { get; private set; } = 0.1f;
    [field: SerializeField] public Ease RestoreEase { get; private set; } = Ease.InCubic;
    [field: SerializeField] public float CrossRestoreDuration { get; private set; } = 0.02f;
    [field: SerializeField] public float SizeRestoreDuration { get; private set; } = 0.05f;
    [field: SerializeField] public Ease SizeRestoreEase { get; private set; } = Ease.OutBack;


}
