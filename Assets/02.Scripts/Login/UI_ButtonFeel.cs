using UnityEngine;
using UnityEngine.EventSystems;

// 역할 : 버튼 손맛. 마우스를 올리면 살짝 커지고, 누르면 살짝 작아졌다가 떼면 돌아온다.
public class UI_ButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float _hoverScale = 1.05f; // 마우스를 올렸을 때 크기
    [SerializeField] private float _pressScale = 0.95f; // 눌렀을 때 크기
    [SerializeField] private float _speed = 15f; // 목표 크기를 따라가는 빠르기

    private Vector3 _baseScale;
    private float _targetScale = 1f;
    private bool _isHover = false;

    private void Awake()
    {
        _baseScale = transform.localScale;
    }

    private void OnDisable()
    {
        // 패널이 꺼졌다 켜져도 커진 채로 남지 않게 되돌린다.
        _isHover = false;
        _targetScale = 1f;
        transform.localScale = _baseScale;
    }

    private void Update()
    {
        Vector3 target = _baseScale * _targetScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target, 1f - Mathf.Exp(-_speed * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHover = true;
        _targetScale = _hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHover = false;
        _targetScale = 1f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _targetScale = _pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _targetScale = _isHover ? _hoverScale : 1f;
    }
}
