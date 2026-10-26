using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 역할 : 플레이어가 맞으면 화면을 붉게 번쩍이거나(Flash), 서서히 사라지게(Fade) 한다.
// 위험 상태(체력이 낮음)면 효과가 끝난 뒤에도 옅게 붉은 채로 남는다.
[RequireComponent(typeof(Image))]
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _flashTime = 0.06f;  // 번쩍이는 시간 (초)
    [SerializeField] private float _fadeTime = 0.3f;    // 사라지는 데 걸리는 시간 (초)
    [SerializeField] private float _dangerAlpha = 0.15f; // 위험 상태에서 남아 있는 진하기

    private Image _image;
    private Coroutine _effectCoroutine; // 돌고 있는 Flash/Fade 코루틴, 없으면 null
    private bool _isDanger = false;

    // 효과가 끝난 뒤 돌아갈 진하기
    private float RestAlpha => _isDanger ? _dangerAlpha : 0f;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.enabled = false; // 처음에는 그리지 않는다.
    }

    // alpha 진하기로 잠깐 켰다가 끈다.
    public void Flash(float alpha)
    {
        SetAlpha(alpha);

        RestartEffect(Flash_Coroutine());
    }

    // alpha 진하기로 시작해서 서서히 사라진다.
    public void Fade(float alpha)
    {
        SetAlpha(alpha);

        RestartEffect(Fade_Coroutine());
    }

    // 위험 상태를 켜고 끈다. 효과가 돌고 있지 않으면 바로 반영한다.
    public void SetDanger(bool isDanger)
    {
        _isDanger = isDanger;

        if (_effectCoroutine == null)
        {
            Rest();
        }
    }

    // 돌던 효과를 멈추고 처음부터 다시 시작한다. (연달아 맞으면 두 코루틴이 함께 알파를 바꾸는 문제를 막는다)
    private void RestartEffect(IEnumerator routine)
    {
        if (_effectCoroutine != null)
        {
            StopCoroutine(_effectCoroutine);
        }

        _effectCoroutine = StartCoroutine(routine);
    }

    private IEnumerator Flash_Coroutine()
    {
        _image.enabled = true;

        yield return new WaitForSeconds(_flashTime);

        Rest();
        _effectCoroutine = null;
    }

    private IEnumerator Fade_Coroutine()
    {
        _image.enabled = true;

        float elapsedTime = 0f;
        float startAlpha = _image.color.a; // 시작 진하기

        while (elapsedTime < _fadeTime)
        {
            // 진하게 시작해서 시간이 지날수록 쉬는 진하기까지 옅어진다.
            float alpha = Mathf.Lerp(startAlpha, RestAlpha, elapsedTime / _fadeTime);
            SetAlpha(alpha);

            elapsedTime += Time.deltaTime;
            yield return null; // 다음 프레임까지 쉰다.
        }

        Rest();
        _effectCoroutine = null;
    }

    // 효과가 없을 때의 모습 : 위험 상태면 옅게 붉게, 아니면 끈다.
    private void Rest()
    {
        SetAlpha(RestAlpha);
        _image.enabled = _isDanger;
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color; // 색을 꺼내서
        color.a = alpha;            // 알파만 바꾸고
        _image.color = color;       // 다시 넣는다.
    }
}
