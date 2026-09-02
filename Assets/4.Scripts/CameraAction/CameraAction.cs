using DG.Tweening;
using System;

[Serializable]
public abstract class CameraAction
{
    public virtual ECameraActionType CameraActionType => ECameraActionType.None; 

    public abstract Tween Action();
}
