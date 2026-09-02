using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TMPEffectController : MonoBehaviour
{
    private TextMeshProUGUI _tmp;
    private List<TextEffectData> _activeEffects = new List<TextEffectData>();

    [Header("Typer Settings")]
    [SerializeField] private float _typingSpeed = 0.05f;

    [Header("Shake Settings")]
    [SerializeField] private float _shakeAmount = 2.0f;

    // [추가] 물결 설정
    [Header("Wave Settings")]
    [SerializeField] private float _waveSpeed = 5.0f;  // 물결치는 속도
    [SerializeField] private float _waveHeight = 3.0f; // 물결 높이

    private void Awake()
    {
        _tmp = GetComponent<TextMeshProUGUI>();
    }

    private void LateUpdate()
    {
        if (_activeEffects.Count > 0)
        {
            ApplyEffects();
        }
    }

    public void SetText(string rawText)
    {
        ParseResult result = TextEffectParser.Parse(rawText);
        _tmp.text = result.RenderableText;
        _activeEffects = result.Effects;

        // StartCoroutine(ss(_tmp.text));
    }

    private IEnumerator ss(string text)
    {
        int index = 0;

        while(index <= text.Length)
        {
            _tmp.maxVisibleCharacters = index++;
            yield return new WaitForSeconds(_typingSpeed);
        }
    }


    private void ApplyEffects()
    {
        // 매 프레임 메쉬를 새로고침해서 흔들림 효과 적용
        _tmp.ForceMeshUpdate();
        var textInfo = _tmp.textInfo;

        foreach (var effect in _activeEffects)
        {
            if (effect.Type == TextEffectType.Shake)
            {
                ShakeEffect(textInfo, effect.StartIndex, effect.EndIndex);
            }
            else if(effect.Type == TextEffectType.Wave)
            {
                WaveEffect(textInfo, effect.StartIndex, effect.EndIndex);
            }
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            _tmp.UpdateGeometry(meshInfo.mesh, i);
        }
    }

    private void ShakeEffect(TMP_TextInfo textInfo, int start, int end)
    {
        for (int i = start; i <= end; i++)
        {
            if (i >= textInfo.characterCount || !textInfo.characterInfo[i].isVisible) continue;

            var charInfo = textInfo.characterInfo[i];
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

            Vector3 jitter = new Vector3(
                Random.Range(-_shakeAmount, _shakeAmount),
                Random.Range(-_shakeAmount, _shakeAmount),
                0f
            );

            sourceVertices[vertexIndex + 0] += jitter;
            sourceVertices[vertexIndex + 1] += jitter;
            sourceVertices[vertexIndex + 2] += jitter;
            sourceVertices[vertexIndex + 3] += jitter;
        }
    }

    private void WaveEffect(TMP_TextInfo textInfo, int start, int end)
    {
        for (int i = start; i <= end; i++)
        {
            if (i >= textInfo.characterCount || !textInfo.characterInfo[i].isVisible) continue;

            var charInfo = textInfo.characterInfo[i];
            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

            float yOffset = Mathf.Sin(Time.time * _waveSpeed + i) * _waveHeight;
            Vector3 waveVector = new Vector3(0, yOffset, 0);

            sourceVertices[vertexIndex + 0] += waveVector;
            sourceVertices[vertexIndex + 1] += waveVector;
            sourceVertices[vertexIndex + 2] += waveVector;
            sourceVertices[vertexIndex + 3] += waveVector;
        }
    }
}