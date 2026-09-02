using System;
using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    public Action OnMarkerTriggered;

    public void OnAnimMarker() 
    {
        OnMarkerTriggered?.Invoke();
    }
}
