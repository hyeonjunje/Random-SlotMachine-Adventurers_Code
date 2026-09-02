using System;
using System.Collections;
using UnityEngine;

public class TitleSlotMachineController : MonoBehaviour
{
    [SerializeField] private SlotMachineReel[] _reels;
    [SerializeField] private SO_SlotMachineConfig _titleConfig;
    [SerializeField] private float _spinTime = 1.5f;

    public void PlayStartSequence(Action onComplete)
    {
        StartCoroutine (CoSequence (onComplete));
    }

    private IEnumerator CoSequence(Action onComplete)
    {
        foreach (var reel in _reels)
        {
            reel.SetTitleConfig (_titleConfig);
        }

        for (int i = 0; i < _reels.Length; i++)
        {
            StartCoroutine (_reels[i].CoSpinTitle (i * 0.15f));
        }

        yield return new WaitForSeconds (_spinTime);

        ETitleKeyword[] results = { ETitleKeyword.지금, ETitleKeyword.게임, ETitleKeyword.시작 };

        for (int i = 0; i < _reels.Length; i++)
        {
            if (i == _reels.Length - 1)
                yield return StartCoroutine (_reels[i].CoStopTitle (results[i], 0.2f));
            else
            {
                _reels[i].StopTitle (results[i], 0.2f);
                yield return new WaitForSeconds (0.5f);
            }
        }

        yield return new WaitForSeconds (1.0f);
        onComplete?.Invoke ();
    }
}