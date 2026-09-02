using UnityEngine;

public class TMPTester : MonoBehaviour
{
    public TMPEffectController dialogueText;

    public string contents = "";

    [ContextMenu("Å×½ºÆ®")]
    public void HHH()
    {
        dialogueText.SetText(contents);
    }
}
