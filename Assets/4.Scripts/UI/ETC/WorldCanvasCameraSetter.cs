using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class WorldCanvasCameraSetter : MonoBehaviour
{
    private void Start()
    {
        GetComponent<Canvas>().worldCamera = UIManager.Instance.OrthographicCamera;
    }
}
