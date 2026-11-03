using UnityEngine;

// 역할 : 배경·제목을 천천히 확대·축소하고 살짝 흘러가게 해서 정지 화면이 살아 있는 느낌을 준다. (켄 번스 효과)
public class UI_SlowZoom : MonoBehaviour
{
    [SerializeField] private float _scaleAmount = 0.06f; // 기본 크기에서 더 커지는 비율
    [SerializeField] private float _scalePeriod = 12f; // 커졌다 돌아오는 한 주기 (초)
    [SerializeField] private Vector2 _panAmount = new Vector2(20f, 8f); // 좌우·위아래로 흘러가는 폭 (픽셀)
    [SerializeField] private float _panPeriod = 18f; // 흘러갔다 돌아오는 한 주기 (초)

    private RectTransform _rectTransform;
    private Vector3 _baseScale;
    private Vector2 _basePosition;

    private void Awake()
    {
        _rectTransform = (RectTransform)transform;
        _baseScale = _rectTransform.localScale;
        _basePosition = _rectTransform.anchoredPosition;
    }

    private void Update()
    {
        float time = Time.unscaledTime;
        float zoom = (1f - Mathf.Cos(time * Mathf.PI * 2f / _scalePeriod)) * 0.5f; // 0 ~ 1
        _rectTransform.localScale = _baseScale * (1f + _scaleAmount * zoom);

        float pan = Mathf.Sin(time * Mathf.PI * 2f / _panPeriod);
        _rectTransform.anchoredPosition = _basePosition + _panAmount * pan;
    }
}
