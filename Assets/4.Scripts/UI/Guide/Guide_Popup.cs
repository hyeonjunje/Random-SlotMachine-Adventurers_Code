using TMPro;
using UnityEngine;

public class Guide_Popup : Guide_Base<string>
{
    [SerializeField] private TMP_Text _textTitle;
    [SerializeField] private TMP_Text _textGuide;

    public void SetTitle(string title)
    {
        _textTitle.text = title;
    }

    public override void ShowGuide(string t)
    {
        _textGuide.text = t;
    }
}
