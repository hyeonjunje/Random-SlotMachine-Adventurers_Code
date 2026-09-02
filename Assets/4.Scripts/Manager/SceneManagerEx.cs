using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

/// <summary>
/// 이건 사용할지 안할지 모르겠어요.
/// 씬 뭐 안만드는 이상 씬은 하나로 만들 생각이라서.
/// 일단은 게임오버하면 처음씬으로 돌아가도록 하기 위해 만들었습니다.
/// </summary>
public class SceneManagerEx : SingletonScene<SceneManagerEx>
{
    public void LoadScene(int index)
    {
        Time.timeScale = 1f;
        DOTween.KillAll(); // 씬 이동 시 남아있는 모든 DOTween 제거
        SceneManager.LoadScene(index);
    }

    public void DelayLoadScene(int index, float delay)
    {
        StartCoroutine(CoDelayLoadScene(index, delay));
    }

    IEnumerator CoDelayLoadScene(int index, float dealy)
    {
        yield return new WaitForSeconds(dealy);
        LoadScene(index);
    }
}
